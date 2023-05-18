﻿using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SolastaUnfinishedBusiness.Api.LanguageExtensions;
using SolastaUnfinishedBusiness.Builders;
using SolastaUnfinishedBusiness.Classes;
using SolastaUnfinishedBusiness.CustomUI;
using UnityEngine;
using UnityModManagerNet;
#if DEBUG
using SolastaUnfinishedBusiness.DataMiner;
#endif

namespace SolastaUnfinishedBusiness.Models;

internal static class BootContext
{
    private const string BaseURL = "https://github.com/SolastaMods/SolastaUnfinishedBusiness/releases/latest/download";

    private static string InstalledVersion { get; } = GetInstalledVersion();
    private static string LatestVersion { get; set; }
    private static string PreviousVersion { get; } = GetPreviousVersion();

    internal static void Startup()
    {
#if DEBUG
        ItemDefinitionVerification.Load();
        EffectFormVerification.Load();
#endif

        // STEP 0: Cache TA definitions for diagnostics and export
        DiagnosticsContext.CacheTaDefinitions();

        // Load Translations and Resources Locator after
        TranslatorContext.Load();
        ResourceLocatorContext.Load();

        // Create our Content Pack for anything that gets further created
        CeContentPackContext.Load();
        CustomActionIdContext.Load();

        // Cache all Merchant definitions and what item types they sell
        MerchantTypeContext.Load();

        // Custom Conditions must load as early as possible
        CustomConditionsContext.Load();

        // AI Context
        AiContext.Load();

        //
        // custom stuff that can be loaded in any order
        //

        CustomReactionsContext.Load();
        CustomWeaponsContext.Load();
        CustomItemsContext.Load();
        PowerBundleContext.Load();

        //
        // other stuff can be loaded in any order
        //

        ToolsContext.Load();
        CharacterExportContext.Load();
        DmProEditorContext.Load();
        GameUiContext.Load();

        // Start will all options under Character
        CharacterContext.Load();

        // Fighting Styles must be loaded before feats to allow feats to generate corresponding fighting style ones.
        FightingStyleContext.Load();

        // Races may rely on spells and powers being in the DB before they can properly load.
        RacesContext.Load();

        // Backgrounds may rely on spells and powers being in the DB before they can properly load.
        BackgroundsContext.Load();

        // Subclasses may rely on spells and powers being in the DB before they can properly load.
        SubclassesContext.Load();

        // Deities may rely on spells and powers being in the DB before they can properly load.
        // DeitiesContext.Load();

        // Classes may rely on spells and powers being in the DB before they can properly load.
        ClassesContext.Load();

        // Level 20 must always load after classes and subclasses
        Level20Context.Load();

        // Item Options must be loaded after Item Crafting
        ItemCraftingMerchantContext.Load();
        RecipeHelper.AddRecipeIcons();

        MerchantContext.Load();

        ServiceRepository.GetService<IRuntimeService>().RuntimeLoaded += _ =>
        {
            // DelegatesContext.LateLoad();

            // Late initialized to allow feats and races from other mods
            CharacterContext.LateLoad();

            // There are feats that need all character classes loaded before they can properly be setup.
            FeatsContext.LateLoad();

            // Custom invocations
            InvocationsContext.LateLoad();

            // Custom metamagic
            MetamagicContext.LateLoad();

            // SRD rules switches
            SrdAndHouseRulesContext.LateLoad();

            // Vanilla Fixes
            FixesContext.LateLoad();

            // Level 20 - patching and final configs
            Level20Context.LateLoad();

            // Multiclass - patching and final configs
            MulticlassContext.LateLoad();

            // Spells context need Level 20 and Multiclass to properly register spells
            SpellsContext.LateLoad();

            // Shared Slots - patching and final configs
            SharedSpellsContext.LateLoad();

            // Set anything on subs that depends on spells and others
            SubclassesContext.LateLoad();
            InventorClass.LateLoadSpellStoringItem();

            // Save by location initialization depends on services to be ready
            SaveByLocationContext.LateLoad();

            // Recache all gui collections
            GuiWrapperContext.Recache();

            // Cache CE definitions for diagnostics and export
            DiagnosticsContext.CacheCeDefinitions();

            // Dump documentations to mod folder
            if (!Directory.Exists($"{Main.ModFolder}/Documentation"))
            {
                Directory.CreateDirectory($"{Main.ModFolder}/Documentation");
            }

            DumpMonsters();
            DumpClasses("UnfinishedBusiness", x => x.ContentPack == CeContentPackContext.CeContentPack);
            DumpClasses("Solasta", x => x.ContentPack != CeContentPackContext.CeContentPack);
            DumpSubclasses("UnfinishedBusiness", x => x.ContentPack == CeContentPackContext.CeContentPack);
            DumpSubclasses("Solasta", x => x.ContentPack != CeContentPackContext.CeContentPack);
            DumpRaces("UnfinishedBusiness", x => x.ContentPack == CeContentPackContext.CeContentPack);
            DumpRaces("Solasta", x => x.ContentPack != CeContentPackContext.CeContentPack);
            DumpOthers<FeatDefinition>("UnfinishedBusinessFeats", x => FeatsContext.Feats.Contains(x));
            DumpOthers<FeatDefinition>("SolastaFeats", x => x.ContentPack != CeContentPackContext.CeContentPack);
            DumpOthers<FightingStyleDefinition>("UnfinishedBusinessFightingStyles",
                x => FightingStyleContext.FightingStyles.Contains(x));
            DumpOthers<FightingStyleDefinition>("SolastaFightingStyles",
                x => x.ContentPack != CeContentPackContext.CeContentPack);
            DumpOthers<InvocationDefinition>("UnfinishedBusinessInvocations",
                x => InvocationsContext.Invocations.Contains(x));
            DumpOthers<InvocationDefinition>("SolastaInvocations",
                x => x.ContentPack != CeContentPackContext.CeContentPack);
            DumpOthers<SpellDefinition>("UnfinishedBusinessSpells",
                x => x.ContentPack == CeContentPackContext.CeContentPack && SpellsContext.Spells.Contains(x));
            DumpOthers<SpellDefinition>("SolastaSpells",
                x => x.ContentPack != CeContentPackContext.CeContentPack);
            DumpOthers<ItemDefinition>("UnfinishedBusinessItems",
                x => x.ContentPack == CeContentPackContext.CeContentPack &&
                     x is ItemDefinition item &&
                     (item.IsArmor || item.IsWeapon));
            DumpOthers<ItemDefinition>("SolastaItems",
                x => x.ContentPack != CeContentPackContext.CeContentPack &&
                     x is ItemDefinition item &&
                     (item.IsArmor || item.IsWeapon));
            DumpOthers<MetamagicOptionDefinition>("UnfinishedBusinessMetamagic",
                x => MetamagicContext.Metamagic.Contains(x));
            DumpOthers<MetamagicOptionDefinition>("SolastaMetamagic",
                x => x.ContentPack != CeContentPackContext.CeContentPack);

            // really don't have a better place for these fixes here ;-)
            ExpandColorTables();
            AddExtraTooltipDefinitions();

            // avoid folks tweaking max party size directly on settings as we don't need to stress cloud servers
            Main.Settings.OverridePartySize = Math.Min(Main.Settings.OverridePartySize, ToolsContext.MaxPartySize);

            // Manages update or welcome messages
            Load();
            Main.Enable();
        };
    }

