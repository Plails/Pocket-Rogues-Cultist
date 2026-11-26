using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

namespace OccultSanctum
{
    /// waits for each Void Crypt floor to be ready (fresh or loaded from a save) and puts up the gates to the sanctum
    /// in one of its walls; the gates are not in the save, they come back at the same place every time
    public class SanctumDirector : MonoBehaviour
    {
        bool done;
        float next;

        void OnEnable() { SceneManager.sceneLoaded += OnLoaded; Music.Host = this; Music.Load(); }
        bool kicked;
        void OnDisable() { SceneManager.sceneLoaded -= OnLoaded; }
        void OnLoaded(Scene s, LoadSceneMode m) { done = false; kicked = false; try { Flow.OnSceneLoaded(s); } catch (Exception e) { SanctumPlugin.Log.LogError("Sanctum scene: " + e); } }

        void Update()
        {
            if (!kicked && Flow.InSanctum && GameManager.Instance != null && GameManager.Instance.Player_Mine() != null) { kicked = true; Music.Start(); }
            if (done || Time.unscaledTime < next) return;
            next = Time.unscaledTime + 0.25f;
            try { Tick(); }
            catch (Exception e) { done = true; SanctumPlugin.Log.LogError("Sanctum director: " + e); }
        }

        void Tick()
        {
            var gm = GameManager.Instance;
            var gmg = GenerationManager.Instance;
            if (gm == null || gmg == null) return;
            if (!SanctumPlugin.CfgEnabled.Value || gm.IsConnected || gm.BossArena) { done = true; return; }
            if (gm.location != Locations.crypt || gmg.mapType != c.MapTypes.CRYPT) { done = true; return; }
            if (!gmg.WasGenerated || gm.Player_Mine() == null) return;
            done = true;
            try { if (FileBasedPrefs.GetInt("ObsidianTowerMode") == 1) return; } catch { }
            int deep = PlayerPrefs.GetInt("curDeep", 1);
            if (deep < SanctumPlugin.CfgFloorFrom.Value || deep > SanctumPlugin.CfgFloorTo.Value) return;
            Builder.Init(gmg.GEN);
            Builder.FloorGate(deep);
        }
    }

    public static class Builder
    {
        /// (0.41.2, the user: no torches in the sanctum - the candles, candelabras and braziers light it) the game's own
        /// torch light (its "Light" glow and its "LightAlt" lamp), taken off the torch and set on our fires, tinted
        static GameObject torchPrefab;
        public static GameObject Torch()
        {
            if (torchPrefab != null) return torchPrefab;
            try { torchPrefab = gen?.decorOnWalls?.FirstOrDefault(d => d != null && d.name == "TorchActive"); } catch { }
            if (torchPrefab == null) torchPrefab = Resources.Load<GameObject>("TorchActive");
            return torchPrefab;
        }
        public static readonly Color FireLight = new Color(1f, 0.66f, 0.32f, 1f);
        public static void Glow(Transform parent, Vector2 pos, float size, Color col)
        {
            try
            {
                var t = Torch();
                var act = t != null ? t.transform.Find("Active") : null;
                if (act == null) return;
                foreach (var n in new[] { "Light", "LightAlt" })
                {
                    var src = act.Find(n);
                    if (src == null) continue;
                    var go = UnityEngine.Object.Instantiate(src.gameObject);
                    go.name = "Sanctum_glow_" + n;
                    if (parent != null) go.transform.SetParent(parent, true);
                    go.transform.position = new Vector3(pos.x, pos.y, src.position.z - t.transform.position.z);
                    go.transform.rotation = Quaternion.identity;
                    float ps = parent != null ? Mathf.Max(0.01f, Mathf.Abs(parent.lossyScale.x)) : 1f;
                    go.transform.localScale = src.lossyScale * size / ps;
                    var sr = go.GetComponent<SpriteRenderer>();
                    if (sr != null) sr.color = new Color(col.r, col.g, col.b, sr.color.a);
                    var sf = go.GetComponent<SFLight>();
                    if (sf != null) { sf.color = col; sf.radius = sf.radius * size; }
                    go.SetActive(true);
                    // it goes out with its fire: when the thing is broken (its picture changes or it is gone)
                    var on = parent != null ? parent.GetComponentInChildren<SpriteRenderer>() : null;
                    if (on != null) { var gl = go.AddComponent<GlowOff>(); gl.on = on; gl.onSprite = on.sprite; }
                }
            }
            catch (Exception e) { SanctumPlugin.Log.LogWarning("Sanctum glow: " + e.Message); }
        }

        public static Generation gen;
        public static int wallLayer, floorLayer, floorOrder, wallsLayerId, wallsOrder, env;
        public static Material floorMat, litMat;
        public static Transform root;

        public static int WallLayer(Generation g) => g.compositeCollider != null ? g.compositeCollider.gameObject.layer : g.tilemap_Walls != null ? g.tilemap_Walls.gameObject.layer : 0;

        public static void Init(Generation g)
        {
            gen = g;
            wallLayer = WallLayer(g);
            var fr = g.tilemap_Floor != null ? g.tilemap_Floor.GetComponent<TilemapRenderer>() : null;
            floorLayer = fr != null ? fr.sortingLayerID : 0; floorOrder = fr != null ? fr.sortingOrder : 0; floorMat = fr != null ? fr.sharedMaterial : null;
            var wr = g.tilemap_Walls != null ? g.tilemap_Walls.GetComponent<TilemapRenderer>() : null;
            wallsLayerId = wr != null ? wr.sortingLayerID : floorLayer; wallsOrder = wr != null ? wr.sortingOrder : floorOrder + 5;
            env = SortingLayer.NameToID("Environment");
            if (!SortingLayer.IsValid(env)) env = wallsLayerId;
            // like the game's crates (in a scene that has none yet: the material of the crate prefab itself)
            litMat = LitMaterial() ?? Prefabs.Cont("Box")?.GetComponentInChildren<SpriteRenderer>(true)?.sharedMaterial ?? floorMat;
            root = new GameObject("OccultSanctum").transform;
        }

        // ------------------------------------------------------------------ the gates on a crypt floor
        const string KGatePos = "OccultSanctum_GatePos";

