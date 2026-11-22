using UnityEngine;

namespace OccultSanctum
{
    /// a dummy, a target or a chopping block rocks when it is struck (and settles back), so one sees the blows land
    public class Wobble : MonoBehaviour
    {
        float baseZ, amp, t = 99f; bool set;

        public static void Hit(Container c, float deg)
        {
            if (c == null || c.hp <= 0f || !c.gameObject.activeInHierarchy) return;
            var w = c.GetComponent<Wobble>() ?? c.gameObject.AddComponent<Wobble>();
            if (!w.set) { w.set = true; w.baseZ = c.transform.eulerAngles.z; }
            w.amp = deg * (Random.value < 0.5f ? 1f : -1f); w.t = 0f;
        }

        void Update()
        {
            if (!set || t > 0.8f) return;
            t += Time.deltaTime;
            float a = t > 0.8f ? 0f : amp * Mathf.Exp(-5f * t) * Mathf.Cos(t * 26f);
            transform.rotation = Quaternion.Euler(0f, 0f, baseZ + a);
        }
    }
}