    private static string LazyManStripXml(string input)
    {
        return input
            .Replace("<color=#add8e6ff>", string.Empty)
            .Replace("<#57BCF4>", "\r\n\t")
            .Replace("</color>", string.Empty)
            .Replace("<b>", string.Empty)
            .Replace("<i>", string.Empty)
            .Replace("</b>", string.Empty)
            .Replace("</i>", string.Empty);
    }

    private static void DumpClasses(string groupName, Func<BaseDefinition, bool> filter)
    {
        var outString = new StringBuilder();
        var counter = 1;

        foreach (var klass in DatabaseRepository.GetDatabase<CharacterClassDefinition>()
                     .Where(x => filter(x))
                     .OrderBy(x => x.FormatTitle()))
        {
            outString.Append($"# {counter++}. - {klass.FormatTitle()}\r\n\r\n");
            outString.Append(klass.FormatDescription());
            outString.Append("\r\n\r\n");

            var level = 0;

            foreach (var featureUnlockByLevel in klass.FeatureUnlocks
                         .Where(x => !x.FeatureDefinition.GuiPresentation.hidden)
                         .OrderBy(x => x.level))
            {
                if (level != featureUnlockByLevel.level)
                {
                    outString.Append($"\r\n## Level {featureUnlockByLevel.level}\r\n\r\n");
                    level = featureUnlockByLevel.level;
                }

                var featureDefinition = featureUnlockByLevel.FeatureDefinition;
                var description = LazyManStripXml(featureDefinition.FormatDescription());

                outString.Append($"* {featureDefinition.FormatTitle()}\r\n\r\n");
                outString.Append(description);
                outString.Append("\r\n\r\n");
            }

            outString.Append("\r\n\r\n\r\n");
        }

        using var sw = new StreamWriter($"{Main.ModFolder}/Documentation/{groupName}Classes.md");
        sw.WriteLine(outString.ToString());
    }

