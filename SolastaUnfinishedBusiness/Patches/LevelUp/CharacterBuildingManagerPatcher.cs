﻿using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection.Emit;
using HarmonyLib;
using JetBrains.Annotations;
using SolastaUnfinishedBusiness.Models;
using TA;
using static FeatureDefinitionCastSpell;

namespace SolastaUnfinishedBusiness.Patches.LevelUp;

[HarmonyPatch(typeof(CharacterBuildingManager), "CreateNewCharacter")]
[SuppressMessage("Minor Code Smell", "S101:Types should be named in PascalCase", Justification = "Patch")]
internal static class CharacterBuildingManager_CreateCharacter
{
    internal static void Postfix([NotNull] CharacterBuildingManager __instance)
    {
        //PATCH: registers the hero getting created
        LevelUpContext.RegisterHero(__instance.CurrentLocalHeroCharacter, false);
    }
}

[HarmonyPatch(typeof(CharacterBuildingManager), "LevelUpCharacter")]
[SuppressMessage("Minor Code Smell", "S101:Types should be named in PascalCase", Justification = "Patch")]
internal static class CharacterBuildingManager_LevelUpCharacter
{
    internal static void Prefix([NotNull] RulesetCharacterHero hero, ref bool force)
    {
        //PATCH: forces no experience on level up setting
        if (Main.Settings.NoExperienceOnLevelUp)
        {
            force = true;
        }

        //PATCH: registers the hero leveling up
        LevelUpContext.RegisterHero(hero, true);
    }
}

[HarmonyPatch(typeof(CharacterBuildingManager), "FinalizeCharacter")]
[SuppressMessage("Minor Code Smell", "S101:Types should be named in PascalCase", Justification = "Patch")]
internal static class CharacterBuildingManager_FinalizeCharacter
{
    internal static void Prefix([NotNull] CharacterBuildingManager __instance, [NotNull] RulesetCharacterHero hero)
    {
        //PATCH: grants race features
        LevelUpContext.GrantRaceFeatures(__instance, hero);

        //PATCH: grants custom features
        LevelUpContext.GrantCustomFeatures(hero);
    }

    internal static void Postfix([NotNull] RulesetCharacterHero hero)
    {
        //PATCH: keeps spell repertoires sorted by class title but ancestry one is always kept first
        LevelUpContext.SortHeroRepertoires(hero);

        //PATCH: adds whole list caster spells to KnownSpells collection to improve the MC spell selection UI
        LevelUpContext.UpdateKnownSpellsForWholeCasters(hero);

        //PATCH: grants items from new classes if required
        LevelUpContext.GrantItemsIfRequired(hero);

        //PATCH: unregisters the hero leveling up
        LevelUpContext.UnregisterHero(hero);
    }
}

[HarmonyPatch(typeof(CharacterBuildingManager), "AssignClassLevel")]
[SuppressMessage("Minor Code Smell", "S101:Types should be named in PascalCase", Justification = "Patch")]
internal static class CharacterBuildingManager_AssignClassLevel
{
    internal static bool Prefix([NotNull] RulesetCharacterHero hero, CharacterClassDefinition classDefinition)
    {
        //PATCH: captures the desired class
        LevelUpContext.SetSelectedClass(hero, classDefinition);

        //PATCH: ensures this doesn't get executed in the class panel level up screen
        var isLevelingUp = LevelUpContext.IsLevelingUp(hero);
        var isClassSelectionStage = LevelUpContext.IsClassSelectionStage(hero);

        return !(isLevelingUp && isClassSelectionStage);
    }
}

[HarmonyPatch(typeof(CharacterBuildingManager), "AssignSubclass")]
[SuppressMessage("Minor Code Smell", "S101:Types should be named in PascalCase", Justification = "Patch")]
internal static class CharacterBuildingManager_AssignSubclass
{
    internal static void Prefix([NotNull] RulesetCharacterHero hero, CharacterSubclassDefinition subclassDefinition)
    {
        //PATCH: captures the desired sub class
        LevelUpContext.SetSelectedSubclass(hero, subclassDefinition);
    }
}

