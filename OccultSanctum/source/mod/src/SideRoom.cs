using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace OccultSanctum
{
    /// The small rooms of a sanctum complex, each with its own use and its own people busy at something until they
    /// notice the hero: a library (a cultist reading at the lectern), a shooting range (a crossbowman putting bolts
    /// into a target), a barracks (a spearman on guard), a shrine (a cultist praying with spread arms before a
    /// statue), a cell block (a spearman walking his round past the cages), a store, a scholar's study.
    public static partial class SideRoom
    {
        static RectInt r; static char door; static System.Random rnd; static int lat, dep;
        static readonly HashSet<Vector2Int> used = new HashSet<Vector2Int>();

        /// a tile by its column across the room (u) and its depth from the door's wall (v = 0 by the door)
        static Vector2Int Tl(int u, int v)
        {
            switch (door)
            {
                case 's': return new Vector2Int(r.xMin + u, r.yMin + v);
                case 'n': return new Vector2Int(r.xMax - 1 - u, r.yMax - 1 - v);
                case 'w': return new Vector2Int(r.xMin + v, r.yMax - 1 - u);
                default: return new Vector2Int(r.xMax - 1 - v, r.yMin + u);
            }
        }
        static Vector2 T(float u, float v)
        {
            var a = Tl(0, 0); var du = (Vector2)(Tl(1, 0) - a); var dv = (Vector2)(Tl(0, 1) - a);
            var p = (Vector2)a + du * u + dv * v;
            return Room.W(p.x, p.y);
        }
        /// facing the door (its back to the far wall)
        static float FaceDoor => door == 's' ? 0f : door == 'n' ? 180f : door == 'w' ? -90f : 90f;
        static float R(float a, float b) => a + (float)rnd.NextDouble() * (b - a);

        static int doorU; static Layout Lay;
        static bool Take(int u, int v)
        {
            if (u < 0 || v < 0 || u >= lat || v >= dep) return false;
            if (clear.Contains(new Vector2Int(u, v)) && !onCarpet) return false;       // the carpet and the walks stay clear
            if (v <= 1 && Mathf.Abs(u - doorU) <= 1) return false;                 // the way in stays clear
            var t = Tl(u, v); if (!Lay.Floor(t.x, t.y)) return false;               // (a pillar stands there)
            var p = new Vector2Int(u, v);
            if (used.Contains(p) || !Open(p)) return false;                         // nothing may shut off a part of the room
            return used.Add(p);
        }
        static bool TakeAll(params (int u, int v)[] ts)
        {
            if (!onCarpet && ts.Any(t => clear.Contains(new Vector2Int(t.u, t.v)))) return false;
            if (ts.Any(t => t.u < 0 || t.v < 0 || t.u >= lat || t.v >= dep || used.Contains(new Vector2Int(t.u, t.v)) || !Lay.Floor(Tl(t.u, t.v).x, Tl(t.u, t.v).y) || (t.v <= 1 && Mathf.Abs(t.u - doorU) <= 1))) return false;
            if (!Open(ts.Select(t => new Vector2Int(t.u, t.v)).ToArray())) return false;
            foreach (var t in ts) used.Add(new Vector2Int(t.u, t.v));
            return true;
        }

        /// with these tiles taken too, can every free floor tile of the room still be reached from the door?
        static bool Open(params Vector2Int[] add)
        {
            var shut = new HashSet<Vector2Int>(used); foreach (var a in add) shut.Add(a);
            var start = new Vector2Int(doorU, 0);
            if (shut.Contains(start)) return false;
            int free = 0;
            for (int u = 0; u < lat; u++) for (int v = 0; v < dep; v++) { var t = Tl(u, v); if (Lay.Floor(t.x, t.y) && !shut.Contains(new Vector2Int(u, v))) free++; }
            var seen = new HashSet<Vector2Int> { start }; var q = new Queue<Vector2Int>(); q.Enqueue(start);
            while (q.Count > 0)
            {
                var c = q.Dequeue();
                foreach (var d in new[] { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right })
                {
                    var n = c + d;
                    if (n.x < 0 || n.y < 0 || n.x >= lat || n.y >= dep || shut.Contains(n) || seen.Contains(n)) continue;
                    var t = Tl(n.x, n.y); if (!Lay.Floor(t.x, t.y)) continue;
                    seen.Add(n); q.Enqueue(n);
                }
            }
            return seen.Count >= free;
        }

        /// every side room furnished on this floor, with what it is
        public static readonly List<(RectInt r, string role)> Rooms = new List<(RectInt, string)>();

        /// the rooms that need space (a drill, a range, long tables, rows of cages...): at least 5 x 5 tiles
        static readonly string[] Roomy = { "range", "training", "guardpost", "armory", "barracks", "refectory", "council", "execution", "prison", "scriptorium", "gallery", "warehouse", "summoning", "research" };
        /// what a small room can be and still look furnished
        public static readonly string[] Tiny = { "store", "pantry", "shrine", "study", "cell", "alchemy", "tomb" };
        public static bool Fits(string role, RectInt r) => !Roomy.Contains(role) || Mathf.Min(r.width, r.height) >= 5 && r.width * r.height >= 30;

        public static void Furnish(RectInt room, string role, Layout L, System.Random rand)
        {
            Rooms.Add((room, role));
            r = room; rnd = rand; used.Clear(); Lay = L;
            // where its door is: the side with floor beyond it
            door = 's'; int doorAt = 0;
            for (int x = r.xMin; x < r.xMax; x++) { if (L.Floor(x, r.yMin - 1)) { door = 's'; doorAt = x; break; } if (L.Floor(x, r.yMax)) { door = 'n'; doorAt = x; break; } }
            for (int y = r.yMin; y < r.yMax; y++) { if (L.Floor(r.xMin - 1, y)) { door = 'w'; doorAt = y; break; } if (L.Floor(r.xMax, y)) { door = 'e'; doorAt = y; break; } }
            lat = door == 's' || door == 'n' ? r.width : r.height; dep = door == 's' || door == 'n' ? r.height : r.width;
            doorU = 0;
            for (int u = 0; u < lat; u++) { var t = Tl(u, 0); if ((door == 's' || door == 'n') ? t.x == doorAt : t.y == doorAt) { doorU = u; break; } }
            int far = dep - 1, mid = lat / 2;
            SanctumPlugin.Log.LogInfo($"Sanctum: side room {r} ({role}), door {door}");
            // (the user, 2026-11-22: the rooms looked strange - a rug lay under the furniture, two rugs on top of each
            // other, jugs and sacks and rubble strewn about the middle, crates askew, half-empty rooms topped up with
            // odd things) these rooms are laid out anew in SideRoomPlans.cs: one thing that matters at the far wall, a
            // carpet or a clear walk from the door to it, the two halves of the room alike
            clear.Clear();
            if (Planned(role, far, mid)) { Fill(role, far); return; }
            switch (role)
            {
                case "library":
                {
                    for (int u = 0; u + 1 < lat; u += 2) if (TakeAll((u, far), (u + 1, far))) Breakables.Shelf((T(u, far) + T(u + 1, far)) / 2f, FaceDoor);
                    for (int v = 2; v + 1 < far - 1; v += 3) { if (TakeAll((0, v), (0, v + 1))) Breakables.Shelf((T(0, v) + T(0, v + 1)) / 2f, FaceDoor - 90f); if (lat >= 5 && TakeAll((lat - 1, v), (lat - 1, v + 1))) Breakables.Shelf((T(lat - 1, v) + T(lat - 1, v + 1)) / 2f, FaceDoor + 90f); }
                    Rug();
                    Vector2 lec = default; bool hasLec = false;
                    if (dep >= 4 && Take(mid, far - 2)) { lec = T(mid, far - 2); Breakables.Lectern(lec); hasLec = true; }
                    Corners(p => Breakables.Candles(p));
                    Books(3, 6);
                    if (hasLec && Take(mid, Mathf.Max(0, far - 3))) Activity.Give(Cultist.Spawn(T(mid, Mathf.Max(0, far - 3))), Activity.Kind.Read, lec);
                    if (lat >= 5 && Take(1, far - 1)) Activity.Give(Cultist.Spawn(T(1, far - 1)), Activity.Kind.Read, T(1, far));
                    break;
                }
                case "range":
                {
                    Vector2 tgt = default; bool has = false;
                    if (TakeAll((mid, far), (mid - 1, far))) { tgt = T(mid - 0.5f, far); Breakables.Target(tgt, FaceDoor); has = true; }
                    Vector2 tgt2 = default; bool has2 = false;
                    if (lat >= 7 && TakeAll((lat - 2, far), (lat - 3, far))) { tgt2 = T(lat - 2.5f, far); Breakables.Target(tgt2, FaceDoor); has2 = true; }
                    float sideRot = door == 's' ? 90f : door == 'n' ? -90f : door == 'w' ? 0f : 180f;
                    if (dep >= 6 && TakeAll((0, 2), (0, 3))) Breakables.RackX((T(0, 2) + T(0, 3)) / 2f, sideRot);
                    if (dep >= 6 && lat >= 5 && TakeAll((lat - 1, 2), (lat - 1, 3))) Breakables.Bench((T(lat - 1, 2) + T(lat - 1, 3)) / 2f, FaceDoor + 90f);
                    for (int v = 1; v < dep - 1; v++) foreach (int u in new[] { 0, lat - 1 }) if (rnd.Next(2) == 0 && Take(u, v)) Prop(rnd.Next(3) == 0 ? "Barrel" : rnd.Next(2) == 0 ? "Box" : "Box2", T(u, v));
                    if (has2 && Take(lat - 3, 1)) Activity.Give(Crossbowman.Spawn(T(lat - 2.5f, 1)), Activity.Kind.Shoot, tgt2);
                    if (has && Take(mid, 1)) Activity.Give(Crossbowman.Spawn(T(mid, 1)), Activity.Kind.Shoot, tgt);
                    Blood(mid, far - 1);
                    break;
                }
                case "barracks":
                {
                    int sat = 0;
                    for (int v = 2; v < dep; v += 2) foreach (int u in new[] { 0, lat - 2 }) if (TakeAll((u, v), (u + 1, v)))
                    {
                        var b = (T(u, v) + T(u + 1, v)) / 2f;
                        Breakables.Bench(b, rnd.Next(2) == 0 ? 0f : 180f);
                        if (sat < 2 && rnd.Next(2) == 0) { sat++; Activity.Give(rnd.Next(2) == 0 ? Spearman.Spawn(b) : Crossbowman.Spawn(b), Activity.Kind.Sit, T(mid, dep / 2f)); }
                    }
                    if (lat >= 5 && TakeAll((mid, far), (mid - 1, far))) Breakables.Table(rnd.Next(2), (T(mid, far) + T(mid - 1, far)) / 2f, FaceDoor);
                    // (no beds: the user had the rooms with beds taken out)
                    if (Take(0, far)) Furniture(rnd.Next(2) == 0 ? "Dresser" : "Cupboard", T(0, far));
                    if (Take(lat - 1, far)) Breakables.RackX(T(lat - 1, far), FaceDoor);
                    for (int i = 0; i < 2; i++) { int u = rnd.Next(lat), v = rnd.Next(1, dep); if (Take(u, v)) Prop("GroundBag", T(u, v)); }
                    if (Take(mid, 2)) Activity.Give(Spearman.Spawn(T(mid, 2)), Activity.Kind.Guard, T(doorU, -2));
                    if (rnd.Next(2) == 0 && Take(lat - 1, far)) Activity.Give(Crossbowman.Spawn(T(lat - 1, far)), Activity.Kind.Guard, T(doorU, 0));
                    break;
                }
                case "shrine":
                {
                    Vector2 st = T(mid, far);
                    if (Take(mid, far)) Breakables.Statue(st, FaceDoor);
                    if (Take(mid - 1, far)) Breakables.Candles(T(mid - 1, far)); if (Take(mid + 1, far)) Breakables.Candles(T(mid + 1, far));
                    foreach (int u in new[] { 0, lat - 1 }) if (Take(u, far)) Breakables.Candelabra(T(u, far));
                    var s = Builder.Sr("occ_sigil_" + rnd.Next(3), T(mid, Mathf.Max(1, far - 2)), Builder.floorLayer, Builder.floorOrder + 2, Builder.floorMat ?? Builder.litMat);
                    s.transform.rotation = Quaternion.Euler(0, 0, rnd.Next(360));
                    if (Take(mid, Mathf.Max(1, far - 2))) Activity.Give(Cultist.Spawn(T(mid, Mathf.Max(1, far - 2))), Activity.Kind.Pray, st);
                    Blood(mid, far - 1);
                    break;
                }
                case "cell":
                {
                    string[] kinds = { "skel", "rot", "zomb" };
                    Container empty = null; Vector2 emptyFront = default;
                    foreach (int u in new[] { 0, lat - 2 }) if (lat >= 5 && TakeAll((u, far), (u + 1, far), (u, far - 1), (u + 1, far - 1)))
                    {
                        bool leave = empty == null && u > 0;                                   // the second cage stands open for the led skeleton
                        var c = Breakables.Cage(leave ? null : kinds[rnd.Next(3)], (T(u, far) + T(u + 1, far - 1)) / 2f);
                        if (leave) { empty = c; emptyFront = (T(u, far - 2) + T(u + 1, far - 2)) / 2f; }
                    }
                    Blood(mid, far / 2); Blood(rnd.Next(lat), 1);
                    var cells = CagesIn();
                    // the jailer moves the prisoners from cage to cage; a second one walks along and looks them over
                    if (cells.Length >= 2) { var a = Activity.Give(Cultist.Spawn(T(mid, 1)), Activity.Kind.Shuffle, T(mid, dep / 2f)); if (a != null) a.Cages = cells; }
                    if (cells.Length > 0) { var a = Activity.Give(rnd.Next(2) == 0 ? Spearman.Spawn(T(mid, 2)) : Cultist.Spawn(T(mid, 2)), Activity.Kind.Inspect, T(mid, dep / 2f), cells.Select(c => (Vector2)c.transform.position).ToArray()); }
                    break;
                }
                case "study":
                {
                    if (lat >= 4 && TakeAll((mid, far), (mid - 1, far))) Breakables.Table(rnd.Next(2), (T(mid, far) + T(mid - 1, far)) / 2f, FaceDoor);
                    if (Take(0, far)) Breakables.Candelabra(T(0, far));
                    if (Take(lat - 1, far)) Breakables.SkullPile(T(lat - 1, far));
                    if (dep >= 5 && TakeAll((0, 2), (0, 3))) Breakables.Shelf((T(0, 2) + T(0, 3)) / 2f, FaceDoor - 90f);
                    if (dep >= 5 && lat >= 5 && TakeAll((lat - 1, 2), (lat - 1, 3))) Breakables.Shelf((T(lat - 1, 2) + T(lat - 1, 3)) / 2f, FaceDoor + 90f);
                    Books(4, 8);
                    var s = Builder.Sr("occ_sigil_" + rnd.Next(3), T(mid, dep / 2f), Builder.floorLayer, Builder.floorOrder + 2, Builder.floorMat ?? Builder.litMat);
                    s.transform.rotation = Quaternion.Euler(0, 0, rnd.Next(360));
                    if (Take(mid, far - 1)) Activity.Give(Cultist.Spawn(T(mid, far - 1)), Activity.Kind.Read, T(mid - 0.5f, far));
                    if (rnd.Next(2) == 0) { var w = Activity.Give(Crossbowman.Spawn(T(mid, 1)), Activity.Kind.Wander, T(mid, dep / 2f)); if (w != null) w.Radius = Mathf.Min(lat, dep) / 2f - 1f; }
                    break;
                }
                case "warehouse":                                                      // (the user: "make the stores really look like stores")
                {
                    // a real store: a main aisle from the door to the far wall; on both sides neat stacks two tiles deep
                    // with cross aisles between them, each stack of one kind of goods (barrels, crates, sacks, jugs)
                    // standing square in rows; great heaps of crates and logs along the far wall; the storekeeper's table
                    // by the door
                    Webs(2); Dirt(2, 4);
                    string[][] goods = { new[] { "Barrel", "BarrelAlt" }, new[] { "Box", "Box2", "Box3" }, new[] { "GroundBag" }, new[] { "Box", "Box" }, new[] { "Barrel", "Barrel" }, new[] { "Jug", "Jug", "Barrel" } };
                    int aisle = doorU, kind = rnd.Next(goods.Length);
                    bool wide = lat >= 8;                                                  // a wide store: the main aisle two tiles
                    bool Aisle(int u) => u == aisle || (wide && u == aisle + (aisle < lat / 2 ? 1 : -1));
                    for (int v = 2; v + 1 < far; v += 3)                                     // the stacks: rows two deep, a cross aisle after each
                        foreach (int side in new[] { -1, 1 })
                        {
                            var g = goods[kind++ % goods.Length];
                            int k = 0;
                            for (int u = side < 0 ? 0 : lat - 1; side < 0 ? u < lat : u >= 0; u -= side)
                            {
                                if (Aisle(u)) break;
                                if (side < 0 ? u > aisle : u < aisle) break;
                                for (int dv = 0; dv < 2; dv++)
                                    if (Take(u, v + dv)) { var name = g[k++ % g.Length]; var go = Builder.Spawn(Prefabs.Cont(name), T(u, v + dv), 0f); Builder.Own(go); }
                            }
                        }
                    for (int u = 0; u < lat; u++)                                            // the far wall: great heaps
                        if (!Aisle(u) && Take(u, far)) Prop(u % 3 == 0 ? "Logs" : u % 3 == 1 ? "GroundBoxes" : "GroundWoods", T(u, far));
                    // the storekeeper's table by the door, beside the aisle, and him at it
                    int tu = aisle + 2 < lat - 1 ? aisle + 2 : aisle - 3;
                    if (tu >= 0 && tu + 1 < lat && TakeAll((tu, 1), (tu + 1, 1)))
                    {
                        Breakables.Table(rnd.Next(2), (T(tu, 1) + T(tu + 1, 1)) / 2f, FaceDoor);
                        Activity.Give(Cultist.Spawn(T(tu, 0) + new Vector2(0.5f, 0f)), Activity.Kind.Guard, (T(tu, 1) + T(tu + 1, 1)) / 2f);
                    }
                    // workers go up and down the aisle; a spearman keeps the store
                    Activity.Give(Cultist.Spawn(T(aisle, 1)), Activity.Kind.Patrol, default, T(aisle, 1.5f), T(aisle, Mathf.Max(2, far - 1)));
                    Pair(Crossbowman.Spawn(T(aisle, 2)), Spearman.Spawn(T(aisle, 1)), T(aisle, 2), T(aisle, Mathf.Max(2, far - 1)));
                    break;
                }
                case "research":                                                       // the cult's study of flesh: specimens in cages, desks, books, sigils
                {
                    string[] kinds = { "rot", "zomb", "skel" };
                    Container empty = null; Vector2 emptyFront = default;
                    foreach (int u in new[] { 0, lat - 2 }) if (TakeAll((u, far), (u + 1, far), (u, far - 1), (u + 1, far - 1)))
                    {
                        bool leave = empty == null && u > 0;
                        var c = Breakables.Cage(leave ? null : kinds[rnd.Next(3)], (T(u, far) + T(u + 1, far - 1)) / 2f);
                        if (leave) { empty = c; emptyFront = (T(u, far - 2) + T(u + 1, far - 2)) / 2f; }
                    }
                    Vector2 desk = default; bool hasDesk = false;
                    for (int v = 2; v < far - 1; v += 3)
                        if (TakeAll((mid - 1, v), (mid, v))) { desk = (T(mid - 1, v) + T(mid, v)) / 2f; Breakables.Table(rnd.Next(2), desk, FaceDoor); hasDesk = true; }
                    if (Take(lat - 1, 1)) Breakables.SkullPile(T(lat - 1, 1));
                    if (Take(0, 1)) Breakables.Lectern(T(0, 1));
                    for (int i = 0; i < 2; i++) { int u = rnd.Next(lat), v = rnd.Next(1, dep); if (Take(u, v)) Prop("Jug", T(u, v)); }
                    for (int i = 0; i < 2; i++) { var s = Builder.Sr("occ_sigil_" + rnd.Next(3), T(R(1, lat - 2), R(1, dep - 2)), Builder.floorLayer, Builder.floorOrder + 2, Builder.floorMat ?? Builder.litMat); s.transform.rotation = Quaternion.Euler(0, 0, rnd.Next(360)); }
                    Books(4, 7); Blood(mid, far - 2); Blood(R(0, lat - 1), R(1, dep - 1));
                    if (hasDesk && Take(mid, 1)) Activity.Give(Cultist.Spawn(T(mid, 1)), Activity.Kind.Read, desk);
                    // one moves the specimens about, another studies them cage by cage and looks over the reader's shoulder
                    var specimens = CagesIn();
                    if (specimens.Length >= 2) { var a = Activity.Give(Cultist.Spawn(T(mid, 2)), Activity.Kind.Shuffle, T(mid, dep / 2f)); if (a != null) a.Cages = specimens; }
                    var looks = specimens.Select(c => (Vector2)c.transform.position).ToList(); if (hasDesk) looks.Add(desk);
                    if (looks.Count > 0) Activity.Give(Cultist.Spawn(T(lat - 2, 2)), Activity.Kind.Inspect, T(mid, dep / 2f), looks.ToArray());
                    break;
                }
                case "execution":                                                      // the block: prisoners led out of the cages and put to death
                {
                    string[] kinds = { "skel", "skel", "zomb", "rot" };
                    foreach (int u in new[] { 0, lat - 2 }) if (lat >= 5 && TakeAll((u, far), (u + 1, far), (u, far - 1), (u + 1, far - 1))) Breakables.Cage(kinds[rnd.Next(kinds.Length)], (T(u, far) + T(u + 1, far - 1)) / 2f);
                    int bv = Mathf.Max(2, dep / 2);
                    Vector2 block = T(mid, bv);
                    if (TakeAll((mid - 1, bv), (mid, bv), (mid + 1, bv))) Breakables.Sacrifice(block);
                    for (int i = 0; i < 4; i++) Blood(mid + R(-1.5f, 1.5f), bv + R(-1.2f, 1.2f));
                    foreach (int u in new[] { 0, lat - 1 }) if (Take(u, 1)) Breakables.SkullPile(T(u, 1));
                    if (Take(mid, far)) Breakables.Candelabra(T(mid, far));
                    var doomed = CagesIn();
                    if (doomed.Length > 0)
                    {
                        var ex = Activity.Give(Cultist.Spawn(T(mid, bv - 1)), Activity.Kind.Execute, T(mid, dep / 2f), T(mid, bv - 1.1f), block);
                        if (ex != null) ex.Cages = doomed;
                        Activity.Give(Cultist.Spawn(T(lat - 1, bv)), Activity.Kind.Inspect, T(mid, dep / 2f), block, doomed[0].transform.position);
                    }
                    break;
                }
                case "prison":                                                         // a great prison: cages all along both walls, a walk between
                {
                    string[] kinds = { "skel", "rot", "zomb" };
                    int left = 0;
                    for (int v = 2; v + 1 < dep; v += 3)
                        foreach (int u in new[] { 0, lat - 2 })
                            if (TakeAll((u, v), (u + 1, v), (u, v + 1), (u + 1, v + 1)))
                            {
                                bool emptyOne = left == 0 || (left < 2 && rnd.Next(4) == 0);   // at least one stands open: room to move them about
                                if (emptyOne) left++;
                                Breakables.Cage(emptyOne ? null : kinds[rnd.Next(3)], (T(u, v) + T(u + 1, v + 1)) / 2f);
                            }
                    for (int i = 0; i < 4; i++) Blood(R(2, lat - 3), R(1, dep - 1));
                    Webs(2);
                    var all = CagesIn();
                    if (all.Length >= 2)
                    {
                        var a = Activity.Give(Cultist.Spawn(T(mid, 1)), Activity.Kind.Shuffle, T(mid, dep / 2f)); if (a != null) a.Cages = all;
                        Activity.Give(Cultist.Spawn(T(mid, far)), Activity.Kind.Inspect, T(mid, dep / 2f), all.Select(c => (Vector2)c.transform.position).OrderBy(_ => rnd.Next()).ToArray());
                    }
                    Pair(Spearman.Spawn(T(mid, 2)), Spearman.Spawn(T(mid, 1)), T(mid, 2), T(mid, far), T(mid, 2));
                    break;
                }
                case "training": Training(far, mid); break;                          // the user: many kinds of training room
                case "refectory":                                                      // the cult's long tables, benches both sides, jugs and barrels
                {
                    for (int v = 2; v < far - 1; v += 4)
                        for (int u = 1; u + 1 < lat - 1; u += 3)
                            if (TakeAll((u, v), (u + 1, v), (u, v - 1), (u + 1, v - 1), (u, v + 1), (u + 1, v + 1)))
                            {
                                var c = (T(u, v) + T(u + 1, v)) / 2f;
                                Breakables.Table(rnd.Next(2), c, FaceDoor);
                                var b1 = (T(u, v - 1) + T(u + 1, v - 1)) / 2f; var b2 = (T(u, v + 1) + T(u + 1, v + 1)) / 2f;
                                Breakables.Bench(b1, FaceDoor);
                                Breakables.Bench(b2, FaceDoor + 180f);
                                if (rnd.Next(3) > 0) Activity.Give(Cultist.Spawn(b1), Activity.Kind.Sit, c);
                                if (rnd.Next(3) == 0) Activity.Give(rnd.Next(2) == 0 ? Spearman.Spawn(b2) : Crossbowman.Spawn(b2), Activity.Kind.Sit, c);
                            }
                    foreach (int u in new[] { 0, lat - 1 }) if (Take(u, far)) Prop("Barrel", T(u, far));
                    if (lat >= 6 && TakeAll((mid - 1, far), (mid, far)))
                    {
                        var block = (T(mid - 1, far) + T(mid, far)) / 2f; Breakables.Table(rnd.Next(2), block, FaceDoor); Blood(mid - 0.5f, far);
                        Activity.Give(Cultist.Spawn(T(mid, far - 1)), Activity.Kind.Work, block);                        // the cook chops for the table
                    }
                    for (int i = 0; i < 3; i++) { int u = rnd.Next(lat), v = rnd.Next(1, dep); if (Take(u, v)) Prop("Jug", T(u, v)); }
                    { int u = rnd.Next(lat), v = rnd.Next(2, dep); if (Take(u, v)) { var a = Activity.Give(Cultist.Spawn(T(u, v)), Activity.Kind.Sit, T(mid, dep / 2f)); if (a != null) a.StartStanding = true; } }   // on his feet; sits down where there is room
                    break;
                }
                case "dormitory":                                                      // beddings along the walls, a chest of drawers or a cupboard
                {
                    var beds = new List<Container>();
                    for (int v = 1; v < dep - 1; v += 2) foreach (int u in new[] { 0, lat - 1 }) if (TakeAll((u, v), (u, v + 1))) { var bd = Breakables.Bedroll((T(u, v) + T(u, v + 1)) / 2f, FaceDoor + 180f); if (bd != null) beds.Add(bd); }
                    // some of the cultists are asleep in their beds (the user)
                    foreach (var bd in beds.OrderBy(_ => rnd.Next()).Take(Mathf.Clamp(beds.Count / 2 + rnd.Next(2), 1, 4)))
                    { var sl = Activity.Give(Cultist.Spawn(bd.transform.position), Activity.Kind.Sleep, bd.transform.position); if (sl != null) sl.Bed = bd; }
                    if (Take(mid, far)) Furniture(rnd.Next(2) == 0 ? "Dresser" : "Cupboard", T(mid, far));
                    Corners(p => Breakables.Candles(p));
                    if (Take(mid, far - 2)) Activity.Give(Cultist.Spawn(T(mid, far - 2)), Activity.Kind.Read, T(mid, far));
                    break;
                }
                case "armory":                                                         // racks of spears, crates of bolts
                {
                    int rk = 0;
                    for (int u = 0; u + 1 < lat; u += 3) if (TakeAll((u, far), (u + 1, far))) { var rp = (T(u, far) + T(u + 1, far)) / 2f; if (rk++ % 2 == 0) Breakables.Rack(rp, FaceDoor); else Breakables.RackX(rp, FaceDoor); }
                    string[] ammo = { "Box", "Box2", "Box3", "Barrel", "GroundBag", "GroundBoxes" };
                    foreach (int u in new[] { 0, lat - 1 }) for (int v = 1; v < far; v++) if (rnd.Next(10) > 0 && Take(u, v)) Prop(ammo[rnd.Next(ammo.Length)], T(u, v));
                    for (int u = 1; u < lat - 1; u++) if (Take(u, far)) Prop(ammo[rnd.Next(ammo.Length)], T(u, far));            // between the racks
                    for (int v = 2; v < far - 1; v += 2) for (int u = 2; u < lat - 2; u++)                                  // stacks in the middle, an aisle left
                        if (Mathf.Abs(u - doorU) > 1 && u != mid && rnd.Next(3) == 0 && Take(u, v)) Prop(ammo[rnd.Next(ammo.Length)], T(u, v));
                    Webs(2); Dirt(3, 5);
                    if (Take(mid, 1)) { var g = Activity.Give(Crossbowman.Spawn(T(mid, 1)), Activity.Kind.Guard, T(doorU, -2)); if (g != null) g.Stay = true; }
                    break;
                }
                case "forge":                                                          // the cult's forge: an anvil table, a red fire, logs and barrels
                {
                    if (TakeAll((mid, far), (mid - 1, far))) Prop("Forge_table", (T(mid, far) + T(mid - 1, far)) / 2f);
                    if (Take(mid + 1, far)) Breakables.RedBrazier(T(mid + 1, far));
                    if (Take(0, far)) Prop("Logs", T(0, far));
                    if (Take(lat - 1, far)) Prop("Barrel", T(lat - 1, far));
                    for (int v = 1; v < far; v++) if (rnd.Next(2) == 0 && Take(0, v)) Prop(rnd.Next(2) == 0 ? "GroundWoods" : "Box", T(0, v));
                    float fr = door == 's' ? -90f : door == 'n' ? 90f : door == 'w' ? 180f : 0f;
                    if (dep >= 5 && TakeAll((lat - 1, 1), (lat - 1, 2))) Breakables.Rack((T(lat - 1, 1) + T(lat - 1, 2)) / 2f, fr);
                    if (dep >= 7 && TakeAll((lat - 1, 4), (lat - 1, 5))) Breakables.RackX((T(lat - 1, 4) + T(lat - 1, 5)) / 2f, fr);
                    Dirt(2, 4);
                    if (Take(mid, far - 1)) Activity.Give(Cultist.Spawn(T(mid, far - 1)), Activity.Kind.Work, (T(mid, far) + T(mid - 1, far)) / 2f);   // the smith at his anvil
                    if (Take(mid, far - 3)) Activity.Give(Spearman.Spawn(T(mid, far - 3)), Activity.Kind.Guard, T(mid, far));
                    break;
                }
                case "tomb":                                                           // tombs of the cult's dead between candles, a statue over them
                {
                    if (Take(mid, far)) Breakables.Statue(T(mid, far), FaceDoor);
                    for (int v = 2; v < far; v += 2) foreach (int u in new[] { 1, lat - 2 }) if (Take(u, v)) Prop(rnd.Next(2) == 0 ? "SkeletonTombs" : "TombObjectEmpty", T(u, v));
                    Corners(p => Breakables.Candles(p));
                    if (Take(mid, Mathf.Max(1, far - 2))) Activity.Give(Cultist.Spawn(T(mid, Mathf.Max(1, far - 2))), Activity.Kind.Pray, T(mid, far));
                    break;
                }
                case "alchemy":                                                        // red fires under the cult's brews, shelves, a desk, jugs
                {
                    if (TakeAll((mid, far), (mid - 1, far))) Breakables.Shelf((T(mid, far) + T(mid - 1, far)) / 2f, FaceDoor);
                    foreach (int u in new[] { 0, lat - 1 }) if (Take(u, far)) Breakables.RedBrazier(T(u, far));
                    Vector2 desk = default; bool hasDesk = false;
                    if (dep >= 5 && TakeAll((mid, far - 2), (mid - 1, far - 2))) { desk = (T(mid, far - 2) + T(mid - 1, far - 2)) / 2f; Breakables.Table(rnd.Next(2), desk, FaceDoor); hasDesk = true; }
                    for (int i = 0; i < 5; i++) { int u = rnd.Next(lat), v = rnd.Next(1, dep); if (Take(u, v)) Prop("Jug", T(u, v)); }
                    if (dep >= 5 && TakeAll((0, 2), (0, 3))) Breakables.Shelf((T(0, 2) + T(0, 3)) / 2f, FaceDoor - 90f);
                    if (Take(lat - 1, 1)) Breakables.SkullPile(T(lat - 1, 1));
                    Dirt(1, 3);
                    Books(1, 3);
                    if (hasDesk && Take(mid, Mathf.Max(1, far - 3))) Activity.Give(Cultist.Spawn(T(mid, Mathf.Max(1, far - 3))), Activity.Kind.Read, desk);
                    { var w = Activity.Give(Cultist.Spawn(T(mid, 1)), Activity.Kind.Wander, T(mid, dep / 2f)); if (w != null) w.Radius = Mathf.Min(lat, dep) / 2f - 1f; }
                    break;
                }

                case "sanctuary":                                                      // the inner sanctum: an obelisk on a sigil in a ring of candles, hooded statues in the far corners
                {
                    int cv = Mathf.Clamp(dep / 2, 2, far - 1);
                    Sigil(mid, cv);
                    bool ob = Take(mid, cv); if (ob) Breakables.Obelisk(T(mid, cv));
                    foreach (var (du, dv) in new[] { (-2, 0), (2, 0), (0, 2), (-1, -1), (1, -1), (-1, 1), (1, 1) }) if (rnd.Next(4) > 0 && Take(mid + du, cv + dv)) Breakables.Candles(T(mid + du, cv + dv));
                    if (Take(0, far)) Breakables.Statue(T(0, far), FaceDoor);           // square, its back to the wall (the user: not askew)
                    if (Take(lat - 1, far)) Breakables.Statue(T(lat - 1, far), FaceDoor);
                    foreach (int u in new[] { 0, lat - 1 }) if (Take(u, 1)) Breakables.Candelabra(T(u, 1));
                    Rug();
                    if (ob && cv - 2 >= 1 && Take(mid, cv - 2)) Activity.Give(Cultist.Spawn(T(mid, cv - 2)), Activity.Kind.Pray, T(mid, cv));
                    if (lat >= 5 && Take(lat - 2, cv)) Activity.Give(Cultist.Spawn(T(lat - 2, cv)), Activity.Kind.Guard, T(mid, cv));   // (he studies the things here)
                    break;
                }
                case "ossuary":                                                        // skulls heaped along the walls, bones on the floor, an altar with candle stands
                {
                    bool alt = lat >= 5 && TakeAll((mid - 1, far), (mid, far), (mid + 1, far));
                    if (alt) Breakables.Altar(T(mid, far));
                    foreach (int u in new[] { mid - 2, mid + 2 }) if (Take(u, far)) Breakables.Candelabra(T(u, far));
                    for (int v = 2; v < far; v += 2) foreach (int u in new[] { 0, lat - 1 }) if (Take(u, v)) Breakables.SkullPile(T(u, v));
                    for (int i = 0, n = rnd.Next(5, 9); i < n; i++) Floor("occ_bones_" + rnd.Next(3), R(0, lat - 1), R(1, dep - 1));
                    Blood(mid, far - 1);
                    Webs(2);
                    if (alt && Take(mid, far - 2)) Activity.Give(Cultist.Spawn(T(mid, far - 2)), Activity.Kind.Pray, T(mid, far));
                    if (rnd.Next(2) == 0 && Take(mid, 1)) Activity.Give(Spearman.Spawn(T(mid, 1)), Activity.Kind.Guard, T(doorU, -2));
                    break;
                }
                case "scriptorium":                                                    // rows of desks, a bench at each, the cult's copyists at work; bookcases at the far wall
                {
                    for (int u = 0; u + 1 < lat; u += 3) if (TakeAll((u, far), (u + 1, far))) Breakables.Shelf((T(u, far) + T(u + 1, far)) / 2f, FaceDoor);
                    int desks = 0;
                    for (int v = 3; v < far - 1; v += 3)
                        for (int u = 0; u + 1 < lat; u += 3)
                        {
                            if (Mathf.Abs(u - doorU) <= 1 && v <= 3) continue;
                            if (!TakeAll((u, v), (u + 1, v), (u, v - 1), (u + 1, v - 1))) continue;
                            var desk = (T(u, v) + T(u + 1, v)) / 2f; var seat = (T(u, v - 1) + T(u + 1, v - 1)) / 2f;
                            Breakables.Table(rnd.Next(2), desk, FaceDoor); Breakables.Bench(seat, FaceDoor);
                            if (rnd.Next(3) > 0) Activity.Give(Cultist.Spawn(seat), Activity.Kind.Sit, desk);
                            desks++;
                        }
                    Corners(p => Breakables.Candles(p));
                    { var a = Activity.Give(Cultist.Spawn(T(mid, 1)), Activity.Kind.Inspect, T(mid, dep / 2f), Shelves()); }
                    break;
                }
                case "gallery":                                                        // a hall of statues: a red runner down the middle, statues facing it from both sides, candles between
                {
                    Runner();
                    for (int v = 2; v < far; v += 2)
                    {
                        if (Take(0, v)) Breakables.Statue(T(0, v), FaceDoor + 90f);
                        if (lat >= 4 && Take(lat - 1, v)) Breakables.Statue(T(lat - 1, v), FaceDoor - 90f);
                        if (v + 1 < far) { if (rnd.Next(2) == 0 && Take(0, v + 1)) Breakables.Candles(T(0, v + 1)); if (lat >= 4 && rnd.Next(2) == 0 && Take(lat - 1, v + 1)) Breakables.Candles(T(lat - 1, v + 1)); }
                    }
                    if (Take(mid, far)) Breakables.Obelisk(T(mid, far));
                    foreach (int u in new[] { mid - 1, mid + 1 }) if (Take(u, far)) Breakables.Candelabra(T(u, far));
                    var statues = new List<Vector2>(); for (int v = 2; v < far; v += 2) { statues.Add(T(0, v)); if (lat >= 4) statues.Add(T(lat - 1, v)); }
                    Activity.Give(Cultist.Spawn(T(mid, 1)), Activity.Kind.Inspect, T(mid, dep / 2f), statues.OrderBy(_ => rnd.Next()).Take(4).ToArray());
                    if (rnd.Next(2) == 0) Activity.Give(Crossbowman.Spawn(T(mid, 2)), Activity.Kind.Guard, T(doorU, -2));
                    break;
                }
                case "summoning":                                                      // a summoning: a great sigil between four red fires, a caged offering, the faithful at the circle
                {
                    int cv = Mathf.Clamp(dep / 2, 2, far - 1);
                    var sg = Builder.Sr("occ_sigil_0", T(mid, cv), Builder.floorLayer, Builder.floorOrder + 2, Builder.floorMat ?? Builder.litMat);
                    sg.transform.rotation = Quaternion.Euler(0, 0, rnd.Next(360));
                    foreach (var (du, dv) in new[] { (-2, -2), (2, -2), (-2, 2), (2, 2) }) if (Take(mid + du, cv + dv)) Breakables.RedBrazier(T(mid + du, cv + dv));
                    if (lat >= 7 && TakeAll((0, far), (1, far), (0, far - 1), (1, far - 1))) Breakables.Cage(new[] { "skel", "zomb", "rot" }[rnd.Next(3)], (T(0, far) + T(1, far - 1)) / 2f);
                    if (Take(lat - 1, far)) Breakables.SkullPile(T(lat - 1, far));
                    for (int i = 0; i < 3; i++) Blood(mid + R(-1.2f, 1.2f), cv + R(-1.2f, 1.2f));
                    foreach (var (du, dv) in new[] { (0, -2), (0, 2) }) if (Take(mid + du, cv + dv)) Activity.Give(Cultist.Spawn(T(mid + du, cv + dv)), Activity.Kind.Pray, T(mid, cv));
                    break;
                }
                case "guardpost":                                                      // a guard post: a rack of spears, a dummy, a target, a bench; the guards drill
                {
                    float rackRot = FaceDoor;
                    if (TakeAll((mid - 1, far), (mid, far))) { var rp = (T(mid - 1, far) + T(mid, far)) / 2f; if (rnd.Next(2) == 0) Breakables.Rack(rp, rackRot); else Breakables.RackX(rp, rackRot); }
                    Vector2 dm = default; bool hasD = false;
                    if (dep >= 5 && Take(0, far - 1)) { dm = T(0, far - 1); Breakables.Dummy(dm, FaceDoor); hasD = true; }
                    Vector2 tg = default; bool hasT = false;
                    if (lat >= 5 && dep >= 6 && TakeAll((lat - 1, far), (lat - 2, far))) { tg = T(lat - 1.5f, far); Breakables.Target(tg, FaceDoor); hasT = true; }
                    if (lat >= 5 && TakeAll((lat - 1, 2), (lat - 1, 3))) Breakables.Bench((T(lat - 1, 2) + T(lat - 1, 3)) / 2f, FaceDoor + 90f);
                    if (Take(0, 1)) Prop("Barrel", T(0, 1));
                    if (hasD) Activity.Give(Spearman.Spawn(dm + (T(0, 0) - T(0, 1)) * 1.3f), Activity.Kind.Train, dm);
                    if (hasT) Activity.Give(Crossbowman.Spawn(T(lat - 1.5f, 1)), Activity.Kind.Shoot, tg);
                    Activity.Give(Spearman.Spawn(T(mid, 2)), Activity.Kind.Guard, T(doorU, -2));
                    break;
                }
                case "pantry":                                                         // the cult's pantry: barrels, crates and jugs along the walls, a table in the middle
                {
                    string[] stuff = { "Barrel", "BarrelAlt", "Box", "Jug", "Jug", "GroundBag", "Box2", "GroundBag" };
                    Webs(rnd.Next(1, 4)); Dirt(1, 3);
                    for (int v = 1; v < dep; v++) foreach (int u in new[] { 0, lat - 1 }) if (rnd.Next(10) > 0 && Take(u, v)) Prop(stuff[rnd.Next(stuff.Length)], T(u, v));
                    for (int u = 1; u < lat - 1; u++) if (rnd.Next(6) > 0 && Take(u, far)) Prop(stuff[rnd.Next(stuff.Length)], T(u, far));
                    foreach (int u in new[] { 1, lat - 2 }) for (int v = 2; v < far; v++) if (rnd.Next(3) == 0 && Take(u, v)) Prop(stuff[rnd.Next(stuff.Length)], T(u, v));   // a second row
                    Vector2 block = default; bool hasBlock = false;
                    if (lat >= 5 && dep >= 5 && TakeAll((mid - 1, dep / 2), (mid, dep / 2))) { block = (T(mid - 1, dep / 2) + T(mid, dep / 2)) / 2f; Breakables.Table(rnd.Next(2), block, FaceDoor); hasBlock = true; }
                    if (hasBlock) { Blood(mid - 0.5f, dep / 2f); Activity.Give(Cultist.Spawn(T(mid, dep / 2 - 1)), Activity.Kind.Work, block); }   // the butcher cuts the meat
                    if (Take(mid, far)) Furniture(rnd.Next(2) == 0 ? "Cupboard" : "Dresser", T(mid, far));
                    Webs(2);
                    { var w = Activity.Give(Cultist.Spawn(T(mid, 1)), Activity.Kind.Wander, T(mid, dep / 2f)); if (w != null) w.Radius = Mathf.Min(lat, dep) / 2f - 1f; }
                    break;
                }
                case "council":                                                        // the council chamber: one long table, benches both sides, a statue at its head, candle stands
                {
                    int cv = Mathf.Clamp(dep / 2, 2, far - 2);
                    var seats = new List<(Vector2 seat, Vector2 table)>();
                    for (int u = 1; u + 1 < lat - 1; u += 2)
                        if (TakeAll((u, cv), (u + 1, cv), (u, cv - 1), (u + 1, cv - 1), (u, cv + 1), (u + 1, cv + 1)))
                        {
                            var c = (T(u, cv) + T(u + 1, cv)) / 2f;
                            Breakables.Table(rnd.Next(2), c, FaceDoor);
                            var b1 = (T(u, cv - 1) + T(u + 1, cv - 1)) / 2f; var b2 = (T(u, cv + 1) + T(u + 1, cv + 1)) / 2f;
                            Breakables.Bench(b1, FaceDoor); Breakables.Bench(b2, FaceDoor + 180f);
                            seats.Add((b1, c)); seats.Add((b2, c));
                        }
                    if (Take(mid, far)) Breakables.Statue(T(mid, far), FaceDoor);
                    Corners(p => Breakables.Candelabra(p));
                    Rug();
                    foreach (var (seat, table) in seats.OrderBy(_ => rnd.Next()).Take(Mathf.Max(1, seats.Count * 2 / 3))) Activity.Give(Cultist.Spawn(seat), Activity.Kind.Sit, table);
                    { int u = rnd.Next(lat), v = rnd.Next(1, dep); if (Take(u, v)) { var a = Activity.Give(Cultist.Spawn(T(u, v)), Activity.Kind.Sit, T(mid, cv)); if (a != null) a.StartStanding = true; } }
                    if (rnd.Next(2) == 0) Activity.Give(Crossbowman.Spawn(T(mid, 1)), Activity.Kind.Guard, T(doorU, -2));
                    break;
                }

                default:                                                                // a store
                {
                    // (the user: stores must look like stores) goods in order: each wall its own kind in a neat row
                    // (barrels along one, crates along the other, sacks or jugs along the far wall), heaps in the far
                    // corners, the middle kept clear to walk
                    string[][] goods = { new[] { "Barrel", "BarrelAlt" }, new[] { "Box", "Box2", "Box3" }, new[] { "GroundBag" }, new[] { "Jug", "Jug", "Barrel" } };
                    int g0 = rnd.Next(goods.Length);
                    Webs(rnd.Next(1, 3)); Dirt(1, 3);
                    foreach (var (u, gi) in new[] { (0, g0), (lat - 1, g0 + 1) })
                    {
                        var g = goods[gi % goods.Length]; int k = 0;
                        for (int v = 1; v < far; v++) if (Take(u, v)) { var go = Builder.Spawn(Prefabs.Cont(g[k++ % g.Length]), T(u, v), 0f); Builder.Own(go); }
                    }
                    { var g = goods[(g0 + 2) % goods.Length]; int k = 0; for (int u = 1; u < lat - 1; u++) if (Take(u, far)) { var go = Builder.Spawn(Prefabs.Cont(g[k++ % g.Length]), T(u, far), 0f); Builder.Own(go); } }
                    if (Take(0, far)) Prop("GroundBoxes", T(0, far));
                    if (Take(lat - 1, far)) Prop("Logs", T(lat - 1, far));
                    if (lat >= 6 && dep >= 6)                                                // a bigger store: a second row by each wall
                        foreach (var (u, gi) in new[] { (1, g0), (lat - 2, g0 + 1) })
                        {
                            var g = goods[gi % goods.Length]; int k = 0;
                            for (int v = 2; v < far - 1; v++) if (Take(u, v)) { var go = Builder.Spawn(Prefabs.Cont(g[k++ % g.Length]), T(u, v), 0f); Builder.Own(go); }
                        }
                    if (lat >= 5 && dep >= 5) Activity.Give(Cultist.Spawn(T(mid, 1)), Activity.Kind.Patrol, default, T(mid, 1.5f), T(mid, Mathf.Max(2, far - 1)));
                    if (rnd.Next(2) == 0 && Take(mid, 2)) Activity.Give(Crossbowman.Spawn(T(mid, 2)), Activity.Kind.Patrol, T(mid, 2), T(mid, Mathf.Max(2, far - 1)));
                    break;
                }
            }
            Fill(role, far);
        }

        /// a room that came out half empty gets the things such a room has along its walls (no room is left with a
        /// lone thing in it - the user)
        static void Fill(string role, int far)
        {
            Dress(role, far);                                                 // (the user: the rooms full, like a real cult's)
            int want = Mathf.Max(3, lat * dep / 6);
            if (used.Count >= want) return;
            string[] stuff = role == "range" || role == "training" || role == "guardpost" || role == "armory" || role == "barracks" ? new[] { "Box", "Box2", "Barrel", "GroundBag", "GroundBoxes" }
                           : role == "library" || role == "study" || role == "scriptorium" || role == "research" ? new[] { "Box", "Jug", "GroundBag" }
                           : new[] { "Box", "Barrel", "BarrelAlt", "Box2", "GroundBag", "Jug" };
            int added = 0;
            // (the user: odd things strewn about) in pairs, the same thing on both side walls at the same depth,
            // every other tile; the quiet rooms get candle stands, the others their own goods
            bool quiet = new[] { "library", "study", "shrine", "tomb", "sanctuary", "council", "gallery", "scriptorium", "summoning", "ossuary" }.Contains(role);
            string thing = stuff[rnd.Next(stuff.Length)];
            for (int v = 2; v < far && used.Count < want; v += 2)
                if (Pair2(0, v, p => { if (quiet) Breakables.Candelabra(p); else Prop(thing, p); })) added += 2;
            if (added > 0) { SanctumPlugin.Log.LogInfo("Sanctum: side room " + role + " was half empty - " + added + " things added along the walls"); }
        }

        static readonly string[] Big = { "SkeletonTombs", "TombObjectEmpty", "TombObject_Hero", "Logs", "GroundWoods", "Forge_table", "Cupboard", "Dresser" };
        static void Prop(string name, Vector2 p) { var pr = Prefabs.Cont(name); if (pr != null) Builder.Own(Builder.Spawn(pr, p, 0f)); }
        /// furniture against the far wall: its back to the wall, its front to the room (the user: a chest of drawers
        /// stood the same way whatever wall it was by)
        static void Furniture(string name, Vector2 p)
        {
            var pr = Prefabs.Cont(name); if (pr == null) return;
            var go = Builder.Spawn(pr, p, FaceDoor); Builder.Own(go);
            if (go != null) foreach (var c in go.GetComponentsInChildren<Container>(true)) { Breakables.Pin(c); c.playDamageAnimation = true; }   // it stands fast against its wall, and jerks when struck like the game's things
        }

        static void Corners(Action<Vector2> put)
        {
            foreach (var (u, v) in new[] { (0, dep - 1), (lat - 1, dep - 1), (0, 0), (lat - 1, 0) }) if (rnd.Next(4) > 0 && Take(u, v)) put(T(u, v));
        }

        /// the cages standing in this room
        static Container[] CagesIn()
        {
            var rr = new Rect(Room.O.x + r.xMin - 0.5f, Room.O.y + r.yMin - 0.5f, r.width, r.height);
            return UnityEngine.Object.FindObjectsOfType<Container>().Where(c => c != null && c.name == "Sanctum_occ_cage" && rr.Contains((Vector2)c.transform.position)).ToArray();
        }

        /// two on their rounds together: the first leads, the second keeps beside him
        public static void Pair(Mob lead, Mob mate, Vector2 look, params Vector2[] route)
        {
            var a = Activity.Give(lead, Activity.Kind.Patrol, look, route);
            if (a != null && mate != null) { var f = Activity.Give(mate, Activity.Kind.Follow, look); if (f != null) f.Leader = lead.transform; }
            else if (mate != null) Activity.Give(mate, Activity.Kind.Guard, look);
        }

        /// a training room, one of several kinds (the user: many kinds of them)
        static void Training(int far, int mid)
        {
            Vector2 du = T(1, 0) - T(0, 0), dv = T(0, 1) - T(0, 0);                       // across the room / toward the far wall
            float sideRot = door == 's' ? 90f : door == 'n' ? -90f : door == 'w' ? 0f : 180f;   // a rack against a side wall
            int kind = lat >= 5 && dep >= 6 ? rnd.Next(6) : 5;
            SanctumPlugin.Log.LogInfo("Sanctum: training room, kind " + new[] { "spear drill", "shooting gallery", "yard", "circle", "rest and drill", "mixed" }[kind]);
            switch (kind)
            {
                case 0:                                                                       // the spear drill: a row of dummies, the spearmen in a line before them, the drill master watching
                {
                    var ds = new List<Vector2>();
                    for (int u = 1; u < lat - 1; u += 2) if (Take(u, far - 1)) { var d = T(u, far - 1); Breakables.Dummy(d, FaceDoor); ds.Add(d); Take(u, far - 2); }
                    foreach (var d in ds) if (rnd.Next(6) > 0) Activity.Give(Spearman.Spawn(d - dv * 1.3f), Activity.Kind.Train, d);
                    for (int v = 1; v + 1 < far - 2; v += 3) foreach (int u in new[] { 0, lat - 1 }) if (TakeAll((u, v), (u, v + 1))) { var rp = (T(u, v) + T(u, v + 1)) / 2f; if (rnd.Next(2) == 0) Breakables.Rack(rp, sideRot); else Prop(rnd.Next(2) == 0 ? "Barrel" : "Box", rp); }
                    if (Take(mid, 1)) { var g = Activity.Give(Crossbowman.Spawn(T(mid, 1)), Activity.Kind.Guard, T(mid, far - 1)); if (g != null) g.Stay = true; }
                    Dirt(2, 4);
                    break;
                }
                case 1:                                                                       // the shooting gallery: targets along the far wall, crossbowmen shooting from the near end, crates of bolts
                {
                    var ts = new List<(Vector2 p, int u)>();
                    for (int u = 0; u + 1 < lat; u += 3) if (TakeAll((u, far), (u + 1, far))) { var t = T(u + 0.5f, far); Breakables.Target(t, FaceDoor); ts.Add((t, u)); }
                    foreach (var t in ts) if (rnd.Next(5) > 0 && Take(t.u, 1)) Activity.Give(Crossbowman.Spawn(T(t.u + 0.5f, 1)), Activity.Kind.Shoot, t.p);
                    foreach (int u in new[] { 0, lat - 1 }) for (int v = 2; v < far - 1; v++) if (rnd.Next(3) > 0 && Take(u, v)) Prop(rnd.Next(3) == 0 ? "Barrel" : rnd.Next(2) == 0 ? "Box" : "Box2", T(u, v));
                    if (TakeAll((0, far - 1), (0, far - 2))) Breakables.RackX((T(0, far - 1) + T(0, far - 2)) / 2f, sideRot);
                    if (TakeAll((lat - 1, far - 1), (lat - 1, far - 2))) Breakables.RackX((T(lat - 1, far - 1) + T(lat - 1, far - 2)) / 2f, sideRot + 180f);
                    Dirt(1, 3);
                    break;
                }
                case 2:                                                                       // the yard: dummies in the middle, spearmen at them from both sides, the rest on benches by the walls
                {
                    int cv = Mathf.Clamp(dep / 2, 2, far - 2);
                    var ds = new List<(Vector2 p, float side)>();
                    foreach (var (u, sd) in new[] { (mid - 1, -1f), (mid + 1, 1f) })
                        if (u > 0 && u < lat - 1 && Take(u, cv)) { var d = T(u, cv); Breakables.Dummy(d, FaceDoor + (sd < 0 ? -90f : 90f)); ds.Add((d, sd)); Take(u + (int)sd, cv); }
                    foreach (var d in ds) Activity.Give(Spearman.Spawn(d.p + du * 1.3f * d.side), Activity.Kind.Train, d.p);
                    int sat = 0;
                    foreach (int u in new[] { 0, lat - 1 })
                        if (TakeAll((u, 1), (u, 2)))
                        {
                            var bp = (T(u, 1) + T(u, 2)) / 2f; Breakables.Bench(bp, FaceDoor + 90f);
                            if (sat++ < 2 && rnd.Next(3) > 0) Activity.Give(rnd.Next(2) == 0 ? Spearman.Spawn(bp) : Crossbowman.Spawn(bp), Activity.Kind.Sit, T(mid, cv));
                        }
                    for (int u = 1; u + 1 < lat - 1; u += 3) if (TakeAll((u, far), (u + 1, far))) { var rp = (T(u, far) + T(u + 1, far)) / 2f; if (rnd.Next(2) == 0) Breakables.Rack(rp, FaceDoor); else Breakables.RackX(rp, FaceDoor); }
                    Dirt(3, 5);
                    break;
                }
                case 3:                                                                       // the circle: a dummy on a red sigil, two spearmen at it from either side, candle stands, a cultist looking on
                {
                    int cv = Mathf.Clamp(dep / 2, 2, far - 2);
                    Sigil(mid, cv);
                    if (Take(mid, cv))
                    {
                        var d = T(mid, cv); Breakables.Dummy(d, FaceDoor);
                        foreach (var sd in new[] { -1, 1 }) if (Take(mid + sd, cv)) Activity.Give(Spearman.Spawn(d + du * 1.3f * sd), Activity.Kind.Train, d);
                    }
                    foreach (int u in new[] { 0, lat - 1 }) { if (Take(u, far)) Breakables.Candelabra(T(u, far)); if (Take(u, 1)) Breakables.Candelabra(T(u, 1)); }
                    if (TakeAll((mid - 1, far), (mid, far))) Breakables.Rack((T(mid - 1, far) + T(mid, far)) / 2f, FaceDoor);
                    if (Take(mid, 1)) Activity.Give(Cultist.Spawn(T(mid, 1)), Activity.Kind.Guard, T(mid, cv));   // (a cultist: he studies the drill)
                    Blood(mid, cv + 1);
                    break;
                }
                case 4:                                                                       // rest and drill: a table with benches by the door, a dummy and a target at the far end
                {
                    if (dep >= 7 && TakeAll((mid - 1, 2), (mid, 2)))
                    {
                        var tb = (T(mid - 1, 2) + T(mid, 2)) / 2f; Breakables.Table(rnd.Next(2), tb, FaceDoor + 90f);
                        foreach (var v in new[] { 1, 3 })
                            if (TakeAll((mid - 1, v), (mid, v))) { var bp = (T(mid - 1, v) + T(mid, v)) / 2f; Breakables.Bench(bp, FaceDoor + 90f); if (rnd.Next(3) > 0) Activity.Give(rnd.Next(2) == 0 ? Spearman.Spawn(bp) : Crossbowman.Spawn(bp), Activity.Kind.Sit, tb); }
                    }
                    if (Take(1, far - 1)) { var d = T(1, far - 1); Breakables.Dummy(d, FaceDoor); Take(1, far - 2); Activity.Give(Spearman.Spawn(d - dv * 1.3f), Activity.Kind.Train, d); }
                    if (TakeAll((lat - 2, far), (lat - 3, far))) { var t = T(lat - 2.5f, far); Breakables.Target(t, FaceDoor); if (Take(lat - 3, 1)) Activity.Give(Crossbowman.Spawn(T(lat - 2.5f, Mathf.Max(1, far - 5))), Activity.Kind.Shoot, t); }
                    foreach (int u in new[] { 0, lat - 1 }) if (Take(u, far)) Prop(rnd.Next(2) == 0 ? "Barrel" : "GroundBag", T(u, far));
                    if (TakeAll((0, far - 3), (0, far - 4))) Breakables.Rack((T(0, far - 3) + T(0, far - 4)) / 2f, sideRot);
                    Dirt(2, 3);
                    break;
                }
                default:                                                                      // dummies to thrust at, a target to shoot, a rack of spears (the first kind)
                {
                    var dummies = new List<Vector2>();
                    for (int u = 1; u < lat - 1 && dummies.Count < 3; u += 3) if (Take(u, far - 1)) { var p = T(u, far - 1); Breakables.Dummy(p, FaceDoor); dummies.Add(p); }
                    Vector2 tgt = default; bool hasT = false;
                    if (lat >= 6 && TakeAll((lat - 2, far), (lat - 3, far))) { tgt = T(lat - 2.5f, far); Breakables.Target(tgt, FaceDoor); hasT = true; }
                    float rackRot = door == 's' ? 90f : door == 'n' ? -90f : door == 'w' ? 0f : 180f;
                    if (TakeAll((0, mid), (0, mid + 1))) { var rp = (T(0, mid) + T(0, mid + 1)) / 2f; if (rnd.Next(2) == 0) Breakables.Rack(rp, rackRot); else Breakables.RackX(rp, rackRot); }
                    if (TakeAll((0, mid + 3), (0, mid + 4))) { var rp = (T(0, mid + 3) + T(0, mid + 4)) / 2f; Breakables.RackX(rp, rackRot); }
                    for (int v = 1; v < far - 2; v += 2) if (TakeAll((lat - 1, v), (lat - 1, v + 1))) Breakables.Bench((T(lat - 1, v) + T(lat - 1, v + 1)) / 2f, FaceDoor + 90f);
                    foreach (var d in dummies.Take(3))
                    {
                        var at = d + (T(0, 0) - T(0, 1)) * 1.3f;
                        Activity.Give(Spearman.Spawn(at), Activity.Kind.Train, d);
                    }
                    if (hasT) Activity.Give(Crossbowman.Spawn(T(lat - 2.5f, 1)), Activity.Kind.Shoot, tgt);
                    break;
                }
            }
        }

        static void Webs(int n)
        {
            var web = Prefabs.Cont("SpiderWebObject");
            if (web == null) return;
            foreach (var (u, v) in new[] { (0, dep - 1), (lat - 1, dep - 1), (0, 0), (lat - 1, 0) }.OrderBy(_ => rnd.Next()).Take(n))
                if (Take(u, v)) Builder.Own(Builder.Spawn(web, T(u, v), R(0, 360)));
        }

        /// a sigil drawn in blood on the floor
        static void Sigil(float u, float v)
        {
            var s = Builder.Sr("occ_sigil_" + rnd.Next(3), T(u, v), Builder.floorLayer, Builder.floorOrder + 2, Builder.floorMat ?? Builder.litMat);
            s.transform.rotation = Quaternion.Euler(0, 0, rnd.Next(360));
        }

        /// something lying on the floor (bones)
        static void Floor(string art, float u, float v)
        {
            var s = Builder.Sr(art, T(u, v), Builder.floorLayer, Builder.floorOrder + 4, Builder.floorMat ?? Builder.litMat);
            s.transform.rotation = Quaternion.Euler(0, 0, rnd.Next(360));
        }

        /// dust and grime: bits of rubble and old bones on the floor
        static void Dirt(int a, int b)
        {
            for (int i = 0, n = rnd.Next(a, b + 1); i < n; i++) Floor(rnd.Next(3) == 0 ? "occ_bones_" + rnd.Next(3) : "occ_rubble", R(0, lat - 1), R(0.5f, dep - 1));
        }

        /// the worn red rug in the middle
        static void Rug()
        {
            return;                                                         // (the user: no carpets)
            if (lat < 3 || dep < 4) return;
            var rug = Builder.Sr("occ_rug", T((lat - 1) / 2f, (dep - 1) / 2f), Builder.floorLayer, Builder.floorOrder + 2, Builder.floorMat ?? Builder.litMat);
            rug.transform.rotation = Quaternion.Euler(0, 0, door == 's' || door == 'n' ? 0f : 90f);
        }

        /// a red runner from the door to the far wall
        static void Runner()
        {
            return;                                                         // (the user: no carpets)
            int u = lat / 2;
            for (int v = 0; v < dep - 1; v++)
            {
                var c = Builder.Sr("occ_carpet", T(u, v), Builder.floorLayer, Builder.floorOrder + 2, Builder.floorMat ?? Builder.litMat);
                c.transform.rotation = Quaternion.Euler(0, 0, door == 's' || door == 'n' ? 0f : 90f);
            }
        }

        /// the bookcases standing in this room
        static Vector2[] Shelves()
        {
            var rr = new Rect(Room.O.x + r.xMin - 0.5f, Room.O.y + r.yMin - 0.5f, r.width, r.height);
            return UnityEngine.Object.FindObjectsOfType<Container>().Where(c => c != null && c.name == "Sanctum_occ_shelf" && rr.Contains((Vector2)c.transform.position)).Select(c => (Vector2)c.transform.position).ToArray();
        }

        static void Books(int a, int b)
        {
            // no books on the floor any more
        }

        static void Blood(float u, float v)
        {
            var s = Builder.Sr("occ_blood_" + rnd.Next(3), T(u + R(-.4f, .4f), v + R(-.4f, .4f)), Builder.floorLayer, Builder.floorOrder + 4, Builder.floorMat ?? Builder.litMat);
            s.transform.rotation = Quaternion.Euler(0f, 0f, rnd.Next(360));
        }
    }
}