    private static void DumpSubclasses(string groupName, Func<BaseDefinition, bool> filter)
    {
        var outString = new StringBuilder();
        var counter = 1;

        foreach (var subclass in DatabaseRepository.GetDatabase<CharacterSubclassDefinition>()
                     .Where(x => filter(x))
                     .OrderBy(x => x.FormatTitle()))
        {
            outString.Append($"# {counter++}. - {subclass.FormatTitle()}\r\n\r\n");
            outString.Append(subclass.FormatDescription());
            outString.Append("\r\n\r\n");

            var level = 0;

            foreach (var featureUnlockByLevel in subclass.FeatureUnlocks
                         .Where(x => !x.FeatureDefinition.GuiPresentation.hidden)
                         .OrderBy(x => x.level))
            {
                if (level != featureUnlockByLevel.level)
                {
                    outString.Append($"\r\n## Level {featureUnlockByLevel.level}\r\n\r\n");
                    level = featureUnlockByLevel.level;
                }

                var featureDefinition = featureUnlockByLevel.FeatureDefinition;
                var description = LazyManStripXml(featureDefinition.FormatDescription());

                outString.Append($"* {featureDefinition.FormatTitle()}\r\n\r\n");
                outString.Append(description);
                outString.Append("\r\n\r\n");
            }

            outString.Append("\r\n\r\n\r\n");
        }

        using var sw = new StreamWriter($"{Main.ModFolder}/Documentation/{groupName}Subclasses.md");
        sw.WriteLine(outString.ToString());
    }

    private static void DumpRaces(string groupName, Func<BaseDefinition, bool> filter)
    {
        var outString = new StringBuilder();
        var counter = 1;

        foreach (var race in DatabaseRepository.GetDatabase<CharacterRaceDefinition>()
                     .Where(x => filter(x))
                     .OrderBy(x => x.FormatTitle()))
        {
            outString.Append($"# {counter++}. - {race.FormatTitle()}\r\n\r\n");
            outString.Append(race.FormatDescription());
            outString.Append("\r\n\r\n");

            var level = 0;

            foreach (var featureUnlockByLevel in race.FeatureUnlocks
                         .Where(x => !x.FeatureDefinition.GuiPresentation.hidden)
                         .OrderBy(x => x.level))
            {
                if (level != featureUnlockByLevel.level)
                {
                    outString.Append($"\r\n## Level {featureUnlockByLevel.level}\r\n\r\n");
                    level = featureUnlockByLevel.level;
                }

                var featureDefinition = featureUnlockByLevel.FeatureDefinition;
                var description = LazyManStripXml(featureDefinition.FormatDescription());

                outString.Append($"* {featureDefinition.FormatTitle()}\r\n\r\n");
                outString.Append(description);
                outString.Append("\r\n\r\n");
            }

            outString.Append("\r\n\r\n\r\n");
        }

        using var sw = new StreamWriter($"{Main.ModFolder}/Documentation/{groupName}Races.md");
        sw.WriteLine(outString.ToString());
    }

