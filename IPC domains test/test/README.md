# IPC HTN Transport Test

This folder contains the partial-order Transport benchmark from the IPC 2020 Hierarchical Planning Competition.

The HDDL files are in the `HTN problems and domain` subfolder:

- `HTN problems and domain/domain.hddl` defines the world, available actions, high-level tasks, and decomposition methods.
- `HTN problems and domain/pfile01.hddl` through `pfile40.hddl` define individual planning problems of increasing size.

## What the planner must do

The world contains:

- packages that must be delivered;
- vehicles that transport packages;
- locations connected by roads;
- symbolic vehicle-capacity levels.

Each problem specifies where every package and vehicle starts. Its HTN task network then requests deliveries such as:

```lisp
(deliver package-0 city-loc-0)
```

This means: make `package-0` end up at `city-loc-0`.

The planner must choose a vehicle, find a route to the package, load it, find a route to the destination, and unload it. It must respect the road graph and the vehicle's available capacity throughout the plan.

## The hierarchy

A requested `deliver` task is not an executable action. It is a compound task that is decomposed into four smaller tasks:

1. Move a vehicle to the package's current location.
2. Load the package into that vehicle.
3. Move the vehicle to the requested destination.
4. Unload the package.

In HDDL, this decomposition is approximately:

```text
deliver(package, destination)
  -> get-to(vehicle, package-location)
  -> load(vehicle, package-location, package)
  -> get-to(vehicle, destination)
  -> unload(vehicle, destination, package)
```

The `get-to` task is recursive. If the destination is not directly connected to the vehicle's current location, the planner chooses an intermediate location, solves another `get-to` task, and then performs a `drive` action. This is how it constructs a route through the road network.

`load` and `unload` decompose directly into the primitive `pick-up` and `drop` actions.

## Primitive actions

The final plan contains only actions that change the world:

- `drive`: moves a vehicle along one road edge;
- `pick-up`: removes a package from a location and puts it in a vehicle;
- `drop`: removes a package from a vehicle and puts it at a location;
- `noop`: represents that a vehicle is already at the requested location.

A plan is valid only when every action's preconditions hold. For example, a package can be picked up only when the package and vehicle are at the same location and the vehicle has free capacity.

## Capacity representation

Capacity is symbolic rather than numeric. Predicates such as:

```lisp
(capacity-predecessor capacity-0 capacity-1)
(capacity truck-0 capacity-1)
```

mean that `truck-0` currently has one free capacity unit. Picking up a package changes its capacity from `capacity-1` to `capacity-0`; dropping the package changes it back. Larger problems can define additional capacity levels in the same way.

This benchmark therefore does not require numeric fluents. It encodes capacity using ordinary Boolean predicates.

## Example: pfile01.hddl

The first problem contains:

- two packages: `package-0` and `package-1`;
- one truck: `truck-0`;
- three locations: `city-loc-0`, `city-loc-1`, and `city-loc-2`;
- one unit of truck capacity;
- roads connecting location 0 to 1 and location 1 to 2 in both directions.

Initially:

- both packages are at `city-loc-1`;
- the truck is at `city-loc-2`;
- `package-0` must be delivered to `city-loc-0`;
- `package-1` must be delivered to `city-loc-2`.

The problem does not impose an order between the two delivery tasks. The planner may choose their order as long as both are completed. One valid high-level execution handles package 0 first:

1. Drive the truck from location 2 to location 1.
2. Pick up package 0.
3. Drive from location 1 to location 0.
4. Drop package 0.
5. Drive from location 0 back to location 1.
6. Pick up package 1.
7. Drive from location 1 to location 2.
8. Drop package 1.

The HTN planner discovers the primitive action sequence by repeatedly selecting applicable decomposition methods and binding their parameters to concrete packages, vehicles, capacities, and locations.

## Why the benchmark becomes difficult

The later problem files contain more packages, vehicles, locations, roads, and delivery tasks. Growth creates choices about:

- which vehicle should deliver each package;
- which route each vehicle should follow;
- how capacity changes affect later actions;
- which applicable HTN method and parameter binding should be selected;
- how ordered or partially ordered delivery tasks interact through shared vehicles.

A monolithic HTN planner considers the complete task network in one planning problem. An APTree evaluation can instead organize deliveries into modules and invoke a classical planner on smaller planning scopes while maintaining a shared current state.

For a fair comparison, both approaches should preserve the same objects, initial state, roads, capacity constraints, primitive action semantics, delivery objectives, and task ordering where one is specified. Planning time, memory, solved coverage, primitive plan length, and all APTree planning-call overhead should be reported.

## Planning features

This Transport benchmark uses:

- hierarchical task decomposition;
- typed objects;
- Boolean predicates;
- negative preconditions;
- instantaneous actions;
- total and partial task-order constraints.

It does not use temporal durations, numeric fluents, or concurrent durative actions.
