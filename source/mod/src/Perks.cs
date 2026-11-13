using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using I2.Loc;
using UnityEngine;

namespace FlagellantMod
{
    /// The Cultist's own class perks, offered instead of the Berserker's ones (MRX's design):
    ///  Sinful Vessel - for every missing armour piece (helmet, body armour): more max HP; from lvl 3 less damage taken;
    ///                  at lvl 5 +1.5 HP/s regeneration.
    ///  Blood Feast   - standing in a blood puddle (his own or an enemy's): enemies' bleeding bar fills faster;
    ///                  from lvl 3 faster attacks; at lvl 5 it works in poison and plasma puddles too.
    /// They are real perks of the game's perk database (appended at its end, so the game's own perk ids stay the same):
    /// the game offers them on level-up, shows their cards, levels and saves them like any other perk.
    internal static class Perks
    {
        public const PassiveSkill.Types Vessel = (PassiveSkill.Types)7000;
        public const PassiveSkill.Types Feast = (PassiveSkill.Types)7001;

        // own effect ids: the game only stores them, the mod does the work (texts come from customTerm)
        const PassiveSkillEffect.EffectTypes E_VesselHp = (PassiveSkillEffect.EffectTypes)7100;
        const PassiveSkillEffect.EffectTypes E_VesselDef = (PassiveSkillEffect.EffectTypes)7101;
        const PassiveSkillEffect.EffectTypes E_VesselRegen = (PassiveSkillEffect.EffectTypes)7102;
        const PassiveSkillEffect.EffectTypes E_FeastBleed = (PassiveSkillEffect.EffectTypes)7110;
        const PassiveSkillEffect.EffectTypes E_FeastSpeed = (PassiveSkillEffect.EffectTypes)7111;
        const PassiveSkillEffect.EffectTypes E_FeastAll = (PassiveSkillEffect.EffectTypes)7112;

        // perk level 1..5 (index 0..4); levels 4 and 5 need the fortress's perk building, like every perk
        public static readonly int[] VesselHp = { 8, 12, 15, 18, 20 };
        public static readonly int[] VesselDef = { 0, 0, 10, 15, 20 };
        public const float VesselRegen = 1.5f;              // per missing piece, level 5
        public static readonly int[] FeastBleed = { 10, 20, 30, 40, 50 };
        public static readonly int[] FeastSpeed = { 0, 0, 15, 20, 25 };

        static PassiveSkill vessel, feast;

        public static bool IsOurs(PassiveSkill p) => p != null && (p.type == Vessel || p.type == Feast);

        public static void Build()
        {
            var db = Traverse.Create(typeof(PassiveSkillsDatabase)).Property("Instance").GetValue<PassiveSkillsDatabase>();
            if (db == null) { FlagellantPlugin.Log.LogWarning("Perks: no perk database"); return; }
            var tr = Traverse.Create(db);
            var arr = tr.Field("skillsDB").GetValue<PassiveSkill[]>() ?? new PassiveSkill[0];
            var wrapped = tr.Field("skillsDBWrapped").GetValue<PassiveSkillWrapper[]>();
            bool migrated = tr.Field("migrationCompleted").GetValue<bool>();
            if (arr.Any(IsOurs) || (wrapped != null && wrapped.Any(w => w != null && IsOurs(w.skill)))) return;   // already there

            vessel = Make(Vessel, "perk_vessel", new[]
            {
                new[] { Eff(E_VesselHp, 8, "FLG/Perk/vesselHp") },
                new[] { Eff(E_VesselHp, 12, "FLG/Perk/vesselHp") },
                new[] { Eff(E_VesselHp, 15, "FLG/Perk/vesselHp"), Eff(E_VesselDef, 10, "FLG/Perk/vesselDef") },
                new[] { Eff(E_VesselHp, 18, "FLG/Perk/vesselHp"), Eff(E_VesselDef, 15, "FLG/Perk/vesselDef") },
                new[] { Eff(E_VesselHp, 20, "FLG/Perk/vesselHp"), Eff(E_VesselDef, 20, "FLG/Perk/vesselDef"), Eff(E_VesselRegen, 15, "FLG/Perk/vesselRegen") },
            });
            feast = Make(Feast, "perk_feast", new[]
            {
                new[] { Eff(E_FeastBleed, 10, "FLG/Perk/feastBleed") },
                new[] { Eff(E_FeastBleed, 20, "FLG/Perk/feastBleed") },
                new[] { Eff(E_FeastBleed, 30, "FLG/Perk/feastBleed"), Eff(E_FeastSpeed, 15, "FLG/Perk/feastSpeed") },
                new[] { Eff(E_FeastBleed, 40, "FLG/Perk/feastBleed"), Eff(E_FeastSpeed, 20, "FLG/Perk/feastSpeed") },
                new[] { Eff(E_FeastBleed, 50, "FLG/Perk/feastBleed"), Eff(E_FeastSpeed, 25, "FLG/Perk/feastSpeed"), Eff(E_FeastAll, 1, "FLG/Perk/feastAll") },
            });

            // the game saves a perk as its index in the database: append at the end of both lists it may read
            tr.Field("skillsDB").SetValue(arr.Concat(new[] { vessel, feast }).ToArray());
            if (wrapped != null)
                tr.Field("skillsDBWrapped").SetValue(wrapped.Concat(new[]
                {
                    new PassiveSkillWrapper { skill = vessel, enabled = true },
                    new PassiveSkillWrapper { skill = feast, enabled = true },
                }).ToArray());
            FlagellantPlugin.Log.LogInfo("Perks: Sinful Vessel id " + PassiveSkillsDatabase.GetSkillID(vessel) + ", Blood Feast id " + PassiveSkillsDatabase.GetSkillID(feast)
                + " (database " + arr.Length + " perks, migrated " + migrated + ")");
        }

