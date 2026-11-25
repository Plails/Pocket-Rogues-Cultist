using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HarmonyLib;
using I2.Loc;
using UnityEngine;

namespace FlagellantMod
{
    /// The Flagellant's four flails. While he is the hero they are the Berserker's weapon lists (forge, blueprints,
    /// loot), so only he finds them. Saved items point to "Items/Weapons/Flagellant/..." which is answered here.
    internal static class Weapons
    {
        public static SO_ItemWeapon[] Flails = new SO_ItemWeapon[0], Retired = new SO_ItemWeapon[0];

        /// the whips: left-hand things (the Ritual Scourge)
        public static bool IsWhip(SO_Item item) => item != null && item.Name == "Flg_ritualScourge";
        public static ItemEffectWeapon Inquisition;
        public const string PathPrefix = "Items/Weapons/Flagellant/";
        public const string InquisitionPath = "Effects/Flagellant/Inquisition";
        static readonly Dictionary<ItemsDataBase, (SO_ItemWeapon[] eq, List<SO_ItemWeapon> found)> originals = new Dictionary<ItemsDataBase, (SO_ItemWeapon[], List<SO_ItemWeapon>)>();

        static Sprite Load(string file, float ppu, Vector2 pivotPx)
        {
            var path = Path.Combine(FlagellantPlugin.Dir, "weapons", file + ".png");
            if (!File.Exists(path)) { FlagellantPlugin.Log.LogWarning("Missing " + path); return null; }
            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp, name = "Flg_" + file };
            ImageConversion.LoadImage(tex, File.ReadAllBytes(path));
            tex.hideFlags = HideFlags.HideAndDontSave;
            var s = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(pivotPx.x / tex.width, pivotPx.y / tex.height), ppu, 0, SpriteMeshType.FullRect);
            s.name = "Flg_" + file; s.hideFlags = HideFlags.HideAndDontSave;
            return s;
        }

        public static void Build()
        {
            var all = Resources.LoadAll<SO_ItemWeapon>("");
            SO_ItemWeapon W(string n) => all.FirstOrDefault(w => w != null && w.name == n) ?? all.First(w => w != null && w.characterClass == CharacterClasses.BERSERKER);
            var effs = Resources.LoadAll<ItemEffectWeapon>("");
            var bleed = effs.FirstOrDefault(e => e != null && e.effect == WeaponEffects.Bloodletting);
            var stun = effs.FirstOrDefault(e => e != null && e.name.Contains("Stun Chance")) ?? effs.FirstOrDefault(e => e != null && (e.effect == WeaponEffects.stunningChance || e.effect == WeaponEffects.stan));
            var undead = effs.FirstOrDefault(e => e != null && e.effect == WeaponEffects.damageMod_Undead);
            if (undead != null)
            {
                Inquisition = UnityEngine.Object.Instantiate(undead);
                Inquisition.name = "Flagellant Effect - Inquisition";
                Inquisition.titleLocalized = new LocalizedString("Items/Flagellant/inquisition");
                Inquisition.descriptionLocalized = new LocalizedString("Items/Flagellant/inquisition_dsc");
                Inquisition.itemPostfixLocalized = new LocalizedString("Items/Flagellant/inquisition_post");
                Inquisition.path = InquisitionPath;
                Inquisition.hideFlags = HideFlags.HideAndDontSave;
            }
            FlagellantPlugin.Log.LogInfo("Flail effects: bloodletting=" + (bleed ? bleed.name : "-") + " stun=" + (stun ? stun.name : "-") + " inquisition=" + (Inquisition ? "made" : "-"));

            // (damage, crit, attack speed -10% = -1 on the game's -5..5 scale, grip, price, built-in effect)
            // all are one-handed (MRX: they look one-handed); ordered by strength, the rusty one is the starter
            Flails = new[]
            {
                Make(W("Bers1AxeIron"), "rustyFlail", 2.5f, 5.5f, 3, -3f, WeaponGrip.OneHanded, 350, bleed),
                Make(W("Bers2AxeBattle"), "pilgrimFlail", 3.5f, 6f, 4, -1f, WeaponGrip.OneHanded, 450, null),
                Make(W("Bers2AxeBattle"), "ritualScourge", 4f, 6f, 6, -1f, WeaponGrip.OneHanded, 600, null),
                Make(W("Bers3AxeBattleDouble"), "penitentChain", 5.5f, 8.5f, 3, -2f, WeaponGrip.OneHanded, 800, bleed),
                Make(W("Bers5AxeBlack"), "inquisitorFlail", 7f, 11.5f, 2, -4f, WeaponGrip.OneHanded, 1100, Inquisition),
                Make(W("Bers5.5Axe"), "thornStar", 9f, 14f, 4, -3f, WeaponGrip.OneHanded, 1400, bleed),
                Make(W("Bers8AxeGiant"), "devastator", 18.5f, 24f, 5, -3.5f, WeaponGrip.OneHanded, 2200, stun),
            };
            // gone from the game (MRX): no longer in the shop, the forge or the loot; a saved one still loads (so no run breaks)
            Retired = new[]
            {
                Make(W("Bers6AxeRune"), "martyrChain", 12f, 17f, 5, -2.5f, WeaponGrip.OneHanded, 1700, Inquisition),
                Make(W("Bers7Axe"), "doomBell", 15f, 21f, 3, -4f, WeaponGrip.OneHanded, 1950, stun),
            };
            FlagellantPlugin.Log.LogInfo("Flails ready: " + string.Join(", ", Flails.Select(f => f.name + " " + f._damageMin + "-" + f._damageMax + " " + f.gripType)));
            GrantBlueprints();
        }

        /// the user asked for all four flails in the shop, bought for gems: their blueprints count as found
        /// (the shop's item value 0 = not found, 1..100 = blueprint found and buyable, <0 bought, >100 equipped)
        static void GrantBlueprints()
        {
            const string flag = "FLG_flailBlueprints2";                               // (2: the five new flails too)
            if (Flag.Raw(() => FileBasedPrefs.GetInt(flag, 0)) == 1) return;
            var got = new List<string>();
            foreach (var f in Flails)
                if (Flag.Raw(() => FileBasedPrefs.GetInt(f.Name, 0)) == 0) { Flag.Raw(() => { FileBasedPrefs.SetInt(f.Name, 1); return 0; }); got.Add(f.Name); }
            Flag.Raw(() => { FileBasedPrefs.SetInt(flag, 1); return 0; });
            PlayerPrefs.Save();
            FlagellantPlugin.Log.LogInfo("Flail blueprints opened in the shop: " + (got.Count > 0 ? string.Join(", ", got) : "already open"));
        }

        /// where the fist holds it: near the end of the handle (pixels from the picture's bottom left)
        static Vector2 EquipPivot(string id)
        {
            var path = Path.Combine(FlagellantPlugin.Dir, "weapons", id + "_equip.png");
            try
            {
                var meta = path.Replace(".png", ".grip");
                if (File.Exists(meta)) { var p = File.ReadAllText(meta).Split(','); return new Vector2(float.Parse(p[0], System.Globalization.CultureInfo.InvariantCulture), float.Parse(p[1], System.Globalization.CultureInfo.InvariantCulture)); }
            }
            catch { }
            return new Vector2(16f, 16f);
        }

        static SO_ItemWeapon Make(SO_ItemWeapon src, string id, float min, float max, int crit, float spd, WeaponGrip grip, int price, ItemEffectWeapon effect)
        {
            var w = UnityEngine.Object.Instantiate(src);
            w.name = "Flg_" + id;
            w.Name = "Flg_" + id;
            w.hideFlags = HideFlags.HideAndDontSave;
            w.titleLocalized = new LocalizedString("Items/Flagellant/" + id);
            w.descriptionLocalized = new LocalizedString("Items/Flagellant/" + id + "_dsc");
            w.TitleRU = Texts.All.TryGetValue("Items/Flagellant/" + id, out var t) ? t.ru : id;
            w.TitleEN = t.en ?? id;
            var icon = Load(id + "_icon", 48f, new Vector2(12f, 12f));
            if (icon != null) w.Icon = icon;
            var eq = Load(id + "_equip", 42f, EquipPivot(id));
            if (eq != null) w.EquipSprite = eq;
            w._damageMin = min; w._damageMax = max;
            w.damageTypeMain = c.DamageTypes.Phys_bludge;
            w.damageTypeSecondary = c.DamageTypes._none; w.damageSecondaryMin = 0f; w.damageSecondaryMax = 0f;
            w._CriticalChanceReal = crit;
            w._SpeedAttackReal = spd;
            w.characterClass = CharacterClasses.BERSERKER;
            w.weaponType = WeaponTypes.melee;
            w.weaponTypeAdditional = WeaponTypesAdditional.hammer;
            w.gripType = grip;
            w.defaultEffects = effect != null ? new List<ItemEffectWeapon> { effect } : new List<ItemEffectWeapon>();
            w.effects = effect != null ? new List<ItemEffectWeapon> { effect } : new List<ItemEffectWeapon>();
            w.allPossibleEffects = new List<ItemEffectWeapon>(src.allPossibleEffects ?? new List<ItemEffectWeapon>());
            w.savingObjectPath = PathPrefix + w.name;
            w.PriceNormal = price;
            w.CanBeRecipe = true;
            w.drawInTable = true;
            w.itemColor = new Color(0.95f, 0.8f, 0.8f, 1f);
            try { Traverse.Create(w).Field("effectsLvl").SetValue(Activator.CreateInstance(AccessTools.Field(typeof(SO_ItemEquip), "effectsLvl").FieldType)); } catch { }
            return w;
        }

        public static UnityEngine.Object ByPath(string path)
        {
            if (string.IsNullOrEmpty(path)) return null;
            if (path == InquisitionPath || path.EndsWith("/Inquisition") && path.Contains("Flagellant")) return Inquisition;
            foreach (var f in Flails.Concat(Retired))
                if (f != null && (path == f.savingObjectPath || path == f.name)) return f;
            return null;
        }

        public static bool IsFlail(SO_Item item) => item != null && item.Name != null && item.Name.StartsWith("Flg_");

        /// The four flails are the Flagellant's own weapons: while he is the hero they are the weapon lists
        /// (shop, forge, loot); the Berserker keeps his axes.
        public static void Apply(bool on)
        {
            if (Flails.Length == 0) return;
            var dbs = new HashSet<ItemsDataBase>();
            try { var p = (Resources.Load("ItemsDB") as GameObject)?.GetComponent<ItemsDataBase>(); if (p != null) dbs.Add(p); } catch { }
            foreach (var d in Resources.FindObjectsOfTypeAll<ItemsDataBase>()) if (d != null) dbs.Add(d);
            foreach (var db in dbs)
            {
                if (!originals.ContainsKey(db) && db.equipmentWeapon_BERSERKER != Flails) originals[db] = (db.equipmentWeapon_BERSERKER, db.itemsWeapon_BERSERKER);
                if (on)
                {
                    db.equipmentWeapon_BERSERKER = Flails;
                    db.itemsWeapon_BERSERKER = Flails.Where(f => f == Flails[0] || Flag.Raw(() => FileBasedPrefs.GetInt(f.Name, 0)) != 0).ToList();
                }
                else if (originals.TryGetValue(db, out var o))
                {
                    db.equipmentWeapon_BERSERKER = o.eq;
                    db.itemsWeapon_BERSERKER = o.found;
                }
            }
        }

        /// Inquisition: the undead part is the game's own; the demons get the same bonus here
        public static float InquisitionMult(Character ch, Creature t)
        {
            try
            {
                if (Inquisition == null || ch?.Weapon == null || !(t is Mob m) || m.mobClass != Mob.MobClassess.demon) return 1f;
                var eff = ch.Weapon.effects?.FirstOrDefault(e => e != null && e.name == Inquisition.name);
                if (eff == null) return 1f;
                return 1f + Mathf.Max(0f, ch.Weapon.GetEffectCustomValue(eff)) * 0.01f;
            }
            catch { return 1f; }
        }

        /// the Flagellant fights with his flails: without one in hand (e.g. the Berserker's starting axe) he gets the rusty flail
        public static void Ensure(Character ch)
        {
            if (!Flag.Active || ch == null || ch.charClass != CharacterClasses.BERSERKER || Flails.Length == 0) return;
            if (IsFlail(ch.Weapon) && !IsWhip(ch.Weapon)) return;
            try
            {
                var w = SO_ItemWeapon.Copy(Flails[0], null);
                ch.EquipItem(w);
                FlagellantPlugin.Log.LogInfo("Flagellant: rusty flail in hand");
            }
            catch (Exception e) { FlagellantPlugin.Log.LogWarning("Ensure flail: " + e.Message); }
        }
    }

    /// saved items and effects of the Flagellant (they are not in the game's Resources)
    [HarmonyPatch]
    internal static class ResourcesLoadPatch
    {
        static System.Reflection.MethodBase TargetMethod() =>
            AccessTools.Method(typeof(ResourcesAPI), "Load", new[] { typeof(string), typeof(Type) });

        static bool Prefix(string path, Type systemTypeInstance, ref UnityEngine.Object __result)
        {
            if (path == null || (!path.StartsWith(Weapons.PathPrefix) && !path.StartsWith("Flg_") && path != Weapons.InquisitionPath)) return true;
            var o = Weapons.ByPath(path);
            if (o == null || (systemTypeInstance != null && !systemTypeInstance.IsInstanceOfType(o))) return true;
            __result = o;
            return false;
        }
    }

    /// Inquisition on every blow of that flail (any hero): the undead part is the game's own effect, demons here
    [HarmonyPatch(typeof(Creature), nameof(Creature.SetDamageRPC))]
    internal static class InquisitionPatch
    {
        static void Prefix(Creature __instance, ref DamageStruct damageStruct, UnityEngine.Object source)
        {
            try
            {
                if (Weapons.Inquisition == null || !(__instance is Mob m) || m.mobClass != Mob.MobClassess.demon) return;
                if (!(source is PlayerController pc) || !(pc._creature is Character ch)) return;
                float k = Weapons.InquisitionMult(ch, __instance);
                if (k > 1.0001f) damageStruct = damageStruct * k;
            }
            catch { }
        }
    }

    [HarmonyPatch(typeof(Character), nameof(Character.EquipItem))]
    internal static class WhipLeftHandPatch
    {
        static bool busy;
        static void Postfix(Character __instance, SO_ItemEquip item)
        {
            if (busy) return;
            try
            {
                var ch = __instance;
                if (ch == null || !(ch.ctrl is PlayerController) || ch.charClass != CharacterClasses.BERSERKER || !Flag.Active) return;
                if (!Weapons.IsWhip(ch.Weapon)) return;
                busy = true;
                var whip = ch.Weapon;
                if (ch.ShieldTemp == null) { ch.ShieldTemp = ScriptableObject.CreateInstance<SO_ItemShield>(); ch.ShieldTemp.EquipSlot = EquipmentSlots.Arrows; }
                else ch.ShieldTemp.defaultSkill = null;
                var other = ch.ShieldWeapon;
                ch.ShieldTemp.Init(whip);
                ch.Shield = ch.ShieldTemp;
                ch.ShieldWeapon = whip;
                ch.Weapon = other != null && !Weapons.IsWhip(other) ? other : null;      // the flail that was in the left hand goes to the right
                try { ch.RightHandSprite.sprite = ch.Weapon != null ? ch.Weapon.EquipSprite : null; ch.RightHandSprite.flipX = false; } catch { }
                try { ch.LeftHandSprite.sprite = whip.EquipSprite; ch.LeftHandSprite.flipX = true; } catch { }
                FlagellantPlugin.Log.LogInfo("Ritual Scourge: into the left hand" + (ch.Weapon != null ? ", " + ch.Weapon.Name + " into the right" : ""));
            }
            catch (System.Exception e) { FlagellantPlugin.Log.LogWarning("Whip to the left hand: " + e.Message); }
            finally { busy = false; }
        }
    }

    [HarmonyPatch(typeof(Character), nameof(Character.LoadItems))]
    internal static class LoadItemsPatch
    {
        static void Postfix(Character __instance)
        {
            try
            {
                if (__instance == null || !(__instance.ctrl is PlayerController)) return;
                if (__instance.charClass == CharacterClasses.BERSERKER)
                {
                    var db = (Resources.Load("SkillsDB") as GameObject)?.GetComponent<SkillsDataBase>();
                    FlagellantPlugin.Log.LogInfo("Hero in the run: Berserker engine, Flagellant active=" + Flag.Active +
                        ", skill tree=" + (db != null && FlagSkills.Data != null && db.skills_berserk == FlagSkills.Data ? "FLAGELLANT" : "berserker") +
                        ", weapon=" + (__instance.Weapon != null ? __instance.Weapon.Name : "-"));
                }
                Weapons.Ensure(__instance);
            }
            catch (System.Exception e) { FlagellantPlugin.Log.LogWarning("LoadItems: " + e.Message); }
        }
    }
}