        public static void FloorGate(int deep)
        {
            var f = gen.floor;
            int w = f.GetWidth(), h = f.GetHeight(), n = 0;
            for (int x = 0; x < w; x++) for (int y = 0; y < h; y++) if (f.GetTile(x, y) == c.TileTypes.floor) n++;
            // decided by the floor's own map: a floor loaded from a save gets the same gates at the same place
            string key = deep + "|" + w + "x" + h + "|" + n;
            var rng = new System.Random(deep * 7919 + w * 131 + h * 17 + n);
            if (rng.Next(100) >= SanctumPlugin.CfgChance.Value) { SanctumPlugin.Log.LogInfo("Sanctum: no gates on crypt floor " + deep); return; }
            // the spot is remembered for this floor: on a floor loaded from a save, far-away crates and monsters have
            // their colliders switched off, so a fresh search would pick another wall (the gates came back elsewhere)
            Vector2? spot = null;
            string rec = PlayerPrefs.GetString(KGatePos, "");
            if (rec.StartsWith(key + "@", StringComparison.Ordinal))
            {
                var xy = rec.Substring(key.Length + 1).Split(';');
                if (xy.Length == 2 && float.TryParse(xy[0], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var gx)
                    && float.TryParse(xy[1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var gy)) spot = new Vector2(gx, gy);
            }
            if (!spot.HasValue)
            {
                spot = FindGateSpot(rng);
                if (!spot.HasValue) { SanctumPlugin.Log.LogWarning("Sanctum: no wall for the gates on crypt floor " + deep); return; }
                PlayerPrefs.SetString(KGatePos, key + "@" + spot.Value.x.ToString(System.Globalization.CultureInfo.InvariantCulture) + ";" + spot.Value.y.ToString(System.Globalization.CultureInfo.InvariantCulture));
                PlayerPrefs.Save();
            }
            bool used = Flow.UsedFloor == key;
            Gate.Create(spot.Value, false, used, key, wallsLayerId, wallsOrder, litMat, wallLayer, root);
            SanctumPlugin.Log.LogInfo($"Sanctum: gates on crypt floor {deep} at {spot.Value}{(used ? " (already used: blocked)" : "")}");
        }

        /// a stretch of north wall 4 tiles wide with open floor in front of it, away from the stairs
        static Vector2? FindGateSpot(System.Random rng)
        {
            var f = gen.floor;
            int w = f.GetWidth(), h = f.GetHeight();
            var F = c.TileTypes.floor;
            bool Fl(int x, int y) => f.GetTile(x, y) == F;
            bool Wl(int x, int y) { var t = f.GetTile(x, y); return t == c.TileTypes.wall || t == c.TileTypes.hidden || t == c.TileTypes.empty; }
            Vector2 stairs = gen.stairsOnThisFloor != null ? (Vector2)gen.stairsOnThisFloor.transform.position : new Vector2(-999, -999);
            var cands = new List<Vector2Int>();
            for (int x = 3; x < w - 3; x++)
                for (int y = 3; y < h - 3; y++)
                {
                    bool ok = true;
                    for (int i = -2; i <= 1 && ok; i++) if (!Fl(x + i, y) || !Wl(x + i, y + 1)) ok = false;
                    for (int i = -1; i <= 0 && ok; i++) if (!Fl(x + i, y - 1) || !Fl(x + i, y - 2)) ok = false;
                    if (!ok) continue;
                    var c0 = new Vector2(x - 0.5f, y);
                    if ((c0 - stairs).sqrMagnitude < 6f * 6f) continue;
                    if (Physics2D.OverlapBoxAll(c0 + new Vector2(0, -0.6f), new Vector2(3.6f, 1.8f), 0f).Any(cl => cl != null && !cl.isTrigger && cl.gameObject.layer != wallLayer && cl.GetComponentInParent<Creature>() == null)) continue;
                    cands.Add(new Vector2Int(x, y));
                }
            if (cands.Count == 0) return null;
            var p = cands[rng.Next(cands.Count)];
            return new Vector2(p.x - 0.5f, p.y + 0.5f);   // the wall's lower edge, between the 4 tiles
        }

        // ------------------------------------------------------------------ helpers
        public static SpriteRenderer Sr(string art, Vector2 pos, int layer, int order, Material mat, Transform parent = null)
        {
            var go = new GameObject("OS_" + art);
            go.transform.SetParent(parent != null ? parent : root, false);
            go.transform.position = new Vector3(pos.x, pos.y, 0f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = Art.Get(art);
            sr.sortingLayerID = layer;
            sr.sortingOrder = order;
            if (mat != null) sr.sharedMaterial = mat;
            return sr;
        }

        /// the material of the floor's barrels and crates: our things are lit by the torches exactly like them
        static Material LitMaterial()
        {
            try
            {
                foreach (var sr in UnityEngine.Object.FindObjectsOfType<SpriteRenderer>())
                {
                    if (sr == null || sr.sharedMaterial == null || sr.sprite == null || sr.GetComponentInParent<Creature>() != null) continue;
                    if (sr.sharedMaterial.name.StartsWith("SFShadowed_Mob", StringComparison.Ordinal)) return sr.sharedMaterial;
                }
            }
            catch { }
            return null;
        }

        /// a thing put here is not saved with the floor (it is built anew every time)
        public static void Own(GameObject go)
        {
            if (go == null) return;
            foreach (var so in go.GetComponentsInChildren<SavingObject>(true)) UnityEngine.Object.DestroyImmediate(so);
        }

        public static GameObject Spawn(GameObject prefab, Vector2 pos, float rot = 0f)
        {
            if (prefab == null) return null;
            try { return CustomPhotonNetwork.Instantiate(prefab, pos, Quaternion.Euler(0f, 0f, rot)); }
            catch (Exception e) { SanctumPlugin.Log.LogWarning("Sanctum spawn " + prefab.name + ": " + e.Message); return null; }
        }
    }

    /// the sanctum itself, built into the pantry scene in place of the pantry: its shape (Room.L) and what fills it
    /// differ every visit - a ritual hall, a library, a prison, a chapel - set out by rules that fit any shape
    public static class SanctumBuilder
    {
        enum Theme { Ritual, Library, Prison, Chapel, Cult, Sacrifice, Ossuary, Barracks, Council }
        static readonly string[] ThemeNames = { "ритуальный зал", "библиотека", "тюрьма", "часовня", "святилище культа", "жертвенная", "склеп костей", "казарма культа", "зал совета" };
        static Layout L; static bool[,] occ; static System.Random rnd; static Generation g;
        static Vector2 W(float x, float y) => Room.W(x, y);
        static bool Fl(int x, int y) => L.Floor(x, y);
        static bool Free(int x, int y) => Fl(x, y) && !occ[x, y];
        static bool FreeRect(int x0, int y0, int w, int h) { for (int x = x0; x < x0 + w; x++) for (int y = y0; y < y0 + h; y++) if (!Free(x, y)) return false; return true; }
        static void Mark(int x0, int y0, int w, int h) { for (int x = x0; x < x0 + w; x++) for (int y = y0; y < y0 + h; y++) if (x >= 0 && y >= 0 && x < L.W && y < L.H) occ[x, y] = true; }
        static T Pick<T>(List<T> l) => l[rnd.Next(l.Count)];
        static float R(float a, float b) => a + (float)rnd.NextDouble() * (b - a);
        static Material FloorMat => Builder.floorMat ?? Builder.litMat;
        const bool Banners = false;          // banners: off for now (the user asked to take them out)
        static readonly bool Torches = false;  // (0.41.2, the user) no torches on the walls: the fires of the sanctum light it

        public static void Build(Generation gen)
        {
            g = gen; Builder.Init(gen); L = Room.L; rnd = new System.Random(); occ = new bool[L.W, L.H]; Ritual.Running = false; hallMobs.Clear(); Sermon.End();
            var theme = (Theme)rnd.Next(ThemeNames.Length);
            int gx = L.GateX, top = L.TopAt(gx);
            SanctumPlugin.Log.LogInfo("Sanctum: " + L.Name + ", " + ThemeNames[(int)theme]);
            Bestiary.Visited();                                                  // the bestiary shows the location from now on
            Safe("floor", () =>
            {
                for (int x = 0; x < L.W; x++) for (int y = 0; y < L.H; y++)
                    if (Fl(x, y)) { var sr = Builder.Sr("occ_floor_big", W(x, y), Builder.floorLayer, Builder.floorOrder + 1, FloorMat); sr.sprite = Art.Tile("occ_floor_big", x % 30, y % 30); }
            });
            Safe("secret room", () => Secret.Build(rnd, FloorMat));
            var secretFront = Secret.Has ? Secret.WallTile * 2 - Secret.Pass : new Vector2Int(-99, -99);   // the hall tile before the breakable wall
            if (Secret.Has) Mark(secretFront.x - 1, secretFront.y - 1, 3, 3);                             // kept clear: the way to it stays open
            // the way in and the carpet's lane stay clear of furniture
            Mark(gx - 1, 0, 3, 2);
            foreach (var sr0 in L.Side) Mark(sr0.xMin, sr0.yMin, sr0.width, sr0.height);   // the side rooms are furnished each on its own, later
            // corridors and doorways stay clear: nothing in a passage one or two tiles wide, nor at its mouths
            for (int x = 0; x < L.W; x++) for (int y = 0; y < L.H; y++)
            {
                if (!Fl(x, y)) continue;
                bool narrow = false;                                                   // in a strip 1 to 3 tiles wide, across or along
                for (int k = 1; k <= 3 && !narrow; k++)
                    for (int o = 0; o < k && !narrow; o++)
                        narrow = (!Fl(x - o - 1, y) && !Fl(x - o + k, y) && Enumerable.Range(0, k).All(i => Fl(x - o + i, y)))
                              || (!Fl(x, y - o - 1) && !Fl(x, y - o + k) && Enumerable.Range(0, k).All(i => Fl(x, y - o + i)));
                if (narrow) Mark(x - 1, y - 1, 3, 3);
            }
            bool circle = false; Vector2 cc = default;
            if (L.Circle.HasValue && (theme == Theme.Ritual || rnd.Next(3) == 0))
            {
                cc = L.Circle.Value;
                if (FreeRect((int)cc.x - 3, (int)cc.y - 3, 7, 7)) { circle = true; Mark((int)cc.x - 4, (int)cc.y - 4, 9, 9); }   // the circle and a clear ring round it: the cultists stand there in their ritual
            }
            Safe("carpet and altar", () =>
            {
                if (circle)
                {
                    var c = Builder.Sr("occ_circle_0", W(cc.x, cc.y), Builder.floorLayer, Builder.floorOrder + 2, FloorMat);
                    c.gameObject.AddComponent<Flicker>().Init(new[] { "occ_circle_0", "occ_circle_1" }, 0.7f, 0.25f, null);
                }
                bool Lay(int y) => y >= 0 && Fl(gx, y) && !(circle && Mathf.Abs(y - cc.y) <= 3) && y <= top - 2;
                for (int y = 0; y <= top - 2; y++)
                {
                    if (!Lay(y)) continue;
                    bool startHere = y > 0 && !Lay(y - 1), endHere = !Lay(y + 1);
                    // (the user, 2026-11-22: no bits of carpet - the hall's carpet was cut short by the circle) the lane
                    // to the altar is kept clear, no carpet drawn
                    Mark(gx - 1, y, 3, 1);
                }
                Breakables.Altar(W(gx, top - 0.4f)); Mark(gx - 1, top - 1, 3, 2);
                foreach (int sx in new[] { -2, 2 }) if (Free(gx + sx, top)) { Breakables.Candles(W(gx + sx * .9f, top - .3f)); Mark(gx + sx, top, 1, 1); }
                for (int k = 0; k < 2; k++) Blood(W(gx + R(-2, 2), top - R(1.5f, 3)));
                if (circle)
                {
                    for (int k = 0; k < 6; k++) { float a = k * Mathf.PI / 3f + .5f; Breakables.Candles(W(cc.x + Mathf.Cos(a) * 4.3f, cc.y + Mathf.Sin(a) * 4.3f)); }   // outside the ring the cultists stand on
                    Blood(W(cc.x + R(-3, 3), cc.y + R(-3, 3)));
                }
            });
            Safe("theme", () =>
            {
                int dx = L.W <= 9 ? 3 : 4;
                bool cult = theme == Theme.Cult, oss = theme == Theme.Ossuary, bar = theme == Theme.Barracks, council = theme == Theme.Council;
                // columns (or hooded statues in the cult's hall) flanking the lane
                if (theme == Theme.Chapel || theme == Theme.Ritual || cult || oss || council || rnd.Next(3) == 0)
                    for (int y = 2; y < top - 2; y += theme == Theme.Chapel ? 3 : 4)
                        foreach (int sx in new[] { -dx, dx })
                            if (Free(gx + sx, y))
                            {
                                if (cult || council || ((theme == Theme.Sacrifice || oss) && rnd.Next(2) == 0)) Breakables.Statue(W(gx + sx, y), sx < 0 ? 90f : -90f);   // its front (the orb) towards the lane
                                else Breakables.Column(W(gx + sx, y));
                                Mark(gx + sx, y, 1, 1);
                            }
                if (theme == Theme.Chapel)
                    for (int y = 2; y < top - 3; y += 2)                                   // pews facing the altar
                        foreach (int sx in new[] { -1, 1 })
                        {
                            int x0 = sx < 0 ? gx - 3 : gx + 2;
                            if (FreeRect(x0, y, 2, 1)) { Breakables.Bench(W(x0 + .5f, y), 0f); Mark(x0, y, 2, 1); }
                        }
                // (no obelisk in the circle: nothing stands on it, the portal opens there)
                int shelves = theme == Theme.Library ? 8 : council ? rnd.Next(1, 3) : bar ? 0 : rnd.Next(0, 2);
                int cages = theme == Theme.Prison ? rnd.Next(5, 8) : theme == Theme.Sacrifice ? rnd.Next(2, 4) : oss ? rnd.Next(1, 3) : theme == Theme.Library || council ? rnd.Next(0, 2) : rnd.Next(1, 3);
                int tables = theme == Theme.Library ? rnd.Next(3, 5) : bar ? rnd.Next(3, 5) : rnd.Next(1, 3), books = theme == Theme.Library ? 14 : council ? rnd.Next(5, 9) : rnd.Next(3, 6);
                int braziers = cult ? rnd.Next(3, 6) : theme == Theme.Sacrifice || bar ? rnd.Next(2, 4) : oss ? rnd.Next(1, 3) : rnd.Next(0, 3);
                int candelabra = cult || council ? rnd.Next(4, 7) : bar ? rnd.Next(0, 2) : rnd.Next(2, 5), lecterns = theme == Theme.Library ? rnd.Next(2, 4) : council ? rnd.Next(2, 4) : cult ? 1 : rnd.Next(0, 2);
                int slabs = theme == Theme.Sacrifice ? rnd.Next(2, 4) : oss ? rnd.Next(1, 3) : rnd.Next(0, 2) == 0 && theme == Theme.Prison ? 1 : 0;
                int skulls = theme == Theme.Sacrifice ? rnd.Next(3, 5) : oss ? rnd.Next(6, 10) : cult ? rnd.Next(1, 3) : rnd.Next(0, 2);
                int sigils = theme == Theme.Ritual || cult ? rnd.Next(2, 4) : council ? rnd.Next(1, 3) : rnd.Next(0, 2);
                // benches: rows of them in the barracks, a ring of them round the council's table
                int benches = bar ? rnd.Next(4, 8) : council ? rnd.Next(3, 6) : rnd.Next(0, 2);
                if (council)                                                               // the council's long table in the middle, benches round it
                {
                    int cx = gx + (rnd.Next(2) == 0 ? -3 : 3), cy = Mathf.Clamp(L.ShiftY + (top - L.ShiftY) / 2, L.ShiftY + 3, Mathf.Max(L.ShiftY + 3, top - 4));
                    for (int k = 0; k < 2; k++)
                        if (FreeRect(cx - 1, cy + k * 3 - 1, 2, 1) && FreeRect(cx - 1, cy + k * 3, 2, 1) && FreeRect(cx - 1, cy + k * 3 + 1, 2, 1))
                        {
                            Breakables.Table(rnd.Next(2), W(cx - .5f, cy + k * 3), 0f); Mark(cx - 1, cy + k * 3, 2, 1);
                            Breakables.Bench(W(cx - .5f, cy + k * 3 - 1), 0f); Mark(cx - 1, cy + k * 3 - 1, 2, 1);
                            Breakables.Bench(W(cx - .5f, cy + k * 3 + 1), 180f); Mark(cx - 1, cy + k * 3 + 1, 2, 1);
                        }
                }
                for (int i = 0; i < benches; i++) if (TryRect(2, 1, false, out var p)) Breakables.Bench(p, rnd.Next(2) == 0 ? 0f : 180f);
                // fireplaces: off for now (the user asked to take them out)
                for (int i = 0; i < shelves; i++) if (TryWall(2, out var p, out var r, 0.65f)) Breakables.Shelf(p, r);
                string[] kinds = { "skel", "rot", "zomb" };
                for (int i = 0; i < cages; i++) if (TryBlock(2, true, out var p)) Breakables.Cage(i == 0 && cages >= 3 ? null : kinds[rnd.Next(kinds.Length)], p);   // with many cages one stands open
                for (int i = 0; i < slabs; i++) if (TryRect(2, 1, false, out var p)) Breakables.Sacrifice(p);
                for (int i = 0; i < tables; i++) if (TryWall(2, out var p, out var r, 1.1f, true)) Breakables.Table(rnd.Next(2), p, r);
                for (int i = 0; i < lecterns; i++) if (TryRect(1, 1, false, out var p)) Breakables.Lectern(p);
                for (int i = 0; i < braziers; i++) if (TryRect(1, 1, false, out var p)) Breakables.RedBrazier(p);
                for (int i = 0; i < candelabra; i++) if (TryRect(1, 1, rnd.Next(2) == 0, out var p)) Breakables.Candelabra(p);
                for (int i = 0; i < skulls; i++) if (TryRect(1, 1, true, out var p)) Breakables.SkullPile(p);
                // candles everywhere: clusters along the walls and scattered about
                int candles = cult || theme == Theme.Ritual || oss ? rnd.Next(10, 16) : bar ? rnd.Next(3, 6) : rnd.Next(6, 11);
                for (int i = 0; i < candles; i++) if (TryRect(1, 1, rnd.Next(3) > 0, out var p)) Breakables.Candles(p + new Vector2(R(-.2f, .2f), R(-.2f, .2f)));
                if (theme == Theme.Prison || theme == Theme.Sacrifice || oss) for (int k = 0; k < 5; k++) Blood(W(R(1, L.W - 2), R(1, L.H - 2)));
                for (int i = 0; i < sigils; i++)                                            // sigils drawn on the floor
                {
                    int x = rnd.Next(L.W), y = rnd.Next(L.H);
                    if (!Free(x, y)) continue;
                    var s = Builder.Sr("occ_sigil_" + rnd.Next(3), W(x, y), Builder.floorLayer, Builder.floorOrder + 2, FloorMat);
                    s.transform.rotation = Quaternion.Euler(0f, 0f, rnd.Next(360));
                }
            });
            if (Banners) Safe("banners", () =>
            {
                // red banners hung flat on the north walls (the walls the camera looks at): the cloth lies on the wall
                // and its hem reaches the wall's foot, like the game's own wall decorations
                int n = theme == Theme.Cult ? rnd.Next(5, 9) : rnd.Next(2, 6);
                var spots = new List<int[]>();
                for (int x = 0; x < L.W; x++) for (int y = 2; y < L.H; y++)
                    if (Fl(x, y) && !Fl(x, y + 1) && !Fl(x, y + 2) && x % 5 == 0 && Mathf.Abs(x - gx) > 2) spots.Add(new[] { x, y });
                for (int i = 0; i < n && spots.Count > 0; i++)
                {
                    var t = Pick(spots); spots.Remove(t);
                    Builder.Sr("occ_banner", W(t[0], t[1] + .5f + (34f / 2f + 2f) / 24f), Builder.wallsLayerId, Builder.wallsOrder + 12, Builder.litMat);   // wholly on the wall, its hem just above the floor
                }
            });
            Safe("crates, sacks, webs", () =>
            {
                var corners = new List<Vector2Int>();
                for (int x = 0; x < L.W; x++) for (int y = 0; y < L.H; y++)
                    if (Free(x, y) && (!Fl(x - 1, y) || !Fl(x + 1, y)) && (!Fl(x, y - 1) || !Fl(x, y + 1))) corners.Add(new Vector2Int(x, y));
                var pool = new List<GameObject>();
                foreach (var n in new[] { "Box", "Barrel", "BarrelAlt", "Box2", "Box3" }) { var p = Prefabs.Cont(n); if (p != null) pool.Add(p); }
                var bag = Prefabs.Cont("GroundBag"); var web = Prefabs.Cont("SpiderWebObject");
                int crates = theme == Theme.Barracks ? rnd.Next(8, 13) : rnd.Next(4, 9), sacks = rnd.Next(3, theme == Theme.Prison || theme == Theme.Barracks ? 9 : 6), webs = rnd.Next(2, theme == Theme.Prison || theme == Theme.Ossuary ? 8 : 5);
                for (int i = 0; i < crates && pool.Count > 0; i++)
                {
                    Vector2Int t;
                    if (corners.Count > 0 && rnd.Next(3) > 0) { t = Pick(corners); corners.Remove(t); } else if (!TryWallTile(out t)) continue;
                    if (!Free(t.x, t.y)) continue;
                    Builder.Own(Builder.Spawn(Pick(pool), W(t.x + R(-.1f, .1f), t.y + R(-.1f, .1f)))); Mark(t.x, t.y, 1, 1);
                }
                for (int i = 0; i < sacks && bag != null; i++)
                    if (TryWallTile(out var t) && Free(t.x, t.y)) { Builder.Own(Builder.Spawn(bag, W(t.x + R(-.2f, .2f), t.y + R(-.2f, .2f)), R(0, 360))); Mark(t.x, t.y, 1, 1); }
                for (int i = 0; i < webs && web != null && corners.Count > 0; i++)
                {
                    var t = Pick(corners); corners.Remove(t);
                    if (!Free(t.x, t.y)) continue;
                    Builder.Own(Builder.Spawn(web, W(t.x, t.y), R(0, 360))); Mark(t.x, t.y, 1, 1);
                }
            });
            Safe("torches", () =>
            {
                // (0.41.2, the user) no torches at all: the sanctum is lit by its own candles, candelabras and braziers
                if (!Torches) return;
                var torch = Builder.Torch();
                if (torch == null) return;
                for (int x = 0; x < L.W; x++) for (int y = 1; y < L.H; y++)
                {
                    if (!Fl(x, y) || (x == secretFront.x && y == secretFront.y)) continue;   // no torch on the breakable wall
                    float rot;
                    if (!Fl(x - 1, y) && y % 4 == 2) rot = 0f;
                    else if (!Fl(x + 1, y) && y % 4 == 2) rot = 180f;
                    else if (!Fl(x, y + 1) && x % 5 == 2 && Mathf.Abs(x - gx) > 2) rot = 270f;
                    else continue;
                    Builder.Own(g.CreateOnWall(new[] { torch }, W(x, y), Quaternion.Euler(0, 0, rot)));
                }
            });
            Safe("cultists", () =>
            {
                // the cult's men are in the hall, past the entrance corridor: they do not fall on the hero as he comes in
                var spots = new List<Vector2Int>();
                for (int x = 0; x < L.W; x++) for (int y = L.ShiftY + 2; y < L.H; y++) if (Free(x, y)) spots.Add(new Vector2Int(x, y));
                int hall = 0;
                for (int x = 0; x < L.W; x++) for (int y = L.ShiftY; y < L.H; y++) if (Fl(x, y)) hall++;
                int n = Mathf.Clamp(hall / 50, 2, 5), made = 0;                 // (0.39.0 /38 2..6; 0.39.3, the user again: fewer mobs)
                if (circle) n = Mathf.Max(n, rnd.Next(6, 8));          // enough of them for the ritual (it begins when five are left)
                Ritual ritual = circle ? Builder.root.gameObject.AddComponent<Ritual>() : null;
                if (ritual != null) ritual.Center = W(cc.x, cc.y);
                for (int i = 0; i < n && spots.Count > 0; i++)
                {
                    var t = Pick(spots); spots.Remove(t);
                    var m = Cultist.Spawn(W(t.x, t.y));
                    if (m != null) { made++; Mark(t.x, t.y, 1, 1); ritual?.Cultists.Add(m); hallMobs.Add(m); }
                }
                // crossbowmen: about half as many as the cultists
                int xb = Mathf.Max(1, Mathf.RoundToInt(made / 2f)), madeXb = 0;
                for (int i = 0; i < xb && spots.Count > 0; i++)
                {
                    var t = Pick(spots); spots.Remove(t);
                    var m = Crossbowman.Spawn(W(t.x, t.y));
                    if (m != null) { madeXb++; Mark(t.x, t.y, 1, 1); hallMobs.Add(m); }
                }
                // spearmen: about a third as many as the cultists, at least one
                int sp = Mathf.Max(1, Mathf.RoundToInt(made / 3f)), madeSp = 0;
                for (int i = 0; i < sp && spots.Count > 0; i++)
                {
                    var t = Pick(spots); spots.Remove(t);
                    var m = Spearman.Spawn(W(t.x, t.y));
                    if (m != null) { madeSp++; Mark(t.x, t.y, 1, 1); hallMobs.Add(m); }
                }
                SanctumPlugin.Log.LogInfo("Sanctum: " + made + " cultists, " + madeXb + " crossbowmen, " + madeSp + " spearmen" + (ritual != null ? ", the circle is ready for their ritual" : ""));
            });
            Safe("busy in the hall", () => HallBusy(spotsLeft()));
            Safe("elder", () =>
            {
                // the elder on the pentagram (or, with no circle in this hall, on a sigil near the hall's heart), facing
                // the way in: that is where his flock gathers
                Vector2 heart = W(L.GateX, (L.ShiftY + L.TopAt(L.GateX)) / 2f), at;
                if (circle) at = W(cc.x, cc.y);
                else
                {
                    var fr = spotsLeft();
                    if (fr.Count == 0) return;
                    var t = fr.OrderBy(q => (W(q.x, q.y) - heart).sqrMagnitude).First(); Mark(t.x, t.y, 1, 1);
                    at = W(t.x, t.y);
                    var s = Builder.Sr("occ_sigil_" + rnd.Next(3), at, Builder.floorLayer, Builder.floorOrder + 3, FloorMat);
                    s.transform.rotation = Quaternion.Euler(0f, 0f, rnd.Next(360));
                }
                var e = Elder.Spawn(at);
                var ea = e != null ? Activity.Give(e, Activity.Kind.Preach, W(L.GateX, 1.2f), at) : null;
                if (ea != null)
                {
                    var pick = UnityEngine.Object.FindObjectsOfType<Activity>()
                        .Where(a => a != null && a != ea && a.GetComponent<CultistTag>() != null && (a.What == Activity.Kind.Wander || a.What == Activity.Kind.Inspect || a.What == Activity.Kind.Guard || a.What == Activity.Kind.Patrol))
                        .OrderBy(a => ((Vector2)a.transform.position - at).sqrMagnitude).Take(2).ToList();
                    int k = 1;
                    foreach (var f in pick) { f.What = Activity.Kind.Follow; f.Leader = e.transform; f.Slot = k++; f.Route = null; ea.Mates.Add(f); }
                    SanctumPlugin.Log.LogInfo("Sanctum: the elder's retinue: " + pick.Count + " (he goes about the sanctum with them)");
                }
            });
            Safe("side rooms", () =>
            {
                var small = new List<string> { "library", "range", "barracks", "shrine", "cell", "store", "study", "armory", "forge", "tomb", "alchemy", "execution",
                                               "sanctuary", "ossuary", "scriptorium", "gallery", "summoning", "guardpost", "pantry", "guardpost", "range" }.OrderBy(_ => rnd.Next()).ToList();
                SideRoom.Rooms.Clear();
                var large = new List<string> { "warehouse", "training", "research", "refectory", "prison", "council", "scriptorium", "gallery", "ossuary", "summoning", "training" }.OrderBy(_ => rnd.Next()).ToList();
                foreach (var r in L.Side)
                {
                    var pool = r.width * r.height >= 56 ? large : small;
                    if (pool.Count == 0) pool = small.Count > 0 ? small : new List<string> { "store" };
                    // only what fits: a training room, a range, a guard post... in a closet came out a single target on
                    // the floor (the user) - such rooms want room to drill in
                    var fits = pool.Where(x => SideRoom.Fits(x, r)).ToList();
                    string role = fits.Count > 0 ? fits[rnd.Next(fits.Count)] : SideRoom.Tiny[rnd.Next(SideRoom.Tiny.Length)];
                    pool.Remove(role);
                    SideRoom.Thinning = true; try { SideRoom.Furnish(r, role, L, rnd); } finally { SideRoom.Thinning = false; }
                }
            });
            Safe("patrols", SpearPatrols);
            if (Champion.Enabled) Safe("champion", () =>
            {
                // the cult's champion with his two soldiers, near the hall's heart; they go the rounds of the whole sanctum
                Vector2 heart = W(L.GateX, (L.ShiftY + L.TopAt(L.GateX)) / 2f);
                var fr = spotsLeft();
                if (fr.Count == 0) return;
                var t = fr.OrderBy(q => Mathf.Abs((W(q.x, q.y) - heart).magnitude - 3f)).First(); Mark(t.x - 1, t.y - 1, 3, 3);
                Champion.Place(W(t.x, t.y), heart, rnd);
            });
            if (false) Safe("archmage", () =>      // (0.41.0, the user) the archmage is gone from the game; Magus.cs kept for a rollback
            {
                // the second champion, the archmage, with two cultists: a free spot farther out from the hall's heart
                Vector2 heart = W(L.GateX, (L.ShiftY + L.TopAt(L.GateX)) / 2f);
                var fr = spotsLeft();
                // (the user could not find him: the hall was full and he was left out) - no free spot: any floor will do
                if (fr.Count == 0) { for (int x = 1; x < L.W - 1; x++) for (int y = L.ShiftY + 2; y < L.H - 1; y++) if (Fl(x, y) && Fl(x - 1, y) && Fl(x + 1, y) && Fl(x, y - 1) && Fl(x, y + 1)) fr.Add(new Vector2Int(x, y)); }
                if (fr.Count == 0) { SanctumPlugin.Log.LogWarning("Sanctum: no floor at all for the archmage"); return; }
                var t = fr.OrderBy(q => Mathf.Abs((W(q.x, q.y) - heart).magnitude - 6f)).First(); Mark(t.x - 1, t.y - 1, 3, 3);
                Magus.Place(W(t.x, t.y), heart, rnd);
            });
            Safe("passages", Unjam);
            Safe("path grid", PathBlock.Install);
            Safe("dressing", () =>
            {
                // the corridors: old bones and blood here and there on their floor
                for (int x = 0; x < L.W; x++) for (int y = L.ShiftY; y < L.H; y++)
                {
                    if (!Fl(x, y) || L.Side.Any(r => r.Contains(new Vector2Int(x, y))) || rnd.Next(100) >= 7) continue;
                    bool corridor = (!Fl(x - 1, y) || !Fl(x + 1, y) || !Fl(x, y - 1) || !Fl(x, y + 1)) && (Fl(x - 1, y) && Fl(x + 1, y) || Fl(x, y - 1) && Fl(x, y + 1));
                    if (!corridor) continue;
                    var d = Builder.Sr(rnd.Next(3) == 0 ? "occ_blood_" + rnd.Next(3) : "occ_bones_" + rnd.Next(3), W(x + R(-.3f, .3f), y + R(-.3f, .3f)), Builder.floorLayer, Builder.floorOrder + 4, FloorMat);
                    d.transform.rotation = Quaternion.Euler(0, 0, rnd.Next(360));
                }
            });
            Safe("exit", () =>
            {
                var exit = Gate.Create(new Vector2(Room.O.x + gx, Room.O.y - 0.5f), true, false, "", Builder.wallsLayerId, Builder.wallsOrder, Builder.litMat, Builder.wallLayer, Builder.root);
                g.stairsOnThisFloor = exit.gameObject;
            });
            Safe("hero", () =>
            {
                var me = GameManager.Instance.Player_Mine();
                if (me == null) return;
                Vector2 to = W(gx, 1.2f);
                var rb = me.GetComponent<Rigidbody2D>();
                if (rb != null) { rb.velocity = Vector2.zero; rb.position = to; }
                me.transform.position = new Vector3(to.x, to.y, me.transform.position.z);
                try { me.GetComponent<PlayerController>()?.ResetLastStablePosition(); } catch { }
                try { CameraController.Instance.HardUpdatePosition(); } catch { }
            });
            SanctumPlugin.Log.LogInfo("Sanctum built");
        }

        static readonly List<Mob> hallMobs = new List<Mob>();
        static List<Vector2Int> spotsLeft()
        {
            var l = new List<Vector2Int>();
            for (int x = 0; x < L.W; x++) for (int y = L.ShiftY + 2; y < L.H; y++) if (Free(x, y)) l.Add(new Vector2Int(x, y));
            return l;
        }

        static void Put(Mob m, Vector2 p)
        {
            var rb = m.GetComponent<Rigidbody2D>();
            if (rb != null) { rb.velocity = Vector2.zero; rb.position = p; }
            m.transform.position = new Vector3(p.x, p.y, m.transform.position.z);
        }

        /// the spearmen who have no dummies to thrust at go the rounds of the store rooms and the hall in twos, threes
        /// and fours: the first leads, the others keep their places behind him (they gather to him first)
        static void SpearPatrols()
        {
            var men = UnityEngine.Object.FindObjectsOfType<Activity>()
                .Where(a => a != null && (a.GetComponent<SpearmanTag>() != null || a.GetComponent<Marksman>() != null) && !a.Stay
                            && a.What != Activity.Kind.Execute && a.What != Activity.Kind.Train && a.What != Activity.Kind.Shoot)
                .OrderBy(_ => rnd.Next()).ToList();
            if (men.Count == 0) return;
            Vector2 heart = W(L.GateX, (L.ShiftY + L.TopAt(L.GateX)) / 2f);
            var stores = new[] { "warehouse", "store", "armory", "prison", "forge", "training", "guardpost", "pantry", "cell" };
            var rooms = SideRoom.Rooms.Select(x => (c: W(x.r.xMin + x.r.width / 2f - .5f, x.r.yMin + x.r.height / 2f - .5f), x.role)).ToList();
            int i = 0, groups = 0;
            while (i < men.Count)
            {
                int left = men.Count - i, n = left <= 4 ? left : rnd.Next(2, 5);
                if (left - n == 1) n = n == 4 ? 3 : n + 1;
                var g = men.GetRange(i, n); i += n; groups++;
                var lead = g[0];
                var pts = rooms.Where(x => stores.Contains(x.role)).OrderBy(_ => rnd.Next()).Take(2)
                               .Concat(rooms.Where(x => !stores.Contains(x.role)).OrderBy(_ => rnd.Next()).Take(rnd.Next(2, 4)))
                               .Select(x => x.c).ToList();
                // nearest first, then on from one to the next; and the hall
                var route = new List<Vector2>(); Vector2 at = lead.transform.position;
                while (pts.Count > 0) { var nx = pts.OrderBy(p => (p - at).sqrMagnitude).First(); route.Add(nx); pts.Remove(nx); at = nx; }
                route.Add(heart + new Vector2(R(-2, 2), R(-1.5f, 1.5f)));
                lead.What = Activity.Kind.Patrol; lead.Route = route.ToArray(); lead.Look = heart; lead.Leader = null; lead.Mates.Clear();
                for (int k = 1; k < g.Count; k++)
                {
                    var f = g[k];
                    f.What = Activity.Kind.Follow; f.Leader = lead.transform; f.Slot = k; f.Route = null; f.Mates.Clear();
                    lead.Mates.Add(f);
                }
            }
            SanctumPlugin.Log.LogInfo("Sanctum: " + men.Count + " soldiers go the rounds in " + groups + " group(s)");
        }

        /// every free floor tile must be reachable from the way in. Where something stands in the way (a thing bigger
        /// than its tile, set askew, a heap that closes a corridor), the fewest things that open it again are taken away.
        static void Unjam()
        {
            Physics2D.SyncTransforms();
            int removed = 0;
            for (int pass = 0; pass < 12; pass++)
            {
                var blk = new bool[L.W, L.H]; var what = new Dictionary<Vector2Int, List<Container>>(); var cage = new bool[L.W, L.H];
                for (int x = 0; x < L.W; x++) for (int y = 0; y < L.H; y++)
                {
                    if (!Fl(x, y)) continue;
                    foreach (var c in Physics2D.OverlapBoxAll(W(x, y), new Vector2(0.55f, 0.55f), 0f))
                    {
                        if (c == null || c.isTrigger) continue;
                        var ct = c.GetComponentInParent<Container>();
                        if (ct == null || !ct.gameObject.activeInHierarchy || ct.GetComponent<DestroyableWall>() != null || ct.name.StartsWith("Sanctum_occ_none", StringComparison.Ordinal)) continue;
                        blk[x, y] = true;
                        var k = new Vector2Int(x, y);
                        if (!what.TryGetValue(k, out var l)) what[k] = l = new List<Container>();
                        if (!l.Contains(ct)) l.Add(ct);
                        if (ct.name == "Sanctum_occ_cage") cage[x, y] = true;
                    }
                }
                // what can be reached from the way in
                var start = new Vector2Int(L.GateX, 0);
                if (!Fl(start.x, start.y) || blk[start.x, start.y]) return;
                var seen = new bool[L.W, L.H]; var q = new Queue<Vector2Int>(); q.Enqueue(start); seen[start.x, start.y] = true;
                var dirs = new[] { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
                while (q.Count > 0)
                {
                    var c = q.Dequeue();
                    foreach (var d in dirs)
                    {
                        var n = c + d;
                        if (n.x < 0 || n.y < 0 || n.x >= L.W || n.y >= L.H || seen[n.x, n.y] || !Fl(n.x, n.y) || blk[n.x, n.y]) continue;
                        seen[n.x, n.y] = true; q.Enqueue(n);
                    }
                }
                var shut = new List<Vector2Int>();
                for (int x = 0; x < L.W; x++) for (int y = 0; y < L.H; y++) if (Fl(x, y) && !blk[x, y] && !seen[x, y]) shut.Add(new Vector2Int(x, y));
                if (shut.Count == 0) break;
                // the cheapest way from the shut-off tiles to the open ones (a thing costs 1 to remove, a cage 50, free floor 0)
                var cost = new int[L.W, L.H]; var from = new Vector2Int?[L.W, L.H];
                for (int x = 0; x < L.W; x++) for (int y = 0; y < L.H; y++) cost[x, y] = int.MaxValue;
                var dq = new LinkedList<Vector2Int>();
                foreach (var s in shut) { cost[s.x, s.y] = 0; dq.AddLast(s); }
                Vector2Int? goal = null;
                while (dq.Count > 0)
                {
                    var c = dq.First.Value; dq.RemoveFirst();
                    if (seen[c.x, c.y]) { goal = c; break; }
                    foreach (var d in dirs)
                    {
                        var n = c + d;
                        if (n.x < 0 || n.y < 0 || n.x >= L.W || n.y >= L.H || !Fl(n.x, n.y)) continue;
                        int w = blk[n.x, n.y] ? (cage[n.x, n.y] ? 50 : 1) : 0;
                        if (cost[c.x, c.y] + w >= cost[n.x, n.y]) continue;
                        cost[n.x, n.y] = cost[c.x, c.y] + w; from[n.x, n.y] = c;
                        if (w == 0) dq.AddFirst(n); else dq.AddLast(n);
                    }
                }
                if (goal == null) break;
                bool any = false;
                for (Vector2Int? t = goal; t.HasValue; t = from[t.Value.x, t.Value.y])
                    if (what.TryGetValue(t.Value, out var l))
                        foreach (var ct in l) if (ct != null && ct.gameObject.activeSelf) { SanctumPlugin.Log.LogInfo("Sanctum: taken away (it shut a passage): " + ct.name + " at " + t.Value); ct.gameObject.SetActive(false); UnityEngine.Object.Destroy(ct.gameObject); removed++; any = true; }
                if (!any) break;
            }
            if (removed > 0) SanctumPlugin.Log.LogInfo("Sanctum: " + removed + " thing(s) taken away to keep every passage open");
        }

        static bool InSide(Vector2 p) => L.Side.Any(r => r.Contains(new Vector2Int(Mathf.RoundToInt(p.x - Room.O.x), Mathf.RoundToInt(p.y - Room.O.y))));

        /// the hall's people at their business: readers at the lecterns, the faithful before the statues and the altar,
        /// a crossbowman at a target, others walking about, standing guard, going the rounds of the side rooms
        static void HallBusy(List<Vector2Int> free)
        {
            if (hallMobs.Count == 0) return;
            Vector2 heart = W(L.GateX, (L.ShiftY + L.TopAt(L.GateX)) / 2f);
            var props = UnityEngine.Object.FindObjectsOfType<Container>().Where(c => c != null && c.name.StartsWith("Sanctum_occ_", StringComparison.Ordinal) && !InSide(c.transform.position)).ToList();
            var lecterns = new Queue<Container>(props.Where(c => c.name == "Sanctum_occ_lectern"));
            var holy = new Queue<Container>(props.Where(c => c.name == "Sanctum_occ_statue" || c.name == "Sanctum_occ_altar").OrderBy(_ => rnd.Next()));
            bool targetSet = false;
            int focusN = 0; Vector2 focus = heart;
            var sights = props.Where(c => c.name == "Sanctum_occ_cage" || c.name == "Sanctum_occ_lectern" || c.name == "Sanctum_occ_statue" || c.name == "Sanctum_occ_altar" || c.name == "Sanctum_occ_sacrifice" || c.name == "Sanctum_occ_shelf")
                              .Select(c => (Vector2)c.transform.position).ToList();
            // the hall's cages: a jailer moves the prisoners about (if a cage stands open), an executioner leads them to a slab
            var hallCages = props.Where(c => c.name == "Sanctum_occ_cage").ToArray();
            var free0 = free.Count > 0 ? W(Pick(free).x, Pick(free).y) : heart;
            if (hallCages.Length >= 2 && hallCages.Any(c => Prisoner.In(c) == null))
            { var a = Activity.Give(Cultist.Spawn(free0), Activity.Kind.Shuffle, heart); if (a != null) a.Cages = hallCages; }
            var slab = props.FirstOrDefault(c => c.name == "Sanctum_occ_sacrifice");
            if (slab != null && hallCages.Any(c => Prisoner.In(c) != null) && rnd.Next(10) < 7)
            {
                Vector2 sp = slab.transform.position, d = (heart - sp).sqrMagnitude > 0.01f ? (heart - sp).normalized : Vector2.down;
                var ex = Activity.Give(Cultist.Spawn(sp + d * 1.9f), Activity.Kind.Execute, heart, sp + d * 1.15f, sp);
                if (ex != null) ex.Cages = hallCages;
            }
            foreach (var m in hallMobs.OrderBy(_ => rnd.Next()).ToList())
            {
                if (m == null) continue;
                Vector2 at = m.transform.position;
                bool cult = m.GetComponent<CultistTag>() != null, spear = m.GetComponent<SpearmanTag>() != null;
                if (cult && lecterns.Count > 0 && rnd.Next(3) > 0)
                {
                    var c = lecterns.Dequeue(); Vector2 p = c.transform.position; Vector2 dir = (heart - p).sqrMagnitude > 0.01f ? (heart - p).normalized : Vector2.down;
                    Put(m, p + dir * 0.95f); Activity.Give(m, Activity.Kind.Read, p); continue;
                }
                if (cult && holy.Count > 0 && rnd.Next(2) == 0)
                {
                    var c = holy.Dequeue(); Vector2 p = c.transform.position; Vector2 dir = (heart - p).sqrMagnitude > 0.01f ? (heart - p).normalized : Vector2.down;
                    Put(m, p + dir * 1.3f); Activity.Give(m, Activity.Kind.Pray, p); continue;
                }
                if (false && !cult && !spear && !targetSet && TryWall(2, out var tp, out var tr, 0.6f))   // (no target in the hall: they shoot in the ranges)
                {
                    targetSet = true;
                    float a = (tr + 270f) * Mathf.Deg2Rad;                                   // in front of the target, a few tiles off
                    Vector2 front = tp + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * 3.5f;
                    Breakables.Target(tp, tr);
                    if (Room.In(front)) { Put(m, front); Activity.Give(m, Activity.Kind.Shoot, tp); continue; }
                }
                if (spear && L.Side.Count > 0 && rnd.Next(3) > 0)
                {
                    // the rounds: from his place in the hall to one side room and another, and back
                    var rooms = L.Side.OrderBy(_ => rnd.Next()).Take(2).Select(r => W(r.xMin + r.width / 2f - .5f, r.yMin + r.height / 2f - .5f)).ToList();
                    SideRoom.Pair(m, Spearman.Spawn(at + new Vector2(0.9f, 0f)), at, new[] { at }.Concat(rooms).ToArray()); continue;   // they go the rounds by twos
                }
                int k = rnd.Next(10);
                if (k < 4 && sights.Count > 0)                                           // goes from one thing to the next and looks: cages, readers, statues
                {
                    // two or three begin at the same thing (they gather round a cage), then each goes his own way
                    if (focusN++ % 3 == 0) focus = sights[rnd.Next(sights.Count)];
                    Activity.Give(m, Activity.Kind.Inspect, heart, new[] { focus }.Concat(sights.Where(s => s != focus).OrderBy(_ => rnd.Next()).Take(3)).ToArray());
                }
                else if (k < 6) { var w = Activity.Give(m, Activity.Kind.Wander, at); if (w != null) w.Radius = 3f; }
                else if (k < 7 && free.Count > 1) Activity.Give(m, Activity.Kind.Patrol, at, at, W(Pick(free).x, Pick(free).y), W(Pick(free).x, Pick(free).y));
                else Activity.Give(m, Activity.Kind.Guard, heart + new Vector2(R(-3, 3), R(-3, 3)));
            }
        }

        static void Blood(Vector2 p)
        {
            var b = Builder.Sr("occ_blood_" + rnd.Next(3), p, Builder.floorLayer, Builder.floorOrder + 4, FloorMat);
            b.transform.rotation = Quaternion.Euler(0f, 0f, rnd.Next(360));
        }

        /// a free stretch of len tiles along a wall (north, west or east); the thing stands with its back to the wall
        /// depth: how deep the thing is from the wall (it is set with its back flush against the wall, not into it);
        /// northOnly: only along the walls the camera looks at (a table with books on it stands the right way round)
        static bool TryWall(int len, out Vector2 pos, out float rot, float depth = 0.6f, bool northOnly = false)
        {
            pos = default; rot = 0f;
            var c = new List<(int x, int y, char s)>();
            for (int x = 0; x < L.W; x++) for (int y = 2; y < L.H; y++)
            {
                if (!Free(x, y)) continue;
                if (FreeRect(x, y, len, 1) && Enumerable.Range(x, len).All(xx => !Fl(xx, y + 1))) c.Add((x, y, 'n'));
                if (northOnly) continue;
                if (FreeRect(x, y, 1, len) && Enumerable.Range(y, len).All(yy => !Fl(x - 1, yy))) c.Add((x, y, 'w'));
                if (FreeRect(x, y, 1, len) && Enumerable.Range(y, len).All(yy => !Fl(x + 1, yy))) c.Add((x, y, 'e'));
            }
            if (c.Count == 0) return false;
            var t = Pick(c);
            float half = (len - 1) / 2f, back = Mathf.Max(0f, 0.5f - depth / 2f - 0.03f);   // its back just touching the wall
            if (t.s == 'n') { pos = W(t.x + half, t.y + back); rot = 0f; Mark(t.x, t.y, len, 1); }
            else if (t.s == 'w') { pos = W(t.x - back, t.y + half); rot = 90f; Mark(t.x, t.y, 1, len); }
            else { pos = W(t.x + back, t.y + half); rot = -90f; Mark(t.x, t.y, 1, len); }
            return true;
        }

        static bool TryWallTile(out Vector2Int t)
        {
            var c = new List<Vector2Int>();
            for (int x = 0; x < L.W; x++) for (int y = 1; y < L.H; y++)
                if (Free(x, y) && (!Fl(x - 1, y) || !Fl(x + 1, y) || !Fl(x, y + 1))) c.Add(new Vector2Int(x, y));
            t = c.Count > 0 ? Pick(c) : default;
            return c.Count > 0;
        }

        /// a free w x h spot (by a wall if asked); its centre
        static bool TryRect(int w, int h, bool byWall, out Vector2 pos)
        {
            pos = default;
            var c = new List<Vector2Int>();
            for (int x = 0; x <= L.W - w; x++) for (int y = 2; y <= L.H - h; y++)
            {
                if (!FreeRect(x, y, w, h)) continue;
                bool wall = false;
                for (int i = 0; i < w; i++) if (!Fl(x + i, y + h) || !Fl(x + i, y - 1)) wall = true;
                for (int i = 0; i < h; i++) if (!Fl(x - 1, y + i) || !Fl(x + w, y + i)) wall = true;
                if (!byWall || wall) c.Add(new Vector2Int(x, y));
            }
            if (c.Count == 0) return false;
            var t = Pick(c);
            Mark(t.x, t.y, w, h);
            pos = W(t.x + (w - 1) / 2f, t.y + (h - 1) / 2f);
            return true;
        }

        /// a free n x n block, by a wall if asked
        static bool TryBlock(int n, bool byWall, out Vector2 pos)
        {
            pos = default;
            var c = new List<Vector2Int>();
            for (int x = 0; x <= L.W - n; x++) for (int y = 2; y <= L.H - n; y++)
            {
                if (!FreeRect(x, y, n, n)) continue;
                bool wall = false;
                for (int i = 0; i < n; i++) if (!Fl(x - 1, y + i) || !Fl(x + n, y + i) || !Fl(x + i, y + n)) wall = true;
                if (!byWall || wall) c.Add(new Vector2Int(x, y));
            }
            if (c.Count == 0) return false;
            var t = Pick(c);
            if (n > 1) Mark(t.x - 1, t.y - 1, n + 2, n + 2); else Mark(t.x, t.y, 1, 1);     // a little room round a cage
            pos = W(t.x + (n - 1) / 2f, t.y + (n - 1) / 2f);
            return true;
        }

        static void Safe(string what, Action a)
        {
            try { a(); } catch (Exception e) { SanctumPlugin.Log.LogError("Sanctum " + what + ": " + e); }
        }
    }

    /// the sanctum's things break like the game's own: each is a copy of one of its breakable props (a crate, a
    /// clay bowl) with our picture, our size and our debris
    public static class Breakables
    {
        static GameObject rockParticles; static AudioClip rockSound; static bool looked;

        static void LookUp()
        {
            if (looked) return;
            looked = true;
            var br = Prefabs.Cont("Brazier")?.GetComponent<Container>();
            rockParticles = br != null ? br.DestroyedParticles : null;
            var st = Prefabs.Cont("TombStatue_SpikesSafe")?.GetComponent<Container>() ?? Prefabs.Cont("TombStatue_Spikes")?.GetComponent<Container>();
            rockSound = st != null ? st.DestroySound : null;
            // (a friend of the user: "add sounds to the sanctum's things breaking") the sounds of the game's own things,
            // by what they are made of: wood (boxes, barrels), stone (statues), iron, clay (bowls, pots)
            Pick("wood", "box", "barrel", "crate", "table", "chair", "wood", "shelf");
            Pick("stone", "statue", "stone", "tomb", "rock", "brazier");
            Pick("iron", "iron", "metal", "armor", "cage", "anvil", "chain", "grate", "lamp");
            Pick("wax", "bowl", "pot", "jar", "vase", "urn", "candle", "skull");
            foreach (var kv in sounds) SanctumPlugin.Log.LogInfo("Sanctum: " + kv.Key + " breaks with the sound of " + kv.Value.from);
        }

        class Snd { public AudioClip one; public AudioClip[] many, hit; public string from; }
        static readonly Dictionary<string, Snd> sounds = new Dictionary<string, Snd>();

        static void Pick(string kind, params string[] words)
        {
            try
            {
                foreach (var w in words)
                    foreach (var c in Resources.LoadAll<Container>(""))
                    {
                        if (c == null || c.name.IndexOf(w, StringComparison.OrdinalIgnoreCase) < 0) continue;
                        bool has = c.DestroySound != null || (c.DestroySoundsArray != null && c.DestroySoundsArray.Any(a => a != null));
                        if (!has) continue;
                        sounds[kind] = new Snd { one = c.DestroySound, many = c.DestroySoundsArray, hit = c.sfx_OnHit, from = c.name };
                        return;
                    }
            }
            catch (Exception e) { SanctumPlugin.Log.LogWarning("Sanctum: sounds of " + kind + ": " + e.Message); }
        }

        static void Sound(Container cont, string kind)
        {
            if (!sounds.TryGetValue(kind, out var sn) && !sounds.TryGetValue("wood", out sn)) return;
            cont.DestroySound = sn.one;
            cont.DestroySoundsArray = sn.many != null ? sn.many.Where(a => a != null).ToArray() : new AudioClip[0];
            if ((cont.sfx_OnHit == null || cont.sfx_OnHit.Length == 0) && sn.hit != null && sn.hit.Length > 0) cont.sfx_OnHit = sn.hit;
        }

        static Sprite[] Sprites(params string[] names) => names.Select(Art.Get).Where(s => s != null).ToArray();

        /// a heavy thing stands fast: it cannot be pushed about, only broken
        /// square to the walls (the user: tables and shelves stood askew)
        public static float Square(float rot) => Mathf.Round(rot / 90f) * 90f;

        public static void Pin(Container c)
        {
            if (c == null) return;
            foreach (var rb in c.GetComponentsInParent<Rigidbody2D>(true).Concat(c.GetComponentsInChildren<Rigidbody2D>(true)))
                if (rb != null) { rb.velocity = Vector2.zero; rb.bodyType = RigidbodyType2D.Static; }
        }

        /// (the user: a table stood with its long side up and down where it should lie across the room) a long thing lies
        /// along the two tiles it was given: centred between two tiles side by side -> lengthwise left-right, between two
        /// tiles one above the other -> up-down; turned the quarter that has its front (its upper edge) towards the
        /// open floor, not the wall
        static float AlongItsTiles(Vector2 pos, float rot, Vector2? box)
        {
            if (!box.HasValue || box.Value.x < box.Value.y * 1.4f) return rot;
            // (0.39.0, the user's picture: an altar stood with its long side away from the wall it was set against)
            // a long thing against ONE wall lies along that wall, whatever tiles it was put between
            {
                bool wN = !Room.In(pos + Vector2.up * 1.1f), wS = !Room.In(pos + Vector2.down * 1.1f), wE = !Room.In(pos + Vector2.right * 1.1f), wW = !Room.In(pos + Vector2.left * 1.1f);
                bool horizWall = wN ^ wS, vertWall = wE ^ wW;
                if (horizWall != vertWall)
                {
                    float rr = Square(rot);
                    bool lyingNow = Mathf.Abs(Mathf.DeltaAngle(rr, 0f)) < 1f || Mathf.Abs(Mathf.DeltaAngle(rr, 180f)) < 1f;
                    if (lyingNow == horizWall) return rr;                               // already along it
                    float ra = rr - 90f, rb = rr + 90f;
                    return Open(pos, rb) > Open(pos, ra) ? rb : ra;
                }
            }
            float fx = Mathf.Abs((pos.x - Room.O.x) - Mathf.Round(pos.x - Room.O.x)), fy = Mathf.Abs((pos.y - Room.O.y) - Mathf.Round(pos.y - Room.O.y));
            bool across = fx > 0.3f && fy < 0.2f, upDown = fy > 0.3f && fx < 0.2f;
            if (!across && !upDown) return rot;
            float r = Square(rot);
            bool lying = Mathf.Abs(Mathf.DeltaAngle(r, 0f)) < 1f || Mathf.Abs(Mathf.DeltaAngle(r, 180f)) < 1f;   // long side left-right now
            if (across == lying) return r;
            float a = r - 90f, b = r + 90f;
            return Open(pos, b) > Open(pos, a) ? b : a;
        }

        /// how much floor lies before a thing's front (its upper edge, turned by rot)
        static int Open(Vector2 pos, float rot)
        {
            Vector2 up = new Vector2(-Mathf.Sin(rot * Mathf.Deg2Rad), Mathf.Cos(rot * Mathf.Deg2Rad));
            int n = 0;
            for (int k = 1; k <= 3; k++) if (Room.In(pos + up * k)) n++;
            return n;
        }

        static Container Make(string template, string art, Vector2 pos, float rot, float hp, string[] debris, bool stone, Vector2? box, float radius, out SpriteRenderer sr)
        {
            LookUp();
            rot = AlongItsTiles(pos, rot, box);
            sr = null;
            var prefab = Prefabs.Cont(template);
            var go = Builder.Spawn(prefab, pos, rot);
            if (go == null) { SanctumPlugin.Log.LogWarning("Sanctum: no prop " + template); return null; }
            Builder.Own(go);
            go.name = "Sanctum_" + art;
            foreach (var rs in go.GetComponentsInChildren<RandomSprite>(true)) UnityEngine.Object.DestroyImmediate(rs);
            var cont = go.GetComponent<Container>() ?? go.GetComponentInChildren<Container>(true);
            sr = cont.GetComponent<SpriteRenderer>() ?? cont.GetComponentInChildren<SpriteRenderer>(true);
            if (sr != null) sr.sprite = Art.Get(art);       // the prop keeps the game's own material and layer
            foreach (var cl in cont.GetComponents<Collider2D>()) UnityEngine.Object.DestroyImmediate(cl);
            foreach (var cmp in cont.GetComponents<Component>()) if (cmp != null && cmp.GetType().Name == "SFPolygon") UnityEngine.Object.DestroyImmediate(cmp);
            if (box.HasValue) cont.gameObject.AddComponent<BoxCollider2D>().size = box.Value;
            else cont.gameObject.AddComponent<CircleCollider2D>().radius = radius;
            cont.hp = hp;
            cont.playDamageAnimation = true;                                                    // (the user, 2026-11-21: every thing jerks a little when struck, like the game's own)
            cont.DestroyObject_Sprites = Sprites(debris);
            cont.damageSprites?.Clear();
            cont.hideUnderWaterSprites?.Clear();
            cont.OnDamageObject_Sprites = new Sprite[0];
            string kind = debris != null && debris.Length > 0 ? (debris[0].Contains("iron") ? "iron" : debris[0].Contains("stone") ? "stone" : debris[0].Contains("wax") ? "wax" : "wood") : "wood";
            Sound(cont, kind);
            if (stone)
            {
                if (rockParticles != null) { cont.DestroyedParticles = rockParticles; cont.DestroyedParticles2 = null; }
                if (kind == "stone" && rockSound != null) { cont.DestroySound = rockSound; cont.DestroySoundsArray = new AudioClip[0]; }
            }
            return cont;
        }

        public static void Table(int v, Vector2 pos, float rot)                                // square to the walls and standing fast: nobody shoves it askew
            => Pin(Make("Box", "occ_table_" + v, pos, Square(rot), 3f, new[] { "occ_wood_destr_0", "occ_wood_destr_1", "occ_wood_destr_2" }, false, new Vector2(1.85f, 1.05f), 0f, out _));

        public static void Altar(Vector2 pos)
            => Pin(Make("Box", "occ_altar", pos, 0f, 8f, new[] { "occ_stone_destr_0", "occ_stone_destr_1", "occ_stone_destr_2" }, true, new Vector2(2.45f, 1.05f), 0f, out _));

        public static void Column(Vector2 pos)
            => Pin(Make("Box", "occ_column", pos, 0f, 6f, new[] { "occ_stone_destr_0", "occ_stone_destr_1", "occ_stone_destr_2" }, true, null, 0.6f, out _));

        /// an iron cage with a live monster of the game shut in it (a skeleton, a rotfiend, a zombie): it stands there
        /// asleep and cannot be hurt; break the cage and it comes out and fights
        public static Container Cage(string kind, Vector2 pos)
        {
            var cont = Make("Box", "occ_cage", pos, 0f, 1f, new[] { "occ_iron_destr_0", "occ_iron_destr_1", "occ_iron_destr_2" }, true, new Vector2(1.35f, 1.35f), 0f, out var sr);
            if (cont == null) return null;
            Pin(cont);                                                                          // a cage stands fast: break it, do not push it
            if (sr != null) { sr.sortingLayerID = TopCreatureLayer(); sr.sortingOrder = 400; }      // the bars in front of whoever is inside
            if (kind != null) Prisoner.Shut(kind, pos, cont);                                    // (no kind: an empty cage)
            return cont;
        }

        public static int TopCreatureLayer()
        {
            int best = SortingLayer.NameToID("Mob"), bestVal = SortingLayer.GetLayerValueFromID(best);
            foreach (var n in new[] { "Character", "Mob" })
            {
                int id = SortingLayer.NameToID(n);
                if (SortingLayer.IsValid(id) && SortingLayer.GetLayerValueFromID(id) > bestVal) { best = id; bestVal = SortingLayer.GetLayerValueFromID(id); }
            }
            return best;
        }

        /// a bookcase against the wall (rot: its back to the wall)
        public static void Shelf(Vector2 pos, float rot) { }                              // no bookcases at all (the user)
        static void ShelfUnused(Vector2 pos, float rot)
            => Pin(Make("Box", "occ_shelf", pos, Square(rot), 4f, new[] { "occ_wood_destr_0", "occ_wood_destr_1", "occ_wood_destr_2" }, false, new Vector2(2.05f, 0.65f), 0f, out _));

        public static void Bench(Vector2 pos, float rot)
            => Make("Box", "occ_bench", pos, Square(rot), 2f, new[] { "occ_wood_destr_0", "occ_wood_destr_2" }, false, new Vector2(1.8f, 0.45f), 0f, out _);   // (0.39.0, the user: benches can be pushed again)

        /// a stone fireplace against the wall with a fire in it; smashed, the fire goes out
        public static void Fireplace(Vector2 pos, float rot)
        {
            var cont = Make("Box", "occ_fireplace", pos, rot, 10f, new[] { "occ_stone_destr_0", "occ_stone_destr_1", "occ_stone_destr_2", "occ_wood_destr_1" }, true, new Vector2(2.9f, 1.15f), 0f, out var sr);
            if (cont == null) return;
            var fl = Builder.Sr("occ_fire_0", pos, sr != null ? sr.sortingLayerID : Builder.env, (sr != null ? sr.sortingOrder : 18) + 1, null, cont.transform);
            fl.transform.localRotation = Quaternion.identity;
            fl.gameObject.AddComponent<Flicker>().Init(new[] { "occ_fire_0", "occ_fire_1", "occ_fire_2" }, 0.11f, 0.04f, sr);
        }

        /// an iron candle stand with five candles; knocked over, the candles go out
        public static void Candelabra(Vector2 pos)
        {
            var cont = Make("CryptBowl", "occ_candelabra", pos, 0f, 2f, new[] { "occ_iron_destr_0", "occ_wax_destr_0", "occ_wax_destr_1" }, false, null, 0.42f, out var sr);
            Pin(cont);                                                                          // a tall iron stand: it stands fast (the user)
            Flame(cont, sr, pos, "occ_candelabra_fl_", 3, 0.13f);
            if (cont != null) Builder.Glow(cont.transform, pos, 0.85f, Builder.FireLight);
        }

        /// an iron bowl of fire on three feet ((0.41.2, the user) an ordinary orange fire now, not the blood-red one; it lights the hall)
        public static void RedBrazier(Vector2 pos)
        {
            var cont = Make("Box", "occ_brazier_fire", pos, 0f, 4f, new[] { "occ_iron_destr_0", "occ_iron_destr_1", "occ_iron_destr_2" }, true, null, 0.48f, out var sr);
            Pin(cont);
            Flame(cont, sr, pos, "occ_ofire_", 3, 0.12f);
            if (cont != null) Builder.Glow(cont.transform, pos, 1.15f, Builder.FireLight);
        }

        /// a hooded cultist of stone on a plinth, facing the lane
        public static void Statue(Vector2 pos, float rot)
            => Pin(Make("Box", "occ_statue", pos, Square(rot), 7f, new[] { "occ_stone_destr_0", "occ_stone_destr_1", "occ_stone_destr_2" }, true, new Vector2(1.05f, 1.05f), 0f, out _));

        /// a sacrifice slab with chains
        public static void Sacrifice(Vector2 pos)
            => Pin(Make("Box", "occ_sacrifice", pos, 0f, 8f, new[] { "occ_stone_destr_0", "occ_stone_destr_1", "occ_iron_destr_1" }, true, new Vector2(1.9f, 0.95f), 0f, out _));

        public static void Lectern(Vector2 pos)
            => Pin(Make("Box", "occ_lectern", pos, 0f, 2f, new[] { "occ_wood_destr_0", "occ_wood_destr_1" }, false, new Vector2(0.85f, 0.6f), 0f, out _));

        /// a black obelisk with a dull red glow round its foot
        public static void Obelisk(Vector2 pos) { }                                       // no blood crystal at all (the user)
        static void ObeliskUnused(Vector2 pos)
        {
            var cont = Make("Box", "occ_obelisk", pos, 0f, 9f, new[] { "occ_stone_destr_0", "occ_stone_destr_1", "occ_stone_destr_2" }, true, null, 0.45f, out var sr);
            if (cont == null) return;
            Pin(cont);
            var glow = Builder.Sr("occ_obelisk_glow_0", pos, Builder.floorLayer, Builder.floorOrder + 5, null, cont.transform);
            glow.gameObject.AddComponent<Flicker>().Init(new[] { "occ_obelisk_glow_0", "occ_obelisk_glow_1" }, 0.6f, 0.2f, sr);
        }

        /// a heap of skulls with candles stuck on it
        public static void SkullPile(Vector2 pos)
        {
            var cont = Make("Box", "occ_skullpile", pos, 0f, 3f, new[] { "occ_stone_destr_0", "occ_wax_destr_0" }, true, null, 0.45f, out var sr);
            Flame(cont, sr, pos, "occ_skullpile_fl_", 3, 0.13f);
            if (cont != null) Builder.Glow(cont.transform, pos, 0.6f, Builder.FireLight);
        }

        static void Flame(Container cont, SpriteRenderer sr, Vector2 pos, string prefix, int n, float step)
        {
            if (cont == null) return;
            var fl = Builder.Sr(prefix + "0", pos, sr != null ? sr.sortingLayerID : Builder.env, (sr != null ? sr.sortingOrder : 18) + 1, null, cont.transform);
            fl.transform.localRotation = Quaternion.identity;
            fl.gameObject.AddComponent<Flicker>().Init(Enumerable.Range(0, n).Select(i => prefix + i).ToArray(), step, step * .5f, sr);
        }

        /// a straw practice target on a wooden stand, bolts stuck in it (the crossbowmen shoot at it)
        public static void Target(Vector2 pos, float rot)
            => Pin(Make("Box", "occ_target", pos, rot, 40f, new[] { "occ_wood_destr_0", "occ_wood_destr_1", "occ_wood_destr_2" }, false, new Vector2(1.5f, 0.6f), 0f, out _));

        /// a straw training dummy on a post (the spearmen practise their thrusts on it)
        public static void Dummy(Vector2 pos, float rot)
            => Pin(Make("Box", "occ_dummy", pos, rot, 30f, new[] { "occ_wood_destr_0", "occ_wood_destr_2" }, false, null, 0.45f, out _));

        /// a weapon rack against the wall, spears and a crossbow on it
        public static void Rack(Vector2 pos, float rot)
            => Pin(Make("Box", "occ_rack", pos, rot, 5f, new[] { "occ_wood_destr_0", "occ_wood_destr_1", "occ_iron_destr_0" }, false, new Vector2(1.65f, 0.5f), 0f, out _));

        /// a rack with crossbows lying on it (like the spear rack)
        public static void RackX(Vector2 pos, float rot)
            => Pin(Make("Box", "occ_rack_xbow", pos, rot, 5f, new[] { "occ_wood_destr_0", "occ_wood_destr_1", "occ_iron_destr_0" }, false, new Vector2(1.65f, 0.5f), 0f, out _));

        /// a bedding rolled out on the floor
        /// a bed (the user: the old beddings looked awful): a wooden frame, a mattress, a pillow, a red blanket
        public static Container Bedroll(Vector2 pos, float rot)
        { var c = Make("Box", "occ_bedroll", pos, Square(rot), 2f, new[] { "occ_wood_destr_0", "occ_wood_destr_2" }, false, new Vector2(0.8f, 1.7f), 0f, out _); Pin(c); return c; }

        /// an unseen prop that takes the blows for a breakable stretch of wall (its picture is the wall itself)
        public static Container Hidden(Vector2 pos)
        {
            var cont = Make("Box", "occ_none", pos, 0f, 6f, new[] { "occ_stone_destr_0", "occ_stone_destr_1", "occ_stone_destr_2" }, true, new Vector2(1.05f, 1.05f), 0f, out var sr);
            if (sr != null) sr.sprite = null;
            return cont;
        }

        public static void Candles(Vector2 pos)
        {
            var cont = Make("CryptBowl", "occ_candles", pos, 0f, 1f, new[] { "occ_wax_destr_0", "occ_wax_destr_1" }, false, null, 0.28f, out var sr);
            if (cont == null) return;
            // the flames shine on their own; they go out with the candles
            var fl = Builder.Sr("occ_flames_0", pos, sr != null ? sr.sortingLayerID : Builder.env, (sr != null ? sr.sortingOrder : 18) + 1, null, cont.transform);
            fl.gameObject.AddComponent<Flicker>().Init(new[] { "occ_flames_0", "occ_flames_1", "occ_flames_2" }, 0.13f, 0.08f, sr);
            Builder.Glow(cont.transform, pos, 0.55f, Builder.FireLight);
        }
    }

    /// swaps a sprite's frames (the circle's dim glow, candle flames); goes out when what it burns on is gone
    /// a fire's light that goes out when its thing is broken
    public class GlowOff : MonoBehaviour
    {
        public SpriteRenderer on; public Sprite onSprite;
        SpriteRenderer sr; SFLight sf; bool was = true;
        void Start() { sr = GetComponent<SpriteRenderer>(); sf = GetComponent<SFLight>(); }
        void Update()
        {
            bool lit = on != null && on.enabled && on.gameObject.activeInHierarchy && on.sprite == onSprite;
            if (lit == was) return;
            was = lit;
            if (sr != null) sr.enabled = lit;
            if (sf != null) sf.enabled = lit;
        }
    }

    public class Flicker : MonoBehaviour
    {
        Sprite[] frames; float step, jitter, nextAt; int i; SpriteRenderer sr, on; Sprite onSprite;
        public void Init(string[] names, float step, float jitter, SpriteRenderer burnsOn)
        {
            frames = names.Select(Art.Get).Where(s => s != null).ToArray();
            this.step = step; this.jitter = jitter; sr = GetComponent<SpriteRenderer>(); on = burnsOn; onSprite = on != null ? on.sprite : null;
            i = UnityEngine.Random.Range(0, Mathf.Max(1, frames.Length));
        }
        void Update()
        {
            if (on != null || onSet)
            {
                onSet = true;
                bool lit = on != null && on.enabled && on.gameObject.activeInHierarchy && on.sprite == onSprite;   // it burns while its thing is whole
                if (sr.enabled != lit) sr.enabled = lit;
                if (!lit) return;
            }
            if (frames == null || frames.Length < 2 || Time.time < nextAt) return;
            nextAt = Time.time + step + UnityEngine.Random.Range(-jitter, jitter);
            i = (i + 1) % frames.Length;
            sr.sprite = frames[i];
        }
        bool onSet;
    }

    public static class Prefabs
    {
        static Dictionary<string, GameObject> conts;
        public static GameObject Cont(string name)
        {
            if (name == "Dresser") name = "Cupboard";                    // (0.41.1, the user) no chests of drawers in the sanctum at all: a cupboard stands there instead
            if (conts == null)
            {
                conts = new Dictionary<string, GameObject>();
                try { foreach (var m in Resources.LoadAll<Container>("")) if (m != null && !conts.ContainsKey(m.name)) conts[m.name] = m.gameObject; }
                catch (Exception e) { SanctumPlugin.Log.LogWarning("Containers: " + e.Message); }
            }
            return conts.TryGetValue(name, out var g) ? g : Resources.Load<GameObject>(name);
        }
    }
}
