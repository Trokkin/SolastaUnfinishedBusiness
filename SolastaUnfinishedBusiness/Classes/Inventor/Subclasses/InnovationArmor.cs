﻿using System.Collections.Generic;
using SolastaUnfinishedBusiness.Builders;
using SolastaUnfinishedBusiness.Builders.Features;
using SolastaUnfinishedBusiness.CustomBehaviors;
using SolastaUnfinishedBusiness.Models;
using SolastaUnfinishedBusiness.Utils;
using static FeatureDefinitionAttributeModifier;
using static RuleDefinitions;
using static SolastaUnfinishedBusiness.Api.DatabaseHelper;
using static SolastaUnfinishedBusiness.Api.DatabaseHelper.SpellDefinitions;

namespace SolastaUnfinishedBusiness.Classes.Inventor.Subclasses;

public static class InnovationArmor
{
    private const string GuardianMarkerName = "ConditionInnovationArmorGuardianMode";
    private const string InfiltratorMarkerName = "ConditionInnovationArmorInfilratorMode";

    public static CharacterSubclassDefinition Build()
    {
        return CharacterSubclassDefinitionBuilder
            .Create("InnovationArmor")
            .SetGuiPresentation(Category.Subclass, FightingStyleDefinitions.Defense)
            .AddFeaturesAtLevel(3, BuildArmoredUp(), BuildAutoPreparedSpells(), BuildArmorModes())
            .AddFeaturesAtLevel(5, BuildExtraAttack())
            .AddToDB();
    }

    private static FeatureDefinition BuildArmoredUp()
    {
        var proficiency = FeatureDefinitionProficiencyBuilder
            .Create("ProficiencyInnovationArmorArmoredUp")
            .SetGuiPresentationNoContent()
            .SetProficiencies(ProficiencyType.Armor, EquipmentDefinitions.HeavyArmorCategory)
            .AddToDB();

        var heavyImmunity = FeatureDefinitionMovementAffinityBuilder
            .Create("MovementAffinityInnovationArmorArmoredUp")
            .SetGuiPresentationNoContent()
            .SetImmunities(heavyArmorImmunity: true)
            .AddToDB();

        return FeatureDefinitionFeatureSetBuilder
            .Create("FeatureSetInnovationArmorArmoredUp")
            .SetGuiPresentation(Category.Feature)
            .AddFeatureSet(proficiency, heavyImmunity)
            .AddToDB();
    }

    private static FeatureDefinition BuildAutoPreparedSpells()
    {
        return FeatureDefinitionAutoPreparedSpellsBuilder
            .Create("AutoPreparedSpellsInnovationArmor")
            .SetGuiPresentation(Category.Feature)
            .SetSpellcastingClass(InventorClass.Class)
            .SetAutoTag("InventorArmorer")
            .AddPreparedSpellGroup(3, MagicMissile, Shield)
            .AddPreparedSpellGroup(5, MirrorImage, Shatter)
            .AddPreparedSpellGroup(9, HypnoticPattern, LightningBolt)
            .AddPreparedSpellGroup(13, FireShield, GreaterInvisibility)
            //TODO: find (or make) replacement for Cloud Kill - supposed to be Wall of Force
            .AddPreparedSpellGroup(17, SpellsContext.FarStep, CloudKill)
            .AddToDB();
    }

