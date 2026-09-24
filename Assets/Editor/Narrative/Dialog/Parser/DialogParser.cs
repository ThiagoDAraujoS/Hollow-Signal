﻿using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Data;

namespace Editor.Dialog.Parser{
    /// High-performance recursive descent parser compiling raw .dialog script files into DialogScriptAst.
    public static class DialogParser{
        private static readonly Regex VarRegex = new(
            @"^VAR\s+(local|map|global)\s+(bool|int|float|string)\s+([a-zA-Z_][a-zA-Z0-9_]*)\s*=\s*(.+)$",
            RegexOptions.Compiled
        );

        private static readonly Regex KnotRegex = new(
            @"^===\s*KNOT:\s*([a-zA-Z0-9_]+)\s*===$",
            RegexOptions.Compiled
        );

        private static readonly Regex SpeakerRegex = new(
            @"^([A-Z0-9_]+)(?:\s*\[portrait:\s*([a-zA-Z0-9_]+)\])?:\s*(.+)$",
            RegexOptions.Compiled
        );

        private static readonly Regex ChoiceRegex = new(
            @"^(\*|\+)\s*(?:\{([^}]+)\}\s*)?\[([^\]]+)\](?:\s*\(([a-zA-Z0-9_]+)\))?\s*->\s*([a-zA-Z0-9_]+)$",
            RegexOptions.Compiled
        );

        private static readonly Regex ItemReqRegex = new(
            @"\(item:\s*([a-zA-Z0-9_]+)(?:\\s*x\\s*([0-9]+))?\)",
            RegexOptions.Compiled | RegexOptions.IgnoreCase
        );

        private static readonly Regex SkillCheckRegex = new(
            @"^~\s*SkillCheck\s*\(\s*([a-zA-Z0-9_]+)\s*,\s*([0-9]+)\s*\)$",
            RegexOptions.Compiled
        );

        private static readonly Regex ProblemRegex = new(
            @"^~\s*Problem\s*\(\s*([a-zA-Z0-9_]+)\s*[:,\s]\s*([0-9]+)\s*\)$",
            RegexOptions.Compiled
        );

        private static readonly Regex OutcomeHeaderRegex = new(
            @"^-\s*(SUCCESS|FAILURE|CRITICAL_SUCCESS|CRITICAL_FAILURE)\s*->\s*$",
            RegexOptions.Compiled
        );

        /// Parses the entire text content of a .dialog file into a validated DialogScriptAst.
        public static DialogScriptAst Parse(string scriptName, string scriptContent){
            DialogScriptAst ast = new() { scriptName = scriptName };
            string[] rawLines = scriptContent.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);

            DialogKnotDef currentKnot = null;
            DialogOutcomeDef currentOutcome = null;
            int choiceCounter = 0;

