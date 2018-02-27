using Harmony;
using HugsLib;
using HugsLib.Settings;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace Amnesia
{
    class Amnesia : ModBase
    {
        public override string ModIdentifier => "amnesia";
        public static string childBS = "ChildAmensia";
        public static string adultBS = "AdultAmensia";

        public static SettingHandle<bool> ForgetBackstories;
        public static SettingHandle<bool> ForgetForcedBackstories;
        public static SettingHandle<bool> ForgetTraits;
        public static SettingHandle<bool> ForgetForcedTraits;

        public override void DefsLoaded()
        {
            ForgetBackstories = Settings.GetHandle<bool>("forgetBackstories", "Forget Most Backstories", "Causes most of the backstories in the game to be generated as Amnesia. Has no effect on already generated pawns.", true);
            ForgetForcedBackstories = Settings.GetHandle<bool>("forgetForcedBackstories", "Also Forget Forced Backstories", "If Forget Most Backstories is enanbled, this option will also remove the backstories of 'special' pawns.", true);
            ForgetTraits = Settings.GetHandle<bool>("forgetTraits", "Forget Most Traits", "Makes newly generated pawns to have no traits other than the ones required by thier backstories.", true);
            ForgetForcedTraits = Settings.GetHandle<bool>("forgetForcedTraits", "Also Forget Forced Traits", "If 'Forget Most Traits' is enabled, this option forces backstory traits to be removed.", true);
        }

        public override void SettingsChanged()
        {
            DefsLoaded();
        }

        public override void Initialize()
        {
            base.Initialize();
            Backstory cbs = new Backstory();
            Backstory abs = new Backstory();

            cbs.identifier = childBS;
            cbs.slot = BackstorySlot.Childhood;
            cbs.baseDesc = "Questions... ";
            cbs.bodyTypeGlobal = BodyType.Undefined;
            cbs.bodyTypeMale = BodyType.Male;
            cbs.bodyTypeFemale = BodyType.Female;
            cbs.shuffleable = false;
            cbs.SetTitleShort("Amnesiac");
            cbs.SetTitle("Child Amnesiac");

            abs.identifier = adultBS;
            abs.slot = BackstorySlot.Adulthood;
            abs.baseDesc = "Nope, still nothing.";
            abs.bodyTypeGlobal = BodyType.Undefined;
            abs.bodyTypeMale = BodyType.Male;
            abs.bodyTypeFemale = BodyType.Female;
            abs.shuffleable = false;
            abs.SetTitleShort("Amnesiac");
            abs.SetTitle("Complete Amnesiac");

            BackstoryDatabase.AddBackstory(abs);
            BackstoryDatabase.AddBackstory(cbs);
        }
    }

    [HarmonyPatch(typeof(PawnBioAndNameGenerator), "GiveShuffledBioTo")]
    class BackstoryShuffledPatch
    {
        [HarmonyPostfix]
        public static void postfix(ref Pawn pawn)
        {
            if (Amnesia.ForgetBackstories)
                if (pawn != null && (pawn.story.childhood.shuffleable || Amnesia.ForgetForcedBackstories))
                {
                    pawn.story.childhood = BackstoryDatabase.allBackstories.TryGetValue(Amnesia.childBS);
                    if (pawn.story.adulthood != null)
                        pawn.story.adulthood = BackstoryDatabase.allBackstories.TryGetValue(Amnesia.adultBS);
                }
        }
    }

    [HarmonyPatch(typeof(PawnBioAndNameGenerator), "TryGiveSolidBioTo")]
    class BackstorySolidPatch
    {
        [HarmonyPostfix]
        public static void postfix(ref Pawn pawn, bool __result)
        {
            if (Amnesia.ForgetBackstories && (bool)__result)
                if (pawn.story.childhood.shuffleable || Amnesia.ForgetForcedBackstories)
                {
                    pawn.story.childhood = BackstoryDatabase.allBackstories.TryGetValue(Amnesia.childBS);
                    if (pawn.story.adulthood != null)
                        pawn.story.adulthood = BackstoryDatabase.allBackstories.TryGetValue(Amnesia.adultBS);
                }
        }
    }

    [HarmonyPatch(typeof(PawnGenerator), "GenerateTraits", new Type[] { typeof(Pawn), typeof(PawnGenerationRequest) })]
    class TraitPatch
    {
        [HarmonyPostfix]
        public static void postfix(ref Pawn pawn)
        {
            if (Amnesia.ForgetTraits)
            {
                pawn.story.traits.allTraits.Clear();
                if (pawn.story.childhood.forcedTraits != null && !Amnesia.ForgetForcedTraits)
                {
                    List<TraitEntry> forcedTraits = pawn.story.childhood.forcedTraits;
                    for (int i = 0; i < forcedTraits.Count; i++)
                    {
                        TraitEntry traitEntry = forcedTraits[i];
                        if (traitEntry.def == null)
                        {
                            Log.Error("Null forced trait def on " + pawn.story.childhood);
                        }
                        else if (!pawn.story.traits.HasTrait(traitEntry.def))
                        {
                            pawn.story.traits.GainTrait(new Trait(traitEntry.def, traitEntry.degree, false));
                        }
                    }
                }
            }
        }
    }
}
