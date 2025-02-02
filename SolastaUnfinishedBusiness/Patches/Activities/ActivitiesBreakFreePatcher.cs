﻿using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using HarmonyLib;
using JetBrains.Annotations;
using SolastaUnfinishedBusiness.Api.Helpers;
using SolastaUnfinishedBusiness.Interfaces;
using SolastaUnfinishedBusiness.Models;
using TA.AI.Activities;
using static RuleDefinitions;

namespace SolastaUnfinishedBusiness.Patches.Activities;

[UsedImplicitly]
public static class BreakFreePatcher
{
    [HarmonyPatch(typeof(BreakFree), nameof(BreakFree.ExecuteImpl))]
    [SuppressMessage("Minor Code Smell", "S101:Types should be named in PascalCase", Justification = "Patch")]
    [UsedImplicitly]
    public static class BreakFree_Patch
    {
        [UsedImplicitly]
        public static IEnumerator Postfix([NotNull] IEnumerator values, AiLocationCharacter character)
        {
            var gameLocationCharacter = character.GameLocationCharacter;
            var rulesetCharacter = gameLocationCharacter.RulesetCharacter;
            var restrainingCondition = AiContext.GetRestrainingCondition(rulesetCharacter);

            if (restrainingCondition == null)
            {
                while (values.MoveNext())
                {
                    yield return values.Current;
                }

                yield break;
            }

            var action = (AiContext.BreakFreeType)restrainingCondition.Amount;
            var success = true;

            switch (action)
            {
                case AiContext.BreakFreeType.DoNoCheckAndRemoveCondition:
                    break;

                case AiContext.BreakFreeType.DoStrengthCheckAgainstCasterDC:
                    yield return RollAttributeCheck(AttributeDefinitions.Strength);
                    break;

                case AiContext.BreakFreeType.DoWisdomCheckAgainstCasterDC:
                    yield return RollAttributeCheck(AttributeDefinitions.Wisdom);
                    break;

                default:
                    while (values.MoveNext())
                    {
                        yield return values.Current;
                    }

                    yield break;
            }

            if (success)
            {
                rulesetCharacter.RemoveCondition(restrainingCondition);
            }

            gameLocationCharacter.SpendActionType(ActionDefinitions.ActionType.Main);
            rulesetCharacter.BreakFreeExecuted?.Invoke(rulesetCharacter, success);

            yield break;

            IEnumerator RollAttributeCheck(string attributeName)
            {
                var checkDC = 10;
                var sourceGuid = restrainingCondition!.SourceGuid;

                if (RulesetEntity.TryGetEntity(sourceGuid, out RulesetCharacterHero rulesetCharacterHero))
                {
                    checkDC = rulesetCharacterHero.SpellRepertoires
                        .Select(x => x.SaveDC)
                        .Max();
                }

                rulesetCharacter.LogCharacterActivatesAbility(
                    string.Empty,
                    "Feedback/&BreakFreeAttempt",
                    extra:
                    [
                        (ConsoleStyleDuplet.ParameterType.Negative,
                            restrainingCondition.ConditionDefinition.FormatTitle())
                    ]);

                var actionModifier = new ActionModifier();
                var abilityCheckRoll = gameLocationCharacter.RollAbilityCheck(
                    attributeName,
                    string.Empty,
                    checkDC,
                    AdvantageType.None,
                    actionModifier,
                    false,
                    -1,
                    out var rollOutcome,
                    out var successDelta,
                    true);

                //PATCH: support for Bardic Inspiration roll off battle and ITryAlterOutcomeAttributeCheck
                var abilityCheckData = new AbilityCheckData
                {
                    AbilityCheckRoll = abilityCheckRoll,
                    AbilityCheckRollOutcome = rollOutcome,
                    AbilityCheckSuccessDelta = successDelta,
                    AbilityCheckActionModifier = actionModifier,
                    Action = null
                };

                yield return TryAlterOutcomeAttributeCheck
                    .HandleITryAlterOutcomeAttributeCheck(gameLocationCharacter, abilityCheckData);

                success = abilityCheckData.AbilityCheckRollOutcome
                    is RollOutcome.Success
                    or RollOutcome.CriticalSuccess;
            }
        }
    }
}
