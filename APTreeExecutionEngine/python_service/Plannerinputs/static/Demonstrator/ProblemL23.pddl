(define (problem demonstrator)
  (:domain trusshl)
  (:objects
    ;; Base elements (layer 22, pre-placed)
    stick87 - stick
    cube18 - cube

    ;; Active elements - layer 23
    stick88 - stick

    ;; Layers
    layer22 - stack
    layer23 - stack

    ;; Agent
    robot1 - robot

    ;; Tools
    gripper1 - gripper
    staplergun1 - staplergun

    ;; Equip Positions
    equiplocgripper - equipposition
    equiplocstapler - equipposition

    ;; Robot Positions
    rppickup - rppickup
    rpmanipulate - rpmanipulate
    rptoolchange - rptoolchange

    ;; Locations - Initial (active elements only)
    initlocstick88 - firstposition

    ;; Locations - Final (base + active)
    finallocstick87 - finalposition
    finloccube18 - finalposition
    finallocstick88 - finalposition
  )

  (:init
    ;; BelongsToLayer
    (belongstolayer stick87 layer22)
    (belongstolayer cube18 layer22)
    (belongstolayer stick88 layer23)

    ;; Clear (active + base elements)
    (clear stick87)
    (clear cube18)
    (clear stick88)

    ;; AtPlace - active elements at initial locations
    (atplace stick88 initlocstick88)

    ;; GripperEmpty
    (gripperempty robot1)
    (atagent robot1 rppickup)
    (hastool robot1 gripper1)
    (attool staplergun1 equiplocstapler)

    ;; ObjectFinalPosition (active elements)
    (objectfinalposition stick88 finallocstick88)

    ;; Base elements (layer 22, pre-placed)
    (fixed stick87)
    (fixed cube18)
    (atfinalposition stick87)
    (atfinalposition cube18)
    (atplace stick87 finallocstick87)
    (atplace cube18 finloccube18)
    (accessible stick87)
    (accessible cube18)
  )

  (:goal (and
    ;; Stacked - layer 23
    (stacked stick88 cube18)
    (stacked stick88 stick87)

    ;; Nailed - layer 23
    (nailed stick88 cube18)
    (nailed stick88 stick87)
  ))
)
