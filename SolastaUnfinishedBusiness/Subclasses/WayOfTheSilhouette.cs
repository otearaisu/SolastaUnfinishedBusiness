﻿using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;
using SolastaUnfinishedBusiness.Api.GameExtensions;
using SolastaUnfinishedBusiness.Api.Helpers;
using SolastaUnfinishedBusiness.Behaviors;
using SolastaUnfinishedBusiness.Behaviors.Specific;
using SolastaUnfinishedBusiness.Builders;
using SolastaUnfinishedBusiness.Builders.Features;
using SolastaUnfinishedBusiness.CustomUI;
using SolastaUnfinishedBusiness.Interfaces;
using SolastaUnfinishedBusiness.Models;
using SolastaUnfinishedBusiness.Validators;
using static RuleDefinitions;
using static SolastaUnfinishedBusiness.Api.DatabaseHelper;
using static SolastaUnfinishedBusiness.Api.DatabaseHelper.FeatureDefinitionSenses;
using static SolastaUnfinishedBusiness.Api.DatabaseHelper.SpellDefinitions;
using Resources = SolastaUnfinishedBusiness.Properties.Resources;

namespace SolastaUnfinishedBusiness.Subclasses;

[UsedImplicitly]
public sealed class WayOfTheSilhouette : AbstractSubclass
{
    private const string Name = "WayOfSilhouette";