            for (int lineIndex = 0; lineIndex < rawLines.Length; lineIndex++){
                int lineNumber = lineIndex + 1;
                string line = StripCommentsAndTrim(rawLines[lineIndex]);

                if (string.IsNullOrEmpty(line)) continue;

                // 1. Map Header (MAP: MapName)
                if (line.StartsWith("MAP:", StringComparison.OrdinalIgnoreCase)){
                    ast.mapName = line.Substring(4).Trim();
                    continue;
                }

                // 2. Default Speaker Header (DEFAULT_SPEAKER: SpeakerId)
                if (line.StartsWith("DEFAULT_SPEAKER:", StringComparison.OrdinalIgnoreCase)){
                    ast.defaultSpeaker = line.Substring(16).Trim();
                    continue;
                }

                // 3. Localization File Header (LOC_FILE: FileName)
                if (line.StartsWith("LOC_FILE:", StringComparison.OrdinalIgnoreCase)){
                    ast.locFileName = line.Substring(9).Trim();
                    continue;
                }

                // 4. Variable Declaration (VAR <scope> <type> <name> = <val>)
                if (line.StartsWith("VAR ", StringComparison.Ordinal)){
                    ParseVariable(ast, line, lineNumber, scriptName);
                    continue;
                }

                // 5. Knot Header (=== KNOT: KnotName ===)
                Match knotMatch = KnotRegex.Match(line);
                if (knotMatch.Success){
                    string knotId = knotMatch.Groups[1].Value;
                    currentKnot = new DialogKnotDef { knotId = knotId };
                    ast.knots.Add(currentKnot);
                    currentOutcome = null;
                    choiceCounter = 0;
                    continue;
                }

                if (currentKnot == null)
                    throw new FormatException($"[{scriptName}:{lineNumber}] Unexpected text found outside of any knot block: '{line}'");

                // 6. Outcome Branch Header (- SUCCESS -> etc.)
                Match outcomeHeaderMatch = OutcomeHeaderRegex.Match(line);
                if (outcomeHeaderMatch.Success){
                    if (currentKnot.skillCheck == null && currentKnot.problem == null)
                        throw new FormatException($"[{scriptName}:{lineNumber}] Outcome branch defined without preceding ~ SkillCheck or ~ Problem: '{line}'");

                    string outcomeType = outcomeHeaderMatch.Groups[1].Value;
                    currentOutcome = new DialogOutcomeDef();
                    if (currentKnot.problem != null)
                        AssignProblemOutcome(currentKnot.problem, outcomeType, currentOutcome, lineNumber, scriptName);
                    else
                        AssignOutcome(currentKnot.skillCheck, outcomeType, currentOutcome, lineNumber, scriptName);
                    continue;
                }

                // 7. Outcome Body Lines (Indented or inside an outcome branch)
                if (currentOutcome != null){
                    if (line.StartsWith("->", StringComparison.Ordinal)){
                        currentOutcome.targetKnot = line.Substring(2).Trim();
                        currentOutcome = null;
                        continue;
                    }

                    if (line.StartsWith("~", StringComparison.Ordinal)){
                        currentOutcome.command = line.Substring(1).Trim();
                        continue;
                    }

                    Match outcomeSpeakerMatch = SpeakerRegex.Match(line);
                    if (outcomeSpeakerMatch.Success){
                        currentOutcome.speakerId = outcomeSpeakerMatch.Groups[1].Value;
                        currentOutcome.text = outcomeSpeakerMatch.Groups[3].Value;
                        continue;
                    }
                }

                // 8. Problem Declaration (~ Problem(Door: 5))
                Match problemMatch = ProblemRegex.Match(line);
                if (problemMatch.Success){
                    string archetypeId = problemMatch.Groups[1].Value;
                    int level = int.Parse(problemMatch.Groups[2].Value);
                    currentKnot.problem = new DialogProblemDef{
                        archetypeId = archetypeId,
                        baseLevel = level
                    };
                    continue;
                }

                // 9. Skill Check Declaration (~ SkillCheck(SkillName, DC))
                Match skillMatch = SkillCheckRegex.Match(line);
                if (skillMatch.Success){
                    string skillName = skillMatch.Groups[1].Value;
                    if (!Enum.TryParse(skillName, out Skill parsedSkill))
                        throw new FormatException($"[{scriptName}:{lineNumber}] Unknown skill identifier '{skillName}' in SkillCheck. Does not match Data.Skill enum.");

                    int dc = int.Parse(skillMatch.Groups[2].Value);
                    currentKnot.skillCheck = new DialogSkillCheckDef {
                        skill = parsedSkill,
                        targetDc = dc
                    };
                    continue;
                }

                // 10. Choices (* or +)
                if (line.StartsWith("*", StringComparison.Ordinal) || line.StartsWith("+", StringComparison.Ordinal)){
                    ParseChoice(currentKnot, line, lineNumber, scriptName, ref choiceCounter);
                    continue;
                }

                // 11. Spoken Prompt Lines (SPEAKER: Prompt)
                Match speakerMatch = SpeakerRegex.Match(line);
                if (speakerMatch.Success){
                    currentKnot.speakerId = speakerMatch.Groups[1].Value;
                    currentKnot.portraitMood = speakerMatch.Groups[2].Value;
                    currentKnot.promptText = speakerMatch.Groups[3].Value;
                    continue;
                }

                // 12. In-line Commands (~ SET, ~ EndCrisisTurn, etc.)
                if (line.StartsWith("~", StringComparison.Ordinal)){
                    currentKnot.inLineCommands.Add(line.Substring(1).Trim());
                    continue;
                }

                throw new FormatException($"[{scriptName}:{lineNumber}] Unrecognized syntax: '{line}'");
            }

