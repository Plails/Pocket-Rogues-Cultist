using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace OccultSanctum
{
    /// (the user, 2026-11-22, with screenshots of near-empty rooms: "make the rooms look good, full, as if a cult
    /// really lived there") after its plan, every room is dressed along its walls: the side walls lined to the far
    /// wall with the things such a room has, alike on both sides, in a steady rhythm (a candle stand, a heap of
    /// skulls with candles, a statue, ...); the far wall filled out to the corners; candle stands by the door; a
    /// ritual room's middle, when free, gets a sigil in a ring of candles; blood and old bones on the floor where
    /// the cult does its work. The walks stay clear and every tile can still be reached (Take/Open).
    public static partial class SideRoom
    {
        enum Look { Ritual, Scholar, Store, Military, Prison, Hall }

        static Look LookOf(string role)
        {
            switch (role)
            {
                case "shrine": case "sanctuary": case "tomb": case "summoning": case "ossuary": case "gallery": return Look.Ritual;
                case "library": case "study": case "scriptorium": case "research": case "council": case "alchemy": return Look.Scholar;
                case "store": case "pantry": case "warehouse": return Look.Store;
                case "barracks": case "armory": case "range": case "guardpost": case "training": case "forge": return Look.Military;
                case "cell": case "prison": case "execution": return Look.Prison;
                default: return Look.Hall;
            }
        }

        /// one thing of the room's kind at p, standing against a wall whose side is s (-1 left, +1 right, 0 far)
        static void Piece(Look look, int k, Vector2 p, int s)
        {
            float rot = s == 0 ? FaceDoor : SideRot(s);
            switch (look)
            {
                case Look.Ritual:
                    switch (k % 4) { case 0: Breakables.Candelabra(p); break; case 1: Breakables.SkullPile(p); break; case 2: Breakables.Candles(p); break; default: Breakables.Statue(p, rot); break; }
                    break;
                case Look.Scholar:
                    switch (k % 4) { case 0: Breakables.Candelabra(p); break; case 1: Furniture2(k % 8 == 1 ? "Cupboard" : "Dresser", p, rot); break; case 2: Breakables.Candles(p); break; default: Breakables.Lectern(p); break; }
                    break;
                case Look.Store:
                    Goods(new[] { "Barrel", "Box", "GroundBag", "Box2", "BarrelAlt", "Jug" }[k % 6], p);
                    break;
                case Look.Military:
                    switch (k % 4) { case 0: Goods("Box", p); break; case 1: Goods("Barrel", p); break; case 2: Breakables.Candelabra(p); break; default: Goods("GroundBag", p); break; }
                    break;
                case Look.Prison:
                    switch (k % 4) { case 0: Breakables.SkullPile(p); break; case 1: Goods("Barrel", p); break; case 2: Breakables.Candles(p); break; default: Goods("Box", p); break; }
                    break;
                default:
                    switch (k % 4) { case 0: Breakables.Candelabra(p); break; case 1: Goods("Barrel", p); break; case 2: Breakables.Candles(p); break; default: Goods("Jug", p); break; }
                    break;
            }
        }

        /// furniture against a side wall: its back to that wall
        static void Furniture2(string name, Vector2 p, float rot)
        {
            var pr = Prefabs.Cont(name); if (pr == null) return;
            var go = Builder.Spawn(pr, p, rot); Builder.Own(go);
            if (go != null) foreach (var c in go.GetComponentsInChildren<Container>(true)) { Breakables.Pin(c); c.playDamageAnimation = true; }
        }

        /// a pair of racks along both side walls (two tiles each)
        static bool RackPair(int v, bool xbow)
            => Sym((p, s) => { if (xbow) Breakables.RackX(p, SideRot(s)); else Breakables.Rack(p, SideRot(s)); }, (0, v), (0, v + 1));

        static void Dress(string role, int far)
        {
            var look = LookOf(role);
            int k = rnd.Next(4), placed = 0;
            // the side walls, from the door to the far wall, alike on both sides
            for (int v = 1; v < far; v++)
            {
                if (look == Look.Military && v + 1 < far && v % 3 == 1 && RackPair(v, rnd.Next(2) == 0)) { v++; placed += 4; continue; }
                if (Sym((p, s) => Piece(look, k, p, s), (0, v))) { placed += 2; k++; }
            }
            // the far wall, out to the corners
            for (int u = 0; u < AxL; u++)
                if (Sym((p, s) => Piece(look, k, p, 0), (u, far))) { placed += 2; k++; }
            // a second row inside the first in a big store or a big ritual room
            if ((look == Look.Store || look == Look.Ritual) && lat >= 9 && dep >= 7)
                for (int v = 2; v < far - 1; v += 2)
                    if (Sym((p, s) => { if (look == Look.Store) Goods(new[] { "Box", "Barrel", "Box3" }[v % 3], p); else Breakables.Candles(p); }, (2, v))) placed += 2;
            // candle stands by the door
            Sym((p, s) => Breakables.Candelabra(p), (0, 0));
            // the middle of a ritual room, if free: a sigil in a ring of candles
            if (look == Look.Ritual && lat >= 7 && dep >= 7)
            {
                int cv = dep / 2; bool free = true;
                for (int u = AxL - 1; u <= AxR + 1 && free; u++) for (int v = cv - 1; v <= cv + 1 && free; v++) if (used.Contains(new Vector2Int(u, v))) free = false;
                if (free && Sym((p, s) => Breakables.Candles(p), (AxL - 1, cv - 1)) && Sym((p, s) => Breakables.Candles(p), (AxL - 1, cv + 1))) SigilAt(T(Cu, cv));
            }
            // the floor where the cult works
            if (look == Look.Prison || look == Look.Ritual) for (int i = 0; i < 2; i++) Blood(R(1, lat - 2), R(1, far - 1));
            if (look == Look.Prison || role == "ossuary" || role == "tomb")
                for (int v = 1; v < far; v += 2) { Floor("occ_bones_" + rnd.Next(3), 1.2f, v + 0.3f); Floor("occ_bones_" + rnd.Next(3), lat - 2.2f, v + 0.3f); }
            if (look == Look.Store || look == Look.Military) Webs(2);
            SanctumPlugin.Log.LogInfo("Sanctum: room " + role + " dressed (" + placed + " things along its walls)");
        }
    }
}
