using System.IO;
using System.Text;
using BehaviorTreeMainProject.Log.Services;
using BehaviorTreeMainProject.Services.AIPlanning;
using BehaviorTreeMainProject.Services.AIPlanning.Replan;

namespace BehaviorTreeMainProject.Decorators.Replan
{
    /// <summary>
    /// Regenerates the ML PDDL problem file each tick from the current
    /// blackboard state, using the ORIGINAL (static) problem file as the
    /// objects/structure source plus any extras contributed by earlier
    /// decorators (e.g. <see cref="DecoratorExtraPDDLObjects"/>).
    ///
    /// Only runs when the owning flow node is an ML subtree (i.e. has a
    /// non-null <c>ctx.ParentAction</c>). HL-level regeneration is handled
    /// by <see cref="DecoratorHLStatePatch"/>.
    /// </summary>
    public class DecoratorRegenerateFromBlackboard : Decorator, IReplanModifier
    {
        public override bool CanPostProcessTickResult => false;
        public override BTNodeResult PostProcessTickResult(BTNodeResult InResult) => InResult;
        protected override bool OnEvaluate(float InDeltaTime) => true;

        public PrePlanResult PrePlan(PDDLPlanningContext ctx) => PrePlanResult.Proceed;

        public void ApplyModifications(PDDLPlanningContext ctx)
        {
            if (ctx.ProblemFileRegenerated) return;
            if (ctx.ParentAction == null || ctx.Blackboard == null) return;
            if (ctx.PlanningRequest == null) return;

            string originalProblemFile = ctx.OriginalProblemFile ?? ctx.PlanningRequest.ProblemFile;
            string newProblemFile = ServicePDDLPlanning.GenerateDynamicPDDLProblem(
                ctx.ParentAction, ctx.Blackboard, originalProblemFile, ctx.ExtraObjects);

            ctx.PlanningRequest.ProblemFile = newProblemFile;

            // Push the freshly-written file content inline so remote planner
            // services don't need to read a path that only exists locally.
            string localPath = $"python_service/Plannerinputs/generated/{Path.GetFileName(newProblemFile)}";
            if (File.Exists(localPath))
                ctx.PlanningRequest.ProblemFileContent = File.ReadAllText(localPath, Encoding.UTF8);

            ctx.ProblemFileRegenerated = true;
            LoggingService.LogInfo(
                $"🔄 DecoratorRegenerateFromBlackboard: Regenerated problem file for re-plan: {newProblemFile}");
        }
    }
}
