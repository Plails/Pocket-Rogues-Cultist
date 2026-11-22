using UnityEngine;

namespace FlagellantMod
{
    /// (the user's list) a plain whip in the Flagellant's empty right hand, only to look at - it is in no inventory and
    /// shows only while that hand holds nothing: a short dark handle with a metal pommel (outlined like the game's
    /// weapons) and a curved leather tail hanging off it, thinner towards its tip, that sways and drags after him as he
    /// walks (a little rope of points). Hidden while a lash of his skills is out.
    internal class IdleWhip : MonoBehaviour
    {
        const int N = 10;                       // points of the tail
        const float Step = 0.055f;              // between them
        Character ch;
        GameObject root;
        SpriteRenderer handle;
        readonly SpriteRenderer[] segs = new SpriteRenderer[N - 1];
        readonly Vector2[] p = new Vector2[N], old = new Vector2[N];
        bool placed;

        void Start()
        {
            ch = GetComponent<Character>();
            var hs = Art.Get("whip", "idle_handle", 42f);
            var seg = Art.Get("whip", "leather_seg", 42f);
            if (ch == null || hs == null || seg == null) { Destroy(this); return; }
            root = new GameObject("Flagellant_idleWhip");
            handle = Piece(hs, 2);
            for (int i = 0; i < segs.Length; i++) segs[i] = Piece(seg, -1);
        }

        SpriteRenderer Piece(Sprite s, int order)
        {
            var g = new GameObject("piece"); g.transform.SetParent(root.transform, false);
            var r = g.AddComponent<SpriteRenderer>(); r.sprite = s; r.sortingOrder = order;
            return r;
        }

        bool Shown()
        {
            if (ch == null || !ch.alive || !ch.gameObject.activeInHierarchy) return false;
            if (FV.FxWhip.Out > 0) return false;                                              // a lash of his skills is out
            try { if (ch.Weapon != null) return false; } catch { }
            try { if (ch.RightHandSprite == null || !ch.RightHandSprite.enabled) return false; } catch { return false; }
            return true;
        }

        void LateUpdate()
        {
            if (root == null) return;
            bool on = Shown();
            if (root.activeSelf != on) { root.SetActive(on); if (on) placed = false; }
            if (!on) return;
            var hand = ch.RightHandSprite;
            int layer = hand.sortingLayerID, ord = hand.sortingOrder;
            handle.sortingLayerID = layer; handle.sortingOrder = ord + 2;
            foreach (var s in segs) { s.sortingLayerID = layer; s.sortingOrder = ord - 1; }

            Vector2 fwd = ch.transform.right, side = new Vector2(-fwd.y, fwd.x);
            Vector2 h = hand.transform.position;
            // the handle: in the fist, pointing forward and a little out to his right
            Vector2 hd = (fwd - side * 0.45f).normalized;
            handle.transform.position = h + hd * 0.05f;
            handle.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(hd.y, hd.x) * Mathf.Rad2Deg);
            Vector2 anchor = h + hd * 0.11f;

            if (!placed)
            {
                placed = true;
                for (int i = 0; i < N; i++) { p[i] = anchor - fwd * Step * i + side * Step * 0.4f * i; old[i] = p[i]; }
            }
            // the tail: it hangs back and a little to the side (seen from above it lies along the floor behind him),
            // sways, keeps its speed (so it drags after him), and every link keeps its length
            float dt = Mathf.Min(Time.deltaTime, 0.05f);
            Vector2 pull = (-fwd * 0.9f - side * 0.35f) * 6f + side * Mathf.Sin(Time.time * 3.1f) * 0.6f;
            for (int i = 1; i < N; i++)
            {
                Vector2 v = (p[i] - old[i]) * 0.86f;
                old[i] = p[i];
                p[i] += v + pull * dt * dt;
            }
            p[0] = anchor;
            for (int it = 0; it < 6; it++)
            {
                p[0] = anchor;
                for (int i = 0; i < N - 1; i++)
                {
                    Vector2 d = p[i + 1] - p[i]; float l = d.magnitude;
                    if (l < 1e-5f) continue;
                    Vector2 c = d * ((l - Step) / l);
                    if (i == 0) p[i + 1] -= c;
                    else { p[i] += c * 0.5f; p[i + 1] -= c * 0.5f; }
                }
            }
            // the links: each along its piece, the tail thinner towards the tip
            float segLen = 8f / 42f;
            for (int i = 0; i < N - 1; i++)
            {
                Vector2 a = p[i], b = p[i + 1], d = b - a;
                var t = segs[i].transform;
                t.position = (a + b) * 0.5f;
                t.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
                float f = i / (float)(N - 2);
                t.localScale = new Vector3(d.magnitude / segLen * 1.15f, Mathf.Lerp(0.85f, 0.4f, f), 1f);
            }
        }

        void OnDestroy() { if (root != null) Destroy(root); }
        void OnDisable() { if (root != null) root.SetActive(false); }
    }
}
