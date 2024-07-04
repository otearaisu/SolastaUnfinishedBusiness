﻿using System.Collections;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using SolastaUnfinishedBusiness.Api.GameExtensions;
using SolastaUnfinishedBusiness.Builders;
using SolastaUnfinishedBusiness.Builders.Features;
using SolastaUnfinishedBusiness.CustomUI;
using SolastaUnfinishedBusiness.Feats;
using SolastaUnfinishedBusiness.Interfaces;
using SolastaUnfinishedBusiness.Models;
using SolastaUnfinishedBusiness.Properties;
using SolastaUnfinishedBusiness.Validators;
using static RuleDefinitions;
using static FeatureDefinitionAttributeModifier;
using static SolastaUnfinishedBusiness.Api.DatabaseHelper;

namespace SolastaUnfinishedBusiness.Subclasses;

[UsedImplicitly]
public sealed class RoguishDuelist : AbstractSubclass
{
    internal const string Name = "RoguishDuelist";
    internal const string ConditionReflexiveParryName = $"Condition{Name}ReflexiveParry";
    private const string SureFooted = "SureFooted";

    public RoguishDuelist()
    {
        // LEVEL 03

        // Daring Duel

        var conditionDaringDuel = ConditionDefinitionBuilder
            .Create($"Condition{Name}DaringDuel")
            .SetGuiPresentationNoContent(true)
            .SetSilent(Silent.WhenAddedOrRemoved)
            .SetSpecialDuration(DurationType.Round, 0, TurnOccurenceType.StartOfTurn)
            .AddToDB();

        var additionalDamageDaringDuel = FeatureDefinitionAdditionalDamageBuilder
            .Create($"AdditionalDamage{Name}DaringDuel")
            .SetGuiPresentation(Category.Feature)
            .SetNotificationTag(TagsDefinitions.AdditionalDamageSneakAttackTag)
            .SetDamageDice(DieType.D6, 1)
            .SetAdvancement(AdditionalDamageAdvancement.ClassLevel, 1, 1, 2)
            .SetTriggerCondition(ExtraAdditionalDamageTriggerCondition.TargetIsDuelingWithYou)
            .SetRequiredProperty(RestrictedContextRequiredProperty.FinesseOrRangeWeapon)
            .SetFrequencyLimit(FeatureLimitedUsage.OncePerTurn)
            .AddConditionOperation(ConditionOperationDescription.ConditionOperation.Add, conditionDaringDuel)
            .AddToDB();

        additionalDamageDaringDuel.AddCustomSubFeatures(
            ModifyAdditionalDamageClassLevelRogue.Instance,
            new ClassFeats.ModifyAdditionalDamageCloseQuarters(additionalDamageDaringDuel));

        // Riposte

        var actionAffinitySwirlingDance = FeatureDefinitionActionAffinityBuilder
            .Create($"ActionAffinity{Name}SwirlingDance")
            .SetGuiPresentation(Category.Feature)
            .SetAuthorizedActions(ActionDefinitions.Id.SwirlingDance)
            .AddToDB();

        // LEVEL 09

        // Bravado

        var attributeModifierSureFooted = FeatureDefinitionAttributeModifierBuilder
            .Create($"AttributeModifier{Name}{SureFooted}")
            .SetGuiPresentation($"FeatureSet{Name}{SureFooted}", Category.Feature)
            .SetModifier(AttributeModifierOperation.AddConditionAmount, AttributeDefinitions.ArmorClass)
            .AddToDB();

        var conditionSureFooted = ConditionDefinitionBuilder
            .Create($"Condition{Name}{SureFooted}")
            .SetGuiPresentationNoContent(true)
            .SetSilent(Silent.WhenAddedOrRemoved)
            .SetFixedAmount(1)
            .SetFeatures(attributeModifierSureFooted)
            .AddToDB();

        var featureSureFooted = FeatureDefinitionBuilder
            .Create($"FeatureSet{Name}{SureFooted}")
            .SetGuiPresentation(Category.Feature)
            .AddCustomSubFeatures(new CustomBehaviorSureFooted(conditionSureFooted))
            .AddToDB();

        // LEVEL 13

        // Reflexive Parry

        var conditionReflexiveParty = ConditionDefinitionBuilder
            .Create($"Condition{Name}ReflexiveParty")
            .SetGuiPresentationNoContent(true)
            .SetSilent(Silent.WhenAddedOrRemoved)
            .SetSpecialInterruptions(ConditionInterruption.AnyBattleTurnEnd)
            .AddToDB();

        var featureReflexiveParry = FeatureDefinitionBuilder
            .Create($"Feature{Name}ReflexiveParry")
            .SetGuiPresentation(Category.Feature)
            .AddToDB();

        featureReflexiveParry.AddCustomSubFeatures(
            new CustomBehaviorReflexiveParry(featureReflexiveParry, conditionReflexiveParty));

        // LEVEL 17

        // Master Duelist

        var featureMasterDuelist = FeatureDefinitionBuilder
            .Create($"Feature{Name}MasterDuelist")
            .SetGuiPresentationNoContent(true)
            .AddCustomSubFeatures(new PhysicalAttackFinishedByMeMasterDuelist(conditionDaringDuel))
            .AddToDB();

        var featureSetMasterDuelist = FeatureDefinitionFeatureSetBuilder
            .Create($"FeatureSet{Name}MasterDuelist")
            .SetGuiPresentation(Category.Feature)
            .AddFeatureSet(featureMasterDuelist)
            .AddToDB();

        // MAIN

        Subclass = CharacterSubclassDefinitionBuilder
            .Create(Name)
            .SetGuiPresentation(Category.Subclass, Sprites.GetSprite(Name, Resources.RoguishDuelist, 256))
            .AddFeaturesAtLevel(3, additionalDamageDaringDuel, actionAffinitySwirlingDance)
            .AddFeaturesAtLevel(9, featureSureFooted)
            .AddFeaturesAtLevel(13, featureReflexiveParry)
            .AddFeaturesAtLevel(17, featureSetMasterDuelist)
            .AddToDB();
    }

