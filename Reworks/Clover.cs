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
    public class Clover
    {
        // Our main hooks
        private static void AddHooks(float Clover_MoneyRequirement, float Clover_MaxLuckMult)
        {
            // Remove luck gain from clover
            IL.RoR2.CharacterMaster.OnInventoryChanged += (il) =>
            {
                ILCursor ilcursor = new(il);
                ilcursor.GotoNext(
                    x => x.MatchLdarg(0),
                    x => x.MatchCall("RoR2.CharacterMaster", "get_inventory"),
                    x => x.MatchLdsfld("RoR2.RoR2Content/Items", "Clover")
                );
                ilcursor.RemoveRange(4);
                ilcursor.Emit(OpCodes.Ldc_I4, 0);
            };

            // Modify the checkroll method to include our clover luck
            On.RoR2.Util.CheckRoll_float_float_CharacterMaster += (orig, percentChance, luck, master) =>
            {
                if (percentChance <= 0f)
                {
                    return false;
                }
                // Our code
                if (master && master.inventory && master.inventory.GetItemCount(RoR2Content.Items.Clover) > 0 && master.money > 0)
                {
                    int cloverAmount = master.inventory.GetItemCount(RoR2Content.Items.Clover);
                    float maxSavings = (Clover_MoneyRequirement * cloverAmount) * Run.instance.difficultyCoefficient;
                    float money = (float)master.money;
                    if (money > maxSavings) { money = maxSavings; }
                    float multiplier = money / maxSavings;
                    percentChance += ((percentChance * multiplier) * cloverAmount) * Clover_MaxLuckMult;
                }
                int num = Mathf.CeilToInt(Mathf.Abs(luck));
                float num2 = UnityEngine.Random.Range(0f, 100f);
                float num3 = num2;
                for (int i = 0; i < num; i++)
                {
                    float b = UnityEngine.Random.Range(0f, 100f);
                    num2 = ((luck > 0f) ? Mathf.Min(num2, b) : Mathf.Max(num2, b));
                }
                if (num2 <= percentChance)
                {
                    if (num3 > percentChance && master)
                    {
                        GameObject bodyObject = master.GetBodyObject();
                        if (bodyObject)
                        {
                            CharacterBody component = bodyObject.GetComponent<CharacterBody>();
                            if (component)
                            {
                                component.wasLucky = true;
                            }
                        }
                    }
                    return true;
                }
                return false;
            };
        }

        // Language token replacer
        private static void ReplaceTokens(float Clover_MoneyRequirement, float Clover_MaxLuckMult)
        {
            LanguageAPI.Add("ITEM_CLOVER_PICKUP", "Hoarding money gradually increases your luck.");
            LanguageAPI.Add("ITEM_CLOVER_DESC", "Hoarding money up to <style=cIsUtility>$" + Clover_MoneyRequirement + "</style> <style=cStack>(+$" + Clover_MoneyRequirement + " per stack, scales with time)</style> increases the chance for all of your luck-based effects by up to <style=cIsUtility>" + (Clover_MaxLuckMult * 100f) + "%</style> <style=cStack>(+" + (Clover_MaxLuckMult * 100f) + "% per stack)</style>");
        }

        // Apply the rework
        public static void Initiate(float Clover_MoneyRequirement, float Clover_MaxLuckMult)
        {
            ReplaceTokens(Clover_MoneyRequirement, Clover_MaxLuckMult);
            AddHooks(Clover_MoneyRequirement, Clover_MaxLuckMult);
        }
    }
}