    public WayOfTheSilhouette()
    {
        // LEVEL 03

        // Silhouette Arts

        var powerDarkness = FeatureDefinitionPowerBuilder
            .Create($"Power{Name}Darkness")
            .SetGuiPresentation(Darkness.GuiPresentation)
            .SetUsesFixed(ActivationTime.Action, RechargeRate.KiPoints)
            .SetShowCasting(false)
            .SetEffectDescription(
                EffectDescriptionBuilder
                    .Create(Darkness)
                    .SetTargetingData(Side.All, RangeType.Distance, 12, TargetType.Sphere, 3)
                    .SetEffectForms()
                    .Build())
            .AddCustomSubFeatures(new MagicEffectFinishedByMeDarkness())
            .AddToDB();

        #region

        // kept for backward compatibility
        _ = FeatureDefinitionPowerBuilder
            .Create($"Power{Name}Darkvision")
            .SetGuiPresentation(Darkvision.GuiPresentation)
            .SetUsesFixed(ActivationTime.Action, RechargeRate.KiPoints, 2, 2)
            .SetEffectDescription(Darkvision.EffectDescription)
            .AddToDB();

        // kept for backward compatibility
        _ = FeatureDefinitionPowerBuilder
            .Create($"Power{Name}PassWithoutTrace")
            .SetGuiPresentation(PassWithoutTrace.GuiPresentation)
            .SetUsesFixed(ActivationTime.Action, RechargeRate.KiPoints, 2, 2)
            .SetEffectDescription(PassWithoutTrace.EffectDescription)
            .AddToDB();

        // kept for backward compatibility
        _ = FeatureDefinitionPowerBuilder
            .Create($"Power{Name}Silence")
            .SetGuiPresentation(Silence.GuiPresentation)
            .SetUsesFixed(ActivationTime.Action, RechargeRate.KiPoints, 2, 2)
            .SetEffectDescription(Silence.EffectDescription)
            .AddToDB();

        #endregion

        var featureSetWayOfSilhouetteSilhouetteArts = FeatureDefinitionFeatureSetBuilder
            .Create($"FeatureSet{Name}SilhouetteArts")
            .SetGuiPresentation(Category.Feature)
            .AddFeatureSet(
                SenseDarkvision12,
                powerDarkness)
            .AddToDB();

        #region

        // kept for backward compatibility
        _ = FeatureDefinitionLightAffinityBuilder
            .Create($"LightAffinity{Name}CloakOfSilhouettesWeak")
            .SetGuiPresentation(Category.Feature)
            .AddLightingEffectAndCondition(new FeatureDefinitionLightAffinity.LightingEffectAndCondition
            {
                lightingState = LocationDefinitions.LightingState.Unlit,
                condition = CustomConditionsContext.InvisibilityEveryRound
            })
            .AddToDB();

        #endregion

        // Strike the Vitals

        var additionalDamageStrikeTheVitals = FeatureDefinitionAdditionalDamageBuilder
            .Create($"AdditionalDamage{Name}StrikeTheVitals")
            .SetGuiPresentation(Category.Feature)
            .SetNotificationTag("StrikeTheVitals")
            .SetDamageDice(DieType.D4, 1)
            .SetRequiredProperty(RestrictedContextRequiredProperty.UnarmedOrMonkWeapon)
            .SetTriggerCondition(AdditionalDamageTriggerCondition.AdvantageOrNearbyAlly)
            .SetFrequencyLimit(FeatureLimitedUsage.OncePerTurn)
            .AddCustomSubFeatures(new ModifyAdditionalDamageFormStrikeTheVitals())
            .AddToDB();

        #region

        // kept for backward compatibility
        _ = FeatureDefinitionAdditionalDamageBuilder
            .Create($"AdditionalDamage{Name}StrikeTheVitalsD6")
            .SetGuiPresentationNoContent(true)
            .SetNotificationTag("StrikeTheVitals")
            .SetDamageDice(DieType.D6, 1)
            .SetRequiredProperty(RestrictedContextRequiredProperty.UnarmedOrMonkWeapon)
            .SetTriggerCondition(AdditionalDamageTriggerCondition.AdvantageOrNearbyAlly)
            .SetFrequencyLimit(FeatureLimitedUsage.OncePerTurn)
            .AddToDB();

        // kept for backward compatibility
        _ = FeatureDefinitionAdditionalDamageBuilder
            .Create($"AdditionalDamage{Name}StrikeTheVitalsD8")
            .SetGuiPresentationNoContent(true)
            .SetNotificationTag("StrikeTheVitals")
            .SetDamageDice(DieType.D8, 2)
            .SetRequiredProperty(RestrictedContextRequiredProperty.UnarmedOrMonkWeapon)
            .SetTriggerCondition(AdditionalDamageTriggerCondition.AdvantageOrNearbyAlly)
            .SetFrequencyLimit(FeatureLimitedUsage.OncePerTurn)
            .AddToDB();

        // kept for backward compatibility
        _ = FeatureDefinitionAdditionalDamageBuilder
            .Create($"AdditionalDamage{Name}StrikeTheVitalsD10")
            .SetGuiPresentationNoContent(true)
            .SetNotificationTag("StrikeTheVitals")
            .SetDamageDice(DieType.D10, 3)
            .SetRequiredProperty(RestrictedContextRequiredProperty.UnarmedOrMonkWeapon)
            .SetTriggerCondition(AdditionalDamageTriggerCondition.AdvantageOrNearbyAlly)
            .SetFrequencyLimit(FeatureLimitedUsage.OncePerTurn)
            .AddToDB();

        #endregion

        // LEVEL 06

        var conditionSilhouetteStep = ConditionDefinitionBuilder
            .Create($"Condition{Name}SilhouetteStep")
            .SetGuiPresentation(Category.Condition, ConditionDefinitions.ConditionHeraldOfBattle)
            .SetPossessive()
            .SetSpecialInterruptions(ConditionInterruption.Attacks, ConditionInterruption.AnyBattleTurnEnd)
            .SetFeatures(
                FeatureDefinitionCombatAffinityBuilder
                    .Create($"CombatAffinity{Name}SilhouetteStep")
                    .SetGuiPresentation($"Condition{Name}SilhouetteStep", Category.Condition, Gui.NoLocalization)
                    .SetMyAttackAdvantage(AdvantageType.Advantage)
                    .AddToDB())
            .AddToDB();

        var powerWayOfSilhouetteSilhouetteStep = FeatureDefinitionPowerBuilder
            .Create($"Power{Name}SilhouetteStep")
            .SetGuiPresentation(Category.Feature, Sprites.GetSprite(Name, Resources.PowerSilhouetteStep, 256, 128))
            .SetUsesFixed(ActivationTime.BonusAction)
            .SetEffectDescription(
                EffectDescriptionBuilder
                    .Create()
                    .SetTargetingData(Side.Ally, RangeType.Distance, 12, TargetType.Position)
                    .SetDurationData(DurationType.Round)
                    .SetEffectForms(
                        EffectFormBuilder.ConditionForm(conditionSilhouetteStep, ConditionForm.ConditionOperation.Add,
                            true, true),
                        EffectFormBuilder
                            .Create()
                            .SetMotionForm(MotionForm.MotionType.TeleportToDestination)
                            .Build())
                    .SetParticleEffectParameters(FeatureDefinitionPowers.PowerRoguishDarkweaverShadowy)
                    .Build())
            .AddCustomSubFeatures(
                new ValidatorsValidatePowerUse(ValidatorsCharacter.IsNotInBrightLight),
                new FilterTargetingPositionSilhouetteStep())
            .AddToDB();

        // LEVEL 11

        #region

        // kept for backward compatibility
        _ = FeatureDefinitionLightAffinityBuilder
            .Create($"LightAffinity{Name}CloakOfSilhouettesStrong")
            .SetGuiPresentation(Category.Feature)
            .AddLightingEffectAndCondition(new FeatureDefinitionLightAffinity.LightingEffectAndCondition
            {
                lightingState = LocationDefinitions.LightingState.Dim,
                condition = CustomConditionsContext.InvisibilityEveryRound
            })
            .AddLightingEffectAndCondition(new FeatureDefinitionLightAffinity.LightingEffectAndCondition
            {
                lightingState = LocationDefinitions.LightingState.Darkness,
                condition = CustomConditionsContext.InvisibilityEveryRound
            })
            .AddToDB();

        // kept for backward compatibility
        _ = FeatureDefinitionPowerBuilder
            .Create($"Power{Name}ImprovedSilhouetteStep")
            .SetGuiPresentation(Category.Feature, DimensionDoor)
            .SetOverriddenPower(powerWayOfSilhouetteSilhouetteStep)
            .SetUsesProficiencyBonus(ActivationTime.BonusAction, RechargeRate.ShortRest)
            .SetEffectDescription(DimensionDoor.EffectDescription)
            .SetUniqueInstance()
            .AddToDB();

        #endregion

        // Shadow Flurry

        var featureShadowFlurry = FeatureDefinitionBuilder
            .Create($"Feature{Name}ShadowFlurry")
            .SetGuiPresentation(Category.Feature)
            .AddToDB();

        featureShadowFlurry.AddCustomSubFeatures(
            new TryAlterOutcomePhysicalAttackByMeShadowFlurry(featureShadowFlurry));

        // LEVEL 17

        // Shadowy Sanctuary

        var powerWayOfSilhouetteShadowySanctuary = FeatureDefinitionPowerBuilder
            .Create(FeatureDefinitionPowers.PowerPatronTimekeeperTimeShift, $"Power{Name}ShadowySanctuary")
            .SetGuiPresentation(Category.Feature)
            .SetUsesFixed(ActivationTime.Reaction, RechargeRate.KiPoints, 3)
            .SetReactionContext(ExtraReactionContext.Custom)
            .SetEffectDescription(
                EffectDescriptionBuilder
                    .Create(FeatureDefinitionPowers.PowerPatronTimekeeperTimeShift)
                    .SetParticleEffectParameters(Banishment)
                    .Build())
            .SetShowCasting(true)
            .AddToDB();

        powerWayOfSilhouetteShadowySanctuary.AddCustomSubFeatures(
            new ValidatorsValidatePowerUse(ValidatorsCharacter.IsNotInBrightLight),
            new CustomBehaviorShadowySanctuary(powerWayOfSilhouetteShadowySanctuary));

        Subclass = CharacterSubclassDefinitionBuilder
            .Create(Name)
            .SetGuiPresentation(Category.Subclass, Sprites.GetSprite(Name, Resources.WayOfTheSilhouette, 256))
            .AddFeaturesAtLevel(3, additionalDamageStrikeTheVitals, featureSetWayOfSilhouetteSilhouetteArts)
            .AddFeaturesAtLevel(6, powerWayOfSilhouetteSilhouetteStep)
            .AddFeaturesAtLevel(11, featureShadowFlurry)
            .AddFeaturesAtLevel(17, powerWayOfSilhouetteShadowySanctuary)
            .AddToDB();
    }

