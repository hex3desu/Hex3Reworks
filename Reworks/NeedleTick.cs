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
    public class NeedleTick
    {
        // Our main hooks
        ItemIndex tickIndex = new ItemIndex();
        private static void AddHooks()
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

                        if (victimItemCount == 0)
                        {
                            for (int i = 0; i < attackerItemCount; i++)
                            {
                                DotController.InflictDot(victim, damageInfo.attacker, DotController.DotIndex.Fracture, dotDef.interval, 1f);
                            }
                        }
                        if (attackerItemCount > 0)
                        {
                            victimBody.inventory.GiveItem(hiddenItemDef);
                        }
                        if (victimItemCount > 8)
                        {
                            for (int i = 0; i < attackerItemCount; i++)
                            {
                                DotController.InflictDot(victim, damageInfo.attacker, DotController.DotIndex.Fracture, dotDef.interval, 1f);
                            }
                            victimBody.inventory.RemoveItem(hiddenItemDef, 9);
                        }
                    }
                }
            };
        }//Enemies recieve a stack <style=cStack>(+1 per stack)</style> of <style=cIsDamage>Collapse</style> when first hit. Every following <style=cIsDamage>10</style> hits inflicts another stack of <style=cIsDamage>Collapse</style>

        // Language token replacer
        private static void ReplaceTokens()
        {
            LanguageAPI.Add("ITEM_BLEEDONHITVOID_PICKUP", "Enemies are Collapsed when hit. Every following 10 hits inflicts Collapse again. <style=cIsVoid>Corrupts all Tri-Tip Daggers.</style>");
            LanguageAPI.Add("ITEM_BLEEDONHITVOID_DESC", "Enemies recieve a stack <style=cStack>(+1 per stack)</style> of <style=cIsDamage>Collapse</style> for <style=cIsDamage>400%</style> damage when first hit. Every following <style=cIsDamage>10</style> hits inflicts <style=cIsDamage>Collapse</style> again. <style=cIsVoid>Corrupts all Tri-Tip Daggers.</style>");
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
        public static void Initiate()
        {
            ReplaceTokens();
            AddHooks();
            ItemAPI.Add(new CustomItem(hiddenItemDef, CreateNullDisplayRules()));
        }
    }
}