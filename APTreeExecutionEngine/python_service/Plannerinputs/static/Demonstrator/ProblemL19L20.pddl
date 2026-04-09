(define (problem demonstrator)
  (:domain trusshl)
  (:objects
    ;; Base elements (layer 18, pre-placed)
    stick77 - stick
    stick78 - stick
    stick79 - stick
    cube16 - cube

    ;; Active elements - layer 19
    stick80 - stick
    stick81 - stick
    stick82 - stick

    ;; Active elements - layer 20
    stick83 - stick
    stick84 - stick
    cube17 - cube

    ;; Layers
    layer18 - stack
    layer19 - stack
    layer20 - stack

    ;; Agent
    robot1 - robot

    ;; Tools
    gripper1 - gripper
    staplergun1 - staplergun

    ;; Equip Positions
   ; equiplocgripper - equipposition
    ;equiplocstapler - equipposition

    ;; Robot Positions
    ;rppickup - rppickup
    ;rpmanipulate - rpmanipulate
    ;rptoolchange - rptoolchange

    ;; Locations - Initial (active elements only)
    initlocstick80 - firstposition
    initlocstick81 - firstposition
    initlocstick82 - firstposition
    initlocstick83 - firstposition
    initlocstick84 - firstposition
    initloccube17 - firstposition

    ;; Locations - Final (base + active)
    finallocstick77 - finalposition
    finallocstick78 - finalposition
    finallocstick79 - finalposition
    finloccube16 - finalposition
    finallocstick80 - finalposition
    finallocstick81 - finalposition
    finallocstick82 - finalposition
    finallocstick83 - finalposition
    finallocstick84 - finalposition
    finloccube17 - finalposition
  )

  (:init
    ;; BelongsToLayer
    (belongstolayer stick77 layer18)
    (belongstolayer stick78 layer18)
    (belongstolayer stick79 layer18)
    (belongstolayer cube16 layer18)
    (belongstolayer stick80 layer19)
    (belongstolayer stick81 layer19)
    (belongstolayer stick82 layer19)
    (belongstolayer stick83 layer20)
    (belongstolayer stick84 layer20)
    (belongstolayer cube17 layer20)

    ;; Clear (active + base elements)
    (clear stick77)
    (clear stick78)
    (clear stick79)
    (clear cube16)
    (clear stick80)
    (clear stick81)
    (clear stick82)
    (clear stick83)
    (clear stick84)
    (clear cube17)

    ;; AtPlace - active elements at initial locations
    (atplace stick80 initlocstick80)
    (atplace stick81 initlocstick81)
    (atplace stick82 initlocstick82)
    (atplace stick83 initlocstick83)
    (atplace stick84 initlocstick84)
    (atplace cube17 initloccube17)

    ;; GripperEmpty
    (gripperempty robot1)
    ;(atagent robot1 rppickup)
    (hastool robot1 gripper1)
    ;(attool staplergun1 equiplocstapler)

    ;; ObjectFinalPosition (active elements)
    (objectfinalposition stick80 finallocstick80)
    (objectfinalposition stick81 finallocstick81)
    (objectfinalposition stick82 finallocstick82)
    (objectfinalposition stick83 finallocstick83)
    (objectfinalposition stick84 finallocstick84)
    (objectfinalposition cube17 finloccube17)

    ;; Base elements (layer 18, pre-placed)
    (fixed stick77)
    (fixed stick78)
    (fixed stick79)
    (fixed cube16)
    (atfinalposition stick77)
    (atfinalposition stick78)
    (atfinalposition stick79)
    (atfinalposition cube16)
    (atplace stick77 finallocstick77)
    (atplace stick78 finallocstick78)
    (atplace stick79 finallocstick79)
    (atplace cube16 finloccube16)
    (accessible stick77)
    (accessible stick78)
    (accessible stick79)
    (accessible cube16)
  )

  (:goal (and
    ;; Stacked - layer 19
    (stacked stick80 cube16)
    (stacked stick80 stick77)
    (stacked stick81 stick77)
    (stacked stick81 stick78)
    (stacked stick82 stick78)
    (stacked stick82 stick79)

    ;; Nailed - layer 19
    (nailed stick80 cube16)
    (nailed stick80 stick77)
    (nailed stick81 stick77)
    (nailed stick81 stick78)
    (nailed stick82 stick78)
    (nailed stick82 stick79)

    ;; Stacked - layer 20
    (stacked cube17 stick80)
    (stacked stick83 stick80)
    (stacked stick83 stick81)
    (stacked stick84 stick81)
    (stacked stick84 stick82)

    ;; Nailed - layer 20
    (nailed cube17 stick80)
    (nailed stick83 stick80)
    (nailed stick83 stick81)
    (nailed stick84 stick81)
    (nailed stick84 stick82)
  ))
)