    internal override CharacterClassDefinition Klass => CharacterClassDefinitions.Monk;

    internal override CharacterSubclassDefinition Subclass { get; }

    internal override FeatureDefinitionSubclassChoice SubclassChoice =>
        FeatureDefinitionSubclassChoices.SubclassChoiceMonkMonasticTraditions;

    // ReSharper disable once UnassignedGetOnlyAutoProperty
    internal override DeityDefinition DeityDefinition { get; }

    //
    // Darkness
    //

    private sealed class MagicEffectFinishedByMeDarkness : IMagicEffectFinishedByMe
    {
        public IEnumerator OnMagicEffectFinishedByMe(CharacterActionMagicEffect action, BaseDefinition baseDefinition)
        {
            var actionService = ServiceRepository.GetService<IGameLocationActionService>();

            if (actionService == null)
            {
                yield break;
            }

            var actingCharacter = action.ActingCharacter;
            var rulesetCharacter = actingCharacter.RulesetCharacter;
            var effectSpell = ServiceRepository.GetService<IRulesetImplementationService>()
                .InstantiateEffectSpell(rulesetCharacter, null, Darkness, 2, false);

            var actionParams = action.ActionParams.Clone();

            actionParams.ActionDefinition = actionService.AllActionDefinitions[ActionDefinitions.Id.CastNoCost];
            actionParams.RulesetEffect = effectSpell;

            rulesetCharacter.SpellsCastByMe.TryAdd(effectSpell);
            actionService.ExecuteAction(actionParams, null, true);
        }
    }

