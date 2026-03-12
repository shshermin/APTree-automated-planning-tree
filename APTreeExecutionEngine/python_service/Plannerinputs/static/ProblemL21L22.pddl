(define (problem demonstrator)
  (:domain trusshl)
  (:objects
    ;; Base elements (layer 20, pre-placed)
    stick83 - stick
    stick84 - stick
    cube17 - cube

    ;; Active elements - layer 21
    stick85 - stick
    stick86 - stick

    ;; Active elements - layer 22
    stick87 - stick
    cube18 - cube

    ;; Layers
    layer20 - stack
    layer21 - stack
    layer22 - stack

    ;; Agent
    robot1 - robot

    ;; Tools
    gripper1 - gripper
    staplergun1 - staplergun

    ;; Locations - Initial (active elements only)
    initlocstick85 - firstposition
    initlocstick86 - firstposition
    initlocstick87 - firstposition
    initloccube18 - firstposition

    ;; Locations - Final (base + active)
    finallocstick83 - finalposition
    finallocstick84 - finalposition
    finloccube17 - finalposition
    finallocstick85 - finalposition
    finallocstick86 - finalposition
    finallocstick87 - finalposition
    finloccube18 - finalposition
  )

  (:init
    ;; BelongsToLayer
    (belongstolayer stick83 layer20)
    (belongstolayer stick84 layer20)
    (belongstolayer cube17 layer20)
    (belongstolayer stick85 layer21)
    (belongstolayer stick86 layer21)
    (belongstolayer stick87 layer22)
    (belongstolayer cube18 layer22)

    ;; Clear (active + base elements)
    (clear stick83)
    (clear stick84)
    (clear cube17)
    (clear stick85)
    (clear stick86)
    (clear stick87)
    (clear cube18)

    ;; AtPlace - active elements at initial locations
    (atplace stick85 initlocstick85)
    (atplace stick86 initlocstick86)
    (atplace stick87 initlocstick87)
    (atplace cube18 initloccube18)

    ;; GripperEmpty
    (gripperempty robot1)
    (hastool robot1 gripper1)

    ;; ObjectFinalPosition (active elements)
    (objectfinalposition stick85 finallocstick85)
    (objectfinalposition stick86 finallocstick86)
    (objectfinalposition stick87 finallocstick87)
    (objectfinalposition cube18 finloccube18)

    ;; Base elements (layer 20, pre-placed)
    (fixed stick83)
    (fixed stick84)
    (fixed cube17)
    (atfinalposition stick83)
    (atfinalposition stick84)
    (atfinalposition cube17)
    (atplace stick83 finallocstick83)
    (atplace stick84 finallocstick84)
    (atplace cube17 finloccube17)
    (accessible stick83)
    (accessible stick84)
    (accessible cube17)
  )

  (:goal (and
    ;; Stacked - layer 21
    (stacked stick85 cube17)
    (stacked stick85 stick83)
    (stacked stick86 stick83)
    (stacked stick86 stick84)

    ;; Nailed - layer 21
    (nailed stick85 cube17)
    (nailed stick85 stick83)
    (nailed stick86 stick83)
    (nailed stick86 stick84)

    ;; Stacked - layer 22
    (stacked cube18 stick85)
    (stacked stick87 stick85)
    (stacked stick87 stick86)

    ;; Nailed - layer 22
    (nailed cube18 stick85)
    (nailed stick87 stick85)
    (nailed stick87 stick86)
  ))
)