    private static void DumpOthers<T>(string groupName, Func<BaseDefinition, bool> filter) where T : BaseDefinition
    {
        var outString = new StringBuilder();
        var db = DatabaseRepository.GetDatabase<T>();
        var counter = 1;

        foreach (var featureDefinition in db
                     .Where(x => filter(x))
                     .OrderBy(x => x.FormatTitle()))
        {
            var description = LazyManStripXml(featureDefinition.FormatDescription());

            outString.Append($"# {counter++}. - {featureDefinition.FormatTitle()}\r\n\r\n");
            outString.Append(description);
            outString.Append("\r\n\r\n");
        }

        using var sw = new StreamWriter($"{Main.ModFolder}/Documentation/{groupName}.md");
        sw.WriteLine(outString.ToString());
    }

    private static void DumpMonsters()
    {
        var outString = new StringBuilder();
        var counter = 1;

        foreach (var monsterDefinition in DatabaseRepository.GetDatabase<MonsterDefinition>()
                     .OrderBy(x => x.FormatTitle()))
        {
            outString.Append(GetMonsterBlock(monsterDefinition, ref counter));
        }

        using var sw = new StreamWriter($"{Main.ModFolder}/Documentation/SolastaMonsters.md");
        sw.WriteLine(outString.ToString());
    }

    private static string GetMonsterBlock([NotNull] MonsterDefinition monsterDefinition, ref int counter)
    {
        var outString = new StringBuilder();

        outString.AppendLine(
            $"# {counter++}. - {monsterDefinition.FormatTitle()} [DM: {monsterDefinition.DungeonMakerPresence}]");
        outString.AppendLine();

        var description = monsterDefinition.FormatDescription();

        if (!string.IsNullOrEmpty(description))
        {
            outString.AppendLine(monsterDefinition.FormatDescription());
        }

        outString.AppendLine();
        outString.AppendLine($"Alignment: *{monsterDefinition.Alignment.SplitCamelCase()}* ");

        outString.AppendLine("| AC | HD | CR |");
        outString.AppendLine("| -- | -- | -- |");

        outString.Append($"| {monsterDefinition.ArmorClass} ");
        outString.Append($"| {monsterDefinition.HitDice:0#}{monsterDefinition.HitDiceType} ");
        outString.Append($"| {monsterDefinition.ChallengeRating} ");
        outString.Append('|');
        outString.AppendLine();

        outString.AppendLine();
        outString.AppendLine("| Str | Dex | Con | Int | Wis | Cha |");
        outString.AppendLine("| --- | --- | --- | --- | --- | --- |");

        outString.Append($"| {monsterDefinition.AbilityScores[0]:0#} ");
        outString.Append($"| {monsterDefinition.AbilityScores[1]:0#} ");
        outString.Append($"| {monsterDefinition.AbilityScores[2]:0#} ");
        outString.Append($"| {monsterDefinition.AbilityScores[3]:0#} ");
        outString.Append($"| {monsterDefinition.AbilityScores[4]:0#} ");
        outString.Append($"| {monsterDefinition.AbilityScores[5]:0#} ");
        outString.Append('|');
        outString.AppendLine();

        outString.AppendLine();
        outString.AppendLine("*Features:*");

        FeatureDefinitionCastSpell featureDefinitionCastSpell = null;

        foreach (var featureDefinition in monsterDefinition.Features)
        {
            switch (featureDefinition)
            {
                case FeatureDefinitionCastSpell definitionCastSpell:
                    featureDefinitionCastSpell = definitionCastSpell;
                    break;
                default:
                    outString.Append(GetMonsterFeatureBlock(featureDefinition));
                    break;
            }
        }

        if (featureDefinitionCastSpell != null)
        {
            outString.AppendLine();
            outString.AppendLine("*Spells:*");
            outString.AppendLine("| Level | Spell | Description |");
            outString.AppendLine("| ----- | ----- | ----------- |");

            foreach (var spellsByLevelDuplet in featureDefinitionCastSpell.SpellListDefinition.SpellsByLevel)
            {
                foreach (var spell in spellsByLevelDuplet.Spells)
                {
                    outString.AppendLine(
                        $"| {spellsByLevelDuplet.level} | {spell.FormatTitle()} | {spell.FormatDescription()} |");
                }
            }

            outString.AppendLine();
        }

        outString.AppendLine();
        outString.AppendLine("*Attacks:*");
        outString.AppendLine("| Type | Reach | Hit Bonus | Max Uses |");
        outString.AppendLine("| ---- | ----- | --------- | -------- |");

        foreach (var attackIteration in monsterDefinition.AttackIterations)
        {
            var title = attackIteration.MonsterAttackDefinition.FormatTitle();

            if (title == "None")
            {
                title = attackIteration.MonsterAttackDefinition.name.SplitCamelCase();
            }

            outString.Append($"| {title} ");
            outString.Append($"| {attackIteration.MonsterAttackDefinition.ReachRange} ");
            outString.Append($"| {attackIteration.MonsterAttackDefinition.ToHitBonus} ");
            outString.Append(attackIteration.MonsterAttackDefinition.MaxUses < 0
                ? "| 1 "
                : $"| {attackIteration.MonsterAttackDefinition.MaxUses} ");
            outString.Append('|');
            outString.AppendLine();
        }

        outString.AppendLine();
        outString.AppendLine("*Battle Decisions:*");
        outString.AppendLine("| Name | Weight | Cooldown |");
        outString.AppendLine("| ---- | ------ | -------- |");

        foreach (var weightedDecision in monsterDefinition.DefaultBattleDecisionPackage.Package.WeightedDecisions)
        {
            var name = weightedDecision.DecisionDefinition.ToString()
                .Replace("_", string.Empty)
                .Replace("(TA.AI.DecisionDefinition)", string.Empty)
                .SplitCamelCase();

            outString.AppendLine($"| {name} | {weightedDecision.Weight} | {weightedDecision.Cooldown} |");
        }

        outString.AppendLine();
        outString.AppendLine();

        return outString.ToString();
    }