namespace FlagellantMod
{
    /// (the user, 2.6.5) the Cultist's second-hand slot is for whips only - like the hunter's bolts, the archer's arrows,
    /// the wizard's book; his main slot takes any weapon as usual. The game puts a Berserker's second one-handed weapon
    /// into the second hand: for the Cultist an ordinary weapon now takes the main hand's place instead (only whips go
    /// to the second hand - they are moved there as before).
    [HarmonyLib.HarmonyPatch(typeof(InventoryManager), nameof(InventoryManager.EquipThisItemButton))]
    internal static class WhipsOnlyInSecondHandPatch
    {
        static readonly AccessTools.FieldRef<InventoryManager, Character> CharRef = AccessTools.FieldRefAccess<InventoryManager, Character>("character");

        static void Prefix(InventoryManager __instance, ref Character __state)
        {
            __state = null;
            try
            {
                var ch = CharRef(__instance);
                if (ch == null || !Flag.Active || ch.charClass != CharacterClasses.BERSERKER || !(ch.ctrl is PlayerController)) return;
                int i = __instance.selectedIndex;
                if (i < 0 || i >= ch.inventory.Count) return;
                if (!(ch.inventory[i] is SO_ItemWeapon w) || Weapons.IsWhip(w) || ch.Weapon == null) return;
                ch.charClass = CharacterClasses.WARRIOR;                 // for this one call: no second weapon in the other hand
                __state = ch;
            }
            catch (System.Exception e) { FlagellantPlugin.Log.LogWarning("Second hand: " + e.Message); }
        }

        static void Finalizer(Character __state)
        {
            if (__state != null) __state.charClass = CharacterClasses.BERSERKER;
        }
    }
}