[HarmonyPatch(typeof(CharacterBuildingManager), "GrantFeatures")]
[SuppressMessage("Minor Code Smell", "S101:Types should be named in PascalCase", Justification = "Patch")]
internal static class CharacterBuildingManager_GrantFeatures
{
    internal static bool Prefix([NotNull] RulesetCharacterHero hero)
    {
        //PATCH: ensures this doesn't get executed in the class panel level up screen
        var isLevelingUp = LevelUpContext.IsLevelingUp(hero);
        var isClassSelectionStage = LevelUpContext.IsClassSelectionStage(hero);

        return !(isLevelingUp && isClassSelectionStage);
    }
}

[HarmonyPatch(typeof(CharacterBuildingManager), "UnassignLastClassLevel")]
[SuppressMessage("Minor Code Smell", "S101:Types should be named in PascalCase", Justification = "Patch")]
internal static class CharacterBuildingManager_UnassignLastClassLevel
{
    internal static bool Prefix([NotNull] RulesetCharacterHero hero)
    {
        //PATCH: ensures this doesn't get executed in the class panel level up screen
        var isLevelingUp = LevelUpContext.IsLevelingUp(hero);
        var isClassSelectionStage = LevelUpContext.IsClassSelectionStage(hero);

        return !(isLevelingUp && isClassSelectionStage);
    }
}

[HarmonyPatch(typeof(CharacterBuildingManager), "EnumerateKnownAndAcquiredSpells")]
[SuppressMessage("Minor Code Smell", "S101:Types should be named in PascalCase", Justification = "Patch")]
internal static class CharacterBuildingManager_EnumerateKnownAndAcquiredSpells
{
    internal static void Postfix(
        [NotNull] CharacterHeroBuildingData heroBuildingData,
        List<SpellDefinition> __result)
    {
        //PATCH: ensures the level up process only presents / offers spells from current class
        LevelUpContext.EnumerateKnownAndAcquiredSpells(heroBuildingData, __result);
    }
}

//PATCH: gets the correct spell feature for the selected class
[HarmonyPatch(typeof(CharacterBuildingManager), "GetSpellFeature")]
[SuppressMessage("Minor Code Smell", "S101:Types should be named in PascalCase", Justification = "Patch")]
internal static class CharacterBuildingManager_GetSpellFeature
{
    internal static bool Prefix(
        [NotNull] CharacterHeroBuildingData heroBuildingData,
        string tag,
        ref FeatureDefinitionCastSpell __result)
    {
        var hero = heroBuildingData.HeroCharacter;
        var isMulticlass = LevelUpContext.IsMulticlass(hero);

        if (!isMulticlass)
        {
            return true;
        }

        var selectedClass = LevelUpContext.GetSelectedClass(hero);

        if (selectedClass == null)
        {
            return true;
        }

        var localTag = tag;

        __result = null;

        if (localTag.StartsWith(AttributeDefinitions.TagClass))
        {
            localTag = AttributeDefinitions.TagClass + selectedClass.Name;
        }
        else if (localTag.StartsWith(AttributeDefinitions.TagSubclass))
        {
            localTag = AttributeDefinitions.TagSubclass + selectedClass.Name;
        }

        // PATCH
        foreach (var activeFeature in hero.ActiveFeatures.Where(x => x.Key.StartsWith(localTag)))
        {
            foreach (var featureDefinition in activeFeature.Value
                         .OfType<FeatureDefinitionCastSpell>())
            {
                __result = featureDefinition;

                return false;
            }
        }

        if (!localTag.StartsWith(AttributeDefinitions.TagSubclass))
        {
            return false;
        }

        localTag = AttributeDefinitions.TagClass + selectedClass.Name;

        // PATCH
        foreach (var activeFeature in hero.ActiveFeatures.Where(x => x.Key.StartsWith(localTag)))
        {
            foreach (var featureDefinition in activeFeature.Value
                         .OfType<FeatureDefinitionCastSpell>())
            {
                __result = featureDefinition;

                return false;
            }
        }

        return false;
    }
}

