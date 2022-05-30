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
    public class BustlingFungus
    {
        // Our main hooks
        private static void AddHooks(float BustlingFungus_Healing, float BustlingFungus_Radius, float BustlingFungus_RadiusStack, float BustlingFungus_ZoneInterval)
        {
            // Remove vanilla Bustling Fungus function by making it return immediately
            IL.RoR2.Items.MushroomBodyBehavior.FixedUpdate += (il) =>
            {
                ILCursor ilcursor = new(il);
                ilcursor.GotoNext(
                    x => x.MatchLdarg(0),
                    x => x.MatchLdfld<RoR2.Items.BaseItemBodyBehavior>("stack"),
                    x => x.MatchStloc(0),
                    x => x.MatchLdloc(0)
                );
                ilcursor.Index -= 3;
                ilcursor.Emit(OpCodes.Ret);
            };

            // Every 10 seconds, create a new zone
            On.RoR2.Items.MushroomBodyBehavior.FixedUpdate += (orig, self) =>
            {
                orig(self);

                if (self.body.GetComponent<MushroomTimer>() == false)
                {
                    self.body.AddItemBehavior<MushroomTimer>(self.stack);
                }
                if (!UnityEngine.Networking.NetworkServer.active)
                {
                    return;
                }

                int stack = self.stack;
                float networkradius = self.body.radius + BustlingFungus_Radius + (BustlingFungus_RadiusStack * (float)stack);
                MushroomTimer mushroomTimer = self.body.GetComponent<MushroomTimer>();

                mushroomTimer.interval += Time.fixedDeltaTime;
                if (mushroomTimer.interval > BustlingFungus_ZoneInterval) // Destroy existing mushroom wards, and create a new one
                {
                    UnityEngine.Object.Destroy(self.mushroomWardGameObject);
                    self.mushroomWardGameObject = null;

                    self.mushroomWardGameObject = UnityEngine.Object.Instantiate<GameObject>(RoR2.Items.MushroomBodyBehavior.mushroomWardPrefab, self.body.footPosition, Quaternion.identity);
                    self.mushroomWardTeamFilter = self.mushroomWardGameObject.GetComponent<TeamFilter>();
                    self.mushroomHealingWard = self.mushroomWardGameObject.GetComponent<HealingWard>();
                    UnityEngine.Networking.NetworkServer.Spawn(self.mushroomWardGameObject);
                    mushroomTimer.interval = 0f;
                }
                if (self.mushroomHealingWard) // Define the healing ward's variables
                {
                    self.mushroomHealingWard.interval = 0.25f;
                    self.mushroomHealingWard.healFraction = (BustlingFungus_Healing + (BustlingFungus_Healing * (float)(stack - 1))) * self.mushroomHealingWard.interval;
                    self.mushroomHealingWard.healPoints = 0f;
                    self.mushroomHealingWard.Networkradius = networkradius;
                }
                if (self.mushroomWardTeamFilter)
                {
                    self.mushroomWardTeamFilter.teamIndex = self.body.teamComponent.teamIndex;
                }
            };
        }

        public class MushroomTimer : CharacterBody.ItemBehavior
        {
            public float interval = 0f;
        }

        // Language token replacer
        private static void ReplaceTokens(float BustlingFungus_Healing, float BustlingFungus_Radius, float BustlingFungus_RadiusStack, float BustlingFungus_ZoneInterval)
        {
            LanguageAPI.Add("ITEM_MUSHROOM_PICKUP", "Occasionally leave behind a healing zone.");
            LanguageAPI.Add("ITEM_MUSHROOM_DESC", "Every <style=cIsUtility>" + BustlingFungus_ZoneInterval + "</style> seconds, leave behind a <style=cIsHealing>fungal zone</style> that <style=cIsHealing>heals</style> for <style=cIsHealing>" + (BustlingFungus_Healing * 100f) + "%</style> <style=cStack>(+" + (BustlingFungus_Healing * 100f) + "% per stack)</style> of your <style=cIsHealing>max health</style> every second to all allies within <style=cIsHealing>" + BustlingFungus_Radius + "m</style> <style=cStack>(+" + BustlingFungus_RadiusStack + "m per stack)</style>.");
        }

        // Apply the rework
        public static void Initiate(float BustlingFungus_Healing, float BustlingFungus_Radius, float BustlingFungus_RadiusStack, float BustlingFungus_ZoneInterval)
        {
            ReplaceTokens(BustlingFungus_Healing, BustlingFungus_Radius, BustlingFungus_RadiusStack, BustlingFungus_ZoneInterval);
            AddHooks(BustlingFungus_Healing, BustlingFungus_Radius, BustlingFungus_RadiusStack, BustlingFungus_ZoneInterval);
        }
    }
}