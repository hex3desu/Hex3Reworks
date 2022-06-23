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
    public class ExecutiveCard
    {
        // Our main hooks
        private static void AddHooks(float ExecutiveCard_TerminalMultiplier, float ExecutiveCard_BaseMoneyGain, float ExecutiveCard_MoneyInterval)
        {
            // Remove the cash-back mechanic
            IL.RoR2.Items.MultiShopCardUtils.OnPurchase += (il) =>
            {
                ILCursor ilcursor = new(il);
                ilcursor.GotoNext(
                    x => x.MatchLdcI4(0),
                    x => x.MatchStloc(2),
                    x => x.MatchLdarg(1),
                    x => x.MatchLdcI4(0)
                );
                ilcursor.Index += 4;
                ilcursor.Next.OpCode = OpCodes.Bgt;
            };

            // Double the cost of remaining terminals on purchase
            On.RoR2.Items.MultiShopCardUtils.OnPurchase += (orig, context, moneyCost) =>
            {
                orig(context, moneyCost);
                ShopTerminalBehavior shopTerminalBehavior = context.purchasedObject.GetComponent<ShopTerminalBehavior>();
                if (shopTerminalBehavior && shopTerminalBehavior.serverMultiShopController)
                {
                    shopTerminalBehavior.serverMultiShopController.Networkcost = (int)(shopTerminalBehavior.serverMultiShopController.Networkcost * ExecutiveCard_TerminalMultiplier);
                    foreach (var terminal in shopTerminalBehavior.serverMultiShopController.terminalGameObjects)
                    {
                        if (terminal)
                        {
                            var purchaseInteraction = terminal.GetComponent<PurchaseInteraction>();
                            if (purchaseInteraction)
                                purchaseInteraction.Networkcost = (int)(purchaseInteraction.Networkcost * ExecutiveCard_TerminalMultiplier);
                        }
                    }
                }
            };

            // On fixed update, track the executive card timer and add interest every few seconds.
            On.RoR2.CharacterBody.FixedUpdate += (orig, self) =>
            {
                orig(self);
                if (self.GetComponent<InterestTimer>() == null )
                {
                    self.AddItemBehavior<InterestTimer>(1);
                }
                if (self.inventory && self.inventory.currentEquipmentIndex == DLC1Content.Equipment.MultiShopCard.equipmentIndex)
                {
                    InterestTimer interestTimer = self.GetComponent<InterestTimer>();
                    interestTimer.interval += Time.fixedDeltaTime;
                    if (interestTimer.interval > ExecutiveCard_MoneyInterval)
                    {
                        GoldOrb goldOrb = new GoldOrb();
                        goldOrb.origin = self.corePosition;
                        goldOrb.target = self.mainHurtBox;
                        goldOrb.goldAmount = (uint)((float)ExecutiveCard_BaseMoneyGain * Run.instance.difficultyCoefficient);
                        OrbManager.instance.AddOrb(goldOrb);
                        EffectManager.SimpleImpactEffect(LegacyResourcesAPI.Load<GameObject>("Prefabs/Effects/ImpactEffects/CoinImpact"), self.corePosition, Vector3.up, true);
                        interestTimer.interval = 0f;
                    }
                }
            };
        }

        // Executive Card money gain timer
        public class InterestTimer : CharacterBody.ItemBehavior
        {
            public float interval = 0f;
        }

        // Language token replacer
        private static void ReplaceTokens(float ExecutiveCard_TerminalMultiplier, float ExecutiveCard_BaseMoneyGain, float ExecutiveCard_MoneyInterval)
        {
            LanguageAPI.Add("EQUIPMENT_MULTISHOPCARD_PICKUP", "Multishop terminals stay open on purchase, but cost double. Passively gain interest.");
            LanguageAPI.Add("EQUIPMENT_MULTISHOPCARD_DESC", "Multishop terminals will <style=cIsUtility>remain open</style> after use, but the remaining terminals will raise their prices by <style=cDeath>" + ExecutiveCard_TerminalMultiplier + "x</style>. Passively gain <style=cIsUtility>$" + ExecutiveCard_BaseMoneyGain + "</style> <style=cStack>(Scales with time)</style> every <style=cIsUtility>" + ExecutiveCard_MoneyInterval + "</style> seconds.");
        }

        // Apply the rework
        public static void Initiate(float ExecutiveCard_TerminalMultiplier, float ExecutiveCard_BaseMoneyGain, float ExecutiveCard_MoneyInterval)
        {
            ReplaceTokens(ExecutiveCard_TerminalMultiplier, ExecutiveCard_BaseMoneyGain, ExecutiveCard_MoneyInterval);
            AddHooks(ExecutiveCard_TerminalMultiplier, ExecutiveCard_BaseMoneyGain, ExecutiveCard_MoneyInterval);
        }
    }
}