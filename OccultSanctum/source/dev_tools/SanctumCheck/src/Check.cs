using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BepInEx;
using HarmonyLib;
using OccultSanctum;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SanctumCheck
{
    /// dev-only, save SANDBOX: Void Crypt floor 1 -> the gates are in a wall; the hero uses them (the floor is saved,
    /// the sanctum scene loads); in the sanctum: the room, its things, the title; some things are broken; the hero
    /// uses the way back (the floor comes back from its save, the hero at the gates, the gates blocked). Logs + shots.
    [BepInPlugin("custom.dev.sanctumcheck", "SanctumCheck", "2.0.0")]
    [BepInDependency("custom.pocketrogues.occultsanctum")]
    public class CheckPlugin : BaseUnityPlugin
    {
        static string Out;
        readonly HashSet<string> done = new HashSet<string>();
        string status = "";
        int shot;

        void Awake()
        {
            Out = Path.Combine(Path.GetDirectoryName(Info.Location), "out");
            Directory.CreateDirectory(Out);
            foreach (var f in Directory.GetFiles(Out)) File.Delete(f);
            var h = new Harmony("custom.dev.sanctumcheck");
            Sandbox.Install(h);
            h.PatchAll(typeof(CheckPlugin).Assembly);
            bool ok = Sandbox.Verify(out var why);
            Log("Sandbox verify: " + ok + " (" + why + ")");
            status = ok ? "ТЕСТ-ПЕСОЧНИЦА: прогресс НЕ сохраняется (dev-плагин SanctumCheck)" : "SanctumCheck: песочница не подтверждена — тест не запущен";
            if (ok) SceneManager.sceneLoaded += (s, m) => StartCoroutine(Later(s.name));
        }

        void OnGUI()
        {
            var st = new GUIStyle(GUI.skin.label) { fontSize = 18 };
            st.normal.textColor = Color.red;
            GUI.Label(new Rect(10, 6, 1400, 30), status, st);
        }

        static void Log(string s) => File.AppendAllText(Path.Combine(Out, "log.txt"), DateTime.Now.ToString("HH:mm:ss ") + s + "\n");
        void Shot(string n) { shot++; ScreenCapture.CaptureScreenshot(Path.Combine(Out, shot.ToString("00") + "_" + n + ".png")); }

        void StartRun(int deep)
        {
            int ch = FileBasedPrefs.GetInt("curChar");
            PlayerPrefs.SetInt("Endless", 1);
            PlayerPrefs.SetInt("curDeep", deep);
            FileBasedPrefs.SetInt("ObsidianTowerMode", 0);
            FileBasedPrefs.SetInt("NeedLoading", 0);
            FileBasedPrefs.SetInt("NeedLoadingAutosave", 0);
            FileBasedPrefs.SetString("NeedLoading_ReturnToTheFortress_" + ch, "");
            FileBasedPrefs.SetString("loc", "floor_crypt");
            FileBasedPrefs.SetString("locationStringBckp", "floor_crypt");
            Fading.LoadScene("floor_crypt", destroyCamera: false, useLoadingScreen: true, useRandomEvents: false);
        }

        static void Teleport(Character me, Vector2 p)
        {
            var rb = me.GetComponent<Rigidbody2D>();
            if (rb != null) { rb.velocity = Vector2.zero; rb.position = p; }
            me.transform.position = p;
            try { CameraController.Instance.HardUpdatePosition(); } catch { }
        }

        static IEnumerator Ready()
        {
            float t0 = Time.realtimeSinceStartup;
            while (Time.realtimeSinceStartup - t0 < 60f && (GenerationManager.Instance == null || !GenerationManager.Instance.WasGenerated || GameManager.Instance?.Player_Mine() == null))
                yield return new WaitForSecondsRealtime(0.5f);
            yield return new WaitForSecondsRealtime(2.5f);
        }

        static void Use(Gate g, Character me)
        {
            var mv = g.GetComponent<UO_Move_to_Location>();
            mv.target = me;
            g.GetComponent<UsableObject>().OnUse();
        }

        IEnumerator Later(string scene)
        {
            Log($"scene {scene} curDeep={PlayerPrefs.GetInt("curDeep", 1)} going={Flow.Going} prevLocation={PlayerPrefs.GetString("prevLocation")} NeedLoading={FileBasedPrefs.GetInt("NeedLoading")}");
            yield return new WaitForSecondsRealtime(3f);
            if ((scene == "mainMenu" || scene == "Base") && done.Add("start")) { yield return new WaitForSecondsRealtime(1f); StartRun(1); yield break; }

            if (scene == "floor_crypt")
            {
                yield return Ready();
                var me = GameManager.Instance.Player_Mine();
                foreach (var m in GameManager.Instance.mobsGo.ToList()) { var ai = m != null ? m.GetComponent<AIController>() : null; if (ai != null) ai._enableAI = false; }
                var gate = FindObjectsOfType<Gate>().FirstOrDefault(g => !g.Inner);
                Log($"crypt: hero at {(Vector2)me.transform.position}, gate {(gate != null ? gate.transform.position.ToString() + " sealed " + gate.Sealed + " key " + gate.FloorKey : "NONE")}");
                if (gate == null) { Shot("crypt_no_gate"); status = "SanctumCheck: врат нет. Игру можно закрыть."; Log("CHECK DONE"); yield break; }
                if (done.Add("crypt1"))
                {
                    Teleport(me, (Vector2)gate.transform.position + Vector2.down * 1.2f);
                    yield return new WaitForSecondsRealtime(1.5f);
                    Shot("crypt_gate");
                    Use(gate, me);
                    Log("used the gates");
                    yield break;
                }
                // back from the sanctum
                Log($"back on the floor: hero {Vector2.Distance(me.transform.position, gate.transform.position):0.00} from the gates, sealed {gate.Sealed}, used floor '{Flow.UsedFloor}'");
                yield return new WaitForSecondsRealtime(1f);
                Shot("crypt_back");
                status = "SanctumCheck: готово. Песочница: прогресс не сохраняется. Игру можно закрыть.";
                Log("CHECK DONE. sandbox writes=" + Sandbox.Writes);
                yield break;
            }

            if (scene == Flow.Scene && done.Add("sanctum"))
            {
                yield return Ready();
                var gm = GameManager.Instance; var gen = GenerationManager.Instance.GEN; var me = gm.Player_Mine();
                var exit = FindObjectsOfType<Gate>().FirstOrDefault(g => g.Inner);
                var conts = FindObjectsOfType<Container>().ToList();
                Log($"sanctum: location {gm.location} type {gm.locationType} title '{gm.customTitleIDLocalized.mTerm}' map {gen.sizeX}x{gen.sizeY} hero {(Vector2)me.transform.position} in room {OccultSanctum.Room.In(me.transform.position)} mobs {gm.mobsGo.Count} exit {(exit != null ? exit.transform.position.ToString() : "NONE")} stairsOnFloor {gen.stairsOnThisFloor?.name} manager on {GameObject.Find("LOCATION_MANAGER") != null}");
                Log("containers: " + string.Join(", ", conts.GroupBy(c => c.name).Select(gr => gr.Key + "x" + gr.Count())));
                Shot("sanctum_enter");
                Teleport(me, OccultSanctum.Room.W(7f, 5.5f));
                yield return new WaitForSecondsRealtime(1.5f);
                Shot("sanctum_middle");
                Teleport(me, OccultSanctum.Room.W(7f, 10.6f));
                yield return new WaitForSecondsRealtime(1.5f);
                Shot("sanctum_altar");
                // break things: a table, a column, some candles, the altar
                foreach (var n in new[] { "Sanctum_occ_table_0", "Sanctum_occ_column", "Sanctum_occ_candles", "Sanctum_occ_altar" })
                {
                    var c = conts.FirstOrDefault(x => x != null && x.name == n) ?? conts.FirstOrDefault(x => x != null && x.transform.root.name == n);
                    if (c == null) { Log("no " + n); continue; }
                    float hp0 = c.hp; Vector2 at = c.transform.position;
                    for (int i = 0; i < 12 && c != null && c.hp > 0; i++) { try { c.SetDamage(1.5f); } catch (Exception e) { Log("dmg " + n + ": " + e.Message); break; } yield return new WaitForSecondsRealtime(0.15f); }
                    yield return new WaitForSecondsRealtime(0.6f);
                    Log($"broke {n}: hp {hp0} -> {(c != null ? c.hp.ToString() : "gone")}, object {(c != null && c.gameObject.activeInHierarchy ? "still there" : "gone")}, debris near {FindObjectsOfType<SpriteRenderer>().Count(s => s.sprite != null && s.sprite.name.Contains("_destr_") && Vector2.Distance(s.transform.position, at) < 2.5f)}");
                }
                Teleport(me, OccultSanctum.Room.W(7f, 9f));
                yield return new WaitForSecondsRealtime(1.2f);
                Shot("sanctum_broken");
                Teleport(me, (Vector2)exit.transform.position + Vector2.up * 1.2f);
                yield return new WaitForSecondsRealtime(1.2f);
                Shot("sanctum_exit");
                Use(exit, me);
                Log("used the way back");
            }
        }
    }

    /// the hero takes no damage during the test
    [HarmonyPatch(typeof(Creature), nameof(Creature.SetDamageRPC))]
    static class GodMode
    {
        [HarmonyPriority(Priority.Last)]
        static bool Prefix(Creature __instance)
        {
            var me = GameManager.Instance != null ? GameManager.Instance.Player_Mine() : null;
            return !(me != null && __instance != null && __instance.gameObject == me.gameObject);
        }
    }
}
