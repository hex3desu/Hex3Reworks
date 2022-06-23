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
    public class GhorsTome
    {
        // Our main hooks
        private static void AddHooks(float GhorsTome_ArmorBuff, float GhorsTome_ArmorBuffDuration, float GhorsTome_PickupChance, int GhorsTome_BaseMoney)
        {
            // Modify the drop chance of the original gold dropping mechanic
            IL.RoR2.GlobalEventManager.OnCharacterDeath += (il) =>
            {
                ILCursor ilcursor = new(il);
                ilcursor.GotoNext(
                    x => x.MatchLdloc(17),
                    x => x.MatchLdsfld("RoR2.RoR2Content/Items", "BonusGoldPackOnKill"),
                    x => x.MatchCallvirt<Inventory>("GetItemCount")
                );
                ilcursor.Index += 7;
                ilcursor.Next.Operand = GhorsTome_PickupChance;
            };

            // Increase gold pickup radius and speed, otherwise it's pretty annoying to use
            GameObject bonusMoneyPack = LegacyResourcesAPI.Load<GameObject>("Prefabs/NetworkedObjects/BonusMoneyPack");
            GravitatePickup gravitatePickup = bonusMoneyPack.GetComponentInChildren<GravitatePickup>();
            Collider pickupCollider = gravitatePickup.gameObject.GetComponent<Collider>();
            gravitatePickup.acceleration = 30f;
            if (pickupCollider && pickupCollider.isTrigger)
            {
                pickupCollider.transform.localScale *= 4f;
            }

            // Grant buff when gold is collected
            On.RoR2.Stats.StatManager.OnGoldCollected += (orig, characterMaster, amount) =>
            {
                orig(characterMaster, amount);
                if (amount > 0 && characterMaster && characterMaster.GetBody() && characterMaster.GetBody().inventory && characterMaster.GetBody().inventory.GetItemCount(RoR2Content.Items.BonusGoldPackOnKill) > 0)
                {
                    characterMaster.GetBody().AddTimedBuff(ghorBuff, GhorsTome_ArmorBuffDuration);
                }
            };

            // Change ghor's pickup earnings
            On.RoR2.MoneyPickup.Start += (orig, self) =>
            {
                self.baseGoldReward = GhorsTome_BaseMoney;
                orig(self);
            };

            // Add armor if buffs are present
            void AddArmor(CharacterBody body, RecalculateStatsAPI.StatHookEventArgs args)
            {
                if (body)
                {
                    if (body.HasBuff(ghorBuff))
                    {
                        args.armorAdd += (GhorsTome_ArmorBuff * body.inventory.GetItemCount(RoR2Content.Items.BonusGoldPackOnKill)) * body.GetBuffCount(ghorBuff);
                    }
                }
            }

            RecalculateStatsAPI.GetStatCoefficients += AddArmor;
        }

        // Language token replacer
        private static void ReplaceTokens(float GhorsTome_ArmorBuff, float GhorsTome_ArmorBuffDuration, float GhorsTome_PickupChance, int GhorsTome_BaseMoney)
        {
            LanguageAPI.Add("ITEM_BONUSGOLDPACKONKILL_PICKUP", "Earned money becomes temporary armor. Enemies may drop extra money on kill.");
            LanguageAPI.Add("ITEM_BONUSGOLDPACKONKILL_DESC", "Every time you earn money, gain <style=cIsHealing>" + GhorsTome_ArmorBuff + "</style> <style=cStack>(+" + GhorsTome_ArmorBuff + " per stack)</style> armor for <style=cIsHealing>" + GhorsTome_ArmorBuffDuration + "</style> seconds. Enemies have a <style=cIsUtility>" + GhorsTome_PickupChance + "%</style> chance to drop a treasure worth <style=cIsUtility>$" + GhorsTome_BaseMoney + "</style>, which scales over time.");
        }
        public static BuffDef ghorBuff { get; private set; }
        public static void AddBuffs() // Just for players to track their discovery stacks
        {
            ghorBuff = ScriptableObject.CreateInstance<BuffDef>();
            ghorBuff.buffColor = new Color(1f, 1f, 1f);
            ghorBuff.canStack = true;
            ghorBuff.isDebuff = false;
            ghorBuff.name = "Ghor's Armor";
            ghorBuff.isHidden = false;
            ghorBuff.isCooldown = false;
            ghorBuff.iconSprite = Main.MainAssets.LoadAsset<Sprite>("Assets/ReworkIcons/GhorsBuff.png");
            ContentAddition.AddBuffDef(ghorBuff);
        }

        // Apply the rework
        public static void Initiate(float GhorsTome_ArmorBuff, float GhorsTome_ArmorBuffDuration, float GhorsTome_PickupChance, int GhorsTome_BaseMoney)
        {
            ReplaceTokens(GhorsTome_ArmorBuff, GhorsTome_ArmorBuffDuration, GhorsTome_PickupChance, GhorsTome_BaseMoney);
            AddHooks(GhorsTome_ArmorBuff, GhorsTome_ArmorBuffDuration, GhorsTome_PickupChance, GhorsTome_BaseMoney);
            AddBuffs();
        }
    }
}