    internal override CharacterClassDefinition Klass => CharacterClassDefinitions.Rogue;

    internal override CharacterSubclassDefinition Subclass { get; }

    internal override FeatureDefinitionSubclassChoice SubclassChoice =>
        FeatureDefinitionSubclassChoices.SubclassChoiceRogueRoguishArchetypes;

    // ReSharper disable once UnassignedGetOnlyAutoProperty
    internal override DeityDefinition DeityDefinition { get; }

    internal static bool TargetIsDuelingWithRoguishDuelist(
        GameLocationCharacter attacker,
        GameLocationCharacter defender,
        AdvantageType advantageType)
    {
        return
            advantageType != AdvantageType.Disadvantage &&
            attacker.RulesetCharacter.GetSubclassLevel(CharacterClassDefinitions.Rogue, Name) > 0 &&
            attacker.IsWithinRange(defender, 1) &&
            Gui.Battle.AllContenders
                .Where(x => x != attacker && x != defender)
                .All(x => !attacker.IsWithinRange(x, 1));
    }

    //
    // Reflexive Parry
    //

    private sealed class CustomBehaviorReflexiveParry(
        FeatureDefinition featureReflexiveParry,
        ConditionDefinition conditionReflexiveParty) : IPhysicalAttackBeforeHitConfirmedOnMe
    {
        public IEnumerator OnPhysicalAttackBeforeHitConfirmedOnMe(
            GameLocationBattleManager battleManager,
            GameLocationCharacter attacker,
            GameLocationCharacter defender,
            ActionModifier actionModifier,
            RulesetAttackMode attackMode,
            bool rangedAttack,
            AdvantageType advantageType,
            List<EffectForm> actualEffectForms,
            bool firstTarget,
            bool criticalHit)
        {
            var rulesetDefender = defender.RulesetCharacter;

            if (!ValidatorsWeapon.IsMelee(attackMode) ||
                rulesetDefender.HasAnyConditionOfTypeOrSubType(
                    conditionReflexiveParty.Name,
                    ConditionDefinitions.ConditionDazzled.Name,
                    ConditionDefinitions.ConditionIncapacitated.Name,
                    ConditionDefinitions.ConditionShocked.Name,
                    ConditionDefinitions.ConditionSlowed.Name))
            {
                yield break;
            }

            actionModifier.DefenderDamageMultiplier *= 0.5f;
            rulesetDefender.DamageHalved(rulesetDefender, featureReflexiveParry);
            rulesetDefender.InflictCondition(
                conditionReflexiveParty.Name,
                DurationType.Round,
                0,
                TurnOccurenceType.StartOfTurn,
                AttributeDefinitions.TagEffect,
                rulesetDefender.guid,
                rulesetDefender.CurrentFaction.Name,
                1,
                conditionReflexiveParty.Name,
                0,
                0,
                0);
        }
    }