    private static FeatureDefinition BuildArmorModes()
    {
        var pool = FeatureDefinitionPowerBuilder
            .Create("PowerInnovationArmorModeSelectorPool")
            .SetGuiPresentation(Category.Feature, hidden: true)
            .SetCustomSubFeatures(new CanUseAttributeForWeapon(AttributeDefinitions.Intelligence, IsBuiltInWeapon))
            .SetUsesFixed(ActivationTime.BonusAction, RechargeRate.ShortRest, 1, 1)
            .AddToDB();

        var guardianMarker = ConditionDefinitionBuilder
            .Create(GuardianMarkerName)
            .SetGuiPresentation(Category.Condition, CustomSprite.ConditionGuardian)
            .SetSilent(Silent.WhenRemoved)
            .SetFeatures(
                FeatureDefinitionBuilder
                    .Create("FeatureInnovationArmorGuardianMode")
                    .SetGuiPresentation(GuardianMarkerName, Category.Condition)
                    .AddToDB())
            .AddToDB();

        var infiltratorMarker = ConditionDefinitionBuilder
            .Create(InfiltratorMarkerName)
            .SetGuiPresentation(Category.Condition, CustomSprite.ConditionInfiltrate)
            .SetSilent(Silent.WhenRemoved)
            .SetFeatures(
                FeatureDefinitionBuilder
                    .Create("FeatureInnovationArmorInfilratorMode")
                    .SetGuiPresentation(InfiltratorMarkerName, Category.Condition)
                    .AddToDB(),
                FeatureDefinitionMovementAffinityBuilder
                    .Create("MovementAffinityInnivationArmorInfiltratorMode")
                    .SetGuiPresentationNoContent()
                    .SetBaseSpeedAdditiveModifier(1)
                    .AddToDB(),
                FeatureDefinitionAbilityCheckAffinityBuilder
                    .Create("AbilityCheckAffinityInnivationArmorInfiltratorMode")
                    .SetGuiPresentationNoContent()
                    .BuildAndSetAffinityGroups(CharacterAbilityCheckAffinity.Advantage,
                        abilityProficiencyPairs: (AttributeDefinitions.Dexterity, SkillDefinitions.Stealth))
                    .AddToDB()
            )
            .AddToDB();

        var guardianMode = FeatureDefinitionPowerSharedPoolBuilder
            .Create("PowerInnovationArmorSwitchModeGuardian")
            .SetGuiPresentation(Category.Feature, CustomSprite.PowerGuardianMode)
            .SetCustomSubFeatures(
                new ValidatorsPowerUse(NotGuardianMode),
                ValidatorsPowerUse.NotInCombat,
                new AddGauntletAttack()
            )
            .SetSharedPool(ActivationTime.BonusAction, pool)
            .SetEffectDescription(EffectDescriptionBuilder.Create()
                .SetTargetingData(Side.Ally, RangeType.Self, 0, TargetType.Self)
                .SetDurationData(DurationType.Permanent)
                .SetEffectForms(
                    EffectFormBuilder.Create()
                        .SetConditionForm(infiltratorMarker, ConditionForm.ConditionOperation.Remove, true, false)
                        .Build(),
                    EffectFormBuilder.Create()
                        .SetConditionForm(guardianMarker, ConditionForm.ConditionOperation.Add, true, false)
                        .Build()
                )
                .Build())
            .AddToDB();

        var infiltratorMode = FeatureDefinitionPowerSharedPoolBuilder
            .Create("PowerInnovationArmorSwitchModeInfiltrator")
            .SetGuiPresentation(Category.Feature, CustomSprite.PowerInfiltratorMode)
            .SetCustomSubFeatures(
                new ValidatorsPowerUse(NotInfiltratorMode),
                ValidatorsPowerUse.NotInCombat,
                new AddLauncherAttack()
            )
            .SetSharedPool(ActivationTime.BonusAction, pool)
            .SetEffectDescription(EffectDescriptionBuilder.Create()
                .SetTargetingData(Side.Ally, RangeType.Self, 0, TargetType.Self)
                .SetDurationData(DurationType.Permanent)
                .SetEffectForms(
                    EffectFormBuilder.Create()
                        .SetConditionForm(guardianMarker, ConditionForm.ConditionOperation.Remove, true, false)
                        .Build(),
                    EffectFormBuilder.Create()
                        .SetConditionForm(infiltratorMarker, ConditionForm.ConditionOperation.Add, true, false)
                        .Build())
                .Build())
            .AddToDB();

        var defensiveField = FeatureDefinitionPowerBuilder
            .Create("PowerInnovationArmorDefensiveField")
            .SetGuiPresentation(Category.Feature, CustomSprite.PowerDefensiveField)
            .SetCustomSubFeatures(new ValidatorsPowerUse(InGuardianMode), InventorClassHolder.Marker)
            .SetUsesProficiencyBonus(ActivationTime.BonusAction, RechargeRate.LongRest)
            .SetEffectDescription(EffectDescriptionBuilder.Create()
                .SetDurationData(DurationType.UntilLongRest)
                .SetTargetingData(Side.Ally, RangeType.Self, 0, TargetType.Self)
                .SetEffectForms(EffectFormBuilder.Create()
                    .SetTempHpForm(1)
                    .SetLevelAdvancement(EffectForm.LevelApplianceType.MultiplyBonus, LevelSourceType.ClassLevel)
                    .Build())
                .Build())
            .AddToDB();

        return FeatureDefinitionFeatureSetBuilder
            .Create("FeatureSetInnovationArmorModes")
            .SetGuiPresentation(Category.Feature)
            .AddFeatureSet(pool, guardianMode, infiltratorMode, defensiveField)
            .AddToDB();
    }

