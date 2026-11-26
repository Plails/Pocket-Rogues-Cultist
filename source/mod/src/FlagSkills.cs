using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using I2.Loc;
using UnityEngine;

namespace FlagellantMod
{
    /// The Flagellant's 9 skills, made from the Berserker's (so the game plays the animations, spends stamina, charges)
    /// with the design numbers as skill parts. The attacks themselves are ours (SkillLogic).
    /// The game shows "Наносимый урон" as 95 + value of the main AddDamagePercent, so main values are (design - 95).
    internal static class FlagSkills
    {
        public static SkillsDataBase.CharacterSkillsData Data;
        private static readonly Dictionary<SkillsDataBase, SkillsDataBase.CharacterSkillsData> originals = new Dictionary<SkillsDataBase, SkillsDataBase.CharacterSkillsData>();
        public static readonly Dictionary<SkillLvl, (string id, bool charged)> LvlToId = new Dictionary<SkillLvl, (string, bool)>();
        public static readonly Dictionary<string, CharacterSkill> ById = new Dictionary<string, CharacterSkill>();
        public static readonly Dictionary<CharacterSkill, string> IdOf = new Dictionary<CharacterSkill, string>();
        public static readonly Dictionary<string, Debuff> Debuffs = new Dictionary<string, Debuff>();

        const BindingFlags F = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        static readonly MethodInfo Clone = typeof(object).GetMethod("MemberwiseClone", F);
        static SkillDamageInjectContainer template;

        const SkillDamageInjectContainer.InjectTypes Pct = SkillDamageInjectContainer.InjectTypes.AddDamagePercent,
            Repl = SkillDamageInjectContainer.InjectTypes.ReplaceDmgType, Range = SkillDamageInjectContainer.InjectTypes.MultiplyRange,
            Deb = SkillDamageInjectContainer.InjectTypes.AddTargetDebuff, Stat = SkillDamageInjectContainer.InjectTypes.AddTargetStatEffect,
            Armor = SkillDamageInjectContainer.InjectTypes.ArmorDamage;

        public static void RememberOriginal(SkillsDataBase db)
        {
            if (!originals.ContainsKey(db) && db.skills_berserk != Data) originals[db] = db.skills_berserk;
        }
        public static SkillsDataBase.CharacterSkillsData OriginalOf(SkillsDataBase db) =>
            originals.TryGetValue(db, out var o) ? o : db.skills_berserk;

        public static Debuff D(string name)
        {
            if (Debuffs.TryGetValue(name, out var d)) return d;
            d = Resources.FindObjectsOfTypeAll<Debuff>().FirstOrDefault(x => x != null && x.name == name);
            if (d != null) Debuffs[name] = d; else FlagellantPlugin.Log.LogWarning("Debuff not found: " + name);
            return d;
        }

        public static void Build()
        {
            var db = (Resources.Load("SkillsDB") as GameObject)?.GetComponent<SkillsDataBase>();
            if (db == null) { FlagellantPlugin.Log.LogError("SkillsDB not found"); return; }
            RememberOriginal(db);
            var bers = OriginalOf(db);
            foreach (var cls in new[] { db.skills_warrior, db.skills_archer, db.skills_wizard, db.skills_hunter, bers, db.skills_necrom })
            {
                if (cls == null) continue;
                foreach (var h in cls.skills_Speed.Concat(cls.skills_Power).Concat(cls.skills_Special))
                    if (h?.skill != null)
                        foreach (var l in AllLvls(h.skill))
                            if (l.damageInjectors != null)
                                foreach (var inj in l.damageInjectors)
                                    if (inj?.GetDebuffsForTarget != null)
                                        foreach (var d in inj.GetDebuffsForTarget) if (d != null && !Debuffs.ContainsKey(d.name)) Debuffs[d.name] = d;
            }
            SkillDb = db;
            Data = BuildTree(bers);
            FlagellantPlugin.Log.LogInfo("Flagellant skill tree ready: " + string.Join(", ", new[] { Data.skills_Speed, Data.skills_Power, Data.skills_Special }.SelectMany(r => r).Select(h => h.skill != null ? h.skill.name + (h.skill.IsDefault ? "*" : "") + "[" + h.skill.animationID + "/" + h.skill.skillType + "]" : "-")));
            try
            {
                foreach (var kv in ById)
                {
                    var sl = kv.Value.skillCharges != null && kv.Value.skillCharges.Count > 0 ? kv.Value.skillCharges[0] : kv.Value.skillLvls;
                    var main = sl?.damageInjectors?.FirstOrDefault(i => i != null && i.isMainLine);
                    if (main != null) FlagellantPlugin.Log.LogInfo("Dealt damage " + kv.Key + ": " + string.Join(" ", new[] { 1, 2, 3, 6, 12, 13, 24, 25, 26, 30 }.Select(v => v + "=" + (95f + main.GetResult(v)).ToString("0.###"))));
                }
            }
            catch (Exception e) { FlagellantPlugin.Log.LogWarning("damage table log: " + e.Message); }
            FlagellantPlugin.Log.LogInfo("Debuffs used: Heavy Hands=" + (D("SlowlyFight") ? "ok" : "-") + " Weakness=" + (D("WeaknessReduceDmg") ? "ok" : "-") + " Slow=" + (D("SlowlyWeb") ? "ok" : "-"));
        }

