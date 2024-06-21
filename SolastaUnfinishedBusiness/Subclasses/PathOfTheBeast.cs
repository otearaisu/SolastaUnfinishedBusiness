﻿using System.Collections;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using SolastaUnfinishedBusiness.Api.GameExtensions;
using SolastaUnfinishedBusiness.Api.Helpers;
using SolastaUnfinishedBusiness.Api.LanguageExtensions;
using SolastaUnfinishedBusiness.Behaviors;
using SolastaUnfinishedBusiness.Behaviors.Specific;
using SolastaUnfinishedBusiness.Builders;
using SolastaUnfinishedBusiness.Builders.Features;
using SolastaUnfinishedBusiness.CustomUI;
using SolastaUnfinishedBusiness.Interfaces;
using SolastaUnfinishedBusiness.Models;
using SolastaUnfinishedBusiness.Properties;
using SolastaUnfinishedBusiness.Validators;
using static FeatureDefinitionAttributeModifier;
using static RuleDefinitions;
using static RulesetImplementationDefinitions;
using static SolastaUnfinishedBusiness.Api.DatabaseHelper;

namespace SolastaUnfinishedBusiness.Subclasses;

[UsedImplicitly]
public sealed class PathOfTheBeast : AbstractSubclass
{
    private const string Name = "PathOfTheBeast";
    private const string TagBeastWeapon = "BeastWeapon";
    private const string TagBeastClawAttack = "BeastClawAttack";
    private const string TagBeastTailArmorClass = "BeastTailArmorClass";
    private static ItemDefinition _beastClaws;
    private static ItemDefinition _beastTail;
    private static ItemDefinition _beastBite;

    public PathOfTheBeast()
    {
        BuildBeastClaws();
        BuildBeastTail();
        BuildBeastBite();

        var powerFormOfTheBeast = BuildPowerFormOfTheBeast();
        var powerBestialSoul = BuildPowerBestialSoul();
        var featureInfectiousFury = BuildFeatureInfectiousFury();
        var featureCallTheHunt = BuildFeatureCallTheHunt();

        Subclass = CharacterSubclassDefinitionBuilder
            .Create(Name)
            .SetGuiPresentation(Category.Subclass, Sprites.GetSprite("PathOfTheBeast", Resources.PathOfTheBeast, 256))
            .AddFeaturesAtLevel(3, powerFormOfTheBeast)
            .AddFeaturesAtLevel(6, powerBestialSoul)
            .AddFeaturesAtLevel(10, featureInfectiousFury)
            .AddFeaturesAtLevel(14, featureCallTheHunt)
            .AddToDB();
    }


    internal override CharacterSubclassDefinition Subclass { get; }

    internal override CharacterClassDefinition Klass => CharacterClassDefinitions.Barbarian;

    internal override FeatureDefinitionSubclassChoice SubclassChoice =>
        FeatureDefinitionSubclassChoices.SubclassChoiceBarbarianPrimalPath;

    // ReSharper disable once UnassignedGetOnlyAutoProperty
    internal override DeityDefinition DeityDefinition { get; }

    private static void BuildBeastBite()
    {
        var baseItem = ItemDefinitions.UnarmedStrikeBase;
        var basePresentation = baseItem.ItemPresentation;
        var baseDescription = new WeaponDescription(baseItem.WeaponDescription);
        var damageForm = baseDescription.EffectDescription.FindFirstDamageForm();

        damageForm.dieType = DieType.D8;
        damageForm.diceNumber = 1;
        damageForm.damageType = DamageTypePiercing;

        _beastBite = CustomWeaponsContext.BuildWeapon(
            "CEBeastBite", baseItem, 0, true, ItemRarity.Common, basePresentation, baseDescription,
            Sprites.GetSprite("BeastBite", Resources.BeastBite, 128));
        _beastBite.itemTags = [TagBeastWeapon];
    }

    private static void BuildBeastTail()
    {
        var baseItem = ItemDefinitions.UnarmedStrikeBase;
        var basePresentation = baseItem.ItemPresentation;
        var baseDescription = new WeaponDescription(baseItem.WeaponDescription)
        {
            reachRange = 2,
            weaponTags =
            [
                TagsDefinitions.WeaponTagReach,
                TagsDefinitions.WeaponTagMelee
            ]
        };
        var damageForm = baseDescription.EffectDescription.FindFirstDamageForm();

        damageForm.dieType = DieType.D8;
        damageForm.diceNumber = 1;
        damageForm.damageType = DamageTypePiercing;

        _beastTail = CustomWeaponsContext.BuildWeapon(
            "CEBeastTail", baseItem, 0, true, ItemRarity.Common, basePresentation, baseDescription,
            Sprites.GetSprite("BeastTail", Resources.BeastTail, 128));
        _beastTail.itemTags = [TagBeastWeapon];
    }

