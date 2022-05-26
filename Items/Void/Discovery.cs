﻿using BepInEx;
using BepInEx.Configuration;
using R2API;
using R2API.Utils;
using RoR2;
using RoR2.ExpansionManagement;
using System;
using System.Runtime;
using System.Linq;
using UnityEngine;
using Hex3Mod;
using Hex3Mod.Logging;
using VoidItemAPI;

namespace Hex3Mod.Items
{
    /*
    If you're going for a full void build, Discovery will be a decent method of getting shields.
    To match with Infusion's purpose and to be thematically consistent with exploration of the void, this item gives shield based on your "discoveries" (interactions)
    */
    public class Discovery
    {
        // Create functions here for defining the ITEM, TOKENS, HOOKS and CONFIG. This is simpler than doing it in Main
        static string itemName = "Discovery"; // Change this name when making a new item
        static string upperName = itemName.ToUpper();
        public static ItemDef itemDefinition = CreateItem();
        public static ItemDef hiddenItemDefinition = CreateHiddenItem();
        public static ItemDef CreateItem()
        {
            ItemDef item = ScriptableObject.CreateInstance<ItemDef>();

            item.name = itemName;
            item.nameToken = "H3_" + upperName + "_NAME";
            item.pickupToken = "H3_" + upperName + "_PICKUP";
            item.descriptionToken = "H3_" + upperName + "_DESC";
            item.loreToken = "H3_" + upperName + "_LORE";

            item.tags = new ItemTag[]{ ItemTag.Healing, ItemTag.AIBlacklist, ItemTag.BrotherBlacklist }; // Would be useless and complicated on monsters
            item.deprecatedTier = ItemTier.VoidTier2;
            item.canRemove = true;
            item.hidden = false;
            item.requiredExpansion = ExpansionCatalog.expansionDefs.FirstOrDefault(x => x.nameToken == "DLC1_NAME");

            item.pickupModelPrefab = Main.MainAssets.LoadAsset<GameObject>("Assets/Models/Prefabs/DiscoveryPrefab.prefab");
            item.pickupIconSprite = Main.MainAssets.LoadAsset<Sprite>("Assets/Textures/Icons/Discovery.png");

            return item;
        }

        public static ItemDisplayRuleDict CreateDisplayRules() // We'll figure item displays out... when our models get better
        {
            return new ItemDisplayRuleDict();
        }

        public static void AddTokens(float Discovery_ShieldAdd, int Discovery_MaxStacks)
        {
            float finalNumber = Discovery_ShieldAdd * Discovery_MaxStacks;
            LanguageAPI.Add("H3_" + upperName + "_NAME", "Discovery");
            LanguageAPI.Add("H3_" + upperName + "_PICKUP", "Using a world interactable grants <style=cIsHealing>regenerating shield</style> to all holders of this item. <style=cIsVoid>Corrupts all Infusions.</style>");
            LanguageAPI.Add("H3_" + upperName + "_DESC", "Using a world interactable grants <style=cIsHealing>" + Discovery_ShieldAdd + "</style> points of <style=cIsHealing>regenerating shield</style> to every player who has this item. Caps at <style=cIsHealing>" + finalNumber + " shield</style> <style=cStack>(+" + finalNumber + " per stack)</style>. <style=cIsVoid>Corrupts all Infusions.</style>");
            LanguageAPI.Add("H3_" + upperName + "_LORE", "EXPLORER'S LOG" +
            "\n// 'Author' information lost, attempting to fix..." +
            "\n// 'Date' information lost, attempting to fix..." +
            "\n// 'Location' information lost, attempting to fix..." +
            "\n\nI no longer know where I am, and I'm certain that nobody else does either. What I do know is that it's dark. It's cold and it's humid, too. Each breath I take is belaboured, like I'm sucking in water. I've started to become used to it though. I think..." +
            "\n\nI am away now from the dangers of the planet, but I've been missing them. It was exhilirating, and almost fun to duck and dodge around and away from certain death, having lasers and balls of fire and crude projectiles launched at me from all directions. But now I'm here, and there's nothing. I came here to explore, but where I am now, everything is the same. I walk into what looks to be a black portal, but on the other side is a repeat of the same terrain. I'm hopelessly bored." +
            "\n\nI forget how many days it's been. I've already turned over every rock, searched each hilltop and tried most of the common passphrases that I know. Nothing. It's cold and yet I'm sweating. Will I be here forever? I have hope that there's a way to escape, but that's only hope. My heart is beating out of my chest... Why do I feel so anxious?" +
            "\n\nMy spyglass and toolkit are gone. I left them beside me before sleeping, woke up and they were missing. I'm so bored... but I'm so anxious. I'll try the portal again. Maybe something will change. Why am I still sweating?" +
            "\n\n<style=cStack>I'm so cold...</style>");
        }

