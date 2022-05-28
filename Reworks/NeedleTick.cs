﻿using BepInEx;
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
using Hex3Reworks.Helpers;

namespace Hex3Reworks.Reworks
{
    public class NeedleTick
    {
        // Our main hooks
        private static void AddHooks(bool NeedleTick_FirstHit, int NeedleTick_InflictInterval, bool NeedleTick_VoidEnemiesCollapse)
        {
            // Remove the existing needletick effect completely
            IL.RoR2.GlobalEventManager.OnHitEnemy += (il) =>
            {
                ILCursor ilcursor = new(il);
                ilcursor.GotoNext(
                    x => x.MatchLdarg(1),
                    x => x.MatchLdfld<DamageInfo>("procChainMask"),
                    x => x.MatchStloc(25)
                );
                ilcursor.RemoveRange(18);
            };

            ItemDef tickDef = new ItemDef();
            On.RoR2.GlobalEventManager.OnHitEnemy += (orig, self, damageInfo, victim) =>
            {
                orig(self, damageInfo, victim);

                // If the itemdef for Needletick is known, no need to retrieve it
                if (tickDef.name != "BleedOnHitVoid")
                {
                    foreach (ItemDef item in ItemCatalog.itemDefs)
                    {
                        if (item.name == "BleedOnHitVoid")
                        {
                            tickDef = item;
                        }
                    }
                }

                // If the attacker has a needletick, give a hidden item to the victim. Having enough hidden items will trigger collapse.
                if (victim.GetComponent<CharacterBody> != null && victim.GetComponent<CharacterBody>().inventory)
                {
                    if (damageInfo.attacker && damageInfo.attacker.GetComponent<CharacterBody> != null && damageInfo.attacker.GetComponent<CharacterBody>().inventory)
                    {
                        CharacterBody attackerBody = damageInfo.attacker.GetComponent<CharacterBody>();
                        CharacterBody victimBody = victim.GetComponent<CharacterBody>();
                        int attackerItemCount = attackerBody.inventory.GetItemCount(tickDef.itemIndex);
                        int victimItemCount = victimBody.inventory.GetItemCount(hiddenItemDef);
                        DotController.DotDef dotDef = DotController.GetDotDef(DotController.DotIndex.Fracture);
                        bool canVoidFracture = false;

                        // Flag to allow enemies to inflict new fracture
                        if (attackerBody.HasBuff(DLC1Content.Buffs.EliteVoid) && NeedleTick_VoidEnemiesCollapse == true)
                        {
                            canVoidFracture = true;
                        }

                        // Check for team first, to prevent self damage...
                        if (attackerBody.teamComponent && victimBody.teamComponent && attackerBody.teamComponent.teamIndex != victimBody.teamComponent.teamIndex)
                        {
                            if (victimItemCount == 0 && NeedleTick_FirstHit == true)
                            {
                                if (canVoidFracture == true && EliteReworksCompatibility.enabled == false)
                                {
                                    ApplyFracture(damageInfo, victim, dotDef);
                                }
                                else
                                {
                                    for (int i = 0; i < attackerItemCount; i++)
                                    {
                                        ApplyFracture(damageInfo, victim, dotDef);
                                    }
                                }
                            }
                            if (attackerItemCount > 0 || canVoidFracture == true)
                            {
                                victimBody.inventory.GiveItem(hiddenItemDef);
                                if (victimBody.inventory.GetItemCount(hiddenItemDef) > 50)
                                {
                                    victimBody.inventory.RemoveItem(hiddenItemDef, 40); // Prevent item buildup
                                }
                            }
                            if (attackerBody.HasBuff(DLC1Content.Buffs.EliteVoid) && NeedleTick_VoidEnemiesCollapse == false && attackerBody.master && EliteReworksCompatibility.enabled == false)
                            {
                                if (Util.CheckRoll(damageInfo.procCoefficient * 100f, attackerBody.master))
                                {
                                    ApplyFracture(damageInfo, victim, dotDef);
                                }
                            }
                            if (victimItemCount > (NeedleTick_InflictInterval - 2))
                            {
                                if (canVoidFracture == true && EliteReworksCompatibility.enabled == false)
                                {
                                    ApplyFracture(damageInfo, victim, dotDef);
                                    victimBody.inventory.RemoveItem(hiddenItemDef, (NeedleTick_InflictInterval - 1));
                                }
                                else
                                {
                                    for (int i = 0; i < attackerItemCount; i++)
                                    {
                                        ApplyFracture(damageInfo, victim, dotDef);
                                    }
                                    victimBody.inventory.RemoveItem(hiddenItemDef, (NeedleTick_InflictInterval - 1));
                                }
                            }
                        }
                    }
                }
            };

            void ApplyFracture(DamageInfo damageInfo, GameObject victim, DotController.DotDef dotDef)
            {
                ProcChainMask procChainMask3 = damageInfo.procChainMask;
                procChainMask3.AddProc(ProcType.FractureOnHit);
                DotController.InflictDot(victim, damageInfo.attacker, DotController.DotIndex.Fracture, dotDef.interval, 1f);
            }
        }

