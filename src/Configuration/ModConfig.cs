using BepInEx.Configuration;

namespace VikingHusbandry.Configuration
{
    public static class ModConfig
    {
        public static ConfigEntry<bool> ModEnabled { get; private set; } = null!;

        // 1 - Companion Stances
        public static ConfigEntry<bool> EnableStances { get; private set; } = null!;
        public static ConfigEntry<bool> AllowBoarCommands { get; private set; } = null!;
        public static ConfigEntry<float> GuardLeashRadius { get; private set; } = null!;

        // 2 - Rodeo Subdual & Taming
        public static ConfigEntry<bool> EnableRodeoSubdual { get; private set; } = null!;
        public static ConfigEntry<float> RodeoStaminaDrain { get; private set; } = null!;
        public static ConfigEntry<float> RodeoDuration { get; private set; } = null!;
        public static ConfigEntry<float> BuckDamage { get; private set; } = null!;
        public static ConfigEntry<float> DownedDuration { get; private set; } = null!;

        // 3 - Feeding Troughs & Paddock Infrastructure
        public static ConfigEntry<bool> EnableTroughs { get; private set; } = null!;
        public static ConfigEntry<float> ScentRadius { get; private set; } = null!;
        public static ConfigEntry<bool> AntiChuteHorizontalOnly { get; private set; } = null!;
        public static ConfigEntry<float> PaddockRadius { get; private set; } = null!;

        public static void Init(ConfigFile config)
        {
            ModEnabled = Bind(
                config,
                "1 - General",
                "Enabled",
                true,
                "Master switch to enable or disable all features of VikingHusbandry.",
                order: 100,
                isAdminOnly: true
            );

            EnableStances = Bind(
                config,
                "2 - Companion Stances",
                "EnableStances",
                true,
                "Enable cycling through 4 companion stances (Follow Aggressive, Follow Passive, Stay Guard, Stay Passive) on interaction.",
                order: 90
            );

            AllowBoarCommands = Bind(
                config,
                "2 - Companion Stances",
                "AllowBoarCommands",
                true,
                "Allow boars to be commanded like wolves and asksvin instead of only petted.",
                order: 85
            );

            GuardLeashRadius = Bind(
                config,
                "2 - Companion Stances",
                "GuardLeashRadius",
                18.0f,
                "Maximum distance (in meters) a guarding creature can pursue enemies before leashing back to its guard post.",
                order: 80
            );

            EnableRodeoSubdual = Bind(
                config,
                "3 - Rodeo Subdual",
                "EnableRodeoSubdual",
                true,
                "Enable active rodeo subdual on staggered or unaware wild creatures.",
                order: 70
            );

            RodeoStaminaDrain = Bind(
                config,
                "3 - Rodeo Subdual",
                "RodeoStaminaDrain",
                10.0f,
                "Player stamina drained per second while holding onto a thrashing beast.",
                order: 65
            );

            RodeoDuration = Bind(
                config,
                "3 - Rodeo Subdual",
                "RodeoDuration",
                10.0f,
                "Total seconds of mounted struggle required to reach 100% subdual progress and down the beast.",
                order: 60
            );

            BuckDamage = Bind(
                config,
                "3 - Rodeo Subdual",
                "BuckDamage",
                5.0f,
                "Blunt damage taken by the player when bucked off due to stamina exhaustion or releasing grip.",
                order: 55
            );

            DownedDuration = Bind(
                config,
                "3 - Rodeo Subdual",
                "DownedDuration",
                20.0f,
                "Duration in seconds that the creature remains collapsed in the Downed state awaiting food.",
                order: 50
            );

            EnableTroughs = Bind(
                config,
                "4 - Troughs & Paddocks",
                "EnableTroughs",
                true,
                "Allow wooden chests to be toggled into Feeding Troughs via Shift + E.",
                order: 40
            );

            ScentRadius = Bind(
                config,
                "4 - Troughs & Paddocks",
                "ScentRadius",
                20.0f,
                "Radius (in meters) within which hungry livestock can detect food inside troughs or on the ground through fences.",
                order: 35
            );

            AntiChuteHorizontalOnly = Bind(
                config,
                "4 - Troughs & Paddocks",
                "AntiChuteHorizontalOnly",
                true,
                "Enforce procreation density checks using a 2D horizontal cylinder, preventing vertical breeding chute exploits.",
                order: 30
            );

            PaddockRadius = Bind(
                config,
                "4 - Troughs & Paddocks",
                "PaddockRadius",
                20.0f,
                "Horizontal radius (in meters) checked around troughs or breeding pairs for population capping.",
                order: 25
            );
        }

        private static ConfigEntry<T> Bind<T>(
            ConfigFile config,
            string section,
            string key,
            T defaultValue,
            string description,
            int order = 0,
            AcceptableValueBase? acceptableValues = null,
            bool isAdminOnly = false)
        {
            var tags = new ConfigurationManagerAttributes
            {
                Order = order,
                Category = section,
                IsAdminOnly = isAdminOnly
            };
            var configDesc = new ConfigDescription(description, acceptableValues, tags);
            return config.Bind(section, key, defaultValue, configDesc);
        }
    }
}