//PATCH: ensures the level up process only offers slots from the leveling up class
[HarmonyPatch(typeof(CharacterBuildingManager), "UpgradeSpellPointPools")]
[SuppressMessage("Minor Code Smell", "S101:Types should be named in PascalCase", Justification = "Patch")]
internal static class CharacterBuildingManager_UpgradeSpellPointPools
{
    internal static bool Prefix(
        CharacterBuildingManager __instance,
        [NotNull] CharacterHeroBuildingData heroBuildingData)
    {
        var hero = heroBuildingData.HeroCharacter;
        var isMulticlass = LevelUpContext.IsMulticlass(hero);

        if (!isMulticlass)
        {
            return true;
        }

        var selectedClass = LevelUpContext.GetSelectedClass(hero);
        var selectedSubclass = LevelUpContext.GetSelectedSubclass(hero);
        var selectedClassLevel = LevelUpContext.GetSelectedClassLevel(hero);

        foreach (var spellRepertoire in hero.SpellRepertoires)
        {
            var poolName = string.Empty;
            var maxPoints = 0;

            switch (spellRepertoire.SpellCastingFeature.SpellCastingOrigin)
            {
                // PATCH: short circuit if the feature is for another class (change from native code)
                case CastingOrigin.Class when spellRepertoire.SpellCastingClass != selectedClass:
                    continue;
                case CastingOrigin.Class:
                    poolName = AttributeDefinitions.GetClassTag(selectedClass, selectedClassLevel);
                    break;
                // PATCH: short circuit if the feature is for another subclass (change from native code)
                case CastingOrigin.Subclass when spellRepertoire.SpellCastingSubclass != selectedSubclass:
                    continue;
                case CastingOrigin.Subclass:
                    poolName = AttributeDefinitions.GetSubclassTag(selectedClass, selectedClassLevel, selectedSubclass);
                    break;
                case CastingOrigin.Race:
                    poolName = AttributeDefinitions.TagRace;
                    break;
            }

            if (__instance.HasAnyActivePoolOfType(heroBuildingData, HeroDefinitions.PointsPoolType.Cantrip)
                && heroBuildingData.PointPoolStacks[HeroDefinitions.PointsPoolType.Cantrip].ActivePools
                    .ContainsKey(poolName))
            {
                maxPoints = heroBuildingData.PointPoolStacks[HeroDefinitions.PointsPoolType.Cantrip]
                    .ActivePools[poolName].MaxPoints;
            }

            heroBuildingData.TempAcquiredCantripsNumber = 0;
            heroBuildingData.TempAcquiredSpellsNumber = 0;
            heroBuildingData.TempUnlearnedSpellsNumber = 0;

            __instance.ApplyFeatureCastSpell(heroBuildingData, spellRepertoire.SpellCastingFeature);
            __instance.SetPointPool(heroBuildingData, HeroDefinitions.PointsPoolType.Cantrip, poolName,
                heroBuildingData.TempAcquiredCantripsNumber + maxPoints);
            __instance.SetPointPool(heroBuildingData, HeroDefinitions.PointsPoolType.Spell, poolName,
                heroBuildingData.TempAcquiredSpellsNumber);
            __instance.SetPointPool(heroBuildingData, HeroDefinitions.PointsPoolType.SpellUnlearn, poolName,
                heroBuildingData.TempUnlearnedSpellsNumber);
        }

        return false;
    }
}

//BUGFIX: fixes a TA issue that not consider subclass morphotype preferences
[HarmonyPatch(typeof(CharacterBuildingManager), "AssignDefaultMorphotypes")]
[SuppressMessage("Minor Code Smell", "S101:Types should be named in PascalCase", Justification = "Patch")]
internal class CharacterBuildingManager_AssignDefaultMorphotypes
{
    public static RangedInt PreferedSkinColors(
        RacePresentation racePresentation,
        [NotNull] CharacterHeroBuildingData heroBuildingData)
    {
        var subRaceDefinition = heroBuildingData.HeroCharacter.SubRaceDefinition;

        return subRaceDefinition != null
            ? subRaceDefinition.RacePresentation.PreferedSkinColors
            : racePresentation.PreferedSkinColors;
    }

