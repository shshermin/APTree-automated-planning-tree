using System.Collections.Generic;

/// <summary>
/// Bridge interface for ingesting world-state predicates from NVIDIA Isaac Sim.
///
/// Isaac Sim's USD stage is itself a scene graph.  Implement this interface to
/// map USD prims/relationships (poses, "on", "in", gripper-holds) onto the
/// predicate-store schema so that simulator ground-truth and BT belief state
/// share one model.
///
/// Workflow
/// ────────
///   1. Implement GetPredicates() using the omni.isaac.core / Isaac SDK APIs
///      to read the current USD stage state.
///   2. Call SqlitePredicateStore.IngestFromIsaac(bridge, bb.SetPredicateSync)
///      before each BT tick to keep the store in sync with the simulation.
///   3. GetEntityTypes() lets the PddlExporter generate a correct :objects block
///      without manual object declarations.
///
/// The interface is intentionally thin so that it can be satisfied from:
///   • Python interop (via IronPython / pythonnet calling a stub)
///   • A REST/gRPC Isaac extension that serialises the scene on request
///   • A stub implementation for offline testing
/// </summary>
public interface IIsaacSceneBridge
{
    /// <summary>
    /// Returns all predicates currently true in the Isaac Sim scene.
    /// Each predicate is paired with the FastName key under which it should
    /// be stored in the predicate store.
    /// </summary>
    IEnumerable<(FastName key, Predicate predicate)> GetPredicates();

    /// <summary>
    /// Returns (entityName, pddlType) pairs for every entity visible in the
    /// scene that should appear in the PDDL :objects block.
    /// </summary>
    IEnumerable<(string name, string pddlType)> GetEntityTypes();
}