    //
    // Strike the Vitals
    //

    private sealed class ModifyAdditionalDamageFormStrikeTheVitals : IModifyAdditionalDamageForm
    {
        public DamageForm AdditionalDamageForm(
            GameLocationCharacter attacker,
            GameLocationCharacter defender,
            IAdditionalDamageProvider provider,
            DamageForm damageForm)
        {
            var rulesetAttacker = attacker.RulesetCharacter;
            var dieType = rulesetAttacker.GetMonkDieType();
            var levels = rulesetAttacker.GetClassLevel(CharacterClassDefinitions.Monk);
            var diceNumber = levels switch
            {
                >= 17 => 3,
                >= 11 => 2,
                _ => 1
            };

            damageForm.dieType = dieType;
            damageForm.diceNumber = diceNumber;

            return damageForm;
        }
    }

    //
    // Silhouette Step
    //

    private class FilterTargetingPositionSilhouetteStep : IFilterTargetingPosition
    {
        public IEnumerator ComputeValidPositions(CursorLocationSelectPosition cursorLocationSelectPosition)
        {
            yield return cursorLocationSelectPosition.MyComputeValidPositions(LocationDefinitions.LightingState.Bright);
        }
    }

    //
    // Shadow Flurry
    //

    private class TryAlterOutcomePhysicalAttackByMeShadowFlurry(
        // ReSharper disable once SuggestBaseTypeForParameterInConstructor
        FeatureDefinition featureShadowFlurry)
        : ITryAlterOutcomePhysicalAttackByMe
    {
        public IEnumerator OnAttackTryAlterOutcomeByMe(
            GameLocationBattleManager battle,
            CharacterAction action,
            GameLocationCharacter me,
            GameLocationCharacter target,
            ActionModifier attackModifier)
        {
            if (action.AttackRollOutcome is not (RollOutcome.Failure or RollOutcome.CriticalFailure))
            {
                yield break;
            }

            var rulesetMe = me.RulesetCharacter;

            if (rulesetMe is not { IsDeadOrDyingOrUnconscious: false })
            {
                yield break;
            }

            if (!ValidatorsCharacter.IsNotInBrightLight(rulesetMe))
            {
                yield break;
            }

            if (!me.OncePerTurnIsValid(featureShadowFlurry.Name))
            {
                yield break;
            }

            me.UsedSpecialFeatures.TryAdd(featureShadowFlurry.Name, 1);

            var attackMode = action.actionParams.attackMode;
            var totalRoll = (action.AttackRoll + attackMode.ToHitBonus).ToString();
            var rollCaption = action.AttackRoll == 1
                ? "Feedback/&RollCheckCriticalFailureTitle"
                : "Feedback/&CriticalAttackFailureOutcome";

            rulesetMe.LogCharacterUsedFeature(featureShadowFlurry,
                "Feedback/&TriggerRerollLine",
                false,
                (ConsoleStyleDuplet.ParameterType.Base, $"{action.AttackRoll}+{attackMode.ToHitBonus}"),
                (ConsoleStyleDuplet.ParameterType.FailedRoll, Gui.Format(rollCaption, totalRoll)));

            var roll = rulesetMe.RollAttack(
                attackMode.toHitBonus,
                target.RulesetCharacter,
                attackMode.sourceDefinition,
                attackModifier.attackToHitTrends,
                attackModifier.IgnoreAdvantage,
                attackModifier.AttackAdvantageTrends,
                attackMode.ranged,
                false,
                attackModifier.attackRollModifier,
                out var outcome,
                out var successDelta,
                -1,
                // testMode true avoids the roll to display on combat log as the original one will get there with altered results
                true);

            action.AttackRollOutcome = outcome;
            action.AttackSuccessDelta = successDelta;
            action.AttackRoll = roll;
        }
    }