    public static RangedInt PreferedHairColors(
        RacePresentation racePresentation,
        [NotNull] CharacterHeroBuildingData heroBuildingData)
    {
        var subRaceDefinition = heroBuildingData.HeroCharacter.SubRaceDefinition;

        return subRaceDefinition != null
            ? subRaceDefinition.RacePresentation.PreferedHairColors
            : racePresentation.PreferedHairColors;
    }

    internal static IEnumerable<CodeInstruction> Transpiler([NotNull] IEnumerable<CodeInstruction> instructions)
    {
        var preferedSkinColorsMethod = typeof(RacePresentation).GetMethod("get_PreferedSkinColors");
        var preferedHairColorsColorsMethod = typeof(RacePresentation).GetMethod("get_PreferedHairColors");
        var myPreferedSkinColorsMethod =
            typeof(CharacterBuildingManager_AssignDefaultMorphotypes).GetMethod("PreferedSkinColors");
        var myPreferedHairColorsColorsMethod =
            typeof(CharacterBuildingManager_AssignDefaultMorphotypes).GetMethod("PreferedHairColors");

        foreach (var instruction in instructions)
        {
            if (instruction.Calls(preferedSkinColorsMethod))
            {
                yield return new CodeInstruction(OpCodes.Ldarg, 1); // heroBuildingData
                yield return new CodeInstruction(OpCodes.Call, myPreferedSkinColorsMethod);
            }
            else if (instruction.Calls(preferedHairColorsColorsMethod))
            {
                yield return new CodeInstruction(OpCodes.Ldarg, 1); // heroBuildingData
                yield return new CodeInstruction(OpCodes.Call, myPreferedHairColorsColorsMethod);
            }
            else
            {
                yield return instruction;
            }
        }
    }
}

//BUGFIX: replaces this method completely to remove weird 'return' on FeatureDefinitionCastSpell check
[HarmonyPatch(typeof(CharacterBuildingManager), "BrowseGrantedFeaturesHierarchically")]
[SuppressMessage("Minor Code Smell", "S101:Types should be named in PascalCase", Justification = "Patch")]
internal static class CharacterBuildingManager_BrowseGrantedFeaturesHierarchically
{
    internal static bool Prefix(
        [NotNull] CharacterBuildingManager __instance,
        [NotNull] CharacterHeroBuildingData heroBuildingData,
        [NotNull] List<FeatureDefinition> grantedFeatures,
        string tag)
    {
        foreach (var grantedFeature in grantedFeatures)
        {
            switch (grantedFeature)
            {
                case FeatureDefinitionCastSpell spell:
                    __instance.SetupSpellPointPools(heroBuildingData, spell, tag);

                    // this was `return` in original code, leading to game skipping granting some features
                    break;
                case FeatureDefinitionBonusCantrips cantrips:
                    using (var enumerator = cantrips.BonusCantrips.GetEnumerator())
                    {
                        while (enumerator.MoveNext())
                        {
                            var current = enumerator.Current;
                            if (current != null)
                            {
                                __instance.AcquireBonusCantrip(heroBuildingData, current, tag);
                            }
                        }
                    }

                    break;
                case FeatureDefinitionProficiency definitionProficiency:
                    if (definitionProficiency.ProficiencyType == RuleDefinitions.ProficiencyType.FightingStyle)
                    {
                        using var enumerator = definitionProficiency.Proficiencies.GetEnumerator();

                        while (enumerator.MoveNext())
                        {
                            var current = enumerator.Current;

                            var element = DatabaseRepository.GetDatabase<FightingStyleDefinition>().GetElement(current);
                            __instance.AcquireBonusFightingStyle(heroBuildingData, element, tag);
                        }
                    }

                    break;
                case FeatureDefinitionFeatureSet definitionFeatureSet:
                    if (definitionFeatureSet.Mode == FeatureDefinitionFeatureSet.FeatureSetMode.Union)
                    {
                        __instance.BrowseGrantedFeaturesHierarchically(heroBuildingData,
                            definitionFeatureSet.FeatureSet, tag);
                    }

                    break;
            }
        }

        return false;
    }
}
