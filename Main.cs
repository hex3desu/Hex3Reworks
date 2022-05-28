using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using R2API;
using R2API.Utils;
using RoR2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using Hex3Reworks.Logging;
using Hex3Reworks.Helpers;

namespace Hex3Reworks
{
    [BepInPlugin(ModGuid, ModName, ModVer)]
    [BepInDependency(R2API.R2API.PluginGUID, R2API.R2API.PluginVersion)]
    [BepInDependency("com.Moffein.EliteReworks", BepInDependency.DependencyFlags.SoftDependency)]
    [NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.EveryoneNeedSameModVersion)]
    [R2APISubmoduleDependency(nameof(ItemAPI), nameof(LanguageAPI), nameof(RecalculateStatsAPI), nameof(PrefabAPI))]
    public class Main : BaseUnityPlugin
    {
        public const string ModGuid = "com.Hex3.Hex3Reworks";
        public const string ModName = "Hex3Reworks";
        public const string ModVer = "0.1.0";

        public static ManualLogSource logger;

        private static void PrintLog(int arrayPointer, bool enabled) // Prints log infos for enabled/disabled reworks to make our lines shorter
        {
            string[] names = new string[3]
            { 
                "Mercurial Rachis",
                "Lepton Daisy",
                "Needletick"
            };

            if (enabled == true){ Log.LogInfo("Modifying " + names[arrayPointer] + "..."); }
            else { Log.LogInfo(names[arrayPointer] + " changes disabled."); }
        }

        // Uncommon
        public ConfigEntry<bool> LeptonDaisy_Enable() { return Config.Bind<bool>(new ConfigDefinition("Lunar - Lepton Daisy", "Enable Changes"), true, new ConfigDescription("Enables changes to Lepton Daisy.", null, Array.Empty<object>())); }
        public ConfigEntry<float> LeptonDaisy_WeakDuration() { return Config.Bind<float>(new ConfigDefinition("Lunar - Lepton Daisy", "Weaken Duration"), 10f, new ConfigDescription("How long enemies should be weakened by each healing nova, in seconds.", null, Array.Empty<object>())); }

        // Lunar
        public ConfigEntry<bool> MercurialRachis_Enable() { return Config.Bind<bool>(new ConfigDefinition("Lunar - Mercurial Rachis", "Enable Changes"), true, new ConfigDescription("Enables changes to Mercurial Rachis.", null, Array.Empty<object>())); }
        public ConfigEntry<bool> MercurialRachis_IsTonic() { return Config.Bind<bool>(new ConfigDefinition("Lunar - Mercurial Rachis", "Apply Spinel Tonic Buff"), true, new ConfigDescription("Apply a Spinel Tonic buff instead of the vanilla Power Ward buff.", null, Array.Empty<object>())); }
        public ConfigEntry<float> MercurialRachis_Radius() { return Config.Bind<float>(new ConfigDefinition("Lunar - Mercurial Rachis", "Radius"), 20f, new ConfigDescription("Radius of Mercurial Rachis zone in meters.", null, Array.Empty<object>())); }
        public ConfigEntry<float> MercurialRachis_PlacementRadius() { return Config.Bind<float>(new ConfigDefinition("Lunar - Mercurial Rachis", "Placement Radius"), 5f, new ConfigDescription("The maximum range from the player at which Mercurial Rachis places its wards, in meters.", null, Array.Empty<object>())); }

        // Void
        public ConfigEntry<bool> NeedleTick_Enable() { return Config.Bind<bool>(new ConfigDefinition("Void - NeedleTick", "Enable Changes"), true, new ConfigDescription("Enables changes to Needletick.", null, Array.Empty<object>())); }
        public ConfigEntry<bool> NeedleTick_FirstHit() { return Config.Bind<bool>(new ConfigDefinition("Void - NeedleTick", "Inflict Collapse on First Hit"), true, new ConfigDescription("Collapse will be inflicted when you first hit an enemy.", null, Array.Empty<object>())); }
        public ConfigEntry<int> NeedleTick_InflictInterval() { return Config.Bind<int>(new ConfigDefinition("Void - NeedleTick", "Collapse Interval"), 10, new ConfigDescription("Amount of hits needed before Collapse is inflicted again. (May break at low values)", null, Array.Empty<object>())); }
        public ConfigEntry<bool> NeedleTick_VoidEnemiesCollapse() { return Config.Bind<bool>(new ConfigDefinition("Void - NeedleTick", "Voidtouched enemies can use new Collapse"), false, new ConfigDescription("Void enemies will collapse on first hit, and on every 10th hit afterwards. If false, enemies will use vanilla Collapse.", null, Array.Empty<object>())); }

        public void Awake()
        {
            Log.Init(Logger);

            // Uncommon
            if (LeptonDaisy_Enable().Value == true) { PrintLog(1, true); Reworks.LeptonDaisy.Initiate(LeptonDaisy_WeakDuration().Value); } else { PrintLog(1, false); }
            // Lunar
            if (MercurialRachis_Enable().Value == true) { PrintLog(0, true); Reworks.MercurialRachis.Initiate(MercurialRachis_Radius().Value, MercurialRachis_PlacementRadius().Value, MercurialRachis_IsTonic().Value); } else { PrintLog(0, false); }
            // Void
            if (NeedleTick_Enable().Value == true) { PrintLog(2, true); Reworks.NeedleTick.Initiate(NeedleTick_FirstHit().Value, NeedleTick_InflictInterval().Value, NeedleTick_VoidEnemiesCollapse().Value); } else { PrintLog(2, false); }

            Log.LogInfo("Done!");
        }
    }
}
