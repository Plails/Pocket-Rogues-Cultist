using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace OccultSanctum
{
    /// (the user, 2026-11-22: "the rooms look very strange - the stores, the rooms with rugs, the ones with the stones
    /// and offerings - redo their level design so they look better") the rooms laid out anew by a few rules:
    ///  - one thing that matters stands at the far wall, on the room's axis (the statue, the altar, the lectern, the
    ///    desk, the workbench);
    ///  - a carpet or a clear walk leads from the door to it, and nothing stands on it;
    ///  - the two halves of the room are alike: whatever stands by the left wall stands by the right one at the same
    ///    depth;
    ///  - things stand square in rows, every row of one kind; nothing is strewn about the middle.
    public static partial class SideRoom
    {
        static readonly HashSet<Vector2Int> clear = new HashSet<Vector2Int>();
        static bool onCarpet;

        /// the room's axis, from the door's wall to the far wall (between two tiles when the room is even wide)
        static float Cu => (lat - 1) / 2f;
        static int AxL => lat % 2 == 1 ? lat / 2 : lat / 2 - 1;          // the axis tile (or the left of the two)
        static int AxR => lat / 2;                                       // (or the right one)

        static bool Planned(string role, int far, int mid)
        {
            switch (role)
            {
                case "library": Library(far); return true;
                case "study": Study(far); return true;
                case "shrine": Shrine(far); return true;
                case "tomb": Tomb(far); return true;
                case "sanctuary": Sanctuary(far); return true;
                case "alchemy": Alchemy(far); return true;
                case "ossuary": Ossuary(far); return true;
                case "summoning": Summoning(far); return true;
                case "warehouse": Store(far, 2); return true;
                case "pantry": Store(far, 1); return true;
                case "store": Store(far, 0); return true;
                case "refectory": Refectory(far); return true;
            }
            return false;
        }

        // ------------------------------------------------------------------ helpers
        /// these tiles and their mirror twins across the axis, taken all at once; put gets the middle of each group
        /// and its side (-1 the left one, +1 its twin; 0 when the group stands on the axis itself)
        static bool Sym(Action<Vector2, int> put, params (int u, int v)[] ts)
        {
            var tw = ts.Select(t => (u: lat - 1 - t.u, t.v)).ToArray();
            bool self = ts.All(t => tw.Contains(t)) && tw.All(t => ts.Contains(t));
            if (!TakeAll(self ? ts : ts.Concat(tw).ToArray())) return false;
            Vector2 Mid((int u, int v)[] g) { var s = Vector2.zero; foreach (var t in g) s += T(t.u, t.v); return s / g.Length; }
            if (self) put(Mid(ts), 0);
            else { put(Mid(ts), -1); put(Mid(tw), 1); }
            return true;
        }
        static bool Pair2(int u, int v, Action<Vector2> put) => Sym((p, s) => put(p), (u, v));

        /// the axis tiles at depth v (one or two), taken even on the carpet: the thing that matters stands there
        static bool Focal(int v, Action<Vector2> put)
        {
            onCarpet = true;
            bool ok = AxL == AxR ? Take(AxL, v) : TakeAll((AxL, v), (AxR, v));
            onCarpet = false;
            if (ok) put(T(Cu, v));
            return ok;
        }
        /// three tiles across the axis at depth v (an altar, a sacrifice slab), even on the carpet
        static bool Focal3(int v, Action<Vector2> put)
        {
            onCarpet = true;
            bool ok = lat % 2 == 1 ? TakeAll((AxL - 1, v), (AxL, v), (AxL + 1, v)) : TakeAll((AxL - 1, v), (AxL, v), (AxR, v), (AxR + 1, v));
            onCarpet = false;
            if (ok) put(T(Cu, v));
            return ok;
        }

        /// a red carpet three tiles wide along the axis from depth v0 to v1; nothing is put on it afterwards
        static void Carpet(int v0, int v1)
        {
            if (lat < 3 || v1 < v0) return;
            for (int v = v0; v <= v1; v++)
            {
                // (the user: no bits of carpet anywhere) only the walk is kept clear, nothing is drawn
                for (int u = 0; u < lat; u++) if (Mathf.Abs(u - Cu) <= 1.5f) clear.Add(new Vector2Int(u, v));
            }
        }
        /// a walk kept clear (no carpet drawn): the axis tiles from v0 to v1, and as many beside them as wide
        static void Walk(int v0, int v1, float halfWidth)
        {
            for (int v = v0; v <= v1; v++) for (int u = 0; u < lat; u++) if (Mathf.Abs(u - Cu) <= halfWidth) clear.Add(new Vector2Int(u, v));
        }
        static void WalkCol(int u, int v0, int v1) { for (int v = v0; v <= v1; v++) clear.Add(new Vector2Int(u, v)); }

        /// the rotation of a thing standing against a side wall, its front to the room
        static float SideRot(int side) => side < 0 ? FaceDoor - 90f : FaceDoor + 90f;

        static void Goods(string name, Vector2 p) { var pr = Prefabs.Cont(name); if (pr != null) Builder.Own(Builder.Spawn(pr, p, 0f)); }

        static void SigilAt(Vector2 p)
        {
            var s = Builder.Sr("occ_sigil_" + rnd.Next(3), p, Builder.floorLayer, Builder.floorOrder + 3, Builder.floorMat ?? Builder.litMat);
            s.transform.rotation = Quaternion.Euler(0, 0, FaceDoor);
        }

        // ------------------------------------------------------------------ the rooms
        /// a reading room (no bookcases - the user): the lectern before a statue at the far wall, candle stands at
        /// its sides, a carpet up to it; reading desks against both side walls, a bench at each
        static void Library(int far)
        {
            bool desks = lat >= 7 && dep >= 6;
            Carpet(0, far - 2);
            Focal(far, p => Breakables.Statue(p, FaceDoor));
            Vector2 lec = default; bool hasLec = Focal(far - 1, p => { Breakables.Lectern(p); lec = p; });
            if (AxL - 1 >= 0) Sym((p, s) => Breakables.Candelabra(p), (AxL - 1, far));
            Sym((p, s) => Breakables.Candles(p), (0, far));
            if (desks)
                for (int v = 2; v + 1 <= far - 2; v += 3)
                {
                    Vector2 tl = default, tr = default;
                    if (!Sym((p, s) => { Breakables.Table(rnd.Next(2), p, SideRot(s)); if (s < 0) tl = p; else tr = p; }, (0, v), (0, v + 1))) continue;
                    var seats = new List<(Vector2 b, Vector2 t)>();
                    Sym((p, s) => { Breakables.Bench(p, SideRot(s)); seats.Add((p, s < 0 ? tl : tr)); }, (1, v), (1, v + 1));
                    foreach (var (b, t) in seats) if (rnd.Next(2) == 0) Activity.Give(Cultist.Spawn(b), Activity.Kind.Sit, t);
                }
            else
                for (int v = 2; v < far - 1; v += 2) Pair2(0, v, p => Breakables.Candles(p));
            if (hasLec) Activity.Give(Cultist.Spawn(T(Cu, far - 2)), Activity.Kind.Read, lec);
        }

        /// a scholar's study: the desk across the axis by the far wall, candle stands at its ends, cupboards in the
        /// far corners, a carpet to the desk; the scholar at his desk
        static void Study(int far)
        {
            Carpet(0, Mathf.Max(0, far - 3));
            Vector2 desk = default; bool hasDesk = false;
            onCarpet = true;
            if (lat >= 4 && TakeAll((AxR - 1, far - 1), (AxR, far - 1))) { desk = (T(AxR - 1, far - 1) + T(AxR, far - 1)) / 2f; Breakables.Table(rnd.Next(2), desk, FaceDoor); hasDesk = true; }
            onCarpet = false;
            if (lat >= 6) Sym((p, s) => Breakables.Candelabra(p), (AxR - 2, far - 1));
            string fur = rnd.Next(2) == 0 ? "Dresser" : "Cupboard";
            Sym((p, s) => Furniture(fur, p), (0, far));
            if (lat >= 5) Sym((p, s) => Breakables.SkullPile(p), (1, far));
            for (int v = 2; v < far - 1; v += 2) Pair2(0, v, p => Breakables.Candles(p));
            if (hasDesk) Activity.Give(Cultist.Spawn(T(Cu, far)), Activity.Kind.Read, desk);
            if (rnd.Next(2) == 0) { var w = Activity.Give(Crossbowman.Spawn(T(Cu, 1)), Activity.Kind.Wander, T(Cu, dep / 2f)); if (w != null) w.Radius = Mathf.Min(lat, dep) / 2f - 1f; }
        }

        /// a shrine: the hooded statue at the far wall, offerings (skulls with candles) on both sides of it, candles
        /// and candle stands in a row, a sigil before it where the faithful kneels, a carpet up to the sigil
        static void Shrine(int far)
        {
            int sv = Mathf.Max(1, far - 2);
            Carpet(0, sv - 1);
            bool st = Focal(far, p => Breakables.Statue(p, FaceDoor));
            if (AxL - 1 >= 0) Sym((p, s) => Breakables.SkullPile(p), (AxL - 1, far));
            if (AxL - 2 >= 0) Sym((p, s) => Breakables.Candelabra(p), (AxL - 2, far));
            if (AxL - 1 >= 0) Sym((p, s) => Breakables.Candles(p), (AxL - 1, far - 1));
            for (int v = 2; v < sv; v += 2) Pair2(0, v, p => Breakables.Candles(p));
            SigilAt(T(Cu, sv));
            if (st) Activity.Give(Cultist.Spawn(T(Cu, sv)), Activity.Kind.Pray, T(Cu, far));
        }

        /// a tomb: the statue at the far wall, the tombs in two rows facing the carpet between them, a candle at the
        /// head of each
        static void Tomb(int far)
        {
            Carpet(0, far - 1);
            bool st = Focal(far, p => Breakables.Statue(p, FaceDoor));
            int tu = Mathf.Max(0, AxL - 2);
            for (int v = 1; v < far; v += 2)
            {
                string k = rnd.Next(3) == 0 ? "TombObjectEmpty" : "SkeletonTombs";
                if (Sym((p, s) => Goods(k, p), (tu, v)) && tu > 0) Sym((p, s) => Breakables.Candles(p), (tu - 1, v));
            }
            Sym((p, s) => Breakables.Candelabra(p), (0, far));
            if (st) Activity.Give(Cultist.Spawn(T(Cu, far - 1)), Activity.Kind.Pray, T(Cu, far));
        }

        /// the inner sanctum: a great sigil in the middle in a full ring of candles, two faithful kneeling at it,
        /// hooded statues in the far corners, candle stands by the door, a carpet up to the ring
        static void Sanctuary(int far)
        {
            int cv = Mathf.Clamp(dep / 2, 2, far - 1);
            if (cv - 3 >= 0) Carpet(0, cv - 3);
            SigilAt(T(Cu, cv));
            onCarpet = true;
            foreach (var (du, dv) in new[] { (-2f, 0f), (2f, 0f), (0f, 2f), (-1.4f, 1.4f), (1.4f, 1.4f), (-1.4f, -1.4f), (1.4f, -1.4f) })
            {
                int u = Mathf.RoundToInt(Cu + du), v = Mathf.RoundToInt(cv + dv);
                if (Take(u, v)) Breakables.Candles(T(Cu + du, cv + dv));
            }
            onCarpet = false;
            Sym((p, s) => Breakables.Statue(p, FaceDoor), (0, far));
            Sym((p, s) => Breakables.Candelabra(p), (0, 1));
            if (lat >= 7) for (int v = 3; v < far; v += 3) Pair2(0, v, p => Breakables.Candles(p));
            Activity.Give(Cultist.Spawn(T(Cu, cv - 1)), Activity.Kind.Pray, T(Cu, cv));
            if (lat >= 5) Activity.Give(Cultist.Spawn(T(Cu, cv + 1)), Activity.Kind.Pray, T(Cu, cv));
        }

        /// an alchemist's room: the workbench across the axis at the far wall with a red fire on each side, jugs in
        /// neat rows along both side walls, skulls in the near corners; the alchemist at his bench
        static void Alchemy(int far)
        {
            Walk(0, far - 2, 0.5f);
            Vector2 bench = default; bool has = false;
            onCarpet = true;
            if (lat >= 4 && TakeAll((AxR - 1, far), (AxR, far))) { bench = (T(AxR - 1, far) + T(AxR, far)) / 2f; Breakables.Table(rnd.Next(2), bench, FaceDoor); has = true; }
            onCarpet = false;
            if (lat >= 6) Sym((p, s) => Breakables.RedBrazier(p), (AxR - 2, far));
            Sym((p, s) => Breakables.RedBrazier(p), (0, far));
            for (int v = 1; v < far; v++) Pair2(0, v, p => Goods(v % 3 == 0 ? "Barrel" : "Jug", p));
            if (has) Activity.Give(Cultist.Spawn(T(Cu, far - 1)), Activity.Kind.Work, bench);
            { var w = Activity.Give(Cultist.Spawn(T(Cu, 1)), Activity.Kind.Wander, T(Cu, dep / 2f)); if (w != null) w.Radius = Mathf.Min(lat, dep) / 2f - 1.5f; }
        }

        /// an ossuary: the altar across the axis at the far wall between candle stands, skull heaps in pairs along
        /// both walls, old bones by the walls, the blood before the altar
        static void Ossuary(int far)
        {
            Walk(0, far - 2, 0.5f);
            bool alt = lat >= 5 && Focal3(far, p => Breakables.Altar(p));
            if (alt && AxL - 2 >= 0) Sym((p, s) => Breakables.Candelabra(p), (AxL - 2, far));
            for (int v = 2; v < far; v += 2) Pair2(0, v, p => Breakables.SkullPile(p));
            for (int v = 1; v < far; v += 2) { Floor("occ_bones_" + rnd.Next(3), 0.6f, v + 0.5f); Floor("occ_bones_" + rnd.Next(3), lat - 1.6f, v + 0.5f); }
            Blood(Cu, far - 1);
            if (alt) Activity.Give(Cultist.Spawn(T(Cu, far - 1)), Activity.Kind.Pray, T(Cu, far));
            if (rnd.Next(2) == 0) Activity.Give(Spearman.Spawn(T(Cu, 1)), Activity.Kind.Guard, T(doorU, -2));
        }

        /// a summoning: a great sigil in the middle between four red fires, two faithful at it, skull offerings in
        /// the far corners, candle stands in the near ones
        static void Summoning(int far)
        {
            int cv = Mathf.Clamp(dep / 2, 2, far - 1);
            SigilAt(T(Cu, cv));
            onCarpet = true;
            foreach (var (du, dv) in new[] { (-2f, -2f), (2f, -2f), (-2f, 2f), (2f, 2f) })
            {
                int u = Mathf.RoundToInt(Cu + du), v = Mathf.RoundToInt(cv + dv);
                if (Take(u, v)) Breakables.RedBrazier(T(Cu + du, cv + dv));
            }
            onCarpet = false;
            Sym((p, s) => Breakables.SkullPile(p), (0, far));
            Sym((p, s) => Breakables.Candelabra(p), (0, 1));
            for (int i = 0; i < 2; i++) Blood(Cu + R(-1f, 1f), cv + R(-1f, 1f));
            Activity.Give(Cultist.Spawn(T(Cu, cv - 1)), Activity.Kind.Pray, T(Cu, cv));
            Activity.Give(Cultist.Spawn(T(Cu, cv + 1)), Activity.Kind.Pray, T(Cu, cv));
        }

        /// a refectory: long tables across the room in rows, a bench on each side, barrels in the far corners, the
        /// cook's table at the far wall; a walk down the axis
        static void Refectory(int far)
        {
            for (int v = 2; v + 1 < far; v += 4)
                for (int u = 0; u + 1 < AxL; u += 3)
                {
                    Vector2 tl = default, tr = default; var seats = new List<(Vector2 b, Vector2 t)>();
                    if (!Sym((p, s) => { Breakables.Table(rnd.Next(2), p, FaceDoor); if (s <= 0) tl = p; else tr = p; }, (u, v), (u + 1, v))) continue;
                    Sym((p, s) => { Breakables.Bench(p, FaceDoor); seats.Add((p, s <= 0 ? tl : tr)); }, (u, v - 1), (u + 1, v - 1));
                    Sym((p, s) => { Breakables.Bench(p, FaceDoor + 180f); seats.Add((p, s <= 0 ? tl : tr)); }, (u, v + 1), (u + 1, v + 1));
                    foreach (var (b, t) in seats) if (rnd.Next(3) > 0) Activity.Give(rnd.Next(4) == 0 ? Spearman.Spawn(b) : Cultist.Spawn(b), Activity.Kind.Sit, t);
                }
            Vector2 block = default; bool has = false;
            if (lat >= 6 && TakeAll((AxR - 1, far), (AxR, far))) { block = (T(AxR - 1, far) + T(AxR, far)) / 2f; Breakables.Table(rnd.Next(2), block, FaceDoor); has = true; }
            Sym((p, s) => Goods("Barrel", p), (0, far));
            if (lat >= 6) Sym((p, s) => Goods("BarrelAlt", p), (1, far));
            if (has) { Blood(Cu, far); Activity.Give(Cultist.Spawn(T(Cu, far - 1)), Activity.Kind.Work, block); }
        }

        /// a store (kind 0), a pantry (1), a warehouse (2): a walk down the middle from the door to the far wall and
        /// a walk along each side; the goods stand in straight rows along the walls, one kind to a wall; a big store
        /// has pallets of one kind of goods each (2 x 2) in rows between the walks, alike on both sides; heaps of
        /// logs and crates at the far wall; the storekeeper's table by the door
        static void Store(int far, int kind)
        {
            string[][] walls = { new[] { "Barrel", "BarrelAlt" }, new[] { "Box", "Box2", "Box3" }, new[] { "GroundBag" }, new[] { "Jug", "Jug", "Barrel" } };
            string[][] pallet = { new[] { "Box" }, new[] { "Barrel" }, new[] { "Box2" }, new[] { "GroundBag" }, new[] { "BarrelAlt" }, new[] { "Box3" } };
            int g = rnd.Next(walls.Length);
            Webs(2);
            Walk(0, far - 1, lat >= 8 ? 1f : 0.5f);                                   // the main walk
            bool sideWalks = lat >= 7;
            if (sideWalks) { WalkCol(1, 1, far - 1); WalkCol(lat - 2, 1, far - 1); }
            // the side walls: a straight row each, one kind to a wall
            {
                var lw = walls[g % walls.Length]; var rw = walls[(g + 1) % walls.Length];
                for (int v = 1; v < far; v++)
                {
                    int vv = v;
                    if (TakeAll((0, v), (lat - 1, v)))
                    { Goods(lw[vv % lw.Length], T(0, v)); Goods(rw[vv % rw.Length], T(lat - 1, v)); }
                }
            }
            // the far wall: heaps, alike on both sides
            string[] heaps = { "Logs", "GroundBoxes", "GroundWoods" };
            if (kind == 1)
            {
                Vector2 block = default; bool has = false;
                onCarpet = true;
                if (lat >= 5 && dep >= 5 && TakeAll((AxR - 1, far - 1), (AxR, far - 1))) { block = (T(AxR - 1, far - 1) + T(AxR, far - 1)) / 2f; Breakables.Table(rnd.Next(2), block, FaceDoor); has = true; }
                onCarpet = false;
                for (int u = 0; u < AxL; u++) Sym((p, s) => Goods(u % 2 == 0 ? "Jug" : "Barrel", p), (u, far));
                if (has) { Blood(Cu, far - 1); Activity.Give(Cultist.Spawn(T(Cu, far - 2)), Activity.Kind.Work, block); }
            }
            else
            {
                for (int u = 0; u < AxL; u++) { var h = heaps[u % heaps.Length]; Sym((p, s) => Goods(h, p), (u, far)); }
                onCarpet = true; Focal(far, p => Goods("GroundBoxes", p)); onCarpet = false;
            }
            // a warehouse: pallets between the walks, two deep, a cross walk after each row
            if (kind == 2)
            {
                int left0 = sideWalks ? 2 : 1, aisle = Mathf.FloorToInt(Cu - (lat >= 8 ? 1f : 0.5f));
                int pk = rnd.Next(pallet.Length);
                for (int v = 2; v + 1 <= far - 2; v += 3)
                    for (int u = left0; u + 1 <= aisle; u += 3)
                    {
                        var pl = pallet[pk++ % pallet.Length];
                        Sym((p, s) => { }, (u, v), (u + 1, v), (u, v + 1), (u + 1, v + 1));
                        foreach (var (du, dv) in new[] { (0, 0), (1, 0), (0, 1), (1, 1) })
                        {
                            if (!used.Contains(new Vector2Int(u + du, v + dv))) continue;
                            Goods(pl[0], T(u + du, v + dv)); Goods(pl[0], T(lat - 1 - u - du, v + dv));
                        }
                    }
            }
            // the storekeeper's table beside the walk by the door
            if (lat >= 6)
            {
                int tu = Mathf.FloorToInt(Cu) - (lat >= 8 ? 3 : 2);
                if (tu >= 1 && TakeAll((tu, 1), (tu + 1, 1)))
                {
                    var t = (T(tu, 1) + T(tu + 1, 1)) / 2f;
                    Breakables.Table(rnd.Next(2), t, FaceDoor);
                    Activity.Give(Cultist.Spawn(T(tu + 0.5f, 2)), Activity.Kind.Guard, t);
                }
            }
            // the workers walk the store; a warehouse is kept by a guard pair
            if (lat >= 5 && dep >= 5) Activity.Give(Cultist.Spawn(T(Cu, 1)), Activity.Kind.Patrol, default, T(Cu, 1.5f), T(Cu, Mathf.Max(2, far - 1)));
            if (kind == 2) Pair(Crossbowman.Spawn(T(Cu, 2)), Spearman.Spawn(T(Cu, 1)), T(Cu, 2), T(Cu, Mathf.Max(2, far - 1)));
        }
    }
}