        static PassiveSkillEffect Eff(PassiveSkillEffect.EffectTypes type, int value, string term) =>
            new PassiveSkillEffect { type = type, value = value, customTerm = term };

        static PassiveSkill Make(PassiveSkill.Types type, string icon, PassiveSkillEffect[][] lvls)
        {
            var p = ScriptableObject.CreateInstance<PassiveSkill>();
            p.name = "Cultist_" + type;
            p.hideFlags = HideFlags.HideAndDontSave;
            p.type = type;
            p.perkType = PassiveSkill.PerkTypes.Class;
            p.icon = Art.Get("perks", icon, 24f);
            p.skillLvls = lvls.Select(e => new PassiveSkillLvl { effects = e }).ToArray();
            p.visualEffects = new PassiveVisualEffect[0];
            p.bodyPartsReplacer = null;
            p.penaltyValue = new PassiveSkillPenalty { effects = new PassiveSkillEffect.EffectTypes[0], value = 0 };
            p.incompatibleSkills = new PassiveSkill[0];
            p.perkRequirements = new PerkRequirements
            {
                universalClass = false,
                characterClasses = new[] { CharacterClasses.BERSERKER },   // the Cultist runs on the Berserker's slot
                requiredSkills = new PassiveSkill[0],
                requiredSkillLvl = 0,
                varialbeConditions = new SerializedVariable[0],
            };
            return p;
        }

        /// the Berserker's own class perks (the Cultist must not get them)
        public static bool BerserkerOnly(PassiveSkill p)
        {
            if (p == null || IsOurs(p)) return false;
            if (p.type.ToString().StartsWith("Berserk_")) return true;
            var r = p.perkRequirements;
            return r != null && !r.universalClass && r.characterClasses != null && r.characterClasses.Length > 0
                   && r.characterClasses.All(c => c == CharacterClasses.BERSERKER);
        }

        // ------------------------------------------------------------------ the hero's perk levels (-1 = no perk)
        public static int Lvl(Character ch, PassiveSkill.Types t)
        {
            try
            {
                var list = ch?.PassiveSkills?.skills;
                if (list == null) return -1;
                for (int i = 0; i < list.Count; i++) if (list[i] != null && list[i].type == t) return list[i].lvl;
            }
            catch { }
            return -1;
        }

        static int At(int[] t, int lvl) => t[Mathf.Clamp(lvl, 0, t.Length - 1)];

        /// helmet and body armour: a slot counts as missing when nothing (or nothing visible) is worn there
        public static int MissingArmour(Character ch)
        {
            int n = 0;
            if (ch.Head == null || ch.Head.EquipSprite == null) n++;
            if (ch.Body == null || ch.Body.EquipSprite == null) n++;
            return n;
        }

        public static float VesselHpMult(Character ch)
        {
            int l = Lvl(ch, Vessel);
            return l < 0 ? 1f : 1f + At(VesselHp, l) * 0.01f * MissingArmour(ch);
        }

        public static float VesselTakenMult(Character ch)
        {
            int l = Lvl(ch, Vessel);
            if (l < 2) return 1f;
            return Mathf.Max(0.2f, 1f - At(VesselDef, l) * 0.01f * MissingArmour(ch));
        }

        public static float VesselRegenBonus(Character ch) => Lvl(ch, Vessel) >= 4 ? VesselRegen * MissingArmour(ch) : 0f;

        // ------------------------------------------------------------------ puddles
        static float nextCheck; static Character checkedFor; static bool inBlood, inOther;
        static System.Reflection.FieldInfo poolField;

