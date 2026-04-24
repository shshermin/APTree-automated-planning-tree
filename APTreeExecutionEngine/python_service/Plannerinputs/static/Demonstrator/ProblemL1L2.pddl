(define (problem demonstrator)
  (:domain trusshl)
  (:objects
    ;; Elements - Sticks
    stick1 - stick
    stick2 - stick
    stick3 - stick
    stick4 - stick
    stick5 - stick
    stick6 - stick
    stick7 - stick
    stick8 - stick
    stick9 - stick

    ;; Elements - Cubes
    cube1 - cube
    cube2 - cube

    ;; Table
    table1 - table

    ;; Layers
    layer0 - stack
    layer1 - stack
    layer2 - stack

    ;; Agent
    robot1 - robot

    ;; Tools
    gripper1 - gripper
    staplergun1 - staplergun

    ;; Equip Positions
    ;equiplocgripper - equipposition
    ;equiplocstapler - equipposition

  ;; Robot Positions
   ; rppickup - rppickup
   ; rpmanipulate - rpmanipulate
   ; rptoolchange - rptoolchange

    ;; Locations - Initial (first positions)
    initlocstick1 - firstposition
    initlocstick2 - firstposition
    initlocstick3 - firstposition
    initlocstick4 - firstposition
    initlocstick5 - firstposition
    initlocstick6 - firstposition
    initlocstick7 - firstposition
    initlocstick8 - firstposition
    initlocstick9 - firstposition
    initloccube1 - firstposition
    initloccube2 - firstposition

    ;; Locations - Final positions
    mp5 - finalposition
    finallocstick1 - finalposition
    finallocstick2 - finalposition
    finallocstick3 - finalposition
    finallocstick4 - finalposition
    finallocstick5 - finalposition
    finallocstick6 - finalposition
    finallocstick7 - finalposition
    finallocstick8 - finalposition
    finallocstick9 - finalposition
    finalloccube1 - finalposition
    finalloccube2 - finalposition

    temploc2 - firstposition
    temploc3 - finalposition
    stickdummy - stick
  )

  (:init
    ;; BelongsToLayer
    (belongstolayer stick1 layer1)
    (belongstolayer stick2 layer1)
    (belongstolayer stick3 layer1)
    (belongstolayer stick4 layer1)
    (belongstolayer stick5 layer1)
    (belongstolayer stick6 layer2)
    (belongstolayer stick7 layer2)
    (belongstolayer stick8 layer2)
    (belongstolayer stick9 layer2)
    (belongstolayer cube1 layer2)
    (belongstolayer cube2 layer2)

    ;; Clear predicates
    (clear stick1)
    (clear stick2)
    (clear stick3)
    (clear stick4)
    (clear stick5)
    (clear stick6)
    (clear stick7)
    (clear stick8)
    (clear stick9)
    (clear cube1)
    (clear cube2)

    ;; AtPlace predicates
    (atplace stick1 initlocstick1)
    (atplace stick2 initlocstick2)
    (atplace stick3 initlocstick3)
    (atplace stick4 initlocstick4)
    (atplace stick5 initlocstick5)
    (atplace stick6 initlocstick6)
    (atplace stick7 initlocstick7)
    (atplace stick8 initlocstick8)
    (atplace stick9 initlocstick9)
    (atplace cube1 initloccube1)
    (atplace cube2 initloccube2)

    ;; GripperEmpty
    (gripperempty robot1)
    ;(atagent robot1 rpmanipulate)
    (hastool robot1 gripper1)
    ;(attool staplergun1 equiplocstapler)

    ;; ObjectFinalPosition
    (objectfinalposition stick1 finallocstick1)
    (objectfinalposition stick2 finallocstick2)
    (objectfinalposition stick3 finallocstick3)
    (objectfinalposition stick4 finallocstick4)
    (objectfinalposition stick5 finallocstick5)
    (objectfinalposition stick6 finallocstick6)
    (objectfinalposition stick7 finallocstick7)
    (objectfinalposition stick8 finallocstick8)
    (objectfinalposition stick9 finallocstick9)
    (objectfinalposition cube1 finalloccube1)
    (objectfinalposition cube2 finalloccube2)

    ;; Table
    (fixed table1)
    (atfinalposition table1)
    (belongstolayer table1 layer0)
    (atplace table1 mp5)
    (accessible table1)
  )

  (:goal (and
    ;; Stacked - layer 1
    (stacked stick1 table1)
    (stacked stick2 table1)
    (stacked stick3 table1)
    (stacked stick4 table1)
    (stacked stick5 table1)

    ;; Stacked - layer 2
    (stacked stick6 stick1)
    (stacked stick6 stick2)
    (stacked stick7 stick2)
    (stacked stick7 stick3)
    (stacked stick8 stick3)
    (stacked stick8 stick4)
    (stacked stick9 stick4)
    (stacked stick9 stick5)
    (stacked cube1 stick1)
    (stacked cube2 stick5)

    ;; Nailed - layer 2
    (nailed stick6 stick1)
    (nailed stick6 stick2)
    (nailed stick7 stick2)
    (nailed stick7 stick3)
    (nailed stick8 stick3)
    (nailed stick8 stick4)
    (nailed stick9 stick4)
    (nailed stick9 stick5)
    (nailed cube1 stick1)
    (nailed cube2 stick5)
  ))
)