    private static void BuildBeastClaws()
    {
        var baseItem = ItemDefinitions.UnarmedStrikeBase;
        var basePresentation = baseItem.ItemPresentation;
        var baseDescription = new WeaponDescription(baseItem.WeaponDescription);
        var damageForm = baseDescription.EffectDescription.FindFirstDamageForm();

        damageForm.dieType = DieType.D6;
        damageForm.diceNumber = 1;
        damageForm.damageType = DamageTypeSlashing;

        _beastClaws = CustomWeaponsContext.BuildWeapon(
            "CEBeastClaws", baseItem, 0, true, ItemRarity.Common, basePresentation, baseDescription,
            Sprites.GetSprite("UnarmedStrikeClaws", Resources.UnarmedStrikeClaws, 128));
        _beastClaws.itemTags = [TagBeastWeapon];
    }

    #region Call the Hunt

    private static FeatureDefinitionPower BuildFeatureCallTheHunt()
    {
        var additionalDamageCallTheHuntBonus = FeatureDefinitionAdditionalDamageBuilder
            .Create($"Power{Name}CallTheHuntBonus")
            .SetGuiPresentationNoContent(true)
            .SetNotificationTag("CallTheHunt")
            .SetFrequencyLimit(FeatureLimitedUsage.OncePerTurn)
            .SetTriggerCondition(AdditionalDamageTriggerCondition.AlwaysActive)
            .SetRequiredProperty(RestrictedContextRequiredProperty.None)
            .SetAdditionalDamageType(AdditionalDamageType.SameAsBaseDamage)
            .SetDamageDice(DieType.D6, 1)
            .AddToDB();

        var conditionCallTheHunt = ConditionDefinitionBuilder
            .Create($"Condition{Name}CallTheHunt")
            .SetGuiPresentation(Category.Condition, ConditionDefinitions.ConditionBlessed)
            .SetPossessive()
            .SetConditionParticleReference(ConditionDefinitions.ConditionHolyAura)
            .SetFeatures(additionalDamageCallTheHuntBonus)
            .SetSpecialInterruptions(ExtraConditionInterruption.SourceRageStop)
            .AddToDB();

        var powerCallTheHunt = FeatureDefinitionPowerBuilder
            .Create($"Power{Name}CallTheHunt")
            .SetGuiPresentation(Category.Feature)
            .SetUsesProficiencyBonus(ActivationTime.OnPowerActivatedAuto)
            .SetEffectDescription(
                EffectDescriptionBuilder.Create()
                    .SetDurationData(DurationType.Minute, 1)
                    .SetTargetingData(Side.Ally, RangeType.Self, 0, TargetType.Self)
                    .AddEffectForms(EffectFormBuilder.ConditionForm(conditionCallTheHunt))
                    .Build())
            .AddToDB();
        // need to handle custom because OnRageStartChoice doesn't seem to affect allies
        powerCallTheHunt.AddCustomSubFeatures(new PowerCallTheHuntHandler(powerCallTheHunt));
        return powerCallTheHunt;
    }

    #endregion

    #region Form of the Beast

    private static FeatureDefinition[] BuildPowerFormOfTheBeast()
    {
        var powerFormOfTheBeast = FeatureDefinitionPowerBuilder
            .Create($"Power{Name}FormOfTheBeast")
            .SetGuiPresentation(Category.Feature)
            .SetUsesFixed(ActivationTime.OnPowerActivatedAuto)
            .AddToDB();

        string[] suffixes = ["Bite", "Claws", "Tail"];

        object[] subFeatures = [new BeastBiteHandler(), new BeastClawsHandler(), new BeastTailHandler()];
        var powers = new List<FeatureDefinitionPower>();

        for (var i = 0; i < suffixes.Length; i++)
        {
            var condition = ConditionDefinitionBuilder
                .Create($"Condition{Name}FormOfTheBeast{suffixes[i]}")
                .SetGuiPresentation($"Feature/&Power{Name}FormOfTheBeast{suffixes[i]}Title",
                    $"Feature/&Power{Name}FormOfTheBeast{suffixes[i]}Description",
                    ConditionDefinitions.ConditionBlessed)
                .SetSpecialInterruptions((ConditionInterruption)ExtraConditionInterruption.SourceRageStop,
                    ConditionInterruption.BattleEnd)
                .SetPossessive()
                .AddCustomSubFeatures(subFeatures[i])
                .AddToDB();

            var power = FeatureDefinitionPowerSharedPoolBuilder
                .Create($"Power{Name}FormOfTheBeast{suffixes[i]}")
                .SetGuiPresentation(Category.Feature)
                .SetSharedPool(ActivationTime.OnPowerActivatedAuto, powerFormOfTheBeast)
                .SetEffectDescription(
                    EffectDescriptionBuilder
                        .Create()
                        .SetTargetingData(Side.All, RangeType.Self, 0, TargetType.Self)
                        .SetDurationData(DurationType.UntilLongRest)
                        .SetEffectForms(EffectFormBuilder.ConditionForm(condition))
                        .Build())
                .AddToDB();

            powers.Add(power);
        }

        PowerBundle.RegisterPowerBundle(powerFormOfTheBeast, false, powers);
        powerFormOfTheBeast.AddCustomSubFeatures(new ActionFinishedByMeBeastForm(powerFormOfTheBeast));

        return [powerFormOfTheBeast, .. powers];
    }


