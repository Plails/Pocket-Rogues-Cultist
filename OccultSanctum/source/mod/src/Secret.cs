using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace OccultSanctum
{
    /// A secret room of the sanctum, hidden the way the game hides its own: the chamber's tiles are "hidden" tiles of
    /// the map (drawn as plain rock, nothing shows), and the way in is one tile of the hall's wall made a breakable
    /// wall tile of the game (it looks like the wall, a little battered). Break it and the game itself opens the
    /// hidden tiles behind it into floor - then the chamber is furnished: a treasury, a shrine, a stash, a cell with
    /// a prisoner, an ossuary or a scholar's cell, every time a little different.
    public static class Secret
    {
        public enum Kind { Treasury, Shrine, Stash, Cell, Ossuary, Study, Reliquary, Rite, HeroTomb }
        static readonly string[] KindNames = { "сокровищница", "тайная молельня", "тайник", "темница", "костница", "келья книжника", "реликварий", "тайный обряд", "гробница героя" };

        public static bool Has;
        public static RectInt Rect;                 // the chamber, in the layout's tiles (may lie outside 0..W)
        public static Vector2Int WallTile, Pass;     // the breakable wall in the hall's wall line, and the hidden tile behind it
        public static char Side;
        public static Kind What;
        static int lat, dep, entU;                   // the chamber's width across, its depth, the column of the way in

        public static IEnumerable<Vector2Int> RoomTiles()
        {
            if (!Has) yield break;
            for (int x = Rect.xMin; x < Rect.xMax; x++) for (int y = Rect.yMin; y < Rect.yMax; y++) yield return new Vector2Int(x, y);
        }

        /// pick the chamber's place and kind for this visit (or none)
        public static void Plan(Layout L, System.Random rnd)
        {
            Has = false;
            if (rnd.Next(100) >= SanctumPlugin.CfgSecretChance.Value) return;
            int lo = -Room.Margin + 2, hiX = L.W + Room.Margin - 3, hiY = L.H + Room.Margin - 3;
            var cands = new List<(RectInt r, Vector2Int a, Vector2Int b, char s, int lat, int dep)>();
            for (int x = 0; x < L.W; x++)
                for (int y = L.ShiftY + 2; y < L.H; y++)
                {
                    if (!L.Floor(x, y)) continue;
                    foreach (char s in new[] { 'w', 'e', 'n' })
                    {
                        int la = rnd.Next(5, 8), de = rnd.Next(5, 8);
                        Vector2Int d = s == 'w' ? Vector2Int.left : s == 'e' ? Vector2Int.right : Vector2Int.up;
                        var a = new Vector2Int(x, y) + d; var b = a + d;
                        RectInt r = s == 'w' ? new RectInt(b.x - de, y - la / 2, de, la)
                                  : s == 'e' ? new RectInt(b.x + 1, y - la / 2, de, la)
                                  : new RectInt(x - la / 2, b.y + 1, la, de);
                        if (r.xMin < lo || r.yMin < lo || r.xMax - 1 > hiX || r.yMax - 1 > hiY) continue;
                        if (L.Floor(a.x, a.y) || L.Floor(b.x, b.y)) continue;
                        bool ok = true;                                                  // solid rock two tiles deep all round it
                        for (int i = r.xMin - 2; i < r.xMax + 2 && ok; i++) for (int j = r.yMin - 2; j < r.yMax + 2 && ok; j++) if (L.Floor(i, j)) ok = false;
                        Vector2Int side = s == 'n' ? Vector2Int.right : Vector2Int.up;  // a plain straight stretch of wall
                        if (L.Floor(a.x + side.x, a.y + side.y) || L.Floor(a.x - side.x, a.y - side.y)) ok = false;
                        if (!L.Floor(x + side.x, y + side.y) || !L.Floor(x - side.x, y - side.y)) ok = false;
                        if (ok) cands.Add((r, a, b, s, la, de));
                    }
                }
            if (cands.Count == 0) { SanctumPlugin.Log.LogInfo("Sanctum: no place for a secret room in " + L.Name); return; }
            var c = cands[rnd.Next(cands.Count)];
            Has = true; Rect = c.r; WallTile = c.a; Pass = c.b; Side = c.s; lat = c.lat; dep = c.dep;
            entU = Side == 'n' ? Pass.x - Rect.xMin : Pass.y - Rect.yMin;
            What = (Kind)rnd.Next(KindNames.Length);
            SanctumPlugin.Log.LogInfo($"Sanctum: a secret room ({KindNames[(int)What]}, {lat}x{dep}) behind the {(Side == 'w' ? "west" : Side == 'e' ? "east" : "north")} wall at {WallTile}");
        }

        /// on the map: the way in is a breakable wall tile, the passage and the chamber are hidden tiles (rock)
        public static void Mark(Floor floor, Vector2Int o)
        {
            if (!Has) return;
            floor.SetTile(o.x + WallTile.x, o.y + WallTile.y, c.TileTypes.destr);
            floor.SetTile(o.x + Pass.x, o.y + Pass.y, c.TileTypes.hidden);
            foreach (var t in RoomTiles()) floor.SetTile(o.x + t.x, o.y + t.y, c.TileTypes.hidden);
        }

        static System.Random rnd; static Material floorMat;

        public static void Build(System.Random r, Material mat)
        {
            if (!Has) return;
            rnd = r; floorMat = mat;
            Builder.root.gameObject.AddComponent<Watcher>();
        }

        /// makes sure the way in can be broken (the game puts its breakable wall there; if this scene has none, ours),
        /// and furnishes the chamber the moment the game opens it
        class Watcher : MonoBehaviour
        {
            IEnumerator Start()
            {
                yield return new WaitForSeconds(0.5f);
                try { EnsureBreakable(); } catch (Exception e) { SanctumPlugin.Log.LogError("Sanctum secret wall: " + e); }
                var g = Builder.gen;
                var p = Room.W(Pass.x, Pass.y);
                while (g != null && g.floor != null && g.floor.GetTile(Mathf.RoundToInt(p.x), Mathf.RoundToInt(p.y)) != c.TileTypes.floor)
                    yield return new WaitForSeconds(0.2f);
                try { Furnish(); } catch (Exception e) { SanctumPlugin.Log.LogError("Sanctum secret room: " + e); }
                Destroy(this);
            }
        }

        static void EnsureBreakable()
        {
            Vector2 at = Room.W(WallTile.x, WallTile.y);
            bool has = UnityEngine.Object.FindObjectsOfType<DestroyableWall>().Any(w => w != null && ((Vector2)w.transform.position - at).sqrMagnitude < 0.2f);
            if (has) { SanctumPlugin.Log.LogInfo("Sanctum: the secret room's way in is the game's breakable wall"); return; }
            var g = Builder.gen;
            if (g.destrWallPrefab != null && g.destrWallPrefab.GetComponentInChildren<DestroyableWall>(true) != null)
            {
                var w = UnityEngine.Object.Instantiate(g.destrWallPrefab, new Vector3(at.x, at.y, 0f), Quaternion.identity, g.transform);
                SanctumPlugin.Log.LogInfo("Sanctum: the secret room's way in: the game's breakable wall, put there");
                return;
            }
            // this scene has no breakable wall of its own: a few cracks on the wall tile and a hidden prop that takes
            // the blows; when it breaks, the game's own tile breaking opens the way like any secret wall
            var cont = Breakables.Hidden(at);
            var cr = Builder.Sr("occ_cracks", at, Builder.wallsLayerId, Builder.wallsOrder + 3, Builder.litMat);
            if (cont != null)
            {
                cr.transform.SetParent(cont.transform, true);
                int tx = Mathf.RoundToInt(at.x), ty = Mathf.RoundToInt(at.y);
                cont.ActionOnDestroy += () => { try { Builder.gen.DestroyTile(tx, ty); } catch (Exception e) { SanctumPlugin.Log.LogError("Sanctum secret wall: " + e); } };
            }
            SanctumPlugin.Log.LogInfo("Sanctum: the secret room's way in: a cracked wall of ours");
        }

        // ---------------------------------------------------------------- the chamber's things
        /// a tile of the chamber by its column across (u, 0 = one side) and its depth (v, 0 = by the way in)
        static Vector2 T(float u, float v)
        {
            if (Side == 'n') return Room.W(Rect.xMin + u, Rect.yMin + v);
            if (Side == 'w') return Room.W(Rect.xMax - 1 - v, Rect.yMin + u);
            return Room.W(Rect.xMin + v, Rect.yMin + u);
        }
        /// a round stone dais with a ring of red runes under a chest (or whatever stands on it)
        static void Dais(float u, float v)
        {
            var d = Builder.Sr("occ_dais", T(u, v), Builder.floorLayer, Builder.floorOrder + 3, floorMat);
            d.transform.rotation = Quaternion.Euler(0, 0, rnd.Next(4) * 90f);
        }

        static float FaceIn => Side == 'n' ? 0f : Side == 'w' ? 90f : -90f;      // facing the way in (back to the far wall)
        static float R(float a, float b) => a + (float)rnd.NextDouble() * (b - a);
        static readonly HashSet<Vector2Int> used = new HashSet<Vector2Int>();
        static bool Take(int u, int v)
        {
            if (u < 0 || v < 0 || u >= lat || v >= dep) return false;
            if (v == 0 && Mathf.Abs(u - entU) <= 1) return false;                  // the way in stays clear
            return used.Add(new Vector2Int(u, v));
        }

        static void Furnish()
        {
            used.Clear();
            SanctumPlugin.Log.LogInfo("Sanctum: the secret room is open - " + KindNames[(int)What]);
            for (int u = 0; u < lat; u++) for (int v = 0; v < dep; v++)
            {
                var p = T(u, v);
                var sr = Builder.Sr("occ_floor_big", p, Builder.floorLayer, Builder.floorOrder + 1, floorMat);
                int ix = Mathf.RoundToInt(p.x), iy = Mathf.RoundToInt(p.y);
                sr.sprite = Art.Tile("occ_floor_big", ((ix % 30) + 30) % 30, ((iy % 30) + 30) % 30);
            }
            { var p = Room.W(Pass.x, Pass.y); var sr = Builder.Sr("occ_floor_big", p, Builder.floorLayer, Builder.floorOrder + 1, floorMat); sr.sprite = Art.Tile("occ_floor_big", Mathf.RoundToInt(p.x) % 30, Mathf.RoundToInt(p.y) % 30); }
            int far = dep - 1, mid = entU, L0 = 0, L1 = lat - 1;
            // a torch on the far wall: the chamber is lit the moment it opens
            try
            {
                var g = Builder.gen;
                var torch = g.decorOnWalls?.FirstOrDefault(d => d != null && d.name == "TorchActive") ?? Resources.Load<GameObject>("TorchActive");
                float trot = Side == 'n' ? 270f : Side == 'w' ? 0f : 180f;
                int tu = lat >= 5 ? (rnd.Next(2) == 0 ? 1 : lat - 2) : mid;
                if (torch != null) Builder.Own(g.CreateOnWall(new[] { torch }, T(tu, far), Quaternion.Euler(0, 0, trot)));
            }
            catch (Exception e) { SanctumPlugin.Log.LogWarning("Sanctum secret torch: " + e.Message); }
            // a red runner from the way in to the far wall (in the treasury, the shrine and the ossuary)
            if (What == Kind.Treasury || What == Kind.Shrine || What == Kind.Ossuary || What == Kind.Reliquary || What == Kind.HeroTomb)
                for (int v = 0; v < far - 1; v++)
                {
                    var c = Builder.Sr(v == 0 || v == far - 2 ? "occ_carpet_end" : "occ_carpet", T(mid, v), Builder.floorLayer, Builder.floorOrder + 2, floorMat);
                    float cr = Side == 'n' ? 0f : 90f;
                    if (v == 0) cr += 180f;
                    c.transform.rotation = Quaternion.Euler(0, 0, cr);
                }
            if (What != Kind.Stash && What != Kind.Cell) Webs(rnd.Next(1, 3));
            switch (What)
            {
                case Kind.Treasury:                                                    // the cult's hoard: the best chest on its dais between red fires and candle stands
                    Chest(mid, far, "Chest6");
                    if (Take(mid - 1, far)) Breakables.RedBrazier(T(mid - 1, far)); if (Take(mid + 1, far)) Breakables.RedBrazier(T(mid + 1, far));
                    if (Take(L0, far)) Breakables.Candelabra(T(L0, far)); if (Take(L1, far)) Breakables.Candelabra(T(L1, far));
                    for (int v = 1; v < far; v += 2) foreach (var u in new[] { L0, L1 }) if (Take(u, v)) Breakables.SkullPile(T(u, v));
                    Corners(Breakables.Candles, 2);
                    Sigil(mid, Mathf.Max(1, far / 2));
                    break;
                case Kind.Reliquary:                                                   // a relic on a dais in the middle, hooded statues keeping watch round it, a ring of candles
                {
                    int cv = dep / 2;
                    Chest(mid, cv, "Chest6");
                    foreach (var (du, dv) in new[] { (-1, 0), (1, 0), (0, 1), (0, -1) }) if (Take(mid + du, cv + dv)) Breakables.Candles(T(mid + du, cv + dv));
                    foreach (var u in new[] { L0, L1 }) foreach (var v in new[] { 1, far }) if (Take(u, v)) Breakables.Statue(T(u, v), u == L0 ? (Side == 'n' ? 90f : 180f) : (Side == 'n' ? -90f : 0f));   // facing the relic
                    if (Take(mid, far)) Breakables.Candelabra(T(mid, far));
                    break;
                }
                case Kind.Rite:                                                        // a hidden rite: sigils all over the floor, skulls with candles in a ring, blood, the offering in the middle
                {
                    int cv = dep / 2;
                    Chest(mid, cv, "Chest3");
                    for (int i = 0; i < 6; i++) { float a = i * Mathf.PI / 3f; int u = Mathf.RoundToInt(mid + Mathf.Cos(a) * 2f), v = Mathf.RoundToInt(cv + Mathf.Sin(a) * 2f); if (Take(u, v)) Breakables.SkullPile(T(u, v)); }
                    for (int i = 0; i < 4; i++) Sigil(R(0.5f, lat - 1.5f), R(0.5f, dep - 1.5f));
                    for (int i = 0; i < 3; i++) Blood(R(1, lat - 2), R(1, dep - 2));
                    if (Take(mid, far)) Breakables.RedBrazier(T(mid, far));
                    Corners(Breakables.Candles, 0);
                    break;
                }
                case Kind.HeroTomb:                                                    // a fallen hero's tomb under candle stands, his treasure beside it
                {
                    if (Take(mid, far) && Take(mid, far - 1)) Prop("TombObject_Hero", (T(mid, far) + T(mid, far - 1)) / 2f);
                    Chest(mid + (rnd.Next(2) == 0 ? -2 : 2), far, "Chest4");
                    foreach (var u in new[] { L0, L1 }) if (Take(u, far)) Breakables.Candelabra(T(u, far));
                    foreach (var u in new[] { L0, L1 }) if (Take(u, 1)) Breakables.Candles(T(u, 1));
                    Floor("occ_bones_", 2, 4, 3);
                    break;
                }
                case Kind.Shrine:                                                      // a hooded statue over an altar of candles, the offering beside it
                    if (Take(mid, far)) Breakables.Statue(T(mid, far), FaceIn);
                    if (far >= 2 && Take(mid, far - 1)) Breakables.SkullPile(T(mid, far - 1));
                    Chest(mid + (rnd.Next(2) == 0 ? -2 : 2), far, "Chest2", "Chest3");
                    if (Take(L0, far)) Breakables.Candelabra(T(L0, far)); if (Take(L1, far)) Breakables.Candelabra(T(L1, far));
                    Corners(Breakables.Candles, 0);
                    Blood(mid, 1);
                    break;
                case Kind.Stash:                                                       // the cult's store: crates and barrels along the walls, a chest behind them
                    Chest(mid, far, "Chest2", "Chest1");
                    foreach (var u in new[] { L0, L1 }) for (int v = 1; v < dep; v++) if (rnd.Next(3) > 0 && Take(u, v)) Prop(rnd.Next(3) == 0 ? "GroundBag" : new[] { "Box", "Barrel", "BarrelAlt", "Box2", "Box3" }[rnd.Next(5)], T(u, v) + new Vector2(R(-.1f, .1f), R(-.1f, .1f)));
                    Corners(_ => { }, 0);
                    Webs(2);
                    break;
                case Kind.Cell:                                                        // a forgotten prisoner in a cage, his keeper's chest by the door
                    if (lat >= 4 && dep >= 4 && Take(mid, far) && Take(mid - 1, far) && Take(mid, far - 1) && Take(mid - 1, far - 1))
                        Breakables.Cage(new[] { "skel", "rot", "zomb" }[rnd.Next(3)], (T(mid, far) + T(mid - 1, far - 1)) / 2f);
                    Chest(rnd.Next(2) == 0 ? L0 : L1, far, "Chest2", "Chest1");
                    { int su = rnd.Next(2) == 0 ? L0 : L1; if (Take(su, 1)) Breakables.SkullPile(T(su, 1)); }
                    Blood(mid, 1); Blood(mid, far - 1);
                    Webs(3);
                    break;
                case Kind.Ossuary:                                                     // bones stacked along the walls round a slab
                    Chest(mid, far, "Chest3", "Chest2");
                    for (int v = 1; v < dep; v++) foreach (var u in new[] { L0, L1 }) if ((v % 2 == 1) && Take(u, v)) Breakables.SkullPile(T(u, v));
                    if (lat >= 5 && dep >= 5 && Take(mid, dep / 2) && Take(mid - 1, dep / 2) && Take(mid + 1, dep / 2)) Breakables.Sacrifice(T(mid, dep / 2));
                    Corners(Breakables.Candles, 1);
                    Blood(mid, 1);
                    break;
                default:                                                               // a scholar's cell: a bookcase, a lectern, a desk, books everywhere
                    if (lat >= 4 && Take(mid, far) && Take(mid - 1, far)) Breakables.Shelf((T(mid, far) + T(mid - 1, far)) / 2f, FaceIn);
                    Chest(rnd.Next(2) == 0 ? L0 : L1, far, "Chest2", "Chest3");
                    if (Take(mid, Mathf.Max(1, far - 2))) Breakables.Lectern(T(mid, Mathf.Max(1, far - 2)));
                    { int cu = rnd.Next(2) == 0 ? L0 : L1; if (Take(cu, 1)) Breakables.Candelabra(T(cu, 1)); }
                    Sigil(mid, 1);
                    break;
            }
        }

        static void Chest(int u, int v, params string[] names)
        {
            u = Mathf.Clamp(u, 0, lat - 1);
            if (!Take(u, v) && !Take(u = Mathf.Clamp(u + 1, 0, lat - 1), v)) return;
            // a secret room's chest is one of the game's green ones (the artifact chests: the best loot), on a dais
            bool best = What == Kind.Treasury || What == Kind.Reliquary || rnd.Next(3) == 0;
            Dais(u, v);
            var prefab = Prefabs.Cont(best ? "ChestArts2" : "ChestArts") ?? Prefabs.Cont("ChestArts") ?? Prefabs.Cont(names[rnd.Next(names.Length)]) ?? Prefabs.Cont("Chest1");
            if (prefab == null) return;
            Vector2 back = Side == 'n' ? Vector2.up : Side == 'w' ? Vector2.left : Vector2.right;
            Builder.Own(Builder.Spawn(prefab, T(u, v) + back * 0.2f, FaceIn));
        }

        static void Prop(string name, Vector2 p) { var pr = Prefabs.Cont(name); if (pr != null) Builder.Own(Builder.Spawn(pr, p, R(-8, 8))); }

        static void Corners(Action<Vector2> put, int skip)
        {
            var cs = new[] { (0, dep - 1), (lat - 1, dep - 1), (0, 0), (lat - 1, 0) };
            for (int i = 0; i < cs.Length; i++) if (i >= skip && Take(cs[i].Item1, cs[i].Item2)) put(T(cs[i].Item1, cs[i].Item2));
        }

        static void Webs(int n)
        {
            var web = Prefabs.Cont("SpiderWebObject");
            if (web == null) return;
            var cs = new[] { (0, dep - 1), (lat - 1, dep - 1), (0, 0), (lat - 1, 0) }.OrderBy(_ => rnd.Next()).Take(n);
            foreach (var c in cs) if (Take(c.Item1, c.Item2)) Builder.Own(Builder.Spawn(web, T(c.Item1, c.Item2), R(0, 360)));
        }

        static void Floor(string prefix, int a, int b, int kinds = 4)
        {
            for (int i = 0, n = rnd.Next(a, b + 1); i < n; i++)
            {
                var s = Builder.Sr(prefix + rnd.Next(kinds), T(R(0, lat - 1), R(0.5f, dep - 1)), Builder.floorLayer, Builder.floorOrder + 4, floorMat);
                s.transform.rotation = Quaternion.Euler(0f, 0f, rnd.Next(360));
            }
        }

        static void Sigil(float u, float v)
        {
            var s = Builder.Sr("occ_sigil_" + rnd.Next(3), T(u, v), Builder.floorLayer, Builder.floorOrder + 2, floorMat);
            s.transform.rotation = Quaternion.Euler(0f, 0f, rnd.Next(360));
        }

        static void Blood(float u, float v)
        {
            var s = Builder.Sr("occ_blood_" + rnd.Next(3), T(u + R(-.4f, .4f), v + R(-.4f, .4f)), Builder.floorLayer, Builder.floorOrder + 4, floorMat);
            s.transform.rotation = Quaternion.Euler(0f, 0f, rnd.Next(360));
        }
    }
}
