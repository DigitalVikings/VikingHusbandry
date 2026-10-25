using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using VikingHusbandry.Configuration;

namespace VikingHusbandry
{
    [BepInPlugin(ModGuid, ModName, ModVersion)]
    [BepInDependency("com.jotunn.jotunn", BepInDependency.DependencyFlags.SoftDependency)]
    public class VikingHusbandryPlugin : BaseUnityPlugin
    {
        public const string ModGuid = "com.digitalvikings.vikinghusbandry";
        public const string ModName = "VikingHusbandry";
        public const string ModVersion = "0.1.0";

        internal static ManualLogSource Log = null!;
        private Harmony _harmony = null!;

        private void Awake()
        {
            Log = Logger;
            Log.LogInfo($"{ModName} v{ModVersion} initializing...");

            // Initialize configuration
            ModConfig.Init(Config);

            // Apply Harmony patches
            _harmony = new Harmony(ModGuid);
            _harmony.PatchAll();

            Log.LogInfo($"{ModName} v{ModVersion} loaded successfully.");

            // Listen for server synchronization events if Jotunn is active
            if (BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("com.jotunn.jotunn"))
            {
                HookJotunnSync();
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private void HookJotunnSync()
        {
            Jotunn.Managers.SynchronizationManager.OnConfigurationSynchronized += (sender, args) =>
            {
                Log.LogInfo("Server configuration synchronized successfully.");
            };
        }

        private void OnDestroy()
        {
            _harmony?.UnpatchSelf();
        }
    }
}