        static SkillDamageInjectContainer Inj(SkillDamageInjectContainer.InjectTypes type, float baseV, float perLvl, int from = 0, int to = 50,
            c.DamageTypes dmg = c.DamageTypes._none, bool mainLine = false, Debuff debuff = null, float time = 0f,
            SkillDamageInjectContainer.StatEffects stat = SkillDamageInjectContainer.StatEffects.Bleed)
        {
            var n = (SkillDamageInjectContainer)Clone.Invoke(template, null);
            var tr = Traverse.Create(n);
            n.injectType = type; n.injectAmmoType = SkillDamageInjectContainer.InjectAmmoTypes.NONE; n.injectSummon = SkillDamageInjectContainer.InjectSummonTypes.NONE;
            tr.Field("defaultValue").SetValue(new SkillDamageInjectContainer.InjectorValue { baseValue = baseV, perLvlMod = perLvl, lvlsInterval = new Vector2Int(from, to) });
            tr.Field("customValues").SetValue(new List<SkillDamageInjectContainer.InjectorValue>());
            n.debuffValuesAlt = new List<SkillDamageInjectContainer.InjectorValue>();
            n.debuffTimeAlt = new List<SkillDamageInjectContainer.InjectorValue>();
            tr.Field("addDebuffsToTarget").SetValue(debuff != null ? new List<Debuff> { debuff } : new List<Debuff>());
            tr.Field("addBuffSelf").SetValue(null);
            tr.Field("spawnObjData").SetValue(null);
            tr.Field("_damageType").SetValue(dmg);
            tr.Field("_useMainDmgType").SetValue(dmg == c.DamageTypes._none);
            tr.Field("baseTime").SetValue(time);
            tr.Field("addTimePerLvl").SetValue(0f);
            n.statEffect = stat;
            n.conditions = new List<ConditionClass>();
            n.onActivate = new List<SkillDamageInjectContainer>();
            n.customDescription = new LocalizedString();
            n.replaceTexts = new ReplaceText[0];
            n.valuePrefix = "";
            n.valuePostfix = new LocalizedString();
            n.customTip = new LocalizedString();
            n.IsEnabled = true; n.showDescriptionLine = true; n.useRandomDebuff = false;
            n.isMainLine = mainLine;
            n.ammoInject = false;
            return n;
        }

        // the designers' tables: "dealt damage" at levels 1..25, whole numbers
        static readonly int[] T_Strike = { 80, 83, 86, 89, 92, 96, 99, 102, 105, 108, 111, 114, 118, 121, 124, 127, 130, 133, 136, 139, 142, 146, 149, 152, 155 };
        static readonly int[] T_Sinner = Enumerable.Range(0, 25).Select(i => 80 + 3 * i).ToArray();          // 80 .. 152 (+3% a level, MRX)
        static readonly int[] T_Rain = { 100, 104, 108, 113, 117, 121, 125, 129, 133, 138, 142, 146, 150, 154, 158, 163, 167, 171, 175, 179, 183, 188, 192, 196, 200 };
        static readonly int[] T_Hammer = Enumerable.Range(0, 25).Select(i => 130 + 5 * i).ToArray();          // 130 .. 250
        public static readonly float[] T_HammerArmor = Enumerable.Range(0, 25).Select(i => i < 24 ? 25f + i : 50f).ToArray();   // 25 .. 48, 50
        static readonly int[] T_Snap = Enumerable.Range(0, 25).Select(i => i < 24 ? 70 + 2 * i : 120).ToArray();   // 70 .. 116, 120
        public static readonly float[] T_SnapSpeed = Enumerable.Range(1, 25).Select(l => l >= 25 ? 30f : l >= 12 ? 20f : 10f).ToArray();
        static readonly int[] T_Wrath = {100, 105, 110, 116, 121, 126, 131, 136, 142, 147, 152, 157, 162, 168, 173, 178, 183, 189, 194, 199, 204, 209, 215, 220, 225 };

