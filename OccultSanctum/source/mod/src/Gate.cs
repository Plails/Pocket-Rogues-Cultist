using System;
using UnityEngine;

namespace OccultSanctum
{
    /// An occult gate in a wall, drawn like the game's lair gates: a low stone bar between two lumpy posts with horned
    /// skulls, over a black hole in the wall. It works with the game's own UO_Move_to_Location, like the gates of the
    /// rat hole or the goblin pantry: the one on a crypt floor saves the floor and loads the sanctum, the one in the
    /// sanctum loads the floor back. Once used, the floor's gates are blocked with rubble.
    public class Gate : MonoBehaviour
    {
        public bool Inner;          // the one inside the sanctum (the way back)
        public bool Sealed;         // the floor's gates after the visit
        public string FloorKey;     // which floor these gates are on
        Vector2 usePos;
        UsableObject uo;
        UO_Move_to_Location move;
        bool buttonShown;

        // the picture is the game's goblin pantry gate (40 x 21 px) with white marble posts; the bar sits a little
        // back into the wall
        const float Inset = 0.05f;   // almost flush with the wall face: the gate stands out of the wall

        /// wallEdge = the middle of the wall's edge on the floor side; inner gates face north, outer ones south
        public static Gate Create(Vector2 wallEdge, bool inner, bool sealedUp, string floorKey, int layer, int order, Material lit, int wallLayer, Transform parent)
        {
            Vector2 face = inner ? Vector2.up : Vector2.down;
            Vector2 at = wallEdge - face * Inset;
            var go = new GameObject(inner ? "SanctumGate_Exit" : "SanctumGate");
            go.transform.SetParent(parent, false);
            go.transform.position = at;
            go.transform.rotation = Quaternion.Euler(0f, 0f, inner ? 180f : 0f);
            var g = go.AddComponent<Gate>();
            g.Inner = inner;
            g.Sealed = sealedUp;
            g.FloorKey = floorKey;
            g.usePos = wallEdge + face * 0.35f;
            Builder.Sr("occ_gate", at, layer, order + 10, lit, go.transform).transform.localRotation = Quaternion.identity;
            // used gates: the way between the posts is heaped with fallen stone
            if (sealedUp) Builder.Sr("occ_rubble", at + face * 0.1f, layer, order + 11, lit, go.transform).transform.localRotation = Quaternion.identity;
            foreach (float sx in new[] { -0.6f, 0.6f })        // the posts are solid, the way between them is open
            {
                var p = new GameObject("GatePost");
                p.transform.SetParent(go.transform, false);
                p.transform.localPosition = new Vector3(sx, 0f, 0f);
                p.layer = wallLayer;
                p.AddComponent<CircleCollider2D>().radius = 0.3f;
            }
            if (sealedUp) return g;
            // the game's own "go to another location" (before UsableObject: it looks for its IUO in Awake)
            g.move = go.AddComponent<UO_Move_to_Location>();
            g.move.targetSceneID = Flow.Scene;
            g.move._returnToDungeon = inner;
            g.move.removeAfterUse = false;
            g.move.titleLocalized = inner ? "Sanctum/Leave" : "Sanctum/Enter";
            g.uo = go.AddComponent<UsableObject>();
            g.uo.Icon = Art.Get("occ_icon");
            g.uo.disableButtonOnUse = false;
            return g;
        }

        void Update()
        {
            if (uo == null) return;
            var gm = GameManager.Instance;
            if (gm == null) return;
            var me = gm.Player_Mine();
            bool near = me != null && me.alive && Vector2.Distance(me.transform.position, usePos) < 0.9f;   // only right at the gate, like the goblin pantry
            if (near && !buttonShown) ShowButton(me);
            else if (!near && buttonShown) HideButton();
        }

        void ShowButton(Character me)
        {
            try
            {
                move.target = me;
                PlayerInputManager.Instance.SetUsableObject(uo, me.GetComponent<PlayerController>().playerID);
                buttonShown = true;
            }
            catch (Exception e) { SanctumPlugin.Log.LogWarning("Gate button: " + e.Message); }
        }

        void HideButton()
        {
            if (!buttonShown) return;
            buttonShown = false;
            try { if (PlayerInputManager.Instance != null && uo != null) PlayerInputManager.Instance.DisableUsableObjectButton(uo); } catch { }
        }

        void OnDestroy() { HideButton(); }

        /// the way back opens once the sanctum is cleared (for now there is nobody in it)
        public static bool Cleared()
        {
            var gm = GameManager.Instance;
            var me = gm?.Player_Mine();
            if (gm?.mobsGo == null || me == null) return true;
            foreach (var m in gm.mobsGo)
            {
                if (m == null || !m.activeInHierarchy) continue;
                var cr = m.GetComponent<Creature>();
                if (cr != null && cr.alive && cr.faction != me.faction && !Prisoner.Caged.Contains(cr)) return false;   // the ones still in cages do not count
            }
            return true;
        }
    }
}
