(define (problem pickuphl)
  (:domain trussml)
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
    equiplocgripper - equipposition
    equiplocstapler - equipposition

    ;; Robot Positions
    rppickup - rppickup
    rpmanipulate - rpmanipulate
    rptoolchange - rptoolchange

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
    finloccube1 - finalposition
    finloccube2 - finalposition
  )
  (:init  
    (atfinalposition table1)
(robotequipped robot1)
(objectfinalposition stick1 finallocstick1)
(objectfinalposition stick2 finallocstick2)
(objectfinalposition stick3 finallocstick3)
(objectfinalposition stick4 finallocstick4)
(objectfinalposition stick5 finallocstick5)
(objectfinalposition stick6 finallocstick6)
(objectfinalposition stick7 finallocstick7)
(objectfinalposition stick8 finallocstick8)
(objectfinalposition stick9 finallocstick9)
(objectfinalposition cube1 finloccube1)
(objectfinalposition cube2 finloccube2)
(positionfree equiplocgripper)
(gripperempty robot1)
(belongstolayer table1 layer0)
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
(hastool robot1 gripper1)
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
(attool staplergun1 equiplocstapler)
(atplace stick1 initlocstick1)
(atplace stick3 initlocstick3)
(atplace stick5 initlocstick5)
(atplace stick6 initlocstick6)
(atplace stick7 initlocstick7)
(atplace stick8 initlocstick8)
(atplace stick9 initlocstick9)
(atplace cube1 initloccube1)
(atplace cube2 initloccube2)
(atplace table1 mp5)
(atagent robot1 rpmanipulate)
(fixed table1)
(activetool gripper1)
(positionfree initlocstick2)
(atfinalposition stick2)
(atplace stick2 finallocstick2)
(accessible stick2)
(stacked stick2 table1)
(positionfree initlocstick4)
(atfinalposition stick4)
(atplace stick4 finallocstick4)
(accessible stick4)
(stacked stick4 table1)
  )
  (:goal 
    (and
      (holding robot1 stick1)
(not (atplace stick1 initlocstick1))
(not (gripperempty robot1))
(not (clear stick1))
(positionfree initlocstick1)
    ) 
  )
)