        public static void AddHooks(ItemDef itemDefToHooks, ItemDef hiddenItemDefToHooks, float Discovery_ShieldAdd, int Discovery_MaxStacks) // Insert hooks here
        {
            // Void transformation
            VoidTransformation.CreateTransformation(itemDefToHooks, "Infusion");

            // Easy way to do this: Make a new hidden item, add one each time an interactable is used
            void DiscoveryInteract(Interactor interactor, PurchaseInteraction interaction)
            {
                // First, make sure the item isn't a printer, so we can't have infinite interaction loops
                if (interaction.costType != CostTypeIndex.WhiteItem && interaction.costType != CostTypeIndex.GreenItem && interaction.costType != CostTypeIndex.RedItem && interaction.costType != CostTypeIndex.BossItem && interaction.costType != CostTypeIndex.LunarItemOrEquipment)
                {
                    if (interactor.gameObject.GetComponent<CharacterBody>())
                    {
                        CharacterBody body = interactor.gameObject.GetComponent<CharacterBody>();
                        var bodyTeamMembers = TeamComponent.GetTeamMembers(body.teamComponent.teamIndex);

                        foreach (var member in bodyTeamMembers)
                        {
                            if (member.body && member.body.inventory && member.body.inventory.GetItemCount(itemDefToHooks) > 0 && body.inventory.GetItemCount(hiddenItemDefToHooks) < Discovery_MaxStacks)
                            {
                                member.body.inventory.GiveItem(hiddenItemDefToHooks, member.body.inventory.GetItemCount(itemDefToHooks));
                            }
                            if (member.body && member.body.inventory && member.body.inventory.GetItemCount(itemDefToHooks) < 1)
                            {
                                member.body.inventory.ResetItem(hiddenItemDefToHooks);
                            }
                        }
                    }
                }
            }
            void DiscoveryBarrelInteract(Interactor interactor)
            {

                if (interactor.gameObject.GetComponent<CharacterBody>())
                {
                    CharacterBody body = interactor.gameObject.GetComponent<CharacterBody>();
                    var bodyTeamMembers = TeamComponent.GetTeamMembers(body.teamComponent.teamIndex);

                    foreach (var member in bodyTeamMembers)
                    {
                        if (member.body && member.body.inventory && member.body.inventory.GetItemCount(itemDefToHooks) > 0 && body.inventory.GetItemCount(hiddenItemDefToHooks) < Discovery_MaxStacks)
                        {
                            member.body.inventory.GiveItem(hiddenItemDefToHooks, member.body.inventory.GetItemCount(itemDefToHooks));
                        }
                    }
                }
            }

            void DiscoveryRecalculateStats(CharacterBody body, RecalculateStatsAPI.StatHookEventArgs args)
            {
                if (body && body.inventory && body.inventory.GetItemCount(itemDefToHooks) > 0)
                {
                    args.baseShieldAdd += Discovery_ShieldAdd * body.inventory.GetItemCount(hiddenItemDefToHooks);
                }
            }

            RecalculateStatsAPI.GetStatCoefficients += DiscoveryRecalculateStats;
            On.RoR2.PurchaseInteraction.OnInteractionBegin += (orig, self, interactor) =>
            {
                orig(self, interactor);
                DiscoveryInteract(interactor, self);
            };
            On.RoR2.BarrelInteraction.OnInteractionBegin += (orig, self, interactor) =>
            {
                orig(self, interactor);
                DiscoveryBarrelInteract(interactor);
            };
        }

        public static ItemDef CreateHiddenItem()
        {
            ItemDef item = ScriptableObject.CreateInstance<ItemDef>(); // New hidden item to keep track of stacks, like infusion

            item.name = "DiscoveryHidden";
            item.nameToken = "H3_" + upperName + "_NAME";
            item.pickupToken = "H3_" + upperName + "_PICKUP";
            item.descriptionToken = "H3_" + upperName + "_DESC";
            item.loreToken = "H3_" + upperName + "_LORE";

            item.tags = new ItemTag[] { ItemTag.CannotCopy, ItemTag.CannotSteal, ItemTag.CannotDuplicate };
            item.deprecatedTier = ItemTier.NoTier;
            item.canRemove = false;
            item.hidden = true;

            item.pickupModelPrefab = Main.MainAssets.LoadAsset<GameObject>("Assets/Models/Prefabs/DiscoveryPrefab.prefab");
            item.pickupIconSprite = Main.MainAssets.LoadAsset<Sprite>("Assets/Textures/Icons/Discovery.png");

            return item;
        }

        public static void Initiate(float Discovery_ShieldAdd, int Discovery_MaxStacks) // Finally, initiate the item and all of its features
        {
            CreateItem();
            CreateHiddenItem();
            ItemAPI.Add(new CustomItem(itemDefinition, CreateDisplayRules()));
            ItemAPI.Add(new CustomItem(hiddenItemDefinition, CreateDisplayRules()));
            AddTokens(Discovery_ShieldAdd, Discovery_MaxStacks);
            AddHooks(itemDefinition, hiddenItemDefinition, Discovery_ShieldAdd, Discovery_MaxStacks);
        }
    }
}
