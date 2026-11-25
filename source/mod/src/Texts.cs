using System.Collections.Generic;
using HarmonyLib;
using I2.Loc;

namespace FlagellantMod
{
    internal static class Texts
    {
        public const string Name = "Characters/Classes/Flagellant";
        public const string Details = "Characters/Classes/FlagellantDetails";
        const string S = "Characters/Skills/Flagellant/";

        public static readonly Dictionary<string, (string ru, string en)> All = new Dictionary<string, (string, string)>
        {
            [Name] = ("Культист", "Cultist"),
            [Details] = (
                "Культист, превративший свою боль в оружие. Чем меньше в нём крови, тем страшнее его удары: он платит за силу собственным здоровьем и обращает на врагов гнев небес.\n\nОсновная характеристика - Выносливость. Регенерация здоровья +2 ОЗ/с.",
                "A cultist who turned his pain into a weapon. The less blood is left in him, the more terrible his blows: he pays for power with his own health and calls the wrath of heaven upon his foes.\n\nMain attribute - Endurance. Health regeneration +2 HP/s."),

            // ---- skill descriptions: what the skill does; the numbers are the stat lines under it (FLG/Line/...) ----
            // ---- main ----
            [S + "bloodStrike"] = ("Кровавый удар", "Bloody Strike"),
            [S + "bloodStrike_dsc"] = (
                "Культист наносит удар с замахом кровавого цвета.\nДлительность: мгновенная.",
                "The Cultist strikes with a blood-coloured swing.\nDuration: instant."),
            [S + "poisonLash"] = ("Ядовитый хлёст", "Poison Lash"),
            [S + "poisonLash_dsc"] = (
                "Культист совершает хлыстом в левой руке два взмаха: один справа налево, другой слева направо. Каждый удар наносит урон и заполняет шкалу отравления.\nДлительность: мгновенная.",
                "The Cultist makes two swings: right to left, then left to right. Each blow deals damage and fills the poison gauge.\nDuration: instant."),
            [S + "penanceHammer"] = ("Молот покаяния", "Hammer of Penance"),
            [S + "penanceHammer_dsc"] = (
                "Культист заносит тяжёлый шипастый шар над головой и с размаху обрушивает его на землю перед собой.\nДлительность: увеличенная (примерно на 80% дольше обычной атаки, зарядка не требуется).",
                "The Cultist raises a heavy spiked ball over his head and brings it crashing down on the ground in front of him.\nDuration: longer (about 80% longer than a normal attack, no charging needed)."),
            [S + "sinnerStrike"] = ("Удар Грешника", "Sinner's Strike"),
            [S + "sinnerStrike_dsc"] = (
                "Культист совершает серию из трёх ударов, каждый из которых сильно отталкивает врагов и переносит его к следующей цели. Если других целей рядом нет, он бьёт ту же цель ещё раз.\nДлительность: мгновенная.",
                "The Cultist makes a series of three blows, each knocking enemies far back and carrying him to the next target. With no other target near, he strikes the same one again.\nDuration: instant."),

            // ---- special ----
            [S + "bloodRain"] = ("Кровавый дождь", "Blood Rain"),
            [S + "bloodRain_dsc"] = (
                "Культист выпускает частичку своей крови, которая сама наводится на врагов, пронзая их плоть и кости.\nДлительность: мгновенная.",
                "The Cultist releases a drop of his own blood that homes in on enemies, piercing flesh and bone.\nDuration: instant."),
            [S + "martyrHand"] = ("Длань Мученика", "Martyr's Hand"),
            [S + "martyrHand_dsc"] = (
                "Культист наносит мощный удар в треугольной области перед собой: 50% дробящего и 50% режущего урона. Чем ниже здоровье, тем выше урон.\nТребует зарядки.",
                "The Cultist strikes hard in a triangle in front of him: 50% blunt and 50% slashing damage. The lower his health, the higher the damage.\nRequires charging."),
            [S + "divinePunishment"] = ("Божья кара", "Divine Punishment"),
            [S + "divinePunishment_dsc"] = (
                "Культист бьёт себя кулаком в грудь и собственной кровью обращает на врагов гнев божий.\nТребует зарядки.",
                "The Cultist beats his chest with his fist and, with his own blood, calls the wrath of God upon his foes.\nRequires charging."),

            // ---- power ----
            [S + "torment"] = ("Муки", "Torment"),
            [S + "torment_dsc"] = (
                "Культист стягивает к себе свою и вражескую кровь и производит большой взрыв. Каждый враг в радиусе захвата отдаёт ему единицу крови.\nТребует зарядки.",
                "The Cultist pulls in his own and his enemies' blood and makes a great explosion. Every enemy in the pull radius gives him a unit of blood.\nRequires charging."),
            [S + "suffering"] = ("Религиозный экстаз", "Religious Ecstasy"),
            [S + "suffering_dsc"] = (
                "Культист хлещет себя цепом и впадает в религиозный экстаз, тело его багровеет. Чем меньше у него здоровья, тем сильнее безумие.",
                "The Cultist lashes himself with the flail and falls into religious ecstasy, his body turns crimson. The less health he has, the stronger the frenzy."),
            [S + "quickSnap"] = ("Быстрый Щелчок", "Quick Snap"),
            [S + "quickSnap_dsc"] = (
                "Культист делает хлыстом в левой руке молниеносный выпад строго по прямой линии на большую дистанцию. Кончик хлыста наносит точечный удар по первой цели на пути.\nДлительность: мгновенная (ускоренная анимация).",
                "The Cultist makes a lightning lunge with the lash in a dead straight line over a long distance. The tip of the whip strikes the first target on its way.\nDuration: instant (sped-up animation)."),
            ["FLG/Line/snapLine"] = ("Удар по прямой на большую дистанцию, по первой цели на пути", "Strikes in a straight line far ahead, the first target on the way"),
            ["FLG/Line/snapCrit"] = ("Удар на максимальной дистанции всегда критический", "A hit at the maximum distance is always critical"),
            ["FLG/Line/snapSpeed"] = ("Каждый удар: +{VALUE}% к скорости атаки на 4 сек. (до 2 раз)", "Every hit: +{VALUE}% attack speed for 4 sec. (stacks twice)"),
            [S + "bloodCleansing"] = ("Очищение кровью", "Blood Cleansing"),
            [S + "bloodCleansing_dsc"] = (
                "Культист вытягивает руку и проводит по ней кинжалом. С кровью уходят все негативные эффекты.\nТребует зарядки.",
                "The Cultist stretches out his arm and draws a dagger along it. All negative effects leave with the blood.\nRequires charging."),

            // ---- stat lines under the descriptions ({VALUE} = the value for the level, with the next level's) ----
            ["FLG/Line/hits"] = ("Количество ударов: {VALUE}", "Number of blows: {VALUE}"),
            ["FLG/Line/whipReach"] = ("Хлыст бьёт на всю свою длину, перед собой", "The lash hits as far as it reaches, in front of him"),
            ["FLG/Line/undead"] = ("Урон по нежити: +{VALUE}%", "Damage to the undead: +{VALUE}%"),
            ["FLG/Line/heavyCrit"] = ("Шанс крита по целям в тяжёлой броне: +{VALUE}%", "Crit chance against heavily armoured targets: +{VALUE}%"),
            ["FLG/Line/hpCost"] = ("Стоимость: {VALUE}% ОЗ", "Cost: {VALUE}% HP"),
            ["FLG/Line/hpCostMax"] = ("Стоимость: {VALUE}% от макс. ОЗ", "Cost: {VALUE}% of max HP"),
            ["FLG/Line/homing"] = ("Снаряд сам наводится на врагов", "The projectile homes in on enemies"),
            ["FLG/Line/repierce"] = ("Может пронзать одну и ту же цель несколько раз", "Can pierce the same target several times"),
            ["FLG/Line/triangle"] = ("Удар в треугольной области перед собой", "Hits a triangle in front of him"),
            ["FLG/Line/missingHp"] = ("Бонус к урону за каждый 1% недостающего ОЗ: {VALUE}%", "Damage bonus per 1% of missing HP: {VALUE}%"),
            ["FLG/Line/killHeal"] = ("Убийство при ОЗ ниже 30% восстанавливает {VALUE} ОЗ", "A kill below 30% HP restores {VALUE} HP"),
            ["FLG/Line/killRegen"] = ("Убийство даёт +{VALUE} ОЗ/с на 5 секунд", "A kill gives +{VALUE} HP/s for 5 seconds"),
            ["FLG/Line/wrathCost"] = ("Стоимость: {VALUE}% ОЗ за каждого поражённого врага", "Cost: {VALUE}% HP per enemy hit"),
            ["FLG/Line/stun"] = ("Шанс оглушения: {VALUE}%", "Stun chance: {VALUE}%"),
            ["FLG/Line/bloodPull"] = ("Радиус захвата крови: {VALUE}%", "Blood pull radius: {VALUE}%"),
            ["FLG/Line/bloodGain"] = ("За каждую единицу крови: +{VALUE}% к урону и радиусу взрыва", "For every unit of blood: +{VALUE}% damage and explosion radius"),
            ["FLG/Line/duration"] = ("Длительность: {VALUE} сек.", "Duration: {VALUE} sec."),
            ["FLG/Line/ecstasyDef"] = ("Получаемый урон снижается на {VALUE}% за каждый 1% недостающего ОЗ (не больше 75%)", "Damage taken is reduced by {VALUE}% per 1% of missing HP (at most 75%)"),
            ["FLG/Line/regen"] = ("Регенерация здоровья: +{VALUE}%", "Health regeneration: +{VALUE}%"),
            ["FLG/Line/cleanse"] = ("Снимает все негативные эффекты", "Removes all negative effects"),
            ["FLG/Line/resist"] = ("Сопротивление яду, кровотечению и окаменению: +{VALUE}", "Resistance to poison, bleeding and petrification: +{VALUE}"),
            ["FLG/Line/resistTime"] = ("Длительность сопротивления: {VALUE} сек.", "Resistance lasts: {VALUE} sec."),
            ["FLG/Line/cleanseBonus"] = ("За каждый снятый эффект: +{VALUE}% к урону на 15 секунд", "For every removed effect: +{VALUE}% damage for 15 seconds"),

            // ---- class perks (the game's perk cards: title + one line per effect, {0} = the value) ----
            ["PassiveSkills/Titles/7000"] = ("Греховный сосуд", "Sinful Vessel"),
            ["PassiveSkills/Titles/7001"] = ("Кровавый пир", "Blood Feast"),
            ["FLG/Perk/vesselHp"] = ("За каждую отсутствующую часть брони (шлем, доспех): +{0}% к максимальному здоровью",
                                     "For every missing armour piece (helmet, body armour): +{0}% max health"),
            ["FLG/Perk/vesselDef"] = ("За каждую отсутствующую часть брони получаемый урон меньше на {0}%",
                                      "For every missing armour piece damage taken is reduced by {0}%"),
            ["FLG/Perk/vesselRegen"] = ("За каждую отсутствующую часть брони: <color=yellow>+1,5</color> ОЗ/с к регенерации",
                                        "For every missing armour piece: <color=yellow>+1.5</color> HP/s regeneration"),
            ["FLG/Perk/feastBleed"] = ("Стоя в луже крови (своей или вражеской), заполняет шкалу кровотечения врагов на {0}% эффективнее",
                                       "Standing in a blood puddle (his own or an enemy's), he fills the enemies' bleeding bar {0}% more effectively"),
            ["FLG/Perk/feastSpeed"] = ("Стоя в луже крови: +{0}% к скорости атаки",
                                       "Standing in a blood puddle: +{0}% attack speed"),
            ["FLG/Perk/feastAll"] = ("Работает не только в лужах крови, но и в лужах яда и плазмы",
                                     "Works not only in blood puddles but in poison and plasma puddles too"),

            // ---- weapons ----
            ["Items/Flagellant/rustyFlail"] = ("Ржавый кистень грешника", "Sinner's Rusty Flail"),
            ["Items/Flagellant/rustyFlail_dsc"] = ("Обычный кистень, покрытый ржавчиной и чужой кровью.", "An ordinary flail covered in rust and someone else's blood."),
            ["Items/Flagellant/ritualScourge"] = ("Обрядовая плеть", "Ritual Scourge"),
            ["Items/Flagellant/ritualScourge_dsc"] = ("Многохвостый кожаный жгут для обрядов покаяния. Держится в левой руке: им хлещут «Ядовитый хлёст» и «Быстрый щелчок».", "A many-tailed leather scourge for rites of penance. Held in the left hand: Poison Lash and Quick Snap are struck with it."),
            ["Items/Flagellant/inquisitorFlail"] = ("Цеп Инквизитора", "Inquisitor's Flail"),
            ["Items/Flagellant/inquisitorFlail_dsc"] = ("Тяжёлое кадило на толстой звеньевой привязи.", "A heavy censer on a thick chain."),
            ["Items/Flagellant/devastator"] = ("Опустошитель", "Devastator"),
            ["Items/Flagellant/devastator_dsc"] = ("Огромный кистень, покрытый запёкшейся кровью.", "A huge flail caked with dried blood."),
            ["Items/Flagellant/pilgrimFlail"] = ("Кистень паломника", "Pilgrim's Flail"),
            ["Items/Flagellant/pilgrimFlail_dsc"] = ("Простое древко, пеньковая верёвка и железный шар с заклёпками. С ним паломники уходят в долгий путь покаяния.", "A plain haft, a hemp cord and a studded iron ball. Pilgrims carry it on the long road of penance."),
            ["Items/Flagellant/penitentChain"] = ("Цепь кающегося", "Penitent's Chain"),
            ["Items/Flagellant/penitentChain_dsc"] = ("Голая железная цепь с крюком на конце. Рвёт плоть - свою и чужую.", "A bare iron chain with a hook at its end. It tears flesh - one's own and others'."),
            ["Items/Flagellant/thornStar"] = ("Терновый моргенштерн", "Thorned Morningstar"),
            ["Items/Flagellant/thornStar_dsc"] = ("Бронзовый шар, поросший терновником. Каждая рана от него кровоточит.", "A bronze ball overgrown with thorns. Every wound it leaves keeps bleeding."),
            ["Items/Flagellant/martyrChain"] = ("Вериги мученика", "Martyr's Fetters"),
            ["Items/Flagellant/martyrChain_dsc"] = ("Тяжёлые вериги святого мученика с позолоченным крестом. Нежить и демоны не выносят его ударов.", "The heavy fetters of a holy martyr with a gilded cross. The undead and demons cannot bear its blows."),
            ["Items/Flagellant/doomBell"] = ("Колокол Судного дня", "Doomsday Bell"),
            ["Items/Flagellant/doomBell_dsc"] = ("Бронзовый колокол на цепи. Его звон оглушает грешников.", "A bronze bell on a chain. Its toll stuns the sinners."),
            ["Items/Flagellant/inquisition"] = ("Инквизиция", "Inquisition"),
            ["Items/Flagellant/inquisition_dsc"] = ("Каждый удар наносит на {0}% больше урона нежити и демонам", "Every blow deals {0}% more damage to the undead and demons"),
            ["Items/Flagellant/inquisition_post"] = ("Инквизиции", "of Inquisition"),

            // ---- the guild building ----
            ["Fortress/Buildings/ClassFlagellant"] = ("Обитель Культистов", "Cultist Cloister"),
            ["Fortress/Descs/ClassFlagellant"] = (
                "Мрачная обитель, где кающиеся грешники обращают боль в силу. Открывает класс Культист и новые уровни его навыков.",
                "A grim cloister where penitent sinners turn pain into strength. Unlocks the Cultist class and new levels of his skills."),

            // ---- popups ----
            ["FLG/Cleansed"] = ("Очищение", "Cleansed"),
            ["FLG/Ecstasy"] = ("Экстаз!", "Ecstasy!"),
            ["Effects/Flagellant/ecstasy"] = ("Религиозный экстаз", "Religious Ecstasy"),
            ["Effects/Flagellant/ecstasy_dsc"] = ("Чем меньше здоровья, тем больше урон", "The less health, the more damage"),
            ["FLG/NoBlood"] = ("Мало крови", "Not enough blood"),
        };

