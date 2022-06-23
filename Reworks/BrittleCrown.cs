using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using R2API;
using R2API.Utils;
using RoR2;
using RoR2.Orbs;
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
    public class BrittleCrown
    {
        // Our main hooks
        private static void AddHooks(float BrittleCrown_InteractIncrease, float BrittleCrown_InteractIncreaseStack, int BrittleCrown_CostIncrease, int BrittleCrown_CostIncreaseStack)
        {
            // Populate the scene with increasing amount of interactables per crown
            On.RoR2.SceneDirector.PopulateScene += (orig, self) =>
            {
                int crownAggregate = 0;
                foreach (PlayerCharacterMasterController playerCharacterMasterController in PlayerCharacterMasterController.instances)
                {
                    if (playerCharacterMasterController.master && playerCharacterMasterController.master.inventory)
                    {
                        crownAggregate += playerCharacterMasterController.master.inventory.GetItemCount(RoR2Content.Items.GoldOnHit);
                    }
                }
                if (crownAggregate > 0)
                {
                    float creditFloat = (float)self.interactableCredit;
                    creditFloat *= BrittleCrown_InteractIncrease + (BrittleCrown_InteractIncreaseStack * (crownAggregate - 1));
                    self.interactableCredit = (int)creditFloat;
                }
                orig(self);
            };

            // Multiply cost of interactables for each crown
            On.RoR2.Run.GetDifficultyScaledCost_int += (orig, self, baseCost) =>
            {
                int crownAggregate = 0;
                int newCost = 1;
                foreach (PlayerCharacterMasterController playerCharacterMasterController in PlayerCharacterMasterController.instances)
                {
                    if (playerCharacterMasterController.master && playerCharacterMasterController.master.inventory)
                    {
                        crownAggregate += playerCharacterMasterController.master.inventory.GetItemCount(RoR2Content.Items.GoldOnHit);
                    }
                }
                if (crownAggregate > 0)
                {
                    newCost = BrittleCrown_CostIncrease + (BrittleCrown_CostIncreaseStack * (crownAggregate - 1));
                }
                baseCost *= newCost;
                return (int)((float)baseCost * Mathf.Pow(self.difficultyCoefficient, 1.25f));
            };

            // Skip Brittle Crown's original effects
            IL.RoR2.GlobalEventManager.OnHitEnemy += (il) =>
            {
                ILCursor ilcursor = new(il);
                ilcursor.GotoNext(
                    x => x.MatchLdcR4(30),
                    x => x.MatchLdarg(1),
                    x => x.MatchLdfld("RoR2.DamageInfo", "procCoefficient"),
                    x => x.MatchMul()
                );
                ilcursor.Next.Operand = 0f;
            };
            IL.RoR2.HealthComponent.TakeDamage += (il) =>
            {
                ILCursor ilcursor = new(il);
                ilcursor.GotoNext(
                    x => x.MatchLdarg(0),
                    x => x.MatchLdflda("RoR2.HealthComponent", "itemCounts"),
                    x => x.MatchLdfld("RoR2.HealthComponent/ItemCounts", "goldOnHit"),
                    x => x.MatchLdcI4(0)
                );
                ilcursor.Index += 3;
                ilcursor.Remove();
                ilcursor.Emit(OpCodes.Ldc_I4, 999999);
            };
        }

        // Language token replacer
        private static void ReplaceTokens(float BrittleCrown_InteractIncrease, float BrittleCrown_InteractIncreaseStack, int BrittleCrown_CostIncrease, int BrittleCrown_CostIncreaseStack)
        {
            LanguageAPI.Add("ITEM_GOLDONHIT_PICKUP", "More chests and interactables spawn each stage... <style=cDeath>BUT they all cost double the money.</style>");
            LanguageAPI.Add("ITEM_GOLDONHIT_DESC", "Each stage has <style=cIsUtility>" + BrittleCrown_InteractIncrease + "x</style> <style=cStack>(+" + BrittleCrown_InteractIncreaseStack + "x per stack)</style> the amount of interactables. These interactables will cost <style=cDeath>" + BrittleCrown_CostIncrease + "x</style> <style=cStack>(+" + BrittleCrown_CostIncreaseStack + "x per stack)</style> as much money.");
        }

        // Apply the rework
        public static void Initiate(float BrittleCrown_InteractIncrease, float BrittleCrown_InteractIncreaseStack, int BrittleCrown_CostIncrease, int BrittleCrown_CostIncreaseStack)
        {
            ReplaceTokens(BrittleCrown_InteractIncrease, BrittleCrown_InteractIncreaseStack, BrittleCrown_CostIncrease, BrittleCrown_CostIncreaseStack);
            AddHooks(BrittleCrown_InteractIncrease, BrittleCrown_InteractIncreaseStack, BrittleCrown_CostIncrease, BrittleCrown_CostIncreaseStack);
        }
    }
}