        /// is the hero standing in a puddle that Blood Feast counts (blood; poison and plasma from level 5)?
        public static bool FeastActive(Character ch, out int lvl)
        {
            lvl = Lvl(ch, Feast);
            if (lvl < 0) return false;
            if (Time.time >= nextCheck || checkedFor != ch) { nextCheck = Time.time + 0.2f; checkedFor = ch; Scan(ch); }
            return inBlood || (lvl >= 4 && inOther);
        }

        static void Scan(Character ch)
        {
            inBlood = inOther = false;
            try
            {
                var bm = BloodManager.Instance;
                if (bm == null) return;
                poolField = poolField ?? AccessTools.Field(typeof(BloodManager), "bloodsPool");
                var pool = poolField?.GetValue(bm) as List<GameObject>;
                if (pool == null) return;
                Vector2 me = ch.transform.position;
                const float r = 0.45f;
                foreach (var go in pool)
                {
                    if (go == null || !go.activeInHierarchy) continue;
                    if (((Vector2)go.transform.position - me).sqrMagnitude > r * r) continue;
                    var sr = go.GetComponent<BloodPhotonController>()?.SR ?? go.GetComponentInChildren<SpriteRenderer>();
                    if (sr == null || !sr.enabled || sr.color.a < 0.05f) continue;
                    var c = sr.color;
                    // the game tints the same puddle sprite: blood = red, poison = green, plasma = blue
                    if (c.r >= c.g && c.r >= c.b) inBlood = true; else inOther = true;
                    if (inBlood && inOther) return;
                }
            }
            catch { }
        }
    }

    // ---------------------------------------------------------------------- patches

    /// the Cultist is offered his own class perks; the Berserker never gets the Cultist's ones
    [HarmonyPatch(typeof(PassiveSkillsDatabase), nameof(PassiveSkillsDatabase.GetAvailablePerksList))]
    internal static class PerkListPatch
    {
        static void Postfix(Character character, List<PassiveSkill> __result)
        {
            try
            {
                if (__result == null) return;
                bool cultist = Flag.Active && character != null && character.charClass == CharacterClasses.BERSERKER;
                if (cultist) __result.RemoveAll(Perks.BerserkerOnly);
                else __result.RemoveAll(Perks.IsOurs);
            }
            catch { }
        }
    }

    /// Sinful Vessel: max HP
    [HarmonyPatch(typeof(Creature), nameof(Creature.HP_max), MethodType.Getter)]
    internal static class VesselHpPatch
    {
        static void Postfix(Creature __instance, ref float __result)
        {
            try
            {
                var ch = FLogic.HeroSelf(__instance);
                if (ch != null && __result > 1f) __result *= Perks.VesselHpMult(ch);
            }
            catch { }
        }
    }

    /// the Cultist's own attack speed bonuses: Blood Feast in a puddle (from perk level 3) and Quick Snap stacks
    internal static class CultSpeed
    {
        public static float Mult(Creature c)
        {
            var ch = FLogic.HeroSelf(c);
            if (ch == null) return 1f;
            float m = FLogic.SnapSpeedMult();
            if (Perks.FeastActive(ch, out int l) && l >= 2) m *= 1f + Perks.FeastSpeed[Mathf.Min(l, 4)] * 0.01f;
            return m;
        }
    }

    [HarmonyPatch(typeof(Creature), nameof(Creature.AttackSpeed), MethodType.Getter)]
    internal static class FeastSpeedPatch
    {
        static void Postfix(Creature __instance, ref float __result)
        {
            try { float m = CultSpeed.Mult(__instance); if (m > 1f) __result = Mathf.Clamp(__result * m, 0.3f, 2.7f); } catch { }
        }
    }

    /// the hero's attack animations run on Character.AttackSpeed(agility multiplier, weapon) - not on the getter above
    [HarmonyPatch(typeof(Character), nameof(Character.AttackSpeed), new[] { typeof(float), typeof(bool) })]
    internal static class HeroSpeedPatch
    {
        static void Postfix(Character __instance, ref float __result)
        {
            try { float m = CultSpeed.Mult(__instance); if (m > 1f) __result = Mathf.Min(__result * m, 3.8f); } catch { }
        }
    }

    /// Blood Feast: while the Cultist stands in a puddle, the bleeding bar of the enemies fills faster
    [HarmonyPatch(typeof(Creature), nameof(Creature.AddBleed), new[] { typeof(float), typeof(float), typeof(Debuff) })]
    internal static class FeastBleedPatch
    {
        static void Prefix(Creature __instance, ref float value)
        {
            try
            {
                if (value <= 0f || !Flag.Active || __instance is Character) return;
                var ch = FLogic.HeroSelf(GameManager.Instance != null ? GameManager.Instance.Player_Mine() : null);
                if (ch == null || !Perks.FeastActive(ch, out int l)) return;
                value *= 1f + Perks.FeastBleed[Mathf.Min(l, 4)] * 0.01f;
            }
            catch { }
        }
    }
}