    private static FeatureDefinition BuildExtraAttack()
    {
        return FeatureDefinitionAttributeModifierBuilder
            .Create("ProficiencyInnovationArmorExtraAttack")
            .SetGuiPresentation(Category.Feature)
            .SetModifier(AttributeModifierOperation.ForceIfBetter, AttributeDefinitions.AttacksNumber, 2)
            .AddToDB();
    }

    private static bool InGuardianMode(RulesetCharacter character)
    {
        return character.HasConditionOfType(GuardianMarkerName);
    }

    private static bool NotGuardianMode(RulesetCharacter character)
    {
        return !character.HasConditionOfType(GuardianMarkerName);
    }

    private static bool InInfiltratorMode(RulesetCharacter character)
    {
        return character.HasConditionOfType(InfiltratorMarkerName);
    }

    private static bool NotInfiltratorMode(RulesetCharacter character)
    {
        return !character.HasConditionOfType(InfiltratorMarkerName);
    }

    private static bool IsBuiltInWeapon(RulesetAttackMode mode, RulesetItem weapon, RulesetCharacter character)
    {
        var item = mode?.sourceDefinition as ItemDefinition;
        return item == CustomWeaponsContext.ThunderGauntlet || item == CustomWeaponsContext.LightningLauncher;
    }

    private class AddGauntletAttack : AddExtraAttackBase
    {
        public AddGauntletAttack() : base(ActionDefinitions.ActionType.Main, false, InGuardianMode,
            ValidatorsCharacter.HasFreeHand)
        {
        }

        protected override List<RulesetAttackMode> GetAttackModes(RulesetCharacterHero hero)
        {
            var strikeDefinition = CustomWeaponsContext.ThunderGauntlet;

            var attackModifiers = hero.attackModifiers;

            var attackMode = hero.RefreshAttackMode(
                ActionType,
                strikeDefinition,
                strikeDefinition.WeaponDescription,
                ValidatorsCharacter.IsFreeOffhandForUnarmedTa(hero),
                true,
                EquipmentDefinitions.SlotTypeMainHand,
                attackModifiers,
                hero.FeaturesOrigin,
                null
            );

            //TODO: count weapon infusions from armor for to hit/to damage bonuses

            return new List<RulesetAttackMode> {attackMode};
        }
    }


    private class AddLauncherAttack : AddExtraAttackBase
    {
        public AddLauncherAttack() : base(ActionDefinitions.ActionType.Main, false, InInfiltratorMode)
        {
        }

        protected override List<RulesetAttackMode> GetAttackModes(RulesetCharacterHero hero)
        {
            var strikeDefinition = CustomWeaponsContext.LightningLauncher;

            var attackModifiers = hero.attackModifiers;

            var attackMode = hero.RefreshAttackMode(
                ActionType,
                strikeDefinition,
                strikeDefinition.WeaponDescription,
                ValidatorsCharacter.IsFreeOffhandForUnarmedTa(hero),
                true,
                EquipmentDefinitions.SlotTypeMainHand,
                attackModifiers,
                hero.FeaturesOrigin,
                null
            );

            //TODO: count weapon infusions from armor for to hit/to damage bonuses

            return new List<RulesetAttackMode> {attackMode};
        }
    }
}
