using System.Collections.Generic;
using Pathfinding;
using UnityEngine;

namespace OccultSanctum
{
    /// the sanctum's furniture (candles, candle stands, benches, tables, cages, statues...) marked as not walkable on
    /// the game's path grid, so the monsters walk round it instead of ramming through; a broken thing frees its spot
    public class PathBlock : MonoBehaviour
    {
        readonly List<(Container c, Bounds b)> things = new List<(Container, Bounds)>();
        float nextCheck, nextApply;

        public static void Install()
        {
            if (Builder.root == null) return;
            Builder.root.gameObject.AddComponent<PathBlock>();
        }

        void Start() { nextApply = Time.time + 1.5f; }

        void Collect()
        {
            things.Clear();
            Vector2 lo = Room.W(-1, -1), hi = Room.W(Room.L.W + 1, Room.L.H + 1);
            foreach (var c in FindObjectsOfType<Container>())
            {
                if (c == null || c.hp <= 0f || !c.gameObject.activeInHierarchy) continue;
                Vector2 p = c.transform.position;
                if (p.x < lo.x || p.y < lo.y || p.x > hi.x || p.y > hi.y) continue;
                if (c.name.StartsWith("Sanctum_occ_none") || c.name.StartsWith("Sanctum_occ_bedroll") || c.name.IndexOf("Destr", System.StringComparison.OrdinalIgnoreCase) >= 0 || c.name.IndexOf("Web", System.StringComparison.OrdinalIgnoreCase) >= 0) continue;
                Bounds b = new Bounds(p, Vector3.zero); bool any = false;
                foreach (var col in c.GetComponentsInChildren<Collider2D>(true))
                    if (col != null && !col.isTrigger && col.enabled) { if (!any) { b = col.bounds; any = true; } else b.Encapsulate(col.bounds); }
                if (!any) continue;
                b.size = new Vector3(Mathf.Max(b.size.x, 0.9f), Mathf.Max(b.size.y, 0.9f), 20f);
                b.center = new Vector3(b.center.x, b.center.y, 0f);
                things.Add((c, b));
            }
        }

        void Apply()
        {
            if (AstarPath.active == null) return;
            foreach (var (_, b) in things)
                AstarPath.active.UpdateGraphs(new GraphUpdateObject(b) { modifyWalkability = true, setWalkability = false, updatePhysics = false });
        }

        void Update()
        {
            try
            {
                if (Time.time >= nextApply)
                {
                    // once the floor is ready, and again now and then (the game may scan its grid anew)
                    nextApply = Time.time + 12f;
                    Collect(); Apply();
                    return;
                }
                if (Time.time < nextCheck) return;
                nextCheck = Time.time + 1f;
                for (int i = things.Count - 1; i >= 0; i--)
                {
                    var (c, b) = things[i];
                    if (c != null && c.hp > 0f && c.gameObject.activeInHierarchy) continue;
                    things.RemoveAt(i);
                    if (AstarPath.active != null) AstarPath.active.UpdateGraphs(b);                 // broken: its spot is free again
                }
            }
            catch (System.Exception e) { SanctumPlugin.Log.LogWarning("Sanctum path block: " + e.Message); nextApply = Time.time + 12f; }
        }
    }
}