    private class ActionFinishedByMeBeastForm(FeatureDefinitionPower powerPool) : IActionFinishedByMe
    {
        public IEnumerator OnActionFinishedByMe(CharacterAction characterAction)
        {
            if (characterAction is not CharacterActionCombatRageStart)
            {
                yield break;
            }

            var actionManager =
                ServiceRepository.GetService<IGameLocationActionService>() as GameLocationActionManager;
            var battleManager = ServiceRepository.GetService<IGameLocationBattleService>() as GameLocationBattleManager;

            if (!actionManager || !battleManager)
            {
                yield break;
            }

            var implementationManager =
                ServiceRepository.GetService<IRulesetImplementationService>() as RulesetImplementationManager;

            var character = characterAction.ActingCharacter;
            var rulesetCharacter = character.RulesetCharacter;
            var usablePower = PowerProvider.Get(powerPool, rulesetCharacter);

            var actionParams =
                new CharacterActionParams(GameLocationCharacter.GetFromActor(rulesetCharacter),
                    ActionDefinitions.Id.SpendPower)
                {
                    StringParameter = "FormOfTheBeast",
                    RulesetEffect = implementationManager
                        .MyInstantiateEffectPower(rulesetCharacter, usablePower, false),
                    UsablePower = usablePower,
                    targetCharacters = [character]
                };
            var count = actionManager.PendingReactionRequestGroups.Count;
            var reactionRequest = new ReactionRequestSpendBundlePower(actionParams);

            actionManager.AddInterruptRequest(reactionRequest);

            yield return battleManager.WaitForReactions(character, actionManager, count);
        }
    }

    private class BeastBiteHandler()
        : AddExtraAttackBase(ActionDefinitions.ActionType.Main), IPhysicalAttackFinishedByMe
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
            if (rollOutcome is not (RollOutcome.Success or RollOutcome.CriticalSuccess))
            {
                yield break;
            }

            if (attackMode.sourceDefinition is not ItemDefinition item ||
                item != _beastBite)
            {
                yield break;
            }

            var rulesetAttacker = attacker.RulesetCharacter;
            var bonus = rulesetAttacker.TryGetAttributeValue(AttributeDefinitions.ProficiencyBonus);

            var applyFormsParams = new ApplyFormsParams
            {
                sourceCharacter = rulesetAttacker,
                targetCharacter = rulesetAttacker,
                position = attacker.LocationPosition
            };

            var healingForm = new HealingForm
            {
                healingComputation = HealingComputation.Dice,
                bonusHealing = bonus,
                diceNumber = 0,
                healingCap = HealingCap.HalfMaximumHitPoints
            };

            var implementationService = ServiceRepository.GetService<IRulesetImplementationService>();

            implementationService.ApplyEffectForms(
                [new EffectForm { healingForm = healingForm, formType = EffectForm.EffectFormType.Healing }],
                applyFormsParams,
                [],
                out _,
                out _);

