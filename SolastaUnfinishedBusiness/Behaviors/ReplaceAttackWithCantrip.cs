﻿using SolastaUnfinishedBusiness.Api.GameExtensions;
using SolastaUnfinishedBusiness.Behaviors.Specific;
using static ActionDefinitions;

namespace SolastaUnfinishedBusiness.Behaviors;

internal interface IAttackReplaceWithCantrip;

internal static class ReplaceAttackWithCantrip
{
    internal static void AllowCastDuringMainAttack(
        GameLocationCharacter character,
        Id actionId,
        ActionScope scope,
        ref ActionStatus result)
    {
        if (scope != ActionScope.Battle ||
            actionId != Id.CastMain ||
            character.UsedMainCantrip ||
            character.UsedMainAttacks == 0 ||
            result != ActionStatus.NoLongerAvailable ||
            !character.RulesetCharacter.HasSubFeatureOfType<IAttackReplaceWithCantrip>())
        {
            return;
        }

        result = ActionStatus.Available;
    }

    internal static void AllowAttacksAfterCantrip(
        GameLocationCharacter character,
        CharacterActionParams actionParams,
        ActionScope scope)
    {
        if (scope != ActionScope.Battle ||
            actionParams.AttackMode == null ||
            actionParams.ActionDefinition.Id != Id.CastMain ||
            actionParams.activeEffect is not RulesetEffectSpell spellEffect ||
            spellEffect.SpellDefinition.SpellLevel > 0 ||
            !character.RulesetCharacter.HasSubFeatureOfType<IAttackReplaceWithCantrip>())
        {
            return;
        }

        var attackMode = actionParams.attackMode;
        var rulesetCharacter = character.RulesetCharacter;

        character.HandleMonkMartialArts();

        // only mark has attacked if not an attack after magic effect
        if (!attackMode.AttackTags.Contains(AttackAfterMagicEffect.AttackAfterMagicEffectTag))
        {
            character.HasAttackedSinceLastTurn = true;
        }

        character.UsedMainAttacks++;
        rulesetCharacter.ExecutedAttacks++;

        var maxAttacks = character.GetAllowedMainAttacks();

        if (character.UsedMainAttacks < maxAttacks)
        {
            character.currentActionRankByType[ActionType.Main]--;
        }
        else
        {
            character.UsedMainAttacks = 0;
        }
    }
}