        /// the main damage line taken level by level from a table (the game shows 95 + value), so every level is a whole number;
        /// above 25 (item bonuses) it keeps growing by a whole step
        static SkillDamageInjectContainer MainTab(int[] t, int stepAfter)
        {
            var n = Inj(Pct, t[0] - 95f, 0f, mainLine: true);
            var list = new List<SkillDamageInjectContainer.InjectorValue>();
            for (int lvl = 1; lvl < t.Length; lvl++)
                list.Add(new SkillDamageInjectContainer.InjectorValue { baseValue = t[lvl - 1] - 95f, perLvlMod = 0f, lvlsInterval = new Vector2Int(lvl, lvl) });
            list.Add(new SkillDamageInjectContainer.InjectorValue { baseValue = t[t.Length - 1] - 95f, perLvlMod = stepAfter, lvlsInterval = new Vector2Int(t.Length, 50) });
            Traverse.Create(n).Field("customValues").SetValue(list);
            return n;
        }

        // other columns of the tables, levels 1..25 (used by the logic too, so the screen tells the truth)
        public static readonly float[] T_Martyr = { 1.00f, 1.04f, 1.08f, 1.12f, 1.17f, 1.21f, 1.25f, 1.29f, 1.33f, 1.38f, 1.42f, 1.46f, 1.50f, 1.54f, 1.58f, 1.62f, 1.67f, 1.71f, 1.75f, 1.79f, 1.83f, 1.88f, 1.92f, 1.96f, 2.00f };
        // the other way round now (MRX): 1% at level 1, growing to 2.5% at 25 (1.1% at 2, ...)
        public static readonly float[] T_WrathCost = Enumerable.Range(0, 25).Select(i => (float)Math.Round(1.0 + 1.5 * i / 24.0, 1, MidpointRounding.AwayFromZero)).ToArray();
        public static readonly float[] T_Ecstasy = { 1.00f, 1.06f, 1.12f, 1.19f, 1.25f, 1.31f, 1.38f, 1.44f, 1.50f, 1.56f, 1.63f, 1.69f, 1.75f, 1.81f, 1.88f, 1.94f, 2.00f, 2.06f, 2.12f, 2.19f, 2.25f, 2.31f, 2.38f, 2.44f, 2.50f };
        public static readonly float[] T_Resist = Enumerable.Range(1, 25).Select(i => 4f * i).ToArray();

        /// a table value for a skill level (1..25; above 25 the level-25 value)
        public static float At(float[] t, int lvl) => t[Mathf.Clamp(lvl, 1, t.Length) - 1];

        /// a 25-level table from steps: (from level, value), ...
        static float[] Steps(params float[] fv)
        {
            var t = new float[25];
            for (int lvl = 1; lvl <= 25; lvl++)
                for (int k = 0; k + 1 < fv.Length; k += 2) if (lvl >= fv[k]) t[lvl - 1] = fv[k + 1];
            return t;
        }

        static void PerLevel(SkillDamageInjectContainer n, float[] t, int from)
        {
            var list = new List<SkillDamageInjectContainer.InjectorValue>();
            for (int lvl = Math.Max(1, from); lvl < t.Length; lvl++)
                list.Add(new SkillDamageInjectContainer.InjectorValue { baseValue = t[lvl - 1], perLvlMod = 0f, lvlsInterval = new Vector2Int(lvl, lvl) });
            list.Add(new SkillDamageInjectContainer.InjectorValue { baseValue = t[t.Length - 1], perLvlMod = 0f, lvlsInterval = new Vector2Int(t.Length, 50) });
            Traverse.Create(n).Field("customValues").SetValue(list);
        }