    private static string GetMonsterFeatureBlock(BaseDefinition featureDefinition)
    {
        var outString = new StringBuilder();

        switch (featureDefinition)
        {
            case FeatureDefinitionFeatureSet featureDefinitionFeatureSet:
            {
                foreach (var featureDefinitionFromSet in featureDefinitionFeatureSet.FeatureSet)
                {
                    outString.Append(GetMonsterFeatureBlock(featureDefinitionFromSet));
                }

                break;
            }
            case FeatureDefinitionMoveMode featureDefinitionMoveMode:
                outString.Append("* ");
                outString.Append(featureDefinitionMoveMode.MoveMode);
                outString.Append(' ');
                outString.Append(featureDefinitionMoveMode.Speed);
                outString.AppendLine();

                break;
            case FeatureDefinitionLightAffinity featureDefinitionLightAffinity:
                foreach (var lightingEffectAndCondition in
                         featureDefinitionLightAffinity.LightingEffectAndConditionList)
                {
                    outString.AppendLine(
                        $"* {lightingEffectAndCondition.condition.FormatTitle()} - {lightingEffectAndCondition.lightingState}");
                }

                break;
            default:
                if (featureDefinition == null)
                {
                    break;
                }

                var title = featureDefinition.FormatTitle();

                if (title == "None")
                {
                    title = featureDefinition.Name.SplitCamelCase();
                }

                outString.Append("* ");
                outString.AppendLine(title);

                break;
        }

        return outString.ToString();
    }

    private static void ExpandColorTables()
    {
        //BUGFIX: expand color tables
        for (var i = 21; i < 33; i++)
        {
            Gui.ModifierColors.Add(i, new Color32(0, 164, byte.MaxValue, byte.MaxValue));
            Gui.CheckModifierColors.Add(i, new Color32(0, 36, 77, byte.MaxValue));
        }
    }

