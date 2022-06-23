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
using UnityEngine.AddressableAssets;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using Hex3Reworks.Logging;

namespace Hex3Reworks.Reworks
{
    public class RollOfPennies
    {
        // Our main hooks
        private static void AddHooks()
        {
            // Heal when gold is gained
            On.RoR2.Stats.StatManager.OnGoldCollected += (orig, characterMaster, amount) =>
            {
                orig(characterMaster, amount);
                if (amount > 0 && characterMaster && characterMaster.GetBody() && characterMaster.GetBody().inventory && characterMaster.GetBody().inventory.GetItemCount(DLC1Content.Items.GoldOnHurt) > 0)
                {
                    if (characterMaster.GetBody().healthComponent)
                    {
                        ProcChainMask emptyChain = new ProcChainMask();
                        characterMaster.GetBody().healthComponent.HealFraction(0.01f + (0.005f * (characterMaster.GetBody().inventory.GetItemCount(DLC1Content.Items.GoldOnHurt) - 1)) , emptyChain);
                    }
                }
            };

            // Remove original Roll Of Pennies functionality
            IL.RoR2.HealthComponent.TakeDamage += (il) =>
            {
                ILCursor ilcursor = new(il);
                ilcursor.GotoNext(
                    x => x.MatchLdarg(0),
                    x => x.MatchLdflda("RoR2.HealthComponent", "itemCounts"),
                    x => x.MatchLdfld("RoR2.HealthComponent/ItemCounts", "goldOnHurt"),
                    x => x.MatchLdcI4(0)
                );
                ilcursor.Index += 3;
                ilcursor.Remove();
                ilcursor.Emit(OpCodes.Ldc_I4, 999999);
            };
        }

        // Language token replacer
        private static void ReplaceTokens()
        {
            LanguageAPI.Add("ITEM_GOLDONHURT_PICKUP", "Heal when you earn money.");
            LanguageAPI.Add("ITEM_GOLDONHURT_DESC", "<style=cIsHealing>Earning money</style> heals you for <style=cIsHealing>1%</style> <style=cStack>(+0.5% per stack)</style> of your <style=cIsHealing>Max HP</style>.");
        }

        // Apply the rework
        public static void Initiate()
        {
            ReplaceTokens();
            AddHooks();
        }
    }
}