    //
    // Shadowy Sanctuary
    //

    private class CustomBehaviorShadowySanctuary(FeatureDefinitionPower featureDefinitionPower)
        : IAttackBeforeHitConfirmedOnMe, IPreventRemoveConcentrationOnPowerUse
    {
        public IEnumerator OnAttackBeforeHitConfirmedOnMe(
            GameLocationBattleManager battle,
            GameLocationCharacter attacker,
            GameLocationCharacter me,
            ActionModifier attackModifier,
            RulesetAttackMode attackMode,
            bool rangedAttack,
            AdvantageType advantageType,
            List<EffectForm> actualEffectForms,
            RulesetEffect rulesetEffect,
            bool firstTarget,
            bool criticalHit)
        {
            if (!me.CanReact())
            {
                yield break;
            }

            var rulesetMe = me.RulesetCharacter;

            if (!rulesetMe.CanUsePower(featureDefinitionPower))
            {
                yield break;
            }

            var rulesetEnemy = attacker.RulesetCharacter;

            if (rulesetEnemy is not { IsDeadOrDyingOrUnconscious: false })
            {
                yield break;
            }

            var gameLocationActionManager =
                ServiceRepository.GetService<IGameLocationActionService>() as GameLocationActionManager;

            if (gameLocationActionManager == null)
            {
                yield break;
            }

            var usablePower = PowerProvider.Get(featureDefinitionPower, rulesetMe);
            var reactionParams =
                new CharacterActionParams(me, (ActionDefinitions.Id)ExtraActionId.DoNothingReaction)
                {
                    StringParameter = "ShadowySanctuary", UsablePower = usablePower
                };

            var previousReactionCount = gameLocationActionManager.PendingReactionRequestGroups.Count;
            var reactionRequest = new ReactionRequestSpendPower(reactionParams);

            gameLocationActionManager.AddInterruptRequest(reactionRequest);

            yield return battle.WaitForReactions(me, gameLocationActionManager, previousReactionCount);

            if (!reactionParams.ReactionValidated)
            {
                yield break;
            }

            // remove any negative effect
            actualEffectForms.Clear();

            rulesetMe.UpdateUsageForPower(featureDefinitionPower, featureDefinitionPower.CostPerUse);

            var implementationManagerService =
                ServiceRepository.GetService<IRulesetImplementationService>() as RulesetImplementationManager;
            //CHECK: must be power no cost
            var actionParams = new CharacterActionParams(me, ActionDefinitions.Id.PowerNoCost)
            {
                ActionModifiers = { new ActionModifier() },
                RulesetEffect = implementationManagerService
                    //CHECK: no need for AddAsActivePowerToSource
                    .MyInstantiateEffectPower(rulesetMe, usablePower, false),
                UsablePower = usablePower,
                TargetCharacters = { me }
            };

            EffectHelpers.StartVisualEffect(me, attacker,
                FeatureDefinitionPowers.PowerGlabrezuGeneralShadowEscape_at_will, EffectHelpers.EffectType.Caster);
            ServiceRepository.GetService<ICommandService>()?
                .ExecuteAction(actionParams, null, false);
        }
    }
}