    private static void AddExtraTooltipDefinitions()
    {
        if (ServiceRepository.GetService<IGuiService>() is not GuiManager gui)
        {
            return;
        }

        var definition = gui.tooltipClassDefinitions[GuiFeatDefinition.tooltipClass];

        var index = definition.tooltipFeatures.FindIndex(f =>
            f.scope == TooltipDefinitions.Scope.All &&
            f.featurePrefab.GetComponent<TooltipFeature>() is TooltipFeaturePrerequisites);

        if (index >= 0)
        {
            var custom = GuiTooltipClassDefinitionBuilder
                .Create(gui.tooltipClassDefinitions["ItemDefinition"], CustomItemTooltipProvider.ItemWithPreReqsTooltip)
                .SetGuiPresentationNoContent()
                .AddTooltipFeature(definition.tooltipFeatures[index])
                //TODO: figure out why only background widens, but not content
                // .SetPanelWidth(400f) //items have 340f by default
                .AddToDB();

            gui.tooltipClassDefinitions.Add(custom.Name, custom);
        }

        //make condition description visible on both modes
        definition = gui.tooltipClassDefinitions[GuiActiveCondition.tooltipClass];
        index = definition.tooltipFeatures.FindIndex(f =>
            f.scope == TooltipDefinitions.Scope.Simplified &&
            f.featurePrefab.GetComponent<TooltipFeature>() is TooltipFeatureDescription);

        if (index < 0)
        {
            return;
        }

        //since FeatureInfo is a struct we get here a copy
        var info = definition.tooltipFeatures[index];
        //modify it
        info.scope = TooltipDefinitions.Scope.All;
        //and then put copy back
        definition.tooltipFeatures[index] = info;
    }

    private static void Load()
    {
        LatestVersion = GetLatestVersion(out var shouldUpdate);

        if (shouldUpdate && !Main.Settings.DisableUpdateMessage)
        {
            DisplayUpdateMessage();
        }
        else if (!Main.Settings.HideWelcomeMessage)
        {
            DisplayWelcomeMessage();

            Main.Settings.HideWelcomeMessage = true;
        }
    }

    private static string GetInstalledVersion()
    {
        var infoPayload = File.ReadAllText(Path.Combine(Main.ModFolder, "Info.json"));
        var infoJson = JsonConvert.DeserializeObject<JObject>(infoPayload);

        // ReSharper disable once AssignNullToNotNullAttribute
        return infoJson["Version"].Value<string>();
    }

    private static string GetPreviousVersion()
    {
        var a1 = InstalledVersion.Split('.');
        var minor = Int32.Parse(a1[3]);

        a1[3] = (--minor).ToString();

        // ReSharper disable once AssignNullToNotNullAttribute
        return string.Join(".", a1);
    }

    private static string GetLatestVersion(out bool shouldUpdate)
    {
        var version = "";

        shouldUpdate = false;

        using var wc = new WebClient();

        wc.Encoding = Encoding.UTF8;

        try
        {
            var infoPayload = wc.DownloadString($"{BaseURL}/Info.json");
            var infoJson = JsonConvert.DeserializeObject<JObject>(infoPayload);

            // ReSharper disable once AssignNullToNotNullAttribute
            version = infoJson["Version"].Value<string>();

            var a1 = InstalledVersion.Split('.');
            var a2 = version.Split('.');
            var v1 = a1[0] + a1[1] + a1[2] + Int32.Parse(a1[3]).ToString("D3");
            var v2 = a2[0] + a2[1] + a2[2] + Int32.Parse(a2[3]).ToString("D3");

            shouldUpdate = String.Compare(v2, v1, StringComparison.Ordinal) > 0;
        }
        catch
        {
            Main.Error("cannot fetch update data.");
        }

        return version;
    }