        private static readonly Dictionary<string, string> WhenActive = new Dictionary<string, string>
        {
            ["Characters/Classes/Berserker"] = Name,
            ["Characters/Classes/BerserkerDetails"] = Details,
        };

        public static bool IsRu
        {
            get
            {
                try { var code = LocalizationManager.CurrentLanguageCode; return code != null && code.StartsWith("ru"); }
                catch { return true; }
            }
        }

        public static int NoSwap;

        public static bool TryGet(string term, string overrideLanguage, out string text)
        {
            text = null;
            if (term == null) return false;
            if (NoSwap == 0 && Flag.Active && WhenActive.TryGetValue(term, out var mapped)) term = mapped;
            if (!All.TryGetValue(term, out var t)) return false;
            bool ru = overrideLanguage != null ? overrideLanguage.StartsWith("Rus") : IsRu;
            text = ru ? t.ru : t.en;
            return true;
        }

        public static string Get(string term) => TryGet(term, null, out var t) ? t : term;
    }

    [HarmonyPatch(typeof(LocalizationManager), nameof(LocalizationManager.TryGetTranslation))]
    internal static class TranslationPatch
    {
        static bool Prefix(string Term, ref string Translation, string overrideLanguage, ref bool __result)
        {
            if (Texts.TryGet(Term, overrideLanguage, out var text)) { Translation = text; __result = true; return false; }
            return true;
        }
    }
}
