﻿using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection.Emit;
using HarmonyLib;
using JetBrains.Annotations;
using SolastaUnfinishedBusiness.Api;
using SolastaUnfinishedBusiness.Api.Helpers;
using SolastaUnfinishedBusiness.Models;

namespace SolastaUnfinishedBusiness.Patches;

[UsedImplicitly]
public static class ArchetypesPreviewModalPatcher
{
    [HarmonyPatch(typeof(ArchetypesPreviewModal), nameof(ArchetypesPreviewModal.Refresh))]
    [SuppressMessage("Minor Code Smell", "S101:Types should be named in PascalCase", Justification = "Patch")]
    [UsedImplicitly]
    public static class Refresh_Patch
    {
        //PATCH: filters out on subclass display features already taken (MULTICLASS)
        private static int Level([NotNull] FeatureUnlockByLevel featureUnlockByLevel)
        {
            var hero = Global.LevelUpHero;

            if (hero == null)
            {
                return featureUnlockByLevel.Level;
            }

            var isLevelingUp = LevelUpContext.IsLevelingUp(hero);
            var selectedClass = LevelUpContext.GetSelectedClass(hero);

            if (selectedClass != null
                && isLevelingUp
                && hero.ClassesAndLevels.TryGetValue(selectedClass, out var levels)
                && featureUnlockByLevel.Level <= levels + 1)
            {
                return int.MaxValue;
            }

            return featureUnlockByLevel.Level;
        }

        [NotNull]
        [UsedImplicitly]
        public static IEnumerable<CodeInstruction> Transpiler([NotNull] IEnumerable<CodeInstruction> instructions)
        {
            var levelMethod = typeof(FeatureUnlockByLevel).GetMethod("get_Level");
            var myLevelMethod = new Func<FeatureUnlockByLevel, int>(Level).Method;

            return instructions.ReplaceCalls(levelMethod, "ArchetypesPreviewModal.Refresh",
                new CodeInstruction(OpCodes.Call, myLevelMethod));
        }
    }

    [HarmonyPatch(typeof(ArchetypesPreviewModal), nameof(ArchetypesPreviewModal.Show))]
    [SuppressMessage("Minor Code Smell", "S101:Types should be named in PascalCase", Justification = "Patch")]
    [UsedImplicitly]
    public static class Show_Patch
    {
        [UsedImplicitly]
        public static void Prefix(ref List<string> subclasses)
        {
            //PATCH: only presents the subclass already taken if one was already selected for this class (MULTICLASS)
            var hero = Global.LevelUpHero;

            if (hero != null)
            {
                var selectedClass = LevelUpContext.GetSelectedClass(hero);

                if (selectedClass != null
                    && hero.ClassesAndSubclasses.TryGetValue(selectedClass, out var characterSubclassDefinition))
                {
                    subclasses = new List<string> { characterSubclassDefinition.Name };
                }
            }

            //PATCH: sort subclasses
            subclasses.Sort((left, right) =>
                string.Compare(
                    DatabaseHelper.GetDefinition<CharacterSubclassDefinition>(left).FormatTitle(),
                    DatabaseHelper.GetDefinition<CharacterSubclassDefinition>(right).FormatTitle(),
                    StringComparison.CurrentCultureIgnoreCase));
        }
    }
}
