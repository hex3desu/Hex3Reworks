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
    [BepInDependency("com.Wolfo.WolfoQualityOfLife", BepInDependency.DependencyFlags.SoftDependency)]
    [NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.EveryoneNeedSameModVersion)]
    [R2APISubmoduleDependency(nameof(ItemAPI), nameof(LanguageAPI), nameof(RecalculateStatsAPI), nameof(PrefabAPI))]
    public class Main : BaseUnityPlugin
    {
        public const string ModGuid = "com.Hex3.Hex3Reworks";
        public const string ModName = "Hex3Reworks";
        public const string ModVer = "0.2.0";

        public static AssetBundle MainAssets;

        public static ManualLogSource logger;

        private static void PrintLog(int arrayPointer, bool enabled) // Prints log infos for enabled/disabled reworks to make our lines shorter
        {
            string[] names = new string[10]
            { 
                "Mercurial Rachis",
                "Lepton Daisy",
                "Needletick",
                "Bustling Fungus",
                "Ghor\'s Tome",
                "Old Guillotine",
                "Brittle Crown",
                "Roll Of Pennies",
                "Executive Card",
                "57 Leaf Clover"
            };

            if (enabled == true){ Log.LogInfo("Modifying " + names[arrayPointer] + "..."); }
            else { Log.LogInfo(names[arrayPointer] + " changes disabled."); }
        }

        // Common
        public ConfigEntry<bool> BustlingFungus_Enable() { return Config.Bind<bool>(new ConfigDefinition("Common - Bustling Fungus", "Enable Changes"), true, new ConfigDescription("Enables changes to Bustling Fungus.", null, Array.Empty<object>())); }
        public ConfigEntry<float> BustlingFungus_Healing() { return Config.Bind<float>(new ConfigDefinition("Common - Bustling Fungus", "Fungus Healing"), 0.02f, new ConfigDescription("Fraction of max health healed per second.", null, Array.Empty<object>())); }
        public ConfigEntry<float> BustlingFungus_Radius() { return Config.Bind<float>(new ConfigDefinition("Common - Bustling Fungus", "Base Zone Radius"), 3f, new ConfigDescription("Base radius of Bustling Fungus zone in meters.", null, Array.Empty<object>())); }
        public ConfigEntry<float> BustlingFungus_RadiusStack() { return Config.Bind<float>(new ConfigDefinition("Common - Bustling Fungus", "Zone Radius Per Stack"), 2f, new ConfigDescription("Extra radius of Bustling Fungus zone per stack in meters.", null, Array.Empty<object>())); }
        public ConfigEntry<float> BustlingFungus_ZoneInterval() { return Config.Bind<float>(new ConfigDefinition("Common - Bustling Fungus", "Zone Placement Interval"), 10f, new ConfigDescription("How often, in seconds, that zones are placed.", null, Array.Empty<object>())); }

        // public ConfigEntry<bool> RollOfPennies_Enable() { return Config.Bind<bool>(new ConfigDefinition("Common - Roll Of Pennies", "Enable Changes"), true, new ConfigDescription("Enables changes to Roll Of Pennies.", null, Array.Empty<object>())); }

        // Uncommon
        public ConfigEntry<bool> LeptonDaisy_Enable() { return Config.Bind<bool>(new ConfigDefinition("Uncommon - Lepton Daisy", "Enable Changes"), true, new ConfigDescription("Enables changes to Lepton Daisy.", null, Array.Empty<object>())); }
        public ConfigEntry<float> LeptonDaisy_WeakDuration() { return Config.Bind<float>(new ConfigDefinition("Uncommon - Lepton Daisy", "Weaken Duration"), 8f, new ConfigDescription("How long enemies should be weakened by each healing nova, in seconds.", null, Array.Empty<object>())); }

        public ConfigEntry<bool> GhorsTome_Enable() { return Config.Bind<bool>(new ConfigDefinition("Uncommon - Ghors Tome", "Enable Changes"), true, new ConfigDescription("Enables changes to Ghor\'s Tome.", null, Array.Empty<object>())); }
        public ConfigEntry<float> GhorsTome_ArmorBuff() { return Config.Bind<float>(new ConfigDefinition("Uncommon - Ghors Tome", "Armor Buff"), 2f, new ConfigDescription("Armor added per buff per stack.", null, Array.Empty<object>())); }
        public ConfigEntry<float> GhorsTome_ArmorBuffDuration() { return Config.Bind<float>(new ConfigDefinition("Uncommon - Ghors Tome", "Armor Buff Duration"), 6f, new ConfigDescription("How long the armor buff lasts in seconds", null, Array.Empty<object>())); }
        public ConfigEntry<float> GhorsTome_PickupChance() { return Config.Bind<float>(new ConfigDefinition("Uncommon - Ghors Tome", "Gold Drop Chance"), 20f, new ConfigDescription("Likelihood for gold pickups to drop in percentage.", null, Array.Empty<object>())); }
        public ConfigEntry<int> GhorsTome_BaseMoney() { return Config.Bind<int>(new ConfigDefinition("Uncommon - Ghors Tome", "Base Money From Gold"), 4, new ConfigDescription("Base money earned from gold pickups.", null, Array.Empty<object>())); }

        public ConfigEntry<bool> OldGuillotine_Enable() { return Config.Bind<bool>(new ConfigDefinition("Uncommon - Old Guillotine", "Enable Changes"), true, new ConfigDescription("Enables changes to Old Guillotine.", null, Array.Empty<object>())); }
        public ConfigEntry<float> OldGuillotine_Chance() { return Config.Bind<float>(new ConfigDefinition("Uncommon - Old Guillotine", "Elite Gold Steal Chance"), 20f, new ConfigDescription("Chance to steal gold from elites.", null, Array.Empty<object>())); }
        public ConfigEntry<float> OldGuillotine_BaseMoney() { return Config.Bind<float>(new ConfigDefinition("Uncommon - Old Guillotine", "Base Gold Reward"), 2f, new ConfigDescription("Base money earned each time you successfully steal (Scales with time).", null, Array.Empty<object>())); }

        // Legendary
        public ConfigEntry<bool> Clover_Enable() { return Config.Bind<bool>(new ConfigDefinition("Legendary - 57 Leaf Clover", "Enable Changes"), true, new ConfigDescription("Enables changes to 57 Leaf Clover.", null, Array.Empty<object>())); }
        public ConfigEntry<float> Clover_MoneyRequirement() { return Config.Bind<float>(new ConfigDefinition("Legendary - 57 Leaf Clover", "Money Required For Max Luck Gain"), 400f, new ConfigDescription("How much money (scaling with time) is required for 1 clover to reach its max potential.", null, Array.Empty<object>())); }
        public ConfigEntry<float> Clover_MaxLuckMult() { return Config.Bind<float>(new ConfigDefinition("Legendary - 57 Leaf Clover", "Max Luck Multiplier"), 0.5f, new ConfigDescription("How much the percent chance for all your rolls can be increased per stack (1 = +100%)", null, Array.Empty<object>())); }

        // Lunar
        public ConfigEntry<bool> MercurialRachis_Enable() { return Config.Bind<bool>(new ConfigDefinition("Lunar - Mercurial Rachis", "Enable Changes"), true, new ConfigDescription("Enables changes to Mercurial Rachis.", null, Array.Empty<object>())); }
        public ConfigEntry<bool> MercurialRachis_IsTonic() { return Config.Bind<bool>(new ConfigDefinition("Lunar - Mercurial Rachis", "Apply Spinel Tonic Buff"), true, new ConfigDescription("Apply a Spinel Tonic buff instead of the vanilla Power Ward buff.", null, Array.Empty<object>())); }
        public ConfigEntry<float> MercurialRachis_Radius() { return Config.Bind<float>(new ConfigDefinition("Lunar - Mercurial Rachis", "Radius"), 20f, new ConfigDescription("Radius of Mercurial Rachis zone in meters.", null, Array.Empty<object>())); }
        public ConfigEntry<float> MercurialRachis_PlacementRadius() { return Config.Bind<float>(new ConfigDefinition("Lunar - Mercurial Rachis", "Placement Radius"), 5f, new ConfigDescription("The maximum range from the player at which Mercurial Rachis places its wards, in meters.", null, Array.Empty<object>())); }
        public ConfigEntry<float> MercurialRachis_EnemyBuffRadius() { return Config.Bind<float>(new ConfigDefinition("Lunar - Mercurial Rachis", "Enemy Buff Radius"), 60f, new ConfigDescription("Radius of Mercurial Rachis enemy buff zone in meters.", null, Array.Empty<object>())); }

        public ConfigEntry<bool> BrittleCrown_Enable() { return Config.Bind<bool>(new ConfigDefinition("Lunar - Brittle Crown", "Enable Changes"), true, new ConfigDescription("Enables changes to Brittle Crown.", null, Array.Empty<object>())); }
        public ConfigEntry<float> BrittleCrown_InteractIncrease() { return Config.Bind<float>(new ConfigDefinition("Lunar - Brittle Crown", "Base Interactables Increase"), 1.5f, new ConfigDescription("Interactables multiplier on first stack of this item.", null, Array.Empty<object>())); }
        public ConfigEntry<float> BrittleCrown_InteractIncreaseStack() { return Config.Bind<float>(new ConfigDefinition("Lunar - Brittle Crown", "Interactables Increase Per Stack"), 0.5f, new ConfigDescription("Added to the interactables multiplier for each additional stack of this item.", null, Array.Empty<object>())); }
        public ConfigEntry<int> BrittleCrown_CostIncrease() { return Config.Bind<int>(new ConfigDefinition("Lunar - Brittle Crown", "Base Cost Increase"), 2, new ConfigDescription("Interactable cost multiplier on first stack of this item.", null, Array.Empty<object>())); }
        public ConfigEntry<int> BrittleCrown_CostIncreaseStack() { return Config.Bind<int>(new ConfigDefinition("Lunar - Brittle Crown", "Cost Increase Per Stack"), 1, new ConfigDescription("Added to the interactable cost multiplier for each additional stack of this item.", null, Array.Empty<object>())); }

        // Void
        public ConfigEntry<bool> NeedleTick_Enable() { return Config.Bind<bool>(new ConfigDefinition("Void - NeedleTick", "Enable Changes"), true, new ConfigDescription("Enables changes to Needletick.", null, Array.Empty<object>())); }
        public ConfigEntry<bool> NeedleTick_FirstHit() { return Config.Bind<bool>(new ConfigDefinition("Void - NeedleTick", "Inflict Collapse on First Hit"), true, new ConfigDescription("Collapse will be inflicted when you first hit an enemy.", null, Array.Empty<object>())); }
        public ConfigEntry<int> NeedleTick_InflictInterval() { return Config.Bind<int>(new ConfigDefinition("Void - NeedleTick", "Collapse Interval"), 20, new ConfigDescription("Amount of hits needed before Collapse is inflicted again. (May break at low values)", null, Array.Empty<object>())); }
        public ConfigEntry<bool> NeedleTick_VoidEnemiesCollapse() { return Config.Bind<bool>(new ConfigDefinition("Void - NeedleTick", "Voidtouched enemies can use new Collapse"), false, new ConfigDescription("Void enemies will collapse on first hit, and on every 10th hit afterwards. If false, enemies will use vanilla Collapse.", null, Array.Empty<object>())); }

        // Equipment
        public ConfigEntry<bool> ExecutiveCard_Enable() { return Config.Bind<bool>(new ConfigDefinition("Equipment - Executive Card", "Enable Changes"), true, new ConfigDescription("Enables changes to Executive Card.", null, Array.Empty<object>())); }
        public ConfigEntry<float> ExecutiveCard_TerminalMultiplier() { return Config.Bind<float>(new ConfigDefinition("Equipment - Executive Card", "Open Terminal Cost Multiplier"), 2f, new ConfigDescription("How much terminals will cost if forced open with Executive Card.", null, Array.Empty<object>())); }
        public ConfigEntry<float> ExecutiveCard_BaseMoneyGain() { return Config.Bind<float>(new ConfigDefinition("Equipment - Executive Card", "Base Interest"), 8f, new ConfigDescription("How much money is earned from interest (Scales over time).", null, Array.Empty<object>())); }
        public ConfigEntry<float> ExecutiveCard_MoneyInterval() { return Config.Bind<float>(new ConfigDefinition("Equipment - Executive Card", "Interest Interval"), 8f, new ConfigDescription("How often interest is gained in seconds.", null, Array.Empty<object>())); }

        public void Awake()
        {
            Log.Init(Logger);

            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Hex3Reworks.reworkvfx"))
            {
                MainAssets = AssetBundle.LoadFromStream(stream); // Load mainassets into stream
            }

            // Common
            if (BustlingFungus_Enable().Value == true) { PrintLog(3, true); Reworks.BustlingFungus.Initiate(BustlingFungus_Healing().Value, BustlingFungus_Radius().Value, BustlingFungus_RadiusStack().Value, BustlingFungus_ZoneInterval().Value); } else { PrintLog(3, false); }
            // if (RollOfPennies_Enable().Value == true) { PrintLog(7, true); Reworks.RollOfPennies.Initiate(); } else { PrintLog(7, false); }
            // Uncommon
            if (LeptonDaisy_Enable().Value == true) { PrintLog(1, true); Reworks.LeptonDaisy.Initiate(LeptonDaisy_WeakDuration().Value); } else { PrintLog(1, false); }
            if (GhorsTome_Enable().Value == true) { PrintLog(4, true); Reworks.GhorsTome.Initiate(GhorsTome_ArmorBuff().Value, GhorsTome_ArmorBuffDuration().Value, GhorsTome_PickupChance().Value, GhorsTome_BaseMoney().Value); } else { PrintLog(4, false); }
            if (OldGuillotine_Enable().Value == true) { PrintLog(5, true); Reworks.OldGuillotine.Initiate(OldGuillotine_Chance().Value, OldGuillotine_BaseMoney().Value); } else { PrintLog(5, false); }
            // Legendary
            if (Clover_Enable().Value == true) { PrintLog(9, true); Reworks.Clover.Initiate(Clover_MoneyRequirement().Value, Clover_MaxLuckMult().Value); } else { PrintLog(9, false); }
            // Lunar
            if (MercurialRachis_Enable().Value == true) { PrintLog(0, true); Reworks.MercurialRachis.Initiate(MercurialRachis_Radius().Value, MercurialRachis_PlacementRadius().Value, MercurialRachis_IsTonic().Value, MercurialRachis_EnemyBuffRadius().Value); } else { PrintLog(0, false); }
            if (BrittleCrown_Enable().Value == true) { PrintLog(6, true); Reworks.BrittleCrown.Initiate(BrittleCrown_InteractIncrease().Value, BrittleCrown_InteractIncreaseStack().Value, BrittleCrown_CostIncrease().Value, BrittleCrown_CostIncreaseStack().Value); } else { PrintLog(6, false); }
            // Void
            if (NeedleTick_Enable().Value == true) { PrintLog(2, true); Reworks.NeedleTick.Initiate(NeedleTick_FirstHit().Value, NeedleTick_InflictInterval().Value, NeedleTick_VoidEnemiesCollapse().Value); } else { PrintLog(2, false); }
            // Equipment
            if (ExecutiveCard_Enable().Value == true) { PrintLog(8, true); Reworks.ExecutiveCard.Initiate(ExecutiveCard_TerminalMultiplier().Value, ExecutiveCard_BaseMoneyGain().Value, ExecutiveCard_MoneyInterval().Value); } else { PrintLog(8, false); }

            Log.LogInfo("Done!");
        }
    }
}