    //
    // Master Duelist
    //

    private sealed class PhysicalAttackFinishedByMeMasterDuelist(
        // ReSharper disable once SuggestBaseTypeForParameterInConstructor
        ConditionDefinition conditionDaringDuel) : IPhysicalAttackFinishedByMe
    {
        public IEnumerator OnPhysicalAttackFinishedByMe(
            GameLocationBattleManager battleManager,
            CharacterAction action,
            GameLocationCharacter attacker,
            GameLocationCharacter defender,
            RulesetAttackMode attackMode,
            RollOutcome rollOutcome,
            int damageAmount)
        {
            var rulesetDefender = defender.RulesetActor;

            if (rulesetDefender is not { IsDeadOrDyingOrUnconscious: false })
            {
                yield break;
            }

            if (!rulesetDefender.TryGetConditionOfCategoryAndType(
                    AttributeDefinitions.TagEffect, conditionDaringDuel.Name, out var activeCondition))
            {
                yield break;
            }

            rulesetDefender.RemoveCondition(activeCondition);

            var actionService = ServiceRepository.GetService<IGameLocationActionService>();
            var actionParams = action.ActionParams.Clone();
            var attackModeMain = attacker.FindActionAttackMode(ActionDefinitions.Id.AttackMain);

            actionParams.ActionDefinition = actionService.AllActionDefinitions[ActionDefinitions.Id.AttackFree];
            actionParams.AttackMode = attackModeMain;

            ServiceRepository.GetService<IGameLocationActionService>()?
                .ExecuteAction(actionParams, null, true);
        }
    }

    //
    // Sure Footed
    //

    private sealed class CustomBehaviorSureFooted(ConditionDefinition conditionSureFooted)
        : IPhysicalAttackInitiatedByMe, IPhysicalAttackFinishedByMe
    {
        public IEnumerator OnPhysicalAttackFinishedByMe(
            GameLocationBattleManager battleManager,
            CharacterAction action,
            GameLocationCharacter attacker,
            GameLocationCharacter defender,
            RulesetAttackMode attackMode,
            RollOutcome rollOutcome,
            int damageAmount)
        {
            if (attacker.UsedSpecialFeatures.ContainsKey("SureFooted"))
            {
                yield break;
            }

            var rulesetAttacker = attacker.RulesetCharacter;
            var roll = rulesetAttacker.RollDie(DieType.D6, RollContext.None, false, AdvantageType.None, out _, out _);

            if (!rulesetAttacker.TryGetConditionOfCategoryAndType(
                    AttributeDefinitions.TagEffect, conditionSureFooted.Name, out var activeCondition) ||
                roll > activeCondition.Amount)
            {
                rulesetAttacker.InflictCondition(
                    conditionSureFooted.Name,
                    DurationType.Round,
                    0,
                    TurnOccurenceType.StartOfTurn,
                    AttributeDefinitions.TagEffect,
                    rulesetAttacker.guid,
                    rulesetAttacker.CurrentFaction.Name,
                    1,
                    conditionSureFooted.Name,
                    roll,
                    0,
                    0);
            }
        }

        public IEnumerator OnPhysicalAttackInitiatedByMe(
            GameLocationBattleManager battleManager,
            CharacterAction action,
            GameLocationCharacter attacker,
            GameLocationCharacter defender,
            ActionModifier attackModifier,
            RulesetAttackMode attackMode)
        {
            var isSneakAttackValid = CharacterContext.IsSneakAttackValid(attackModifier, attacker, defender);

            if (isSneakAttackValid)
            {
                attacker.UsedSpecialFeatures.TryAdd("SureFooted", 0);
            }
            else
            {
                attacker.UsedSpecialFeatures.Remove("SureFooted");
            }

            yield break;
        }
    }
}
