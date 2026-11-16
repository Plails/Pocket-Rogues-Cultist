using System;
using UnityEngine;

namespace OccultSanctum
{
    /// the cultists and the crossbowmen are people: they cry out, die and bleed like the game's own human cultist
    /// (the renegade), not like the goblin and the orc they are built on - his voices and his red blood are copied
    public static class Human
    {
        static Mob donor; static bool looked;

        static Mob Donor()
        {
            if (!looked)
            {
                looked = true;
                var p = Prisoner.FindMob(new[] { "CultistRenegade", "Cultist_Renegade" }, "Cultist");
                donor = p != null ? p.GetComponent<Mob>() : null;
                SanctumPlugin.Log.LogInfo("Sanctum: human voices and blood from " + (donor != null ? donor.name : "nobody (not found)"));
            }
            return donor;
        }

        public static void Apply(Mob m)
        {
            var d = Donor();
            if (d == null || m == null) return;
            try
            {
                if (d.GetDamageSound != null && d.GetDamageSound.Length > 0) m.GetDamageSound = d.GetDamageSound;
                if (d.DeathSound != null) m.DeathSound = d.DeathSound;
                if (d.SeeTargetSound != null) m.SeeTargetSound = d.SeeTargetSound;
                if (d.StepsSound != null && d.StepsSound.Length > 0) m.StepsSound = d.StepsSound;
                m.bloodType = d.bloodType;
                if (d.bloodParticle != null) m.bloodParticle = d.bloodParticle;
                if (d.bloodParticleDirected != null) m.bloodParticleDirected = d.bloodParticleDirected;
                m.cachedBloodParticle = null; m.cachedBloodParticleDirected = null;
            }
            catch (Exception e) { SanctumPlugin.Log.LogWarning("Sanctum human: " + e.Message); }
        }
    }
}