        /// a working part that does not show its line under the description (the user didn't want the area line;
        /// it still sets the size of the hit zone)
        static SkillDamageInjectContainer NoLine(SkillDamageInjectContainer n) { n.showDescriptionLine = false; return n; }

        /// a working part whose value follows a 25-level table (one line on the screen: "10 -> 20")
        static SkillDamageInjectContainer Tab(SkillDamageInjectContainer n, float[] t) { PerLevel(n, t, 0); return n; }

        public static SkillsDataBase SkillDb;
        static Sprite Ico(Func<Sprite> f) { try { return f(); } catch { return null; } }

        /// a display-only line under the description (does nothing in battle; the text is ours, "{VALUE}" = value per level)
        static SkillDamageInjectContainer Line(string key, float flat, int from = 0, Sprite icon = null) => Line(key, null, flat, from, icon);
        static SkillDamageInjectContainer Line(string key, float[] table, int from = 0, Sprite icon = null) => Line(key, table, 0f, from, icon);
        static SkillDamageInjectContainer Line(string key, float[] table, float flat, int from, Sprite icon)
        {
            var n = Inj(SkillDamageInjectContainer.InjectTypes.NONE, table != null ? At(table, Math.Max(1, from)) : flat, 0f, from, 50);
            n.customDescription = new LocalizedString("FLG/Line/" + key);
            n.percentValue = false;
            n.customIconPreview = icon;
            if (table != null) PerLevel(n, table, from);
            return n;
        }

        /// (2.6.8, the user) Religious Ecstasy shown like the Berserker's skills: the game's line "puts an effect on
        /// himself for 15 sec" with the effect's card under it, and the card holds the damage bonus per 1% of missing HP
        /// (level by level, "1,06 > 1,12"). Display only: the effect is put on him by FlagRuntime.StartEcstasy.
        static Buff ecstasyCard;
        static SkillDamageInjectContainer EcstasySelfEffect(Sprite icon)
        {
            if (ecstasyCard == null)
            {
                ecstasyCard = ScriptableObject.CreateInstance<Buff>();
                ecstasyCard.name = "Flagellant_EcstasyCard"; ecstasyCard.hideFlags = HideFlags.HideAndDontSave;
                ecstasyCard.buffType = BuffTypes.damageMod;
                ecstasyCard.titleLocalized = new LocalizedString("Effects/Flagellant/ecstasy");
                ecstasyCard.descriptionLocalized = new LocalizedString("Effects/Flagellant/ecstasy_card");
            }
            ecstasyCard.icon = IconOf("suffering");
            ecstasyCard.time = 15f; ecstasyCard.timeReal = 15f; ecstasyCard.value = T_Ecstasy[0];
            var n = Inj(SkillDamageInjectContainer.InjectTypes.AddBuffSelf, T_Ecstasy[0], 0f, time: 15f);
            Traverse.Create(n).Field("addBuffSelf").SetValue(ecstasyCard);
            n.showEffect = true; n.percentValue = false;
            n.customIconPreview = icon;
            PerLevel(n, T_Ecstasy, 0);
            return n;
        }

        /// the game's own projectile lines ("Pierces N targets", "Damage for every pierced target"), display only
        static SkillDamageInjectContainer Pierce(SkillDamageInjectContainer.InjectAmmoTypes type, float[] table, float flat = 0f, int from = 0)
        {
            var n = Inj(SkillDamageInjectContainer.InjectTypes.NONE, table != null ? At(table, Math.Max(1, from)) : flat, 0f, from, 50);
            n.mainType = SkillDamageInjectContainer.InjectGlobalType.Projectile;
            n.injectAmmoType = type;
            n.customDescription = new LocalizedString();
            n.customIconPreview = null;
            if (table != null) PerLevel(n, table, from);
            return n;
        }

        public static IEnumerable<SkillLvl> AllLvls(CharacterSkill s)
        {
            if (s.skillLvls != null) yield return s.skillLvls;
            if (s.skillCharges != null) foreach (var l in s.skillCharges) if (l != null) yield return l;
        }