            EffectHelpers.StartVisualEffect(attacker, attacker, SpellDefinitions.CureWounds,
                EffectHelpers.EffectType.Effect);
        }

        protected override AttackModeOrder GetOrder(RulesetCharacter character)
        {
            return AttackModeOrder.Start;
        }

        protected override List<RulesetAttackMode> GetAttackModes(RulesetCharacter character)
        {
            if (character is not RulesetCharacterHero hero)
            {
                return null;
            }

            var attackModifiers = hero.attackModifiers;
            ActionDefinitions.ActionType[] list =
                [ActionDefinitions.ActionType.Reaction, ActionDefinitions.ActionType.Main];

            return list
                .Select(type =>
                    character.TryRefreshAttackMode(
                        type,
                        _beastBite,
                        _beastBite.WeaponDescription,
                        ValidatorsCharacter.IsFreeOffhandVanilla(character),
                        true,
                        EquipmentDefinitions.SlotTypeMainHand,
                        attackModifiers,
                        character.FeaturesOrigin))
                .ToList();
        }
    }

    private class BeastClawsHandler() : AddExtraAttackBase(ActionDefinitions.ActionType.None),
        IPhysicalAttackFinishedByMe, ICharacterTurnStartListener
    {
        public void OnCharacterTurnStarted(GameLocationCharacter locationCharacter)
        {
            locationCharacter.UsedSpecialFeatures.Remove(TagBeastClawAttack);
        }

        public IEnumerator OnPhysicalAttackFinishedByMe(
            GameLocationBattleManager battleManager,
            CharacterAction action,
            GameLocationCharacter attacker,
            GameLocationCharacter defender,
            RulesetAttackMode attackMode,
            RollOutcome rollOutcome,
            int damageAmount)
        {
            if (attackMode.SourceDefinition is not ItemDefinition item ||
                item != _beastClaws)
            {
                yield break;
            }

            if (defender.RulesetCharacter is not
                { IsDeadOrDyingOrUnconscious: false })
            {
                yield break;
            }

            var actionManager =
                ServiceRepository.GetService<IGameLocationActionService>() as GameLocationActionManager;

            if (!actionManager)
            {
                yield break;
            }

            if (attacker.UsedSpecialFeatures.ContainsKey(TagBeastClawAttack))
            {
                yield break;
            }

            var reactionParams =
                new CharacterActionParams(attacker, (ActionDefinitions.Id)ExtraActionId.DoNothingReaction)
                {
                    StringParameter = Gui.Format("Reaction/&CustomReactionExtraClawAttackDescription")
                };
            var reactionRequest = new ReactionRequestCustom("ExtraClawAttack", reactionParams);
            var count = actionManager.PendingReactionRequestGroups.Count;

            actionManager.AddInterruptRequest(reactionRequest);

            yield return battleManager.WaitForReactions(attacker, actionManager, count);

            if (!reactionParams.reactionValidated)
            {
                yield break;
            }

            attacker.UsedSpecialFeatures.Add(TagBeastClawAttack, 0);

            var attackModeCopy = attackMode.DeepCopy();

            attackModeCopy.ActionType = ActionDefinitions.ActionType.NoCost;

            var actionParams = new CharacterActionParams(attacker, ActionDefinitions.Id.AttackFree)
            {
                ActionModifiers = { new ActionModifier() },
                AttackMode = attackModeCopy,
                TargetCharacters = { defender }
            };

            ServiceRepository.GetService<IGameLocationActionService>()?
                .ExecuteAction(actionParams, null, true);
        }

        protected override AttackModeOrder GetOrder(RulesetCharacter character)
        {
            return AttackModeOrder.Start;
        }

        protected override List<RulesetAttackMode> GetAttackModes(RulesetCharacter character)
        {
            if (character is not RulesetCharacterHero hero || !hero.HasFreeHandSlot())
            {
                return null;
            }

            var attackModifiers = hero.attackModifiers;
            List<ActionDefinitions.ActionType> list = [ActionDefinitions.ActionType.Reaction];

            if (hero.GetMainWeapon() == null)
            {
                list.Add(ActionDefinitions.ActionType.Main);
            }

            if (hero.GetOffhandWeapon() == null)
            {
                list.Add(ActionDefinitions.ActionType.Bonus);
            }

            return list
                .Select(type =>
                    character.TryRefreshAttackMode(
                        type,
                        _beastClaws,
                        _beastClaws.WeaponDescription,
                        ValidatorsCharacter.IsFreeOffhandVanilla(character),
                        type != ActionDefinitions.ActionType.Bonus,
                        type != ActionDefinitions.ActionType.Bonus
                            ? EquipmentDefinitions.SlotTypeMainHand
                            : EquipmentDefinitions.SlotTypeOffHand,
                        attackModifiers,
                        character.FeaturesOrigin))
                .ToList();
        }
    }

    private class BeastTailModifyArmorClass(ConditionDefinition condition) : IModifyAC
    {
        public void ModifyAC(RulesetCharacter owner,
            [UsedImplicitly] bool callRefresh,
            [UsedImplicitly] bool dryRun,
            [UsedImplicitly] FeatureDefinition dryRunFeature,
            RulesetAttribute armorClass)
        {
            var character = GameLocationCharacter.GetFromActor(owner);

            if (!character.UsedSpecialFeatures.TryGetValue(TagBeastTailArmorClass, out var bonus))
            {
                bonus = owner.RollDie(DieType.D8, RollContext.None, false, AdvantageType.None, out _, out _);
                character.UsedSpecialFeatures.Add(TagBeastTailArmorClass, bonus);
                // add console message
                var console = Gui.Game.GameConsole;
                var entry = new GameConsoleEntry("Feedback/&BeastTailSwipeAdditionalArmorClass",
                    console.consoleTableDefinition);

                console.AddCharacterEntry(owner, entry);
                entry.AddParameter(ConsoleStyleDuplet.ParameterType.Positive, $"{bonus}");
                console.AddEntry(entry);
            }

            var acBonus = bonus;
            var attributeModifier = RulesetAttributeModifier.BuildAttributeModifier(
                AttributeModifierOperation.Additive,
                acBonus, AttributeDefinitions.TagEffect);
            var trendInfo = new TrendInfo(acBonus, FeatureSourceType.Condition, condition.Name, null,
                attributeModifier);

            armorClass.AddModifier(attributeModifier);
            armorClass.ValueTrends.Add(trendInfo);
        }
    }

    private class BeastTailHandler : AddExtraAttackBase, ITryAlterOutcomeAttack
    {
        private readonly FeatureDefinitionPower _powerTailSwipe;

        internal BeastTailHandler() : base(ActionDefinitions.ActionType.Main)
        {
            var conditionTailSwipe = ConditionDefinitionBuilder
                .Create($"Condition{Name}TailSwipe")
                .SetGuiPresentation(Category.Condition)
                .SetSpecialInterruptions(ConditionInterruption.AnyBattleTurnEnd)
                .SetPossessive()
                .AddToDB();
            conditionTailSwipe.AddCustomSubFeatures(new BeastTailModifyArmorClass(conditionTailSwipe));

            _powerTailSwipe = FeatureDefinitionPowerBuilder
                .Create($"Power{Name}TailSwipe")
                .SetGuiPresentation(Category.Feature)
                .SetUsesFixed(ActivationTime.Reaction)
                .SetEffectDescription(
                    EffectDescriptionBuilder
                        .Create()
                        .SetDurationData(DurationType.Round, 0, TurnOccurenceType.StartOfTurn)
                        .SetTargetingData(Side.Ally, RangeType.Self, 0, TargetType.Self)
                        .SetEffectForms(EffectFormBuilder.ConditionForm(conditionTailSwipe))
                        .Build())
                .AddToDB();
        }

        public int HandlerPriority => -10;

        public IEnumerator OnTryAlterOutcomeAttack(
            GameLocationBattleManager instance,
            CharacterAction action,
            GameLocationCharacter attacker,
            GameLocationCharacter defender,
            GameLocationCharacter helper,
            ActionModifier actionModifier,
            RulesetAttackMode attackMode,
            RulesetEffect rulesetEffect)
        {
            var battleManager = ServiceRepository.GetService<IGameLocationBattleService>() as GameLocationBattleManager;

            if (!battleManager)
            {
                yield break;
            }

            var rulesetDefender = defender.RulesetCharacter;

            if (action.AttackRollOutcome is not (RollOutcome.Success or RollOutcome.CriticalSuccess) ||
                helper != defender ||
                defender.IsMyTurn() ||
                !defender.CanReact() ||
                !defender.CanPerceiveTarget(attacker) ||
                DistanceCalculation.GetDistanceFromCharacters(attacker, defender) > 2)
            {
                yield break;
            }

            defender.UsedSpecialFeatures.Remove(TagBeastTailArmorClass);

            var actionService = ServiceRepository.GetService<IGameLocationActionService>();
            var implementationManager =
                ServiceRepository.GetService<IRulesetImplementationService>() as RulesetImplementationManager;

            var usablePower = PowerProvider.Get(_powerTailSwipe, rulesetDefender);
            var actionParams =
                new CharacterActionParams(defender, ActionDefinitions.Id.PowerReaction)
                {
                    StringParameter = "BeastTailSwipe",
                    ActionModifiers = { new ActionModifier() },
                    RulesetEffect = implementationManager
                        .MyInstantiateEffectPower(rulesetDefender, usablePower, false),
                    UsablePower = usablePower,
                    TargetCharacters = { defender }
                };
            var count = actionService.PendingReactionRequestGroups.Count;

            actionService.ReactToUsePower(actionParams, "UsePower", defender);

            yield return battleManager.WaitForReactions(attacker, actionService, count);
        }

        protected override AttackModeOrder GetOrder(RulesetCharacter character)
        {
            return AttackModeOrder.Start;
        }

        protected override List<RulesetAttackMode> GetAttackModes(RulesetCharacter character)
        {
            if (character is not RulesetCharacterHero hero)
            {
                return null;
            }

            var attackModifiers = hero.attackModifiers;
            ActionDefinitions.ActionType[] list =
                [ActionDefinitions.ActionType.Reaction, ActionDefinitions.ActionType.Main];

            return list
                .Select(type =>
                    character.TryRefreshAttackMode(
                        type,
                        _beastTail,
                        _beastTail.WeaponDescription,
                        ValidatorsCharacter.IsFreeOffhandVanilla(character),
                        true,
                        EquipmentDefinitions.SlotTypeMainHand,
                        attackModifiers,
                        character.FeaturesOrigin))
                .ToList();
        }
    }

    #endregion

    #region Bestial Soul

    private static FeatureDefinitionPower BuildPowerBestialSoul()
    {
        var powerBestialSoul = FeatureDefinitionPowerBuilder
            .Create($"Power{Name}BestialSoul")
            .SetGuiPresentation(Category.Feature,
                Sprites.GetSprite("PowerBestialSoul", Resources.PowerBestialSoul, 256, 128))
            .SetUsesFixed(ActivationTime.NoCost, RechargeRate.ShortRest)
            .AddCustomSubFeatures(new BestialSoulMagicalAttack())
            .AddToDB();

        var featureBestialSoulJump = FeatureDefinitionMovementAffinityBuilder
            .Create($"MovementAffinity{Name}BestialSoulJump")
            .SetGuiPresentationNoContent(true)
            .SetAdditionalJumpCells(3)
            .AddToDB();

        featureBestialSoulJump.enhancedJump = true;

        string[] suffixes = ["Hare", "Spider", "Snake"];

        FeatureDefinition[][] features =
        [
            [featureBestialSoulJump],
            [FeatureDefinitionMovementAffinitys.MovementAffinitySpiderClimb],
            [
                FeatureDefinitionMovementAffinitys.MovementAffinityFreedomOfMovement,
                FeatureDefinitionMoveThroughEnemyModifiers.MoveThroughEnemyModifierHalflingNimbleness
            ]
        ];
        var powers = new List<FeatureDefinitionPower>();

        for (var i = 0; i < 3; i++)
        {
            var condition = ConditionDefinitionBuilder
                .Create($"Condition{Name}BestialSoul{suffixes[i]}")
                .SetGuiPresentation($"Feature/&Power{Name}BestialSoul{suffixes[i]}Title",
                    $"Feature/&Power{Name}BestialSoul{suffixes[i]}Description", ConditionDefinitions.ConditionBlessed)
                .SetFeatures(features[i])
                .AddToDB();

            var suffix = suffixes[i];
            var power = FeatureDefinitionPowerSharedPoolBuilder
                .Create($"Power{Name}BestialSoul{suffix}")
                .SetGuiPresentation(Category.Feature, hidden: true)
                .SetSharedPool(ActivationTime.NoCost, powerBestialSoul)
                .SetEffectDescription(
                    EffectDescriptionBuilder
                        .Create()
                        .SetTargetingData(Side.All, RangeType.Self, 0, TargetType.Self)
                        .SetDurationData(DurationType.UntilAnyRest)
                        .SetEffectForms(
                            EffectFormBuilder.ConditionForm(condition)
                        )
                        .Build())
                .AddToDB();
            powers.Add(power);
        }

        PowerBundle.RegisterPowerBundle(powerBestialSoul, false, powers);

        return powerBestialSoul;
    }

    private class BestialSoulMagicalAttack : IModifyWeaponAttackMode
    {
        public void ModifyAttackMode(RulesetCharacter character, RulesetAttackMode attackMode)
        {
            if (attackMode.sourceDefinition is not ItemDefinition item ||
                !item.ItemTags.Contains(TagBeastWeapon))
            {
                return;
            }

            attackMode.AttackTags.Add("MagicalWeapon");
        }
    }

    #endregion

    #region Infectious Fury

    private static FeatureDefinition[] BuildFeatureInfectiousFury()
    {
        var conditionInfectiousFury = ConditionDefinitionBuilder
            .Create($"Condition{Name}InfectiousFury")
            .SetGuiPresentation(Category.Condition, ConditionDefinitions.ConditionBaned)
            .SetConditionType(ConditionType.Detrimental)
            .AddToDB();

        var powerInfectiousFury = FeatureDefinitionPowerBuilder
            .Create($"Power{Name}InfectiousFury")
            .SetGuiPresentation(Category.Feature)
            .SetShowCasting(false)
            .SetUsesProficiencyBonus(ActivationTime.OnPowerActivatedAuto)
            .SetEffectDescription(EffectDescriptionBuilder
                .Create()
                .SetDurationData(DurationType.Round)
                .SetTargetingData(Side.All, RangeType.Distance, 12, TargetType.IndividualsUnique)
                .SetSavingThrowData(false, AttributeDefinitions.Wisdom, true,
                    EffectDifficultyClassComputation.AbilityScoreAndProficiency, AttributeDefinitions.Constitution)
                .SetEffectForms(EffectFormBuilder.ConditionForm(conditionInfectiousFury))
                .Build())
            .AddToDB();

        var powerInfectiousFuryCompelledStrike = FeatureDefinitionPowerBuilder
            .Create($"Power{Name}InfectiousFuryCompelledStrike")
            .SetGuiPresentation(Category.Feature,
                Sprites.GetSprite("PowerInfectiousFuryCompelledStrike", Resources.PowerInfectiousFuryCompelledStrike,
                    256, 128))
            .SetUsesFixed(ActivationTime.NoCost)
            .SetShowCasting(false)
            .SetEffectDescription(EffectDescriptionBuilder
                .Create()
                .SetTargetingData(Side.All, RangeType.Distance, 12, TargetType.IndividualsUnique, 2)
                .Build())
            .AddToDB();

        powerInfectiousFuryCompelledStrike.AddCustomSubFeatures(new CompelledStrikeHandler(conditionInfectiousFury));

        var powerInfectiousFuryMindlash = FeatureDefinitionPowerBuilder
            .Create($"Power{Name}InfectiousFuryMindlash")
            .SetGuiPresentation(Category.Feature,
                Sprites.GetSprite("PowerInfectiousFuryMindlash", Resources.PowerInfectiousFuryMindlash, 256, 128))
            .SetUsesFixed(ActivationTime.NoCost)
            .SetEffectDescription(EffectDescriptionBuilder
                .Create()
                .SetTargetingData(Side.All, RangeType.Distance, 12, TargetType.IndividualsUnique)
                .SetRequiredCondition(conditionInfectiousFury)
                .SetEffectForms(
                    EffectFormBuilder.DamageForm(DamageTypePsychic, 2, DieType.D12),
                    EffectFormBuilder.ConditionForm(conditionInfectiousFury, ConditionForm.ConditionOperation.Remove))
                .Build())
            .AddToDB();

        powerInfectiousFury.AddCustomSubFeatures(
            new InfectiousFuryHandler(powerInfectiousFury, conditionInfectiousFury));

        return [powerInfectiousFury, powerInfectiousFuryCompelledStrike, powerInfectiousFuryMindlash];
    }

    private sealed class InfectiousFuryHandler(
        FeatureDefinitionPower powerInfectiousFury,
        ConditionDefinition condition) : IPhysicalAttackFinishedByMe
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
            if (rollOutcome is not (RollOutcome.Success or RollOutcome.CriticalSuccess))
            {
                yield break;
            }

            if (!attacker.IsMyTurn())
            {
                yield break;
            }

            if (defender.RulesetCharacter is not { IsDeadOrDyingOrUnconscious: false } ||
                defender.RulesetCharacter.HasAnyConditionOfType(condition.name) ||
                attacker.RulesetCharacter.GetRemainingPowerUses(powerInfectiousFury) == 0)
            {
                yield break;
            }

            if (attackMode.SourceDefinition is not ItemDefinition item ||
                !item.ItemTags.Contains(TagBeastWeapon))
            {
                yield break;
            }

            var actionService = ServiceRepository.GetService<IGameLocationActionService>();
            var implementationService = ServiceRepository.GetService<IRulesetImplementationService>();
            var usablePower = PowerProvider.Get(powerInfectiousFury, attacker.RulesetCharacter);
            var reactionParams = new CharacterActionParams(attacker, ActionDefinitions.Id.SpendPower)
            {
                StringParameter = "InfectiousFury",
                RulesetEffect =
                    implementationService.InstantiateEffectPower(attacker.RulesetCharacter, usablePower, false),
                UsablePower = usablePower,
                targetCharacters = [defender],
                actionModifiers = [new ActionModifier()]
            };
            var count = actionService.PendingReactionRequestGroups.Count;

            actionService.ReactToSpendPower(reactionParams);

            yield return battleManager.WaitForReactions(attacker, actionService, count);
        }
    }

    private class CompelledStrikeHandler(ConditionDefinition condition)
        : IFilterTargetingCharacter, IPowerOrSpellFinishedByMe
    {
        public bool EnforceFullSelection => true;

        public bool IsValid(CursorLocationSelectTarget __instance, GameLocationCharacter target)
        {
            var selectedTargets = __instance.SelectionService.SelectedTargets;

            if (selectedTargets.Count == 0)
            {
                if (!target.RulesetCharacter.HasConditionOfType(condition))
                {
                    __instance.actionModifier.FailureFlags.Add("Tooltip/&TargetMustHaveInfectiousFury");

                    return false;
                }

                // ReSharper disable once InvertIf
                if (!target.CanReact())
                {
                    __instance.actionModifier.FailureFlags.Add("Tooltip/&AllyMustBeAbleToReact");

                    return false;
                }

                return true;
            }

            var selectedTarget = selectedTargets[0];
            var attackMode = selectedTarget.FindActionAttackMode(ActionDefinitions.Id.AttackMain);

            // ReSharper disable once InvertIf
            if (attackMode == null ||
                !IsValidAttack(__instance, attackMode, selectedTarget, target))
            {
                __instance.actionModifier.FailureFlags.Add("Tooltip/&MustBeAbleToAttackTarget");

                return false;
            }

            return true;
        }

        public IEnumerator OnPowerOrSpellFinishedByMe(CharacterActionMagicEffect action, BaseDefinition baseDefinition)
        {
            var targetCharacters = action.ActionParams.TargetCharacters;
            var attacker = targetCharacters[0];
            var defender = targetCharacters[1];

            // issue ally's attack
            var attackMode = attacker.FindActionAttackMode(ActionDefinitions.Id.AttackMain);

            if (attackMode == null)
            {
                yield break;
            }

            var attackModifier = new ActionModifier();
            var attackModeCopy = RulesetAttackMode.AttackModesPool.Get();

            attackModeCopy.Copy(attackMode);
            attackModeCopy.ActionType = ActionDefinitions.ActionType.Reaction;
            attacker.RulesetCharacter.RemoveAllConditionsOfType(condition.name);
            Attack(attacker, defender, attackModeCopy, attackModifier, ActionDefinitions.Id.AttackOpportunity);
        }

        private static bool IsValidAttack(
            CursorLocationSelectTarget __instance,
            RulesetAttackMode attackMode,
            GameLocationCharacter selectedCharacter,
            GameLocationCharacter targetedCharacter)
        {
            __instance.predictivePosition = selectedCharacter.LocationPosition;

            var attackParams1 = new BattleDefinitions.AttackEvaluationParams();

            attackParams1.FillForPhysicalReachAttack(selectedCharacter, __instance.predictivePosition, attackMode,
                targetedCharacter, targetedCharacter.LocationPosition, __instance.actionModifier);

            if (__instance.BattleService.CanAttack(attackParams1))
            {
                return true;
            }

            var attackParams2 = new BattleDefinitions.AttackEvaluationParams();

            attackParams2.FillForPhysicalRangeAttack(selectedCharacter, __instance.predictivePosition, attackMode,
                targetedCharacter, targetedCharacter.LocationPosition, __instance.actionModifier);

            return __instance.BattleService.CanAttack(attackParams2);
        }

        private static void Attack(
            GameLocationCharacter actingCharacter,
            GameLocationCharacter target,
            RulesetAttackMode attackMode,
            ActionModifier attackModifier,
            ActionDefinitions.Id actionId = ActionDefinitions.Id.AttackFree)
        {
            var actionService = ServiceRepository.GetService<IGameLocationActionService>();
            var attackActionParams =
                new CharacterActionParams(actingCharacter, actionId)
                {
                    AttackMode = attackMode, TargetCharacters = { target }, ActionModifiers = { attackModifier }
                };

            actionService.ExecuteAction(attackActionParams, null, true);
        }
    }

    #endregion
}

