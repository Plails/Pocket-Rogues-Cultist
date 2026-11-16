using HarmonyLib;
using UnityEngine;

namespace OccultSanctum
{
    public static class Patches
    {
        static readonly AccessTools.FieldRef<AIController, Transform> targetRef = AccessTools.FieldRefAccess<AIController, Transform>("_target");
        public static Transform Target(AIController ai) { try { return ai != null ? targetRef(ai) : null; } catch { return null; } }
    }
}
