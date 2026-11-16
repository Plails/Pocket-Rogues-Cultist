using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Text;
using BepInEx;
using HarmonyLib;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SanctumProbe
{
    /// dev-only, save SANDBOX: dumps how the game's breakable props are built (hierarchy, sprites, colliders, Container
    /// fields) and what the one-room sub-location scenes contain (location, generator, managers, gates), by loading
    /// them straight from the main menu.
    [BepInPlugin("custom.dev.sanctumprobe", "SanctumProbe", "1.0.0")]
    public class ProbePlugin : BaseUnityPlugin
    {
        static string Out;
        string status = "";
        int step;
        static readonly string[] Scenes = { "locOneRoom_prisonGoblinKitchen", "locOneRoom_altarLair", "locOneRoom_catacombsRatsLair" };

        void Awake()
        {
            Out = Path.Combine(Path.GetDirectoryName(Info.Location), "out");
            Directory.CreateDirectory(Out);
            foreach (var f in Directory.GetFiles(Out)) File.Delete(f);
            var h = new Harmony("custom.dev.sanctumprobe");
            SanctumCheck.Sandbox.Install(h);
            bool ok = SanctumCheck.Sandbox.Verify(out var why);
            Log("Sandbox verify: " + ok + " (" + why + ")");
            status = ok ? "ТЕСТ-ПЕСОЧНИЦА: прогресс НЕ сохраняется (dev-плагин SanctumProbe)" : "SanctumProbe: песочница не подтверждена";
            if (ok) SceneManager.sceneLoaded += (s, m) => StartCoroutine(Later(s.name));
        }

        void OnGUI()
        {
            var st = new GUIStyle(GUI.skin.label) { fontSize = 18 };
            st.normal.textColor = Color.red;
            GUI.Label(new Rect(10, 6, 1400, 30), status, st);
        }

        static void Log(string s) => File.AppendAllText(Path.Combine(Out, "log.txt"), DateTime.Now.ToString("HH:mm:ss ") + s + "\n");
        static void Dump(string file, string s) => File.WriteAllText(Path.Combine(Out, file), s);

        static string Tree(Transform t, int depth = 0)
        {
            var sb = new StringBuilder();
            sb.Append(new string(' ', depth * 2)).Append(t.name).Append(t.gameObject.activeSelf ? "" : " (off)").Append(" pos ").Append(t.localPosition).Append(" rot ").Append(t.localEulerAngles.z.ToString("0")).Append(" scale ").Append(t.localScale).Append(" layer ").Append(LayerMask.LayerToName(t.gameObject.layer)).Append(" :");
            foreach (var c in t.GetComponents<Component>())
            {
                if (c == null) continue;
                sb.Append(" ").Append(c.GetType().Name);
                if (c is SpriteRenderer sr) sb.Append($"[{(sr.sprite != null ? sr.sprite.name + " " + sr.sprite.rect.width + "x" + sr.sprite.rect.height + " ppu" + sr.sprite.pixelsPerUnit + " piv" + sr.sprite.pivot : "-")} {SortingLayer.IDToName(sr.sortingLayerID)}/{sr.sortingOrder} {(sr.sharedMaterial != null ? sr.sharedMaterial.name : "-")}]");
                if (c is BoxCollider2D bc) sb.Append($"[{bc.size} off {bc.offset} trig {bc.isTrigger}]");
                if (c is CircleCollider2D cc) sb.Append($"[r {cc.radius} off {cc.offset} trig {cc.isTrigger}]");
                if (c is PolygonCollider2D pc) sb.Append($"[poly {pc.points.Length} trig {pc.isTrigger}]");
                if (c is UO_Move_to_Location mv) sb.Append($"[target {mv.targetSceneID} return {mv._returnToDungeon} saveAfter {mv.savingPath_afterUse} remove {mv.removeAfterUse} title {mv.titleLocalized.mTerm}]");
            }
            sb.Append("\n");
            foreach (Transform ch in t) sb.Append(Tree(ch, depth + 1));
            return sb.ToString();
        }

        static string Fields(object o)
        {
            if (o == null) return "null";
            var sb = new StringBuilder();
            foreach (var f in AccessTools.GetDeclaredFields(o.GetType()).Concat(o.GetType().BaseType != null && o.GetType().BaseType != typeof(MonoBehaviour) ? AccessTools.GetDeclaredFields(o.GetType().BaseType) : Enumerable.Empty<System.Reflection.FieldInfo>()))
            {
                if (f.IsStatic) continue;
                object v; try { v = f.GetValue(o); } catch { continue; }
                string s;
                if (v == null) s = "null";
                else if (v is UnityEngine.Object uo) s = uo != null ? uo.name + " <" + uo.GetType().Name + ">" : "null";
                else if (v is System.Collections.IEnumerable en && !(v is string)) s = "[" + string.Join(", ", en.Cast<object>().Take(12).Select(x => x is UnityEngine.Object u2 ? (u2 != null ? u2.name : "null") : x?.ToString())) + "]";
                else s = v.ToString();
                sb.Append("  ").Append(f.Name).Append(" = ").Append(s).Append("\n");
            }
            return sb.ToString();
        }

        IEnumerator Later(string scene)
        {
            Log("scene " + scene);
            yield return new WaitForSecondsRealtime(3f);
            if (scene == "mainMenu" && step == 0)
            {
                step = 1;
                // every breakable prop of the game and the ones we would dress the sanctum with
                var conts = Resources.LoadAll<Container>("");
                Log("containers: " + conts.Length);
                Dump("containers_all.txt", string.Join("\n", conts.Select(c => $"{c.name} hp {c.hp} type {c.containerType} items {c.itemsList?.Count} destrSprites {c.DestroyObject_Sprites?.Length} dmgSprites {c.damageSprites?.Count} coins {c.minCoins}-{c.maxCoins} trash {c.maxTrashDrop} other {c.maxOtherDrop} sr {c.GetComponentInChildren<SpriteRenderer>(true)?.sprite?.name}")));
                var sb = new StringBuilder();
                foreach (var n in new[] { "Box", "Barrel", "BarrelAlt", "Box2", "Brazier", "TableCircle", "Table", "table1", "TombStatue", "CryptBowl", "SkullsPile", "Column_Destroyable", "KitchenCabinet", "Cauldron" })
                    foreach (var c in conts.Where(x => x.name.StartsWith(n, StringComparison.OrdinalIgnoreCase)).Take(3))
                        sb.Append("==== ").Append(c.name).Append("\n").Append(Tree(c.transform)).Append(Fields(c)).Append("\n");
                Dump("containers_detail.txt", sb.ToString());
                LoadNext();
                yield break;
            }
            if (Scenes.Contains(scene))
            {
                float t0 = Time.realtimeSinceStartup;
                while (Time.realtimeSinceStartup - t0 < 40f && (GenerationManager.Instance == null || !GenerationManager.Instance.WasGenerated || GameManager.Instance?.Player_Mine() == null))
                    yield return new WaitForSecondsRealtime(0.5f);
                yield return new WaitForSecondsRealtime(2f);
                var gm = GameManager.Instance; var gmg = GenerationManager.Instance; var gen = gmg?.GEN;
                Log($"{scene}: location {gm?.location} type {gm?.locationType} mapType {gmg?.mapType} gen {gen?.GetType().Name} size {gen?.sizeX}x{gen?.sizeY} rooms {Traverse.Create(gen).Field("rooms").GetValue<IList>()?.Count} stairs prefab {gen?.stairs?.name} stairsOnFloor {gen?.stairsOnThisFloor?.name} hero {(Vector2)gm.Player_Mine().transform.position} mobs {gm.mobsGo?.Count} title {gm.customTitleIDLocalized}");
                var sb = new StringBuilder();
                foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects()) if (!root.name.StartsWith("Canvas") && !root.name.Contains("UI")) sb.Append(Tree(root.transform));
                Dump(scene + "_tree.txt", sb.ToString());
                Dump(scene + "_gm.txt", "GenerationManager\n" + Fields(gmg) + "\nGeneration\n" + Fields(gen) + "\nLocationManagers\n" + string.Join("\n", FindObjectsOfType<LocationManager>().Select(l => l.GetType().Name + "\n" + Fields(l))));
                if (gen?.stairs != null) Dump(scene + "_stairs.txt", Tree(gen.stairs.transform));
                ScreenCapture.CaptureScreenshot(Path.Combine(Out, scene + ".png"));
                yield return new WaitForSecondsRealtime(1f);
                LoadNext();
            }
        }

        void LoadNext()
        {
            int i = step - 1;
            if (i >= Scenes.Length) { status = "SanctumProbe: готово. Игру можно закрыть."; Log("PROBE DONE"); return; }
            step++;
            PlayerPrefs.SetInt("curDeep", 1);
            FileBasedPrefs.SetInt("NeedLoading", 0);
            FileBasedPrefs.SetInt("NeedLoadingAutosave", 0);
            Fading.LoadScene(Scenes[i], destroyCamera: false, useLoadingScreen: true, useRandomEvents: false);
        }
    }
}