        // Language token replacer
        private static void ReplaceTokens(bool NeedleTick_FirstHit, int NeedleTick_InflictInterval)
        {
            if (NeedleTick_FirstHit == true)
            {
                LanguageAPI.Add("ITEM_BLEEDONHITVOID_PICKUP", "Enemies are Collapsed when hit. Every following " + NeedleTick_InflictInterval + " hits, inflict Collapse again. <style=cIsVoid>Corrupts all Tri-Tip Daggers.</style>");
                LanguageAPI.Add("ITEM_BLEEDONHITVOID_DESC", "Enemies recieve a stack <style=cStack>(+1 per stack)</style> of <style=cIsDamage>Collapse</style> for <style=cIsDamage>400%</style> damage when first hit. Every following <style=cIsDamage>" + NeedleTick_InflictInterval + "</style> hits, inflict <style=cIsDamage>Collapse</style> again. <style=cIsVoid>Corrupts all Tri-Tip Daggers.</style>");
            }
            else
            {
                LanguageAPI.Add("ITEM_BLEEDONHITVOID_PICKUP", "Every " + NeedleTick_InflictInterval + " hits, inflict Collapse on your enemy. <style=cIsVoid>Corrupts all Tri-Tip Daggers.</style>");
                LanguageAPI.Add("ITEM_BLEEDONHITVOID_DESC", "Every <style=cIsDamage>" + NeedleTick_InflictInterval + "</style> hits, inflict <style=cIsDamage>Collapse</style> for <style=cIsDamage>400%</style> damage. <style=cIsVoid>Corrupts all Tri-Tip Daggers.</style>");
            }
        }

        // Create a hidden item to track how many times each enemy has been hit
        public static ItemDef hiddenItemDef = CreateHiddenItem();
        public static ItemDef CreateHiddenItem()
        {
            ItemDef item = ScriptableObject.CreateInstance<ItemDef>();

            item.name = "NeedletickHidden";
            item.nameToken = "H3R_NEEDLETICKHIDDEN_NAME";
            item.pickupToken = "H3R_NEEDLETICKHIDDEN_PICKUP";
            item.descriptionToken = "H3R_NEEDLETICKHIDDEN_DESC";
            item.loreToken = "H3R_NEEDLETICKHIDDEN_LORE";

            item.tags = new ItemTag[] { ItemTag.CannotCopy, ItemTag.CannotSteal, ItemTag.CannotDuplicate };
            item.deprecatedTier = ItemTier.NoTier;
            item.canRemove = false;
            item.hidden = true;

            return item;
        }
        public static ItemDisplayRuleDict CreateNullDisplayRules() // No need for item displays...
        {
            return null;
        }

        // Apply the rework
        public static void Initiate(bool NeedleTick_FirstHit, int NeedleTick_InflictInterval, bool NeedleTick_VoidEnemiesCollapse)
        {
            ReplaceTokens(NeedleTick_FirstHit, NeedleTick_InflictInterval);
            AddHooks(NeedleTick_FirstHit, NeedleTick_InflictInterval, NeedleTick_VoidEnemiesCollapse);
            ItemAPI.Add(new CustomItem(hiddenItemDef, CreateNullDisplayRules()));
        }
    }
}