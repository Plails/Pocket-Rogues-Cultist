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
    public static class SideRoom
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
            if (v <= 1 && Mathf.Abs(u - doorU) <= 1) return false;                 // the way in stays clear
            var t = Tl(u, v); if (!Lay.Floor(t.x, t.y)) return false;               // (a pillar stands there)
            var p = new Vector2Int(u, v);
            if (used.Contains(p) || !Open(p)) return false;                         // nothing may shut off a part of the room
            return used.Add(p);
        }
        static bool TakeAll(params (int u, int v)[] ts)
        {
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
            // a worn red rug in the middle of the quieter rooms
            if (new[] { "library", "study", "dormitory", "shrine", "refectory", "alchemy", "tomb" }.Contains(role) && lat >= 3 && dep >= 4)
            {
                var rug = Builder.Sr("occ_rug", T((lat - 1) / 2f, (dep - 1) / 2f), Builder.floorLayer, Builder.floorOrder + 2, Builder.floorMat ?? Builder.litMat);
                rug.transform.rotation = Quaternion.Euler(0, 0, door == 's' || door == 'n' ? 0f : 90f);
            }
            switch (role)
            {
                case "library":
                {
                    for (int u = 0; u + 1 < lat; u += 3) if (TakeAll((u, far), (u + 1, far))) Breakables.Shelf((T(u, far) + T(u + 1, far)) / 2f, FaceDoor);
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
                    for (int v = 1; v < dep - 1; v++) foreach (int u in new[] { 0, lat - 1 }) if (rnd.Next(3) == 0 && Take(u, v)) Prop(rnd.Next(2) == 0 ? "Box" : "Barrel", T(u, v));
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
                    Books(4, 8);
                    var s = Builder.Sr("occ_sigil_" + rnd.Next(3), T(mid, dep / 2f), Builder.floorLayer, Builder.floorOrder + 2, Builder.floorMat ?? Builder.litMat);
                    s.transform.rotation = Quaternion.Euler(0, 0, rnd.Next(360));
                    if (Take(mid, far - 1)) Activity.Give(Cultist.Spawn(T(mid, far - 1)), Activity.Kind.Read, T(mid - 0.5f, far));
                    if (rnd.Next(2) == 0) { var w = Activity.Give(Crossbowman.Spawn(T(mid, 1)), Activity.Kind.Wander, T(mid, dep / 2f)); if (w != null) w.Radius = Mathf.Min(lat, dep) / 2f - 1f; }
                    break;
                }
                case "warehouse":                                                      // a great store: rows of barrels and crates with aisles between
                {
                    string[] stuff = { "Box", "Barrel", "BarrelAlt", "Box2", "Box3", "GroundBoxes", "Logs", "GroundBag", "Barrel", "Box", "GroundBag", "Box2" };
                    Webs(4); Dirt(4, 7);
                    for (int v = 2; v < dep; v += 3)
                        for (int u = 0; u < lat; u++)
                        {
                            if (Mathf.Abs(u - doorU) <= 1 || u == lat / 2) continue;           // the aisles
                            if (rnd.Next(5) > 0 && Take(u, v)) Prop(stuff[rnd.Next(stuff.Length)], T(u, v) + new Vector2(R(-.12f, .12f), R(-.12f, .12f)));
                            if (v + 1 < dep && rnd.Next(3) == 0 && Take(u, v + 1)) Prop(stuff[rnd.Next(stuff.Length)], T(u, v + 1));
                        }
                    foreach (int u in new[] { 0, lat - 1 }) for (int v = 1; v < dep; v++) if (rnd.Next(5) > 0 && Take(u, v)) Prop(stuff[rnd.Next(stuff.Length)], T(u, v));
                    for (int u = 1; u < lat - 1; u++) if (rnd.Next(4) > 0 && Take(u, far)) Prop(stuff[rnd.Next(stuff.Length)], T(u, far));
                    // they work: loaded carts pushed up and down the aisles; a crossbowman and a spearman go the rounds together
                    Activity.Give(Cultist.Spawn(T(lat / 2, 1)), Activity.Kind.Patrol, default, T(lat / 2, 1.5f), T(lat / 2, Mathf.Max(2, far - 1)));
                    if (Mathf.Abs(doorU - lat / 2) >= 3) Activity.Give(Spearman.Spawn(T(doorU, far - 1)), Activity.Kind.Patrol, default, T(doorU, Mathf.Max(2, far - 1)), T(doorU, 2));
                    Pair(Crossbowman.Spawn(T(doorU, 2)), Spearman.Spawn(T(doorU, 1)), T(doorU, 2), T(doorU, far), T(lat / 2, far), T(lat / 2, 1));
                    if (rnd.Next(2) == 0 && Take(lat / 2, far)) Activity.Give(Spearman.Spawn(T(lat / 2, far)), Activity.Kind.Guard, T(doorU, 0));
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
                case "training":                                                       // dummies to thrust at, a target to shoot, a rack of spears
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
                    for (int i = 0; i < 3; i++) { int u = rnd.Next(lat), v = rnd.Next(1, dep); if (Take(u, v)) Prop("Jug", T(u, v)); }
                    { int u = rnd.Next(lat), v = rnd.Next(2, dep); if (Take(u, v)) { var a = Activity.Give(Cultist.Spawn(T(u, v)), Activity.Kind.Sit, T(mid, dep / 2f)); if (a != null) a.StartStanding = true; } }   // on his feet; sits down where there is room
                    break;
                }
                case "dormitory":                                                      // beddings along the walls, a chest of drawers or a cupboard
                {
                    for (int v = 1; v < dep - 1; v += 2) foreach (int u in new[] { 0, lat - 1 }) if (TakeAll((u, v), (u, v + 1))) Breakables.Bedroll((T(u, v) + T(u, v + 1)) / 2f, FaceDoor + 180f);
                    if (Take(mid, far)) Prop(rnd.Next(2) == 0 ? "Dresser" : "Cupboard", T(mid, far));
                    Corners(p => Breakables.Candles(p));
                    if (Take(mid, far - 2)) Activity.Give(Cultist.Spawn(T(mid, far - 2)), Activity.Kind.Read, T(mid, far));
                    break;
                }
                case "armory":                                                         // racks of spears, crates of bolts
                {
                    int rk = 0;
                    for (int u = 0; u + 1 < lat; u += 3) if (TakeAll((u, far), (u + 1, far))) { var rp = (T(u, far) + T(u + 1, far)) / 2f; if (rk++ % 2 == 0) Breakables.Rack(rp, FaceDoor); else Breakables.RackX(rp, FaceDoor); }
                    string[] ammo = { "Box", "Box2", "Box3", "Barrel", "GroundBag", "GroundBoxes" };
                    foreach (int u in new[] { 0, lat - 1 }) for (int v = 1; v < far; v++) if (rnd.Next(4) > 0 && Take(u, v)) Prop(ammo[rnd.Next(ammo.Length)], T(u, v));
                    Webs(2); Dirt(2, 4);
                    if (Take(mid, 1)) { var g = Activity.Give(Crossbowman.Spawn(T(mid, 1)), Activity.Kind.Guard, T(doorU, -2)); if (g != null) g.Stay = true; }
                    break;
                }
                case "forge":                                                          // the cult's forge: an anvil table, a red fire, logs and barrels
                {
                    if (TakeAll((mid, far), (mid - 1, far))) Prop("Forge_table", (T(mid, far) + T(mid - 1, far)) / 2f);
                    if (Take(mid + 1, far)) Breakables.RedBrazier(T(mid + 1, far));
                    if (Take(0, far)) Prop("Logs", T(0, far));
                    if (Take(lat - 1, far)) Prop("Barrel", T(lat - 1, far));
                    for (int v = 1; v < far; v++) if (rnd.Next(3) == 0 && Take(0, v)) Prop(rnd.Next(2) == 0 ? "GroundWoods" : "Box", T(0, v));
                    if (Take(mid, far - 2)) Activity.Give(Spearman.Spawn(T(mid, far - 2)), Activity.Kind.Guard, T(mid, far));
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
                    for (int i = 0; i < 3; i++) { int u = rnd.Next(lat), v = rnd.Next(1, dep); if (Take(u, v)) Prop("Jug", T(u, v)); }
                    if (Take(lat - 1, 1)) Breakables.SkullPile(T(lat - 1, 1));
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
                    if (Take(0, far)) Breakables.Statue(T(0, far), FaceDoor + 135f);
                    if (Take(lat - 1, far)) Breakables.Statue(T(lat - 1, far), FaceDoor - 135f);
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
                    for (int v = 1; v < dep; v++) foreach (int u in new[] { 0, lat - 1 }) if (rnd.Next(3) > 0 && Take(u, v)) Prop(stuff[rnd.Next(stuff.Length)], T(u, v));
                    for (int u = 1; u < lat - 1; u++) if (rnd.Next(2) == 0 && Take(u, far)) Prop(stuff[rnd.Next(stuff.Length)], T(u, far));
                    if (lat >= 5 && dep >= 5 && TakeAll((mid - 1, dep / 2), (mid, dep / 2))) Breakables.Table(rnd.Next(2), (T(mid - 1, dep / 2) + T(mid, dep / 2)) / 2f, FaceDoor);
                    if (Take(mid, far)) Prop(rnd.Next(2) == 0 ? "Cupboard" : "Dresser", T(mid, far));
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
                    string[] stuff = { "Box", "Barrel", "BarrelAlt", "Box2", "Box3", "GroundBag", "GroundBoxes", "GroundBag", "Jug" };
                    Webs(rnd.Next(2, 5)); Dirt(2, 5);
                    for (int v = 1; v < dep; v++) foreach (int u in new[] { 0, lat - 1 }) if (rnd.Next(6) > 0 && Take(u, v)) Prop(stuff[rnd.Next(stuff.Length)], T(u, v) + new Vector2(R(-.1f, .1f), R(-.1f, .1f)));
                    for (int u = 1; u < lat - 1; u++) if (rnd.Next(4) > 0 && Take(u, far)) Prop(stuff[rnd.Next(stuff.Length)], T(u, far));
                    // a heap or two in the middle, with room to walk round
                    for (int i = 0; i < (lat * dep) / 14; i++) { int u = rnd.Next(2, Mathf.Max(3, lat - 2)), v = rnd.Next(2, Mathf.Max(3, far - 1)); if (Take(u, v)) Prop(stuff[rnd.Next(stuff.Length)], T(u, v)); }
                    if (lat >= 5 && dep >= 5) Activity.Give(Cultist.Spawn(T(mid, 1)), Activity.Kind.Patrol, default, T(mid, 1.5f), T(mid, Mathf.Max(2, far - 1)));
                    if (rnd.Next(2) == 0 && Take(mid, 2)) Activity.Give(Crossbowman.Spawn(T(mid, 2)), Activity.Kind.Patrol, T(mid, 2), T(mid, Mathf.Max(2, far - 1)));
                    break;
                }
            }
        }

        static readonly string[] Big = { "SkeletonTombs", "TombObjectEmpty", "TombObject_Hero", "Logs", "GroundWoods", "Forge_table", "Cupboard", "Dresser" };
        static void Prop(string name, Vector2 p) { var pr = Prefabs.Cont(name); if (pr != null) Builder.Own(Builder.Spawn(pr, p, Big.Contains(name) ? 0f : R(-4, 4))); }

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
            if (lat < 3 || dep < 4) return;
            var rug = Builder.Sr("occ_rug", T((lat - 1) / 2f, (dep - 1) / 2f), Builder.floorLayer, Builder.floorOrder + 2, Builder.floorMat ?? Builder.litMat);
            rug.transform.rotation = Quaternion.Euler(0, 0, door == 's' || door == 'n' ? 0f : 90f);
        }

        /// a red runner from the door to the far wall
        static void Runner()
        {
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
