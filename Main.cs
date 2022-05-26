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

namespace Hex3Reworks
{
    [BepInPlugin(ModGuid, ModName, ModVer)]
    [BepInDependency(R2API.R2API.PluginGUID, R2API.R2API.PluginVersion)]
    [NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.EveryoneNeedSameModVersion)]
    [R2APISubmoduleDependency(nameof(ItemAPI), nameof(LanguageAPI), nameof(RecalculateStatsAPI), nameof(PrefabAPI))]
    public class Main : BaseUnityPlugin
    {
        public const string ModGuid = "com.Hex3.Hex3Reworks";
        public const string ModName = "Hex3Reworks";
        public const string ModVer = "0.0.1";

        public static ManualLogSource logger;

        /*
        Rules of the Rework:
        1 - Never ADD a new system to the game! To avoid overcomplicating things, just use what already exists in Vanilla.
        2 - Items should still be FUN and POWERFUL. Items that are always OP should be narrowed to specific circumstances, underpowered items should be given a niche.
        3 - You should ALWAYS have a reason to take a lunar item. Rather than flat stat changes, lunars should change how you play.
        4 - Void items should have a different NICHE than their normal counterparts, but should never be objectively better. (Looking at you, Polylute)
        5 - SYNERGY! Cool item combos and build types should be encouraged.
        6 - The rework should be modular, so one can disable/enable it whenever they want without affecting other reworks.
        7 - Configurability. Most of the reworks' values should be modifiable in the config editor, so people can change things about vanilla items that they couldn't before.
        */

        private static void PrintLog(int arrayPointer, bool enabled) // Prints log infos for enabled/disabled reworks to make our lines shorter
        {
            string[] names = new string[1]
            { 
                "Mercurial Rachis"
            };

            if (enabled == true){ Log.LogInfo("Modifying " + names[arrayPointer] + "..."); }
            else { Log.LogInfo(names[arrayPointer] + " changes disabled."); }
        }

        public ConfigEntry<bool> MercurialRachis_Enable(){ return Config.Bind<bool>(new ConfigDefinition("Lunar - Mercurial Rachis", "Enable Changes"), true, new ConfigDescription("Enables changes to Mercurial Rachis.", null, Array.Empty<object>())); }
        public ConfigEntry<bool> MercurialRachis_IsTonic() { return Config.Bind<bool>(new ConfigDefinition("Lunar - Mercurial Rachis", "Apply Spinel Tonic Buff"), true, new ConfigDescription("Apply a Spinel Tonic buff instead of the vanilla Power Ward buff.", null, Array.Empty<object>())); }
        public ConfigEntry<float> MercurialRachis_Radius(){ return Config.Bind<float>(new ConfigDefinition("Lunar - Mercurial Rachis", "Radius"), 16f, new ConfigDescription("Radius of Mercurial Rachis zone in meters.", null, Array.Empty<object>())); }
        public ConfigEntry<float> MercurialRachis_PlacementRadius() { return Config.Bind<float>(new ConfigDefinition("Lunar - Mercurial Rachis", "Placement Radius"), 5f, new ConfigDescription("The maximum range from the player at which Mercurial Rachis places its wards.", null, Array.Empty<object>())); }

        public void Awake()
        {
            Log.Init(Logger);

            // Lunar
            if (MercurialRachis_Enable().Value == true) { PrintLog(0, true); Reworks.MercurialRachis.Initiate(MercurialRachis_Radius().Value, MercurialRachis_PlacementRadius().Value, MercurialRachis_IsTonic().Value); } else { PrintLog(0, false); }

            Log.LogInfo("Done!");
        }
    }
}
