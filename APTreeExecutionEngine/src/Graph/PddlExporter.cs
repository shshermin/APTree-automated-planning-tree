using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using BehaviorTreeMainProject.Log.Services;

namespace BehaviorTreeMainProject.Graph
{
    /// <summary>
    /// Generates well-formed PDDL problem files directly from the predicate
    /// store and the blackboard's entity collections.
    ///
    /// Usage (simplest form)
    /// ─────────────────────
    ///   var exporter = new PddlExporter(blackboard);
    ///   string pddl  = exporter.GenerateProblem(domainName: "trussml",
    ///                                            problemName: "my_problem");
    ///   File.WriteAllText("problem.pddl", pddl);
    ///
    /// Isaac Sim integration
    /// ──────────────────────
    /// Call IngestFromIsaac(bridge) on a SqlitePredicateStore before calling
    /// GenerateProblem() to let the sim ground-truth drive PDDL generation
    /// without touching the BT engine.
    /// </summary>
    public sealed class PddlExporter
    {
        private readonly Blackboard<FastName> _bb;

        public PddlExporter(Blackboard<FastName> blackboard)
        {
            _bb = blackboard ?? throw new ArgumentNullException(nameof(blackboard));
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Generates a complete PDDL problem file from the current blackboard
        /// state.  All sections are derived automatically:
        ///
        ///   :objects — every entity registered on the blackboard
        ///   :init    — all non-negated predicates in the init store
        ///   :goal    — all predicates in the goal store (wrapped in (and …))
        ///
        /// Optionally write the result to <paramref name="outputPath"/>.
        /// </summary>
        public string GenerateProblem(
            string domainName,
            string problemName,
            string? outputPath = null,
            IReadOnlyDictionary<string, string>? extraObjects = null)
        {
            var sb = new StringBuilder();

            sb.AppendLine($"(define (problem {problemName.ToLowerInvariant()})");
            sb.AppendLine($"  (:domain {domainName.ToLowerInvariant()})");
            sb.AppendLine();

            AppendObjects(sb, extraObjects);
            sb.AppendLine();
            AppendInit(sb);
            sb.AppendLine();
            AppendGoal(sb);
            sb.AppendLine();
            sb.AppendLine(")");

            string content = sb.ToString();

            if (!string.IsNullOrEmpty(outputPath))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
                File.WriteAllText(outputPath, content, Encoding.UTF8);
                LoggingService.LogInfo($"[PddlExporter] Written to {outputPath}");
            }

            LoggingService.LogInfo(
                $"[PddlExporter] Generated problem '{problemName}' — " +
                $"{_bb.GetTruePredicates().Count} init, " +
                $"{_bb.GetGoalStatePredicates().Count} goal predicates");

            return content;
        }

        /// <summary>
        /// Generates only the inner content of the (:init …) block from the
        /// store's non-negated predicates, optionally filtered to declared objects.
        /// Ready for injection into an existing file.
        /// </summary>
        public string GenerateInitBlock(ISet<string>? declaredObjects = null)
        {
            return RenderPredicateList(_bb.GetTruePredicates(), declaredObjects);
        }

        /// <summary>
        /// Generates only the inner content of the (:goal (and …)) block,
        /// optionally filtered to declared objects.
        /// </summary>
        public string GenerateGoalBlock(ISet<string>? declaredObjects = null)
        {
            return RenderPredicateList(_bb.GetGoalStatePredicates(), declaredObjects);
        }

        // ── Section builders ──────────────────────────────────────────────────

        private void AppendObjects(StringBuilder sb, IReadOnlyDictionary<string, string>? extras)
        {
            sb.AppendLine("  (:objects");

            AppendEntityGroup(sb, _bb.GetAllAgents(),    a => (a.NameKey!.ToString(), AgentPddlType(a)));
            AppendEntityGroup(sb, _bb.GetAllElements(),  e => (e.NameKey!.ToString(), e.GetType().Name.ToLowerInvariant()));
            AppendEntityGroup(sb, _bb.GetAllLocations(), l => (l.NameKey!.ToString(), l.GetType().Name.ToLowerInvariant()));
            AppendEntityGroup(sb, _bb.GetAllTools(),     t => (t.NameKey!.ToString(), t.GetType().Name.ToLowerInvariant()));
            AppendEntityGroup(sb, _bb.GetAllLayers(),    l => (l.NameKey!.ToString(), "layer"));
            AppendEntityGroup(sb, _bb.GetAllModules(),   m => (m.NameKey!.ToString(), "module"));

            if (extras != null)
                foreach (var kv in extras)
                    sb.AppendLine($"    {kv.Key} - {kv.Value}");

            sb.AppendLine("  )");
        }

        private static void AppendEntityGroup<TEntity>(
            StringBuilder sb,
            IEnumerable<TEntity> entities,
            Func<TEntity, (string name, string type)> selector)
        {
            var groups = entities
                .Select(selector)
                .Where(t => !string.IsNullOrEmpty(t.name))
                .GroupBy(t => t.type);

            foreach (var grp in groups)
            {
                var names = string.Join(" ", grp.Select(t => t.name));
                sb.AppendLine($"    {names} - {grp.Key}");
            }
        }

        private void AppendInit(StringBuilder sb)
        {
            sb.AppendLine("  (:init");
            foreach (var line in _bb.GetTruePredicates().Select(PredicateToPddl).Where(s => !string.IsNullOrEmpty(s)))
                sb.AppendLine($"    {line}");
            sb.AppendLine("  )");
        }

        private void AppendGoal(StringBuilder sb)
        {
            sb.AppendLine("  (:goal");
            sb.AppendLine("    (and");
            foreach (var line in _bb.GetGoalStatePredicates().Select(PredicateToPddl).Where(s => !string.IsNullOrEmpty(s)))
                sb.AppendLine($"      {line}");
            sb.AppendLine("    )");
            sb.AppendLine("  )");
        }

        // ── Predicate rendering ───────────────────────────────────────────────

        private static string RenderPredicateList(IEnumerable<Predicate> predicates, ISet<string>? filter)
        {
            var lines = predicates.Select(PredicateToPddl).Where(s => !string.IsNullOrEmpty(s));
            if (filter != null)
                lines = lines.Where(line => PredicateReferencesOnlyDeclared(line, filter));
            return string.Join("\n", lines);
        }

        private static string PredicateToPddl(Predicate p)
        {
            try
            {
                var pv = p.GetPDDLParameterValues();
                string inner = pv.Count == 0
                    ? $"({p.PredicateTypeName})"
                    : $"({p.PredicateTypeName} {string.Join(" ", pv)})";
                return p.not ? $"(not {inner})" : inner;
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"[PddlExporter] Could not render predicate {p?.PredicateTypeName}: {ex.Message}");
                return string.Empty;
            }
        }

        private static bool PredicateReferencesOnlyDeclared(string pddlLine, ISet<string> declared)
        {
            var inner = pddlLine.Trim();
            if (inner.StartsWith("(not ")) inner = inner[5..^1].Trim();
            if (inner.StartsWith("(") && inner.EndsWith(")")) inner = inner[1..^1].Trim();
            var tokens = inner.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            return tokens.Skip(1).All(t => declared.Contains(t));
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private static string AgentPddlType(Agent agent) =>
            agent.GetType().Name.ToLowerInvariant() switch
            {
                "agent" => "robot",
                var n   => n
            };
    }
}