internal class PowerCallTheHuntHandler(FeatureDefinitionPower power) : IActionFinishedByMe
{
    public IEnumerator OnActionFinishedByMe(CharacterAction characterAction)
    {
        if (characterAction is not CharacterActionCombatRageStart)
            {
            yield break;
        }

        var actionManager =
            ServiceRepository.GetService<IGameLocationActionService>() as GameLocationActionManager;
        var battleManager = ServiceRepository.GetService<IGameLocationBattleService>() as GameLocationBattleManager;

        if (!actionManager || !battleManager)
        {
            yield break;
        }

        var character = characterAction.ActingCharacter;
        var rulesetCharacter = character.RulesetCharacter;

        var implementationManager =
            ServiceRepository.GetService<IRulesetImplementationService>() as RulesetImplementationManager;

        var locationCharacterService = ServiceRepository.GetService<IGameLocationCharacterService>();

        var usablePower = PowerProvider.Get(power, rulesetCharacter);
        var actionParams = new CharacterActionParams(character, ActionDefinitions.Id.SpendPower)
        {
            StringParameter = "CallTheHunt",
            RulesetEffect = implementationManager
                .MyInstantiateEffectPower(rulesetCharacter, usablePower, false),
            UsablePower = usablePower,
            targetCharacters = [],
            actionModifiers = []
        };

        foreach (var ally in locationCharacterService.AllValidEntities
                     .Where(x =>
                         x.Side == character.Side &&
                         x.IsWithinRange(character, 6) &&
                         x.CanAct())
                     .ToList())
        {
            actionParams.targetCharacters.Add(ally);
            actionParams.actionModifiers.Add(new ActionModifier());
        }

        var count = actionManager.PendingReactionRequestGroups.Count;
        var reactionRequest = new ReactionRequestSpendPower(actionParams);

        actionManager.AddInterruptRequest(reactionRequest);

        yield return battleManager.WaitForReactions(character, actionManager, count);

        if (!reactionRequest.Validated)
        {
            yield break;
        }

        var applyFormsParams = new ApplyFormsParams
        {
            sourceCharacter = rulesetCharacter,
            targetCharacter = rulesetCharacter,
            position = character.locationPosition
        };

        _ = implementationManager!.ApplyEffectForms(
            [
                new EffectForm
                {
                    formType = EffectForm.EffectFormType.TemporaryHitPoints,
                    temporaryHitPointsForm = new TemporaryHitPointsForm
                    {
                        applyToSelf = true, bonusHitPoints = 15, diceNumber = 0
                    }
                }
            ],
            applyFormsParams,
            [],
            out _,
            out _);
    }
}