        static void SetInj(SkillLvl l, c.DamageTypes main, params SkillDamageInjectContainer[] add)
        {
            l.mainDamageType = main;
            l.effectsList = new List<SkillEffectContainer>();
            var list = add.Where(i => i != null).Select(i => (SkillDamageInjectContainer)Clone.Invoke(i, null)).ToList();
            for (int k = 0; k < list.Count; k++) list[k].indexInList = k;
            l.damageInjectors = list;
        }

        public static Sprite IconOf(string id) => Icon(id, null);

        static Sprite Icon(string id, Sprite fallback)
        {
            try
            {
                var path = System.IO.Path.Combine(FlagellantPlugin.Dir, "icons", id + ".png");
                if (!System.IO.File.Exists(path)) return fallback;
                var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, name = "Flagellant_" + id };
                ImageConversion.LoadImage(tex, System.IO.File.ReadAllBytes(path));
                tex.hideFlags = HideFlags.HideAndDontSave;
                var sp = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 34f);
                sp.name = "Flagellant_" + id;
                sp.hideFlags = HideFlags.HideAndDontSave;
                return sp;
            }
            catch (Exception e) { FlagellantPlugin.Log.LogWarning("Icon " + id + ": " + e.Message); return fallback; }
        }

        static CharacterSkill Make(CharacterSkill source, string id, SO_Skill.SkillType slot, bool isDefault, string saveId = null)
        {
            var c = UnityEngine.Object.Instantiate(source);
            c.name = "Flagellant_" + id;
            c.hideFlags = HideFlags.HideAndDontSave;
            var tr = Traverse.Create(c);
            tr.Field("localizedTitle").SetValue(new LocalizedString("Characters/Skills/Flagellant/" + id));
            tr.Field("localizedDescription").SetValue(new LocalizedString("Characters/Skills/Flagellant/" + id + "_dsc"));
            var icon = Icon(id, source.Icon);
            if (icon != null) tr.Field("skillIcon").SetValue(icon);
            tr.Field("isDef").SetValue(isDefault);
            c.mainSkillSlot = slot;
            c.mainClass = CharacterClasses.BERSERKER;
            c.skillStates = new List<SkillState>();
            if (source.serializedVariable != null)
            {
                var v = UnityEngine.Object.Instantiate(source.serializedVariable);
                v.name = "Flagellant_" + id + "_lvl";
                v.variableID = "FLG_skill_" + (saveId ?? id);
                v.hideFlags = HideFlags.HideAndDontSave;
                c.serializedVariable = v;
            }
            ById[id] = c;
            IdOf[c] = id;
            return c;
        }

        /// skills paid with blood instead of stamina
        static void NoStamina(CharacterSkill s)
        {
            try
            {
                var st = Traverse.Create(s).Field("stPrice");
                var price = st.GetValue();
                if (price != null)
                {
                    Traverse.Create(price).Field("defaultPriceModifier").SetValue(0f);
                    Traverse.Create(price).Field("additionalPrice").SetValue(0f);
                    st.SetValue(price);
                }
                Traverse.Create(s).Field("stRequired").SetValue(false);
            }
            catch (Exception e) { FlagellantPlugin.Log.LogWarning("NoStamina " + s.name + ": " + e.Message); }
        }

        /// a charged skill with one charge stage that only works when charged
        static void OneCharge(CharacterSkill s)
        {
            s.skillLvls.effectsList = new List<SkillEffectContainer>();
            s.skillLvls.damageInjectors = new List<SkillDamageInjectContainer>();
            if (s.skillCharges != null && s.skillCharges.Count > 1) s.skillCharges = s.skillCharges.Take(1).ToList();
            foreach (var l in s.skillCharges) l.lvlsInterval = new Vector2Int(0, 25);
        }

        static SkillsDataBase.CharacterSkillsData BuildTree(SkillsDataBase.CharacterSkillsData bers)
        {
            var all = bers.skills_Speed.Concat(bers.skills_Power).Concat(bers.skills_Special).Where(h => h?.skill != null).Select(h => h.skill).ToList();
            CharacterSkill S(string n) => all.First(s => s.name == n);
            bool Def(SkillHolder[] row, int i) => row != null && row.Length > i && row[i]?.skill != null && row[i].skill.IsDefault;

            var main = S("Brs_Spd_Main"); var bone = S("Brs_Spd_BoneBreak"); var vamp = S("Brs_Spd_MpVamp");
            var power = S("Brs_Str_PowerMain");
            var rage = S("Brs_Spec_BuffRage"); var mad = S("Brs_Spec_BuffMad"); var bvamp = S("Brs_Spec_BuffVamp");
            template = main.skillLvls.damageInjectors.First(i => i != null);

            var A = CharacterSkill.AnimationTriggers.Attack;
            var Heavy = CharacterSkill.AnimationTriggers.AttackStr;
            var Lunge = CharacterSkill.AnimationTriggers.attackForward;
            var Cast = CharacterSkill.AnimationTriggers.WizAtckSpd;
            var Chest = CharacterSkill.AnimationTriggers.t_berserkSpec;
            var Gather = CharacterSkill.AnimationTriggers.charge_wizPower;
            var BigCast = CharacterSkill.AnimationTriggers.WizStr;
            var Heal = CharacterSkill.AnimationTriggers.WizHeal;

            var bludge = c.DamageTypes.Phys_bludge; var slash = c.DamageTypes.Phys_slash;
            var heavyHands = D("SlowlyFight"); var weakness = D("WeaknessReduceDmg"); var slow = D("SlowlyWeb");

            // Every skill shows its numbers as stat lines under the description, like the game's own skills:
            // the working parts (damage, area, damage type, gauges, debuffs, armour) plus display-only lines (Line/Pierce)
            // for what our own logic does. All values follow the designers' tables.
            var sk = SkillDb;
            Sprite bloodIco = Ico(() => sk.statusIcons[SkillDamageInjectContainer.StatEffects.Bleed]);
            Sprite poisonIco = Ico(() => sk.statusIcons[SkillDamageInjectContainer.StatEffects.Poison]);
            Sprite petrIco = Ico(() => sk.statusIcons[SkillDamageInjectContainer.StatEffects.Petrification]);
            Sprite dmgIco = Ico(() => sk.skillStatsIcons[Pct]);
            Sprite critIco = Ico(() => sk.skillStatsIcons[SkillDamageInjectContainer.InjectTypes.CritChance]);
            Sprite hpIco = Ico(() => sk.skillStatsIcons[SkillDamageInjectContainer.InjectTypes.Vampire_HP]);
            Sprite powerIco = Ico(() => sk.skillStatsIcons[SkillDamageInjectContainer.InjectTypes.PowerAttack]);
            Sprite rangeIco = Ico(() => sk.skillStatsIcons[Range]);
            Sprite darkIco = Ico(() => sk.dmgTypeIcons[c.DamageTypes.Dark]);
            Sprite holyIco = Ico(() => sk.dmgTypeIcons[c.DamageTypes.Light]);
            Sprite ammoIco = Ico(() => sk.ammoStatsIcons[SkillDamageInjectContainer.InjectAmmoTypes.MaxTargetsThrow]);

            // ---------------- main (right) ----------------
            // Bloody Strike: dealt 80..155 (blunt), bleeding gauge 10/20/30, Heavy Hands at 25
            var s1 = Make(main, "bloodStrike", SO_Skill.SkillType.SpeedAttack, Def(bers.skills_Speed, 0));
            s1.animationID = A;
            foreach (var l in AllLvls(s1))
                SetInj(l, bludge,
                    MainTab(T_Strike, 3), NoLine(Inj(Range, 60f, 0f)), Inj(Repl, 100f, 0f, dmg: bludge),
                    Tab(Inj(Stat, 10f, 0f, stat: SkillDamageInjectContainer.StatEffects.Bleed), Steps(0, 10f, 12, 20f, 25, 30f)),
                    heavyHands != null ? Inj(Deb, 25f, 0f, 25, 50, debuff: heavyHands, time: 6f) : null);

            // Poison Lash: two swings, each dealt 80..155 (blunt), poison gauge 10/20/30, Weakness -30% from 12, +40% vs undead at 25
            // (the user had Poison Lash and Martyr's Hand swap places: Poison Lash sits in the top row, Martyr's Hand in the right row)
            var s2 = Make(bone, "poisonLash", SO_Skill.SkillType.StrengthSkill, Def(bers.skills_Power, 0));
            s2.animationID = A;
            foreach (var l in AllLvls(s2))
                SetInj(l, slash,                                                   // a whip cuts (the user: slashing, not blunt)
                    MainTab(T_Strike, 3), Line("hits", 2f, icon: dmgIco), Line("whipReach", 0f, icon: rangeIco), NoLine(Inj(Range, 50f, 0f)), Inj(Repl, 100f, 0f, dmg: slash),
                    Tab(Inj(Stat, 10f, 0f, stat: SkillDamageInjectContainer.StatEffects.Poison), Steps(0, 10f, 12, 20f, 25, 30f)),
                    weakness != null ? Inj(Deb, 30f, 0f, 12, 50, debuff: weakness, time: 6f) : null,
                    Line("undead", 40f, 25, icon: dmgIco));

            // Sinner's Strike: three blows with teleports (the same target may take several), dealt 80..152 (blunt), armour -10/-15/-20, +15% crit vs heavy armour at 25
            var s3 = Make(vamp, "sinnerStrike", SO_Skill.SkillType.SpeedAttack, Def(bers.skills_Speed, 2));
            s3.animationID = Lunge;
            foreach (var l in AllLvls(s3))
                SetInj(l, bludge,
                    MainTab(T_Sinner, 3), Line("hits", 3f, icon: dmgIco), NoLine(Inj(Range, 50f, 0f)), Inj(Repl, 100f, 0f, dmg: bludge),
                    Tab(Inj(Armor, 10f, 0f), Steps(0, 10f, 12, 15f, 25, 20f)),
                    Line("heavyCrit", 15f, 25, icon: critIco));

            // ---------------- special (left) ----------------
            // Blood Rain: 5% HP, a homing clot, magic 100..200, pierces 1/2/3, +30% per pierce at 25
            var x1 = Make(rage, "bloodRain", SO_Skill.SkillType.SpecialSkill, Def(bers.skills_Special, 0));
            x1.animationID = Cast;
            NoStamina(x1);
            foreach (var l in AllLvls(x1))
                SetInj(l, c.DamageTypes.Magic,
                    MainTab(T_Rain, 4), Inj(Repl, 100f, 0f, dmg: c.DamageTypes.Magic), Line("hpCost", 5f, icon: bloodIco),
                    Line("homing", 0f, icon: ammoIco),
                    Pierce(SkillDamageInjectContainer.InjectAmmoTypes.MaxTargetsThrow, Steps(0, 1f, 12, 2f, 25, 3f)),
                    Line("repierce", 0f, icon: ammoIco),
                    Pierce(SkillDamageInjectContainer.InjectAmmoTypes.AddDmgOnThrow, null, 30f, 25));

            // Hammer of Penance (replaced Martyr's Hand in the right row, middle, at the user's request; MRX's table):
            // a slow heavy blow, no charging, ~40% longer than a normal attack; dealt 130..250 (blunt),
            // armour ignored 25..50%, stun 7% from 12, 15% at 25. Keeps Martyr's Hand's save key, so the levels stay.
            var x2 = Make(main, "penanceHammer", SO_Skill.SkillType.SpeedAttack, Def(bers.skills_Speed, 1), "martyrHand");
            x2.animationID = A;
            x2.animationSpeedMod = main.animationSpeedMod / 1.4f;                 // the 2.4 speed again (the user)
            foreach (var l in AllLvls(x2))
                SetInj(l, bludge,
                    MainTab(T_Hammer, 5), NoLine(Inj(Range, 80f, 0f)), Inj(Repl, 100f, 0f, dmg: bludge),
                    Tab(Inj(SkillDamageInjectContainer.InjectTypes.IgnoreArmor, 25f, 0f), T_HammerArmor),
                    Line("stun", Steps(12, 7f, 25, 15f), 12, icon: powerIco));

            // Divine Punishment (charged): area 500%, cursed (dark) 100..225, HP per enemy 2.5..1%, stun 5/10%, slow at 25
            var x3 = Make(power, "divinePunishment", SO_Skill.SkillType.SpecialSkill, Def(bers.skills_Special, 2));
            x3.animationID = Chest; x3.animationID_charge = Gather;
            OneCharge(x3);
            foreach (var l in AllLvls(x3))
                SetInj(l, c.DamageTypes.Dark, MainTab(T_Wrath, 5), Inj(Range, 500f, 0f), Inj(Repl, 100f, 0f, dmg: c.DamageTypes.Dark),
                    Line("wrathCost", T_WrathCost, icon: bloodIco),
                    Line("stun", Steps(12, 5f, 25, 10f), 12, icon: powerIco),
                    slow != null ? Inj(Deb, 30f, 0f, 25, 50, debuff: slow, time: 4f) : null);

            // ---------------- power (top) ----------------
            // Torment (charged): pull radius 750%, explosion 250%, magic (50% fire) 100..225, Weakness 25 from 12, Heavy Hands 25 at 25
            var p1 = Make(power, "torment", SO_Skill.SkillType.SpecialSkill, Def(bers.skills_Special, 1));
            p1.animationID = BigCast; p1.animationID_charge = Gather;
            OneCharge(p1);
            foreach (var l in AllLvls(p1))
                SetInj(l, c.DamageTypes.Magic, MainTab(T_Wrath, 5), Inj(Range, 250f, 0f), Line("bloodPull", 750f, icon: rangeIco),
                    Line("bloodGain", 3f, icon: bloodIco), Inj(Repl, 50f, 0f, dmg: c.DamageTypes.Fire),
                    weakness != null ? Inj(Deb, 25f, 0f, 12, 50, debuff: weakness, time: 6f) : null,
                    heavyHands != null ? Inj(Deb, 25f, 0f, 25, 50, debuff: heavyHands, time: 6f) : null);

            // Religious Ecstasy: the self-flogging buff (all ours)
            var p2 = Make(bvamp, "suffering", SO_Skill.SkillType.StrengthSkill, Def(bers.skills_Power, 1));
            p2.animationID = Chest;
            foreach (var l in AllLvls(p2))
                SetInj(l, c.DamageTypes._none,
                    EcstasySelfEffect(holyIco),
                    Line("ecstasyDef", 1f, 12, icon: hpIco),
                    Line("regen", 50f, 25, icon: hpIco));

            // Quick Snap (replaced Blood Cleansing at the user's request; MRX's table): a lightning lunge of the lash in a
            // straight line, far; the tip hits the first target on the way; dealt 70..116, 120 at 25 (blunt);
            // a hit at the full reach is always critical; every hit gives +10/20/30% attack speed for a while (2 stacks).
            // Keeps Blood Cleansing's save key, so the levels stay.
            var p3 = Make(main, "quickSnap", SO_Skill.SkillType.StrengthSkill, Def(bers.skills_Power, 2), "bloodCleansing");
            p3.animationID = A;
            p3.animationSpeedMod = main.animationSpeedMod * 1.35f;                 // a sped-up attack
            foreach (var l in AllLvls(p3))
                SetInj(l, slash,
                    MainTab(T_Snap, 4), Line("snapLine", 0f, icon: rangeIco), NoLine(Inj(Range, 50f, 0f)), Inj(Repl, 100f, 0f, dmg: slash),
                    Line("snapCrit", 0f, icon: critIco),
                    Line("snapSpeed", T_SnapSpeed, icon: powerIco));

            foreach (var kv in ById)
            {
                var s = kv.Value;
                if (s.skillLvls != null) LvlToId[s.skillLvls] = (kv.Key, false);
                if (s.skillCharges != null) foreach (var l in s.skillCharges) if (l != null) LvlToId[l] = (kv.Key, true);
            }

            return new SkillsDataBase.CharacterSkillsData
            {
                skills_Speed = new[] { Hd(s1, bers.skills_Speed, 0), Hd(x2, bers.skills_Speed, 1), Hd(s3, bers.skills_Speed, 2) },
                skills_Power = new[] { Hd(s2, bers.skills_Power, 0), Hd(p2, bers.skills_Power, 1), Hd(p3, bers.skills_Power, 2) },
                skills_Special = new[] { Hd(x1, bers.skills_Special, 0), Hd(p1, bers.skills_Special, 1), Hd(x3, bers.skills_Special, 2) },
            };
        }

        static SkillHolder Hd(CharacterSkill s, SkillHolder[] row, int i) =>
            new SkillHolder(s, 0, row != null && row.Length > i && row[i] != null ? row[i].customLvlStat : CharacterStats._null);
    }
}
