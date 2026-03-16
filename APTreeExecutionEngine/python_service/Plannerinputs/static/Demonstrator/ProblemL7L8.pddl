(define (problem demonstrator)
  (:domain trusshl)
  (:objects
    ;; Base elements (layer 6, pre-placed)
    stick24 - stick
    stick25 - stick
    stick26 - stick
    stick27 - stick
    cube5 - cube
    cube6 - cube

    ;; Active elements - layer 7
    stick28 - stick
    stick29 - stick
    stick30 - stick
    stick31 - stick
    stick32 - stick

    ;; Active elements - layer 8
    stick33 - stick
    stick34 - stick
    stick35 - stick
    stick36 - stick
    cube7 - cube
    cube8 - cube

    ;; Layers
    layer6 - stack
    layer7 - stack
    layer8 - stack

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
    initlocstick28 - firstposition
    initlocstick29 - firstposition
    initlocstick30 - firstposition
    initlocstick31 - firstposition
    initlocstick32 - firstposition
    initlocstick33 - firstposition
    initlocstick34 - firstposition
    initlocstick35 - firstposition
    initlocstick36 - firstposition
    initloccube7 - firstposition
    initloccube8 - firstposition

    ;; Locations - Final (base + active)
    finallocstick24 - finalposition
    finallocstick25 - finalposition
    finallocstick26 - finalposition
    finallocstick27 - finalposition
    finloccube5 - finalposition
    finloccube6 - finalposition
    finallocstick28 - finalposition
    finallocstick29 - finalposition
    finallocstick30 - finalposition
    finallocstick31 - finalposition
    finallocstick32 - finalposition
    finallocstick33 - finalposition
    finallocstick34 - finalposition
    finallocstick35 - finalposition
    finallocstick36 - finalposition
    finloccube7 - finalposition
    finloccube8 - finalposition
  )

  (:init
    ;; BelongsToLayer
    (belongstolayer stick24 layer6)
    (belongstolayer stick25 layer6)
    (belongstolayer stick26 layer6)
    (belongstolayer stick27 layer6)
    (belongstolayer cube5 layer6)
    (belongstolayer cube6 layer6)
    (belongstolayer stick28 layer7)
    (belongstolayer stick29 layer7)
    (belongstolayer stick30 layer7)
    (belongstolayer stick31 layer7)
    (belongstolayer stick32 layer7)
    (belongstolayer stick33 layer8)
    (belongstolayer stick34 layer8)
    (belongstolayer stick35 layer8)
    (belongstolayer stick36 layer8)
    (belongstolayer cube7 layer8)
    (belongstolayer cube8 layer8)

    ;; Clear (active + base elements)
    (clear stick24)
    (clear stick25)
    (clear stick26)
    (clear stick27)
    (clear cube5)
    (clear cube6)
    (clear stick28)
    (clear stick29)
    (clear stick30)
    (clear stick31)
    (clear stick32)
    (clear stick33)
    (clear stick34)
    (clear stick35)
    (clear stick36)
    (clear cube7)
    (clear cube8)

    ;; AtPlace - active elements at initial locations
    (atplace stick28 initlocstick28)
    (atplace stick29 initlocstick29)
    (atplace stick30 initlocstick30)
    (atplace stick31 initlocstick31)
    (atplace stick32 initlocstick32)
    (atplace stick33 initlocstick33)
    (atplace stick34 initlocstick34)
    (atplace stick35 initlocstick35)
    (atplace stick36 initlocstick36)
    (atplace cube7 initloccube7)
    (atplace cube8 initloccube8)

    ;; GripperEmpty
    (gripperempty robot1)
    (atagent robot1 rppickup)
    (hastool robot1 gripper1)
    (attool staplergun1 equiplocstapler)

    ;; ObjectFinalPosition (active elements)
    (objectfinalposition stick28 finallocstick28)
    (objectfinalposition stick29 finallocstick29)
    (objectfinalposition stick30 finallocstick30)
    (objectfinalposition stick31 finallocstick31)
    (objectfinalposition stick32 finallocstick32)
    (objectfinalposition stick33 finallocstick33)
    (objectfinalposition stick34 finallocstick34)
    (objectfinalposition stick35 finallocstick35)
    (objectfinalposition stick36 finallocstick36)
    (objectfinalposition cube7 finloccube7)
    (objectfinalposition cube8 finloccube8)

    ;; Base elements (layer 6, pre-placed)
    (fixed stick24)
    (fixed stick25)
    (fixed stick26)
    (fixed stick27)
    (fixed cube5)
    (fixed cube6)
    (atfinalposition stick24)
    (atfinalposition stick25)
    (atfinalposition stick26)
    (atfinalposition stick27)
    (atfinalposition cube5)
    (atfinalposition cube6)
    (atplace stick24 finallocstick24)
    (atplace stick25 finallocstick25)
    (atplace stick26 finallocstick26)
    (atplace stick27 finallocstick27)
    (atplace cube5 finloccube5)
    (atplace cube6 finloccube6)
    (accessible stick24)
    (accessible stick25)
    (accessible stick26)
    (accessible stick27)
    (accessible cube5)
    (accessible cube6)
  )

  (:goal (and
    ;; Stacked - layer 7
    (stacked stick28 cube5)
    (stacked stick28 stick24)
    (stacked stick29 stick24)
    (stacked stick29 stick25)
    (stacked stick30 stick25)
    (stacked stick30 stick26)
    (stacked stick31 stick26)
    (stacked stick31 stick27)
    (stacked stick32 cube6)
    (stacked stick32 stick27)

    ;; Nailed - layer 7
    (nailed stick28 cube5)
    (nailed stick28 stick24)
    (nailed stick29 stick24)
    (nailed stick29 stick25)
    (nailed stick30 stick25)
    (nailed stick30 stick26)
    (nailed stick31 stick26)
    (nailed stick31 stick27)
    (nailed stick32 cube6)
    (nailed stick32 stick27)

    ;; Stacked - layer 8
    (stacked cube7 stick28)
    (stacked cube8 stick32)
    (stacked stick33 stick28)
    (stacked stick33 stick29)
    (stacked stick34 stick29)
    (stacked stick34 stick30)
    (stacked stick35 stick30)
    (stacked stick35 stick31)
    (stacked stick36 stick31)
    (stacked stick36 stick32)

    ;; Nailed - layer 8
    (nailed cube7 stick28)
    (nailed cube8 stick32)
    (nailed stick33 stick28)
    (nailed stick33 stick29)
    (nailed stick34 stick29)
    (nailed stick34 stick30)
    (nailed stick35 stick30)
    (nailed stick35 stick31)
    (nailed stick36 stick31)
    (nailed stick36 stick32)
  ))
)