    internal static void UpdateMod(bool toLatest = true)
    {
        UnityModManager.UI.Instance.ToggleWindow(false);

        var version = toLatest ? LatestVersion : PreviousVersion;
        var destFiles = new[] { "Info.json", "SolastaUnfinishedBusiness.dll" };

        using var wc = new WebClient();

        wc.Encoding = Encoding.UTF8;

        string message;
        var zipFile = $"SolastaUnfinishedBusiness-{version}.zip";
        var fullZipFile = Path.Combine(Main.ModFolder, zipFile);
        var fullZipFolder = Path.Combine(Main.ModFolder, "SolastaUnfinishedBusiness");
        var baseUrlByVersion = BaseURL.Replace("latest/download", $"download/{version}");
        var url = $"{baseUrlByVersion}/{zipFile}";

        try
        {
            wc.DownloadFile(url, fullZipFile);

            if (Directory.Exists(fullZipFolder))
            {
                Directory.Delete(fullZipFolder, true);
            }

            ZipFile.ExtractToDirectory(fullZipFile, Main.ModFolder);
            File.Delete(fullZipFile);

            foreach (var destFile in destFiles)
            {
                var fullDestFile = Path.Combine(Main.ModFolder, destFile);

                File.Delete(fullDestFile);
                File.Move(
                    Path.Combine(fullZipFolder, destFile),
                    fullDestFile);
            }

            Directory.Delete(fullZipFolder, true);

            message = "Mod version change successful. Please restart.";
        }
        catch
        {
            message = $"Cannot fetch update payload. Try again or download from:\r\n{url}.";
        }

        Gui.GuiService.ShowMessage(
            MessageModal.Severity.Attention2,
            "Message/&MessageModWelcomeTitle",
            message,
            "Donate",
            "Message/&MessageOkTitle",
            OpenDonatePayPal,
            () => { });
    }

    internal static void DisplayRollbackMessage()
    {
        UnityModManager.UI.Instance.ToggleWindow(false);

        Gui.GuiService.ShowMessage(
            MessageModal.Severity.Attention2,
            "Message/&MessageModWelcomeTitle",
            $"Would you like to rollback to {PreviousVersion}?",
            "Message/&MessageOkTitle",
            "Message/&MessageCancelTitle",
            () => UpdateMod(false),
            () => { });
    }

    private static void DisplayUpdateMessage()
    {
        const string MESSAGE =
            "\n\nDedico este mod a galera que me introduziu ao D&D nos bons anos 80:\n\nDentinho, Dumbo, Jelly, Leo & Dani, Marcio, Marcelo Padeiro e tantos outros\n[Colegios Andrews / Santo Agostinho, Rio de Janeiro 1986-1988]";

        Gui.GuiService.ShowMessage(
            MessageModal.Severity.Attention2,
            "Message/&MessageModWelcomeTitle",
            $"Version {LatestVersion} is now available. Open Mod UI > Gameplay > Tools to update.{MESSAGE}",
            "Changelog",
            "Message/&MessageOkTitle",
            OpenChangeLog,
            () => { });
    }

    private static void DisplayWelcomeMessage()
    {
        Gui.GuiService.ShowMessage(
            MessageModal.Severity.Attention2,
            "Message/&MessageModWelcomeTitle",
            "Message/&MessageModWelcomeDescription",
            "Donate",
            "Message/&MessageOkTitle",
            OpenDonatePayPal,
            () => { });
    }

    internal static void OpenDonateGithub()
    {
        OpenUrl("https://github.com/sponsors/ThyWoof");
    }

    internal static void OpenDonatePatreon()
    {
        OpenUrl("https://patreon.com/SolastaMods");
    }

    internal static void OpenDonatePayPal()
    {
        OpenUrl("https://www.paypal.com/donate/?business=JG4FX47DNHQAG&item_name=Support+Solasta+Unfinished+Business");
    }

    internal static void OpenChangeLog()
    {
        OpenUrl(
            "https://raw.githubusercontent.com/SolastaMods/SolastaUnfinishedBusiness/master/SolastaUnfinishedBusiness/ChangelogHistory.txt");
    }

    internal static void OpenDocumentation(string filename)
    {
        OpenUrl($"file://{Main.ModFolder}/Documentation/{filename}");
    }

    private static void OpenUrl(string url)
    {
        try
        {
            Process.Start(url);
        }
        catch
        {
            // because of this: https://github.com/dotnet/corefx/issues/10361
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                url = url.Replace("&", "^&");
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                Process.Start("xdg-open", url);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                Process.Start("open", url);
            }
            else
            {
                throw;
            }
        }
    }
}