            ValidateAst(ast);
            return ast;
        }

        /// Parses a strongly typed variable declaration into the script AST.
        private static void ParseVariable(DialogScriptAst ast, string line, int lineNumber, string scriptName){
            Match match = VarRegex.Match(line);
            if (!match.Success)
                throw new FormatException($"[{scriptName}:{lineNumber}] Malformed VAR declaration: '{line}'. Expected syntax: VAR <local|map|global> <bool|int|float|string> <name> = <default>");

            ast.variables.Add(new DialogVarDef {
                scope = match.Groups[1].Value,
                typeName = match.Groups[2].Value,
                name = match.Groups[3].Value,
                defaultValue = match.Groups[4].Value.Trim()
            });
        }

        /// Parses a choice line into a DialogChoiceDef AST node, extracting item gates and tactical tags.
        private static void ParseChoice(DialogKnotDef knot, string line, int lineNumber, string scriptName, ref int choiceCounter){
            Match match = ChoiceRegex.Match(line);
            if (!match.Success)
                throw new FormatException($"[{scriptName}:{lineNumber}] Malformed choice syntax: '{line}'. Expected syntax: * or + {{optional_condition}} [Choice Text](OptionalEvent) -> TargetKnot");

            bool isOneShot = match.Groups[1].Value == "*";
            string rawCondition = match.Groups[2].Success ? match.Groups[2].Value.Trim() : null;
            string rawText = match.Groups[3].Value.Trim();
            string eventName = match.Groups[4].Success ? match.Groups[4].Value.Trim() : null;
            string targetKnot = match.Groups[5].Value.Trim();

            string requiredItem = null;
            int itemAmount = 0;

            Match itemMatch = ItemReqRegex.Match(rawText);
            if (itemMatch.Success){
                requiredItem = itemMatch.Groups[1].Value;
                itemAmount = itemMatch.Groups[2].Success ? int.Parse(itemMatch.Groups[2].Value) : 1;
                rawText = ItemReqRegex.Replace(rawText, "").Trim();
            }

            bool endsTurn = false;
            bool consumesAction = false;
            bool consumesMove = false;

            if (rawText.Contains("<!!>")){
                endsTurn = true;
                rawText = rawText.Replace("<!!>", "").Trim();
            }
            else if (rawText.Contains("<!>")){
                consumesAction = true;
                rawText = rawText.Replace("<!>", "").Trim();
            }

            Match moveMatch = Regex.Match(rawText, @"<[mM]>");
            if (moveMatch.Success){
                consumesMove = true;
                rawText = Regex.Replace(rawText, @"<[mM]>", "").Trim();
            }

            string choiceId = $"{knot.knotId.ToUpperInvariant()}_C{choiceCounter++:D2}";

            knot.choices.Add(new DialogChoiceDef {
                choiceId = choiceId,
                isOneShot = isOneShot,
                condition = rawCondition,
                text = rawText,
                eventName = eventName,
                requiredItemId = requiredItem,
                requiredItemAmount = itemAmount,
                targetKnot = targetKnot,
                consumesAction = consumesAction,
                endsTurn = endsTurn,
                consumesMove = consumesMove
            });
        }

