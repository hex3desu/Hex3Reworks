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
    public class LeptonDaisy
    {
        // Our main hooks
        private static void AddHooks(float LeptonDaisy_WeakDuration)
        {
            // When a target is picked, decide whether to weaken or heal it based on its team
            On.EntityStates.TeleporterHealNovaController.TeleporterHealNovaPulse.HealPulse.HealTarget += (orig, self, target) =>
            {
                if (target.gameObject.GetComponent<TeamComponent> != null && target.body)
                {
                    TeamIndex teamIndex = target.gameObject.GetComponent<TeamComponent>().teamIndex;
                    if (teamIndex == TeamIndex.Monster || teamIndex == TeamIndex.Lunar || teamIndex == TeamIndex.Void)
                    {
                        target.body.AddTimedBuff(RoR2Content.Buffs.Weak, LeptonDaisy_WeakDuration);
                    }
                    if (teamIndex == TeamIndex.Player)
                    {
                        orig(self, target);
                    }
                }
            };

            TeamMask mask = TeamMask.none;

            // Remove the check for the TeamMask so all entities are targeted
            IL.EntityStates.TeleporterHealNovaController.TeleporterHealNovaPulse.HealPulse.Update += (il) =>
            {
                ILCursor ilcursor = new(il);
                ilcursor.GotoNext(
                    x => x.MatchLdarg(0),
                    x => x.MatchLdfld<EntityStates.TeleporterHealNovaController.TeleporterHealNovaPulse.HealPulse>("teamMask"),
                    x => x.MatchCallvirt<SphereSearch>("FilterCandidatesByHurtBoxTeam")
                );
                ilcursor.RemoveRange(3);
            };
        }

        // Language token replacer
        private static void ReplaceTokens(float LeptonDaisy_WeakDuration)
        {
            LanguageAPI.Add("ITEM_TPHEALINGNOVA_PICKUP", "Periodically release a nova during the Teleporter event which heals you and weakens enemies.");
            LanguageAPI.Add("ITEM_TPHEALINGNOVA_DESC", "Release a <style=cIsHealing>healing nova</style> during the Teleporter event, <style=cIsHealing>healing</style> allies for <style=cIsHealing>50%</style> of their maximum health and <style=cIsDamage>weakening</style> enemies for <style=cIsDamage>" + LeptonDaisy_WeakDuration + "</style> seconds. Occurs <style=cIsHealing>1</style> <style=cStack>(+1 per stack)</style> times.");
        }

        // Apply the rework
        public static void Initiate(float LeptonDaisy_WeakDuration)
        {
            ReplaceTokens(LeptonDaisy_WeakDuration);
            AddHooks(LeptonDaisy_WeakDuration);
        }
    }
}

/*
ilhook has driven me mentally insane

[Intro]
(Secure the bag, know what I mean? Banrisk on the beat)
(Ayo, Perish, this is hot, boy)

[Verse 1]
I wear a mask with a smile for hours at a time
Stare at the ceiling while I hold back what's on my mind
And when they ask me how I'm doing, I say, "I'm just fine"
And when they ask me how I'm doing, I say, "I'm just fine"
But the fact is
I can never get off of my mattress
And all that they can ask is
"Why are you so sad, kid?" (Why are you so sad, kid?)
[Pre-Chorus]
That's what the mask is
That's what the point of the mask is

[Chorus]
So you can see I'm tryin', you won't see me cryin'
I'll just keep on smilin', I'm good (Yeah, I'm good)
And it just keeps on pilin', it's so terrifying
But I keep on smilin', I'm good (Yeah, I'm good)
I've been carin' too much for so long
Been comparin' myself for so long
Been wearin' a smile for so long, it's real
So long, it's rеal, so long, it's real

[Verse 2]
Always bein' judged by a bunch of strangе faces
Scared to go outside, haven't seen the light in ages
But I've been places
So I'm okay-ish, so I'm okay-ish
Yeah, I'm okay, bitch
But the fact is
I need help, I'm failin' all my classes
They think that I need glasses
I just really wish that I could pass this (Wish that I could pass this)

[Pre-Chorus]
That's what the mask is
That's what the point of the mask is
[Chorus]
So you can see I'm tryin', you won't see me cryin'
I'll just keep on smilin', I'm good (Yeah, I'm good)
And it just keeps on pilin', it's so terrifying
But I keep on smilin', I'm good (Yeah, I'm good)
I've been carin' too much for so long
Been comparin' myself for so long
Been wearin' a smile for so long, it's real
So long, it's real, so long, it's real

[Outro]
So long, it's real
So long, it's real
*/