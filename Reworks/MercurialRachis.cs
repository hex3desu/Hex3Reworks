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
using UnityEngine.Networking;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using Hex3Reworks.Logging;

namespace Hex3Reworks.Reworks
{
    public class MercurialRachis
    {
        // Our main hooks
        private static void AddHooks(float MercurialRachis_Radius, float MercurialRachis_PlacementRadius, bool MercurialRachis_IsTonic, float MercurialRachis_EnemyBuffRadius)
        {
            // If the ward's buff is a Power Buff, change it to a Tonic Buff instead
            On.RoR2.BuffWard.BuffTeam += (orig, self, recipients, radisuSqr, currentPosition) =>
            {
                if (self.buffDef == RoR2Content.Buffs.PowerBuff && self.teamFilter && self.teamFilter.teamIndex == TeamIndex.None)
                {
                    if (MercurialRachis_IsTonic == true) { self.buffDef = RoR2Content.Buffs.TonicBuff; }
                }
                orig(self, recipients, radisuSqr, currentPosition);
            };

            // Create a second buff zone that gives enemies the Power Buff
            On.RoR2.CharacterMaster.AddDeployable += (orig, self, deployable, slot) =>
            {
                orig(self, deployable, slot);
                if (slot == DeployableSlot.PowerWard && self.inventory)
                {
                    GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(LegacyResourcesAPI.Load<GameObject>("Prefabs/NetworkedObjects/DamageZoneWard"), deployable.transform.position, Quaternion.identity);
                    gameObject.GetComponent<TeamFilter>().teamIndex = TeamIndex.Player;
                    BuffWard component = gameObject.GetComponent<BuffWard>();
                    component.Networkradius = MercurialRachis_EnemyBuffRadius * Mathf.Pow(1.5f, self.inventory.GetItemCount(RoR2Content.Items.RandomDamageZone));
                    component.expireDuration = RoR2.Items.RandomDamageZoneBodyBehavior.wardDuration;
                    NetworkServer.Spawn(gameObject);
                }
            };

            // Modify Ward location to be predictable
            IL.RoR2.Items.RandomDamageZoneBodyBehavior.FindWardSpawnPosition += (il) =>
            {
                ILCursor ilcursor = new(il);
                ilcursor.GotoNext(
                    x => x.MatchLdloc(0),
                    x => x.MatchLdarg(1),
                    x => x.MatchLdcR4(0f),
                    x => x.MatchLdcR4(50f)
                );
                ilcursor.Index += 3;
                ilcursor.Next.Operand = MercurialRachis_PlacementRadius;
            };

            // Set ward range
            IL.RoR2.Items.RandomDamageZoneBodyBehavior.FixedUpdate += (il) =>
            {
                ILCursor ilcursor = new(il);
                ilcursor.GotoNext(
                    x => x.MatchDup(),
                    x => x.MatchLdsfld<RoR2.Items.RandomDamageZoneBodyBehavior>("baseWardRadius")
                );
                ilcursor.Index += 1;
                ilcursor.Remove();
                ilcursor.Emit(OpCodes.Ldc_R4, MercurialRachis_Radius);
            };
        }

        // Language token replacer
        private static void ReplaceTokens(float MercurialRachis_Radius, bool MercurialRachis_IsTonic, float MercurialRachis_EnemyBuffRadius)
        {
            if (MercurialRachis_IsTonic == true)
            {
                LanguageAPI.Add("ITEM_RANDOMDAMAGEZONE_PICKUP", "Occasionally creates a Ward Of Power that provides a <style=cIsUtility>Spinel Tonic</style> buff to ALL characters in range... <style=cDeath>BUT all nearby enemies will deal more damage.</style>");
                LanguageAPI.Add("ITEM_RANDOMDAMAGEZONE_DESC", "Creates a Ward Of Power at your location that provides a <style=cIsUtility>Spinel Tonic</style> buff to all characters within <style=cIsUtility>" + MercurialRachis_Radius + "m</style> <style=cStack>(+" + (MercurialRachis_Radius / 2) + "m per stack)</style>, as well as a <style=cDeath>50% damage buff</style> to all enemies within <style=cDeath>" + MercurialRachis_EnemyBuffRadius + "m</style> <style=cStack>(+" + (MercurialRachis_EnemyBuffRadius * 0.5f) + "m per stack)</style>.");
            }
            else
            {
                LanguageAPI.Add("ITEM_RANDOMDAMAGEZONE_PICKUP", "Occasionally creates a Ward Of Power that provides a <style=cIsDamage>50% damage buff</style> to ALL characters in range... <style=cDeath>BUT all nearby enemies will also deal more damage.</style>");
                LanguageAPI.Add("ITEM_RANDOMDAMAGEZONE_DESC", "Creates a Ward Of Power at your location that provides a <style=cIsDamage>50% damage buff</style> to allies within <style=cIsUtility>" + MercurialRachis_Radius + "m</style> <style=cStack>(+" + (MercurialRachis_Radius / 2) + "m per stack)</style>, as well as all enemies within <style=cDeath>" + MercurialRachis_EnemyBuffRadius + "m</style> <style=cStack>(+" + (MercurialRachis_EnemyBuffRadius * 0.5f) + "m per stack)</style>.");
            }
        }

        // Apply the rework
        public static void Initiate(float MercurialRachis_Radius, float MercurialRachis_PlacementRadius, bool MercurialRachis_IsTonic, float MercurialRachis_EnemyBuffRadius)
        {
            ReplaceTokens(MercurialRachis_Radius, MercurialRachis_IsTonic, MercurialRachis_EnemyBuffRadius);
            AddHooks(MercurialRachis_Radius, MercurialRachis_PlacementRadius, MercurialRachis_IsTonic, MercurialRachis_EnemyBuffRadius);
        }
    }
}
