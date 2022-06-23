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
    public class OldGuillotine
    {
        // Our main hooks
        private static void AddHooks(float OldGuillotine_Chance, float OldGuillotine_BaseMoney)
        {
            // Steal money & deal damage simultaneously
            On.RoR2.GlobalEventManager.OnHitEnemy += (orig, self, damageInfo, victim) =>
            {
                orig(self, damageInfo, victim);
                if (damageInfo.attacker && damageInfo.attacker.GetComponent<CharacterBody>() && damageInfo.attacker.GetComponent<CharacterBody>().inventory)
                {
                    CharacterBody attackerBody = damageInfo.attacker.GetComponent<CharacterBody>();
                    int itemCount = attackerBody.inventory.GetItemCount(RoR2Content.Items.ExecuteLowHealthElite);

                    if (attackerBody.master && victim.GetComponent<CharacterBody>() && victim.GetComponent<CharacterBody>().healthComponent)
                    {
                        CharacterBody victimBody = victim.GetComponent<CharacterBody>();
                        if (itemCount > 0 && Util.CheckRoll(OldGuillotine_Chance * damageInfo.procCoefficient, attackerBody.master) && victimBody.isElite)
                        {
                            GoldOrb goldOrb = new GoldOrb();
                            goldOrb.origin = damageInfo.position;
                            goldOrb.target = attackerBody.mainHurtBox;
                            goldOrb.goldAmount = (uint)((float)itemCount * OldGuillotine_BaseMoney * Run.instance.difficultyCoefficient);
                            OrbManager.instance.AddOrb(goldOrb);
                            EffectManager.SimpleImpactEffect(LegacyResourcesAPI.Load<GameObject>("Prefabs/Effects/ImpactEffects/CoinImpact"), damageInfo.position, Vector3.up, true);

                            DamageInfo orbDamageInfo = new DamageInfo();
                            orbDamageInfo.damageType = DamageType.BypassArmor;
                            orbDamageInfo.damageColorIndex = DamageColorIndex.Item;
                            orbDamageInfo.procCoefficient = 0f;
                            orbDamageInfo.position = victimBody.corePosition;
                            orbDamageInfo.attacker = damageInfo.attacker;
                            orbDamageInfo.damage = (itemCount * 4) * (OldGuillotine_BaseMoney * Run.instance.difficultyCoefficient);
                            victimBody.healthComponent.TakeDamage(orbDamageInfo);
                        }
                    }
                }
            };

            // Remove the guillotine execution threshold altogether
            IL.RoR2.CharacterBody.OnInventoryChanged += (il) =>
            {
                ILCursor ilcursor = new(il);
                ilcursor.GotoNext(
                    x => x.MatchLdarg(0),
                    x => x.MatchLdcR4(13f),
                    x => x.MatchLdarg(0)
                );
                ilcursor.Index += 1;
                ilcursor.Next.Operand = 0f;
            };
        }

        // Language token replacer
        private static void ReplaceTokens(float OldGuillotine_Chance, float OldGuillotine_BaseMoney)
        {
            LanguageAPI.Add("ITEM_EXECUTELOWHEALTHELITE_PICKUP", "Hitting elite enemies has a chance to steal their gold, dealing damage for every dollar stolen.");
            LanguageAPI.Add("ITEM_EXECUTELOWHEALTHELITE_DESC", "Hitting an <style=cIsUtility>elite enemy</style> has a <style=cIsUtility>" + OldGuillotine_Chance + "%</style> chance to <style=cIsDamage>steal $" + OldGuillotine_BaseMoney + " of their money</style> <style=cStack>(+$" + OldGuillotine_BaseMoney + " per stack, scales over time)</style>, dealing damage equivalent to <style=cIsDamage>quadruple</style> the amount you've stolen.");
        }

        // Apply the rework
        public static void Initiate(float OldGuillotine_Chance, float OldGuillotine_BaseMoney)
        {
            ReplaceTokens(OldGuillotine_Chance, OldGuillotine_BaseMoney);
            AddHooks(OldGuillotine_Chance, OldGuillotine_BaseMoney);
        }
    }
}