        /// Assigns outcome definition to the appropriate skill check branch.
        private static void AssignOutcome(DialogSkillCheckDef check, string type, DialogOutcomeDef outcome, int lineNumber, string scriptName){
            switch (type){
                case "SUCCESS":
                    check.onSuccess = outcome;
                    break;
                case "FAILURE":
                    check.onFailure = outcome;
                    break;
                case "CRITICAL_SUCCESS":
                    check.onCriticalSuccess = outcome;
                    break;
                case "CRITICAL_FAILURE":
                    check.onCriticalFailure = outcome;
                    break;
                default:
                    throw new FormatException($"[{scriptName}:{lineNumber}] Unknown skill check outcome branch '{type}'.");
            }
        }

        /// Assigns outcome definition to the appropriate problem outcome branch.
        private static void AssignProblemOutcome(DialogProblemDef problem, string type, DialogOutcomeDef outcome, int lineNumber, string scriptName){
            switch (type){
                case "SUCCESS":
                    problem.onSuccess = outcome;
                    break;
                case "FAILURE":
                    problem.onFailure = outcome;
                    break;
                default:
                    throw new FormatException($"[{scriptName}:{lineNumber}] Unknown problem outcome branch '{type}'. Expected SUCCESS or FAILURE.");
            }
        }

        /// Validates knot structure, uniqueness, and destination targets.
        private static void ValidateAst(DialogScriptAst ast){
            if (ast.knots.Count == 0)
                throw new FormatException($"[{ast.scriptName}] Script does not define any knots. Must contain at least one '=== KNOT: KnotName ===' block.");

            HashSet<string> definedKnotIds = new(StringComparer.Ordinal);
            foreach (DialogKnotDef knot in ast.knots){
                if (!definedKnotIds.Add(knot.knotId))
                    throw new FormatException($"[{ast.scriptName}] Duplicate knot ID detected: '{knot.knotId}'. Knot names must be unique.");
            }

            foreach (DialogKnotDef knot in ast.knots){
                foreach (DialogChoiceDef choice in knot.choices)
                    if (choice.targetKnot != "END" && !definedKnotIds.Contains(choice.targetKnot))
                        throw new FormatException($"[{ast.scriptName}] Knot '{knot.knotId}' has choice targeting non-existent knot '{choice.targetKnot}'.");

                if (knot.skillCheck != null){
                    ValidateOutcomeKnot(ast.scriptName, knot.knotId, "SUCCESS", knot.skillCheck.onSuccess, definedKnotIds);
                    ValidateOutcomeKnot(ast.scriptName, knot.knotId, "FAILURE", knot.skillCheck.onFailure, definedKnotIds);
                }

                if (knot.problem != null){
                    ValidateOutcomeKnot(ast.scriptName, knot.knotId, "SUCCESS", knot.problem.onSuccess, definedKnotIds);
                    ValidateOutcomeKnot(ast.scriptName, knot.knotId, "FAILURE", knot.problem.onFailure, definedKnotIds);
                }
            }
        }

        /// Validates that an outcome target knot exists in the script AST.
        private static void ValidateOutcomeKnot(string scriptName, string knotId, string outcomeName, DialogOutcomeDef outcome, HashSet<string> definedKnots){
            if (outcome == null) return;
            if (!string.IsNullOrEmpty(outcome.targetKnot) && outcome.targetKnot != "END" && !definedKnots.Contains(outcome.targetKnot))
                throw new FormatException($"[{scriptName}] Knot '{knotId}' outcome '{outcomeName}' targets non-existent knot '{outcome.targetKnot}'.");
        }

        /// Removes inline comment text and trims whitespace.
        private static string StripCommentsAndTrim(string line){
            int commentIndex = line.IndexOf("//", StringComparison.Ordinal);
            if (commentIndex >= 0)
                line = line.Substring(0, commentIndex);
            return line.Trim();
        }
    }
}
