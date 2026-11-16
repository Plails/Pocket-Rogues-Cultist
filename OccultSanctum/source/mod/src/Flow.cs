using System;
using System.Collections;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace OccultSanctum
{
    /// The sanctum is a sub-location of its own, reached the way the game reaches its rat hole or goblin pantry:
    /// the gates on a Void Crypt floor save the floor and load another scene (the game's UO_Move_to_Location does it);
    /// the gates inside load the saved floor back. The scene is the game's one-room goblin pantry scene, whose own
    /// room, monsters and managers are replaced by the sanctum while the "going to the sanctum" flag is up.
    public static class Flow
    {
        public const string Scene = "locOneRoom_prisonGoblinKitchen";
        const string KGoing = "OccultSanctum_Going";
        const string KUsed = "OccultSanctum_Used";

        public static bool Going { get => PlayerPrefs.GetInt(KGoing, 0) == 1; set { PlayerPrefs.SetInt(KGoing, value ? 1 : 0); PlayerPrefs.Save(); } }
        /// (while the scene generates, the active scene can still be the loading screen: the location tells)
        public static bool InSanctum => Going && GameManager.Instance != null && GameManager.Instance.location == Locations.subLocMini_goblinKitchen;

        /// the floor whose gates were used (its gates are then blocked)
        public static string UsedFloor { get => PlayerPrefs.GetString(KUsed, ""); set { PlayerPrefs.SetString(KUsed, value ?? ""); PlayerPrefs.Save(); } }

        public static void OnSceneLoaded(Scene s)
        {
            if (s.name == Scene && Going)
            {
                // the pantry's own manager would bring goblins and a bonus chest: not in the sanctum
                foreach (var root in s.GetRootGameObjects())
                    if (root.name == "LOCATION_MANAGER") { root.SetActive(false); SanctumPlugin.Log.LogInfo("Sanctum: the pantry's location manager is off"); }
                return;
            }
            // any other scene: we are not (or no longer) on the way to the sanctum
            if (Going && s.name != "_loadingScreen" && s.name != "preLoad") Going = false;
        }
    }

    // ------------------------------------------------------------------ the map of the sanctum scene
    /// the sanctum's map for this visit: a shape picked at random (see Layout), with 9 tiles of rock round it
    public static class Room
    {
        public const int Margin = 9;
        public static readonly Vector2Int O = new Vector2Int(Margin, Margin);    // the layout's tile (0, 0)
        public static Layout L;
        public static Vector2 W(float rx, float ry) => new Vector2(O.x + rx, O.y + ry);
        public static bool In(Vector2 p) => L != null && L.Floor(Mathf.RoundToInt(p.x - O.x), Mathf.RoundToInt(p.y - O.y));
        public static bool Pending;

        public static void BuildMap(Generation gen)
        {
            var rnd = new System.Random();
            L = Layout.Pick(rnd);
            Secret.Plan(L, rnd);
            int sx = L.W + 2 * Margin, sy = L.H + 2 * Margin;
            var t = Traverse.Create(gen);
            gen.wideCorridors = false;
            gen.sizeX = sx;
            gen.sizeY = sy;
            t.Field("occupieds").SetValue(new bool[sx, sy]);
            var gm = GameManager.Instance;
            if (gm.WG == null) gm.WG = gm.GetComponent<WorldGrid>();
            gm.WG.InitGrid(sx, sy);
            ClustersManager.Instance.GenerateClusters(Vector2Int.zero, new Vector2Int(sx, sy));
            var floor = new Floor(sx, sy);
            floor.ClearMap();
            gen.floor = floor;
            (t.Field("rooms").GetValue() as IList)?.Clear();
            (t.Field("secretRoomsArray").GetValue() as IList)?.Clear();
            (t.Field("lockedRoomsArray").GetValue() as IList)?.Clear();
            for (int x = 0; x < L.W; x++) for (int y = 0; y < L.H; y++) if (L.Floor(x, y)) floor.SetTile(O.x + x, O.y + y, c.TileTypes.floor);
            Secret.Mark(floor, O);                                                    // the secret room: hidden tiles behind a breakable wall tile
            var rooms = t.Field("rooms").GetValue<List<Generation.Room>>();
            var e = L.Entry;
            rooms.Add(new Generation.Room(O.x + e.x, e.width, O.y + e.y, e.height));     // the hero starts here
            t.Field("Graph").SetValue(new bool[1, 1]);
            t.Field("manualRoomsGeneration").SetValue(false);
            ClustersManager.Instance.NormalizeClusters(floor);
            Pending = true;
            SanctumPlugin.Log.LogInfo($"Sanctum map built: {L.Name} ({sx}x{sy})");
        }
    }

    [HarmonyPatch(typeof(Generation), nameof(Generation.TryGenerateWalls))]
    internal static class MapPatch
    {
        [HarmonyPriority(Priority.High)]
        static bool Prefix(Generation __instance, ref bool __result)
        {
            if (!Flow.InSanctum) return true;
            try { Room.BuildMap(__instance); __result = true; return false; }
            catch (Exception e) { SanctumPlugin.Log.LogError("Sanctum map failed, the pantry instead: " + e); Flow.Going = false; return true; }
        }
    }

    [HarmonyPatch(typeof(Generation), nameof(Generation.TryBuildOtherForRooms))]
    internal static class ContentPatch
    {
        [HarmonyPriority(Priority.High)]
        static bool Prefix(Generation __instance, ref bool __result)
        {
            if (!Room.Pending) return true;
            Room.Pending = false;
            try { SanctumBuilder.Build(__instance); }
            catch (Exception e) { SanctumPlugin.Log.LogError("Sanctum content: " + e); }
            try { GameManager.Instance.Recalculate_AI_Range(); } catch { }
            __result = true;
            return false;
        }
    }

    [HarmonyPatch(typeof(GameManager), nameof(GameManager.StartInv1))]
    internal static class TitlePatch
    {
        static void Prefix(GameManager __instance)
        {
            try { if (Flow.InSanctum) __instance.customTitleIDLocalized = "Sanctum/Title"; } catch { }
        }
    }

    /// our gates: the way in raises the flag (and blocks this floor's gates for later), the way back opens only
    /// once the sanctum is cleared
    [HarmonyPatch(typeof(UO_Move_to_Location), nameof(UO_Move_to_Location.OnUse))]
    internal static class GatePatch
    {
        [HarmonyPriority(Priority.High)]
        static bool Prefix(UO_Move_to_Location __instance)
        {
            try
            {
                var g = __instance.GetComponent<Gate>();
                if (g == null) return true;
                if (!g.Inner)
                {
                    if (g.Sealed) return false;
                    Flow.Going = true;
                    Flow.UsedFloor = g.FloorKey;
                    SanctumPlugin.Log.LogInfo("Into the sanctum from " + g.FloorKey);
                    return true;
                }
                if (!Gate.Cleared())
                {
                    try { PopupTextManager.Instance.CreatePopupText_WorldPosition(__instance.transform.position, Texts.Get("Sanctum/NotCleared"), Color.red); } catch { }
                    return false;
                }
                Flow.Going = false;
                SanctumPlugin.Log.LogInfo("Out of the sanctum, back to " + PlayerPrefs.GetString("prevLocation"));
                return true;
            }
            catch (Exception e) { SanctumPlugin.Log.LogError("Sanctum gate: " + e); return true; }
        }
    }
}
