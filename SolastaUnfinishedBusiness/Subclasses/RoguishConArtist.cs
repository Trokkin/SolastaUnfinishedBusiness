﻿using SolastaUnfinishedBusiness.Builders;
using SolastaUnfinishedBusiness.Builders.Features;
using static SolastaUnfinishedBusiness.Api.DatabaseHelper;
using static SolastaUnfinishedBusiness.Api.DatabaseHelper.CharacterSubclassDefinitions;
using static SolastaUnfinishedBusiness.Api.DatabaseHelper.SchoolOfMagicDefinitions;

namespace SolastaUnfinishedBusiness.Subclasses;

internal sealed class RoguishConArtist : AbstractSubclass
{
    private static FeatureDefinitionMagicAffinity _dcIncreaseAffinity;

    // ReSharper disable once InconsistentNaming
    private readonly CharacterSubclassDefinition Subclass;

    internal RoguishConArtist()
    {
        // Make Con Artist subclass
        var abilityAffinity = FeatureDefinitionAbilityCheckAffinityBuilder
            .Create("AbilityCheckAffinityConArtist")
            .SetGuiPresentation(Category.Feature)
            .BuildAndSetAffinityGroups(
                RuleDefinitions.CharacterAbilityCheckAffinity.Advantage, RuleDefinitions.DieType.D8, 0,
                (AttributeDefinitions.Dexterity, SkillDefinitions.SleightOfHand),
                (AttributeDefinitions.Charisma, SkillDefinitions.Persuasion),
                (AttributeDefinitions.Charisma, SkillDefinitions.Deception),
                (AttributeDefinitions.Charisma, SkillDefinitions.Performance))
            .AddToDB();

        var spellCasting = FeatureDefinitionCastSpellBuilder
            .Create("CastSpellConArtist")
            .SetGuiPresentation(Category.Feature)
            .SetSpellCastingOrigin(FeatureDefinitionCastSpell.CastingOrigin.Subclass)
            .SetSpellCastingAbility(AttributeDefinitions.Charisma)
            .SetSpellList(SpellListDefinitions.SpellListWizard)
            .AddRestrictedSchools(SchoolConjuration, SchoolTransmutation, SchoolEnchantment, SchoolIllusion)
            .SetSpellKnowledge(RuleDefinitions.SpellKnowledge.Selection)
            .SetSpellReadyness(RuleDefinitions.SpellReadyness.AllKnown)
            .SetSlotsRecharge(RuleDefinitions.RechargeRate.LongRest)
            .SetReplacedSpells(4, 1)
            .SetKnownCantrips(3, 3, FeatureDefinitionCastSpellBuilder.CasterProgression.ThirdCaster)
            .SetKnownSpells(4, FeatureDefinitionCastSpellBuilder.CasterProgression.ThirdCaster)
            .SetSlotsPerLevel(FeatureDefinitionCastSpellBuilder.CasterProgression.ThirdCaster);

        var feintBuilder = EffectDescriptionBuilder
            .Create()
            .SetTargetingData(
                RuleDefinitions.Side.Enemy, RuleDefinitions.RangeType.Distance, 12,
                RuleDefinitions.TargetType.Individuals, 1, 0)
            .SetDurationData(RuleDefinitions.DurationType.Round, 1, RuleDefinitions.TurnOccurenceType.EndOfTurn)
            .SetSavingThrowData(
                true, false, AttributeDefinitions.Wisdom, true,
                RuleDefinitions.EffectDifficultyClassComputation.SpellCastingFeature, AttributeDefinitions.Charisma,
                15);

        var condition = ConditionDefinitionBuilder
            .Create(ConditionDefinitions.ConditionTrueStrike, "ConditionConArtistFeint")
            .SetGuiPresentation(Category.Feature,
                ConditionDefinitions.ConditionTrueStrike.GuiPresentation.SpriteReference)
            .SetSpecialInterruptions(RuleDefinitions.ConditionInterruption.Attacked)
            .SetAdditionalDamageData(RuleDefinitions.DieType.D8, 3, ConditionDefinition.DamageQuantity.Dice, true)
            .AddToDB();

        feintBuilder.AddEffectForm(
            EffectFormBuilder
                .Create()
                .CreatedByCharacter()
                .SetConditionForm(
                    condition, ConditionForm.ConditionOperation.Add, false, false)
                .Build());

        var feint = FeatureDefinitionPowerBuilder
            .Create("PowerConArtistFeint")
            .SetGuiPresentation(Category.Feature)
            .Configure(
                0, RuleDefinitions.UsesDetermination.AbilityBonusPlusFixed, AttributeDefinitions.Charisma,
                RuleDefinitions.ActivationTime.BonusAction, 0, RuleDefinitions.RechargeRate.AtWill,
                false, false, AttributeDefinitions.Charisma, feintBuilder.Build() /* unique instance */)
            .AddToDB();

        var proficiency = FeatureDefinitionProficiencyBuilder
            .Create("ProficiencyConArtistMentalSavingThrows")
            .SetGuiPresentation(Category.Feature)
            .SetProficiencies(RuleDefinitions.ProficiencyType.SavingThrow, AttributeDefinitions.Charisma,
                AttributeDefinitions.Wisdom)
            .AddToDB();

        // add subclass to db and add subclass to rogue class
        Subclass = CharacterSubclassDefinitionBuilder
            .Create("RoguishConArtist")
            .SetGuiPresentation(Category.Subclass, DomainInsight.GuiPresentation.SpriteReference)
            .AddFeaturesAtLevel(3, abilityAffinity)
            .AddFeaturesAtLevel(3, spellCasting.AddToDB())
            .AddFeaturesAtLevel(9, feint)
            .AddFeaturesAtLevel(13, DcIncreaseAffinity)
            .AddFeaturesAtLevel(17, proficiency)
            .AddToDB();
    }

    internal override FeatureDefinitionSubclassChoice GetSubclassChoiceList()
    {
        return FeatureDefinitionSubclassChoices.SubclassChoiceRogueRoguishArchetypes;
    }

    internal override CharacterSubclassDefinition GetSubclass()
    {
        return Subclass;
    }

#if false
    [NotNull]
    private static GuiPresentation GetSpellDcPresentation()
    {
        return new GuiPresentationBuilder(
                "Feature/&MagicAffinityConArtistDCTitle",
                Gui.Format("Feature/&MagicAffinityConArtistDCDescription",
                    Main.Settings.OverrideRogueConArtistImprovedManipulationSpellDc.ToString()))
            .Build();
    }

    internal static void UpdateSpellDcBoost()
    {
        if (!DcIncreaseAffinity)
        {
            return;
        }

        DcIncreaseAffinity.saveDCModifier = Main.Settings.OverrideRogueConArtistImprovedManipulationSpellDc;
        DcIncreaseAffinity.guiPresentation = GetSpellDcPresentation();
    }
#endif

    private static FeatureDefinitionMagicAffinity DcIncreaseAffinity => _dcIncreaseAffinity ??=
        FeatureDefinitionMagicAffinityBuilder
            .Create("MagicAffinityConArtistDC")
            .SetGuiPresentation(Category.Feature)
            .SetCastingModifiers(0, RuleDefinitions.SpellParamsModifierType.None,
                3, // Main.Settings.OverrideRogueConArtistImprovedManipulationSpellDc,
                RuleDefinitions.SpellParamsModifierType.FlatValue, false, false, false)
            .AddToDB();
}
