(define (problem stackontwohl)
  (:domain trussml)
  (:objects 
    ;; Base elements (layer 2, pre-placed)
    stick6 - stick
    stick7 - stick
    stick8 - stick
    stick9 - stick
    cube1 - cube
    cube2 - cube

    ;; Active elements - layer 3
    stick10 - stick
    stick11 - stick
    stick12 - stick
    stick13 - stick
    stick14 - stick

    ;; Active elements - layer 4
    stick15 - stick
    stick16 - stick
    stick17 - stick
    stick18 - stick
    cube3 - cube
    cube4 - cube

    ;; Layers
    layer2 - stack
    layer3 - stack
    layer4 - stack

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
    initlocstick10 - firstposition
    initlocstick11 - firstposition
    initlocstick12 - firstposition
    initlocstick13 - firstposition
    initlocstick14 - firstposition
    initlocstick15 - firstposition
    initlocstick16 - firstposition
    initlocstick17 - firstposition
    initlocstick18 - firstposition
    initloccube3 - firstposition
    initloccube4 - firstposition

    ;; Locations - Final (base + active)
    finallocstick6 - finalposition
    finallocstick7 - finalposition
    finallocstick8 - finalposition
    finallocstick9 - finalposition
    finloccube1 - finalposition
    finloccube2 - finalposition
    finallocstick10 - finalposition
    finallocstick11 - finalposition
    finallocstick12 - finalposition
    finallocstick13 - finalposition
    finallocstick14 - finalposition
    finallocstick15 - finalposition
    finallocstick16 - finalposition
    finallocstick17 - finalposition
    finallocstick18 - finalposition
    finloccube3 - finalposition
    finloccube4 - finalposition
  )
  (:init  
    (robotequipped robot1)
(objectfinalposition stick6 finallocstick6)
(objectfinalposition stick7 finallocstick7)
(objectfinalposition stick8 finallocstick8)
(objectfinalposition stick9 finallocstick9)
(objectfinalposition stick10 finallocstick10)
(objectfinalposition stick11 finallocstick11)
(objectfinalposition stick12 finallocstick12)
(objectfinalposition stick13 finallocstick13)
(objectfinalposition stick14 finallocstick14)
(objectfinalposition stick15 finallocstick15)
(objectfinalposition stick16 finallocstick16)
(objectfinalposition stick17 finallocstick17)
(objectfinalposition stick18 finallocstick18)
(objectfinalposition cube1 finloccube1)
(objectfinalposition cube2 finloccube2)
(objectfinalposition cube3 finloccube3)
(objectfinalposition cube4 finloccube4)
(positionfree equiplocgripper)
(belongstolayer stick6 layer2)
(belongstolayer stick7 layer2)
(belongstolayer stick8 layer2)
(belongstolayer stick9 layer2)
(belongstolayer cube1 layer2)
(belongstolayer cube2 layer2)
(belongstolayer stick10 layer3)
(belongstolayer stick11 layer3)
(belongstolayer stick12 layer3)
(belongstolayer stick13 layer3)
(belongstolayer stick14 layer3)
(belongstolayer stick15 layer4)
(belongstolayer stick16 layer4)
(belongstolayer stick17 layer4)
(belongstolayer stick18 layer4)
(belongstolayer cube3 layer4)
(belongstolayer cube4 layer4)
(hastool robot1 gripper1)
(clear stick6)
(clear stick7)
(clear stick8)
(clear stick9)
(clear stick10)
(clear stick11)
(clear stick12)
(clear stick13)
(clear stick15)
(clear stick16)
(clear stick17)
(clear stick18)
(clear cube1)
(clear cube2)
(clear cube3)
(clear cube4)
(attool staplergun1 equiplocstapler)
(atplace stick10 initlocstick10)
(atplace stick11 initlocstick11)
(atplace stick13 initlocstick13)
(atplace stick15 initlocstick15)
(atplace stick16 initlocstick16)
(atplace stick17 initlocstick17)
(atplace stick18 initlocstick18)
(atplace cube3 initloccube3)
(atplace cube4 initloccube4)
(atagent robot1 rppickup)
(atfinalposition cube1)
(atplace cube1 finloccube1)
(accessible cube1)
(atplace stick9 finallocstick9)
(accessible stick9)
(atfinalposition stick9)
(fixed stick9)
(fixed cube1)
(atfinalposition cube2)
(atplace cube2 finloccube2)
(accessible cube2)
(atplace stick6 finallocstick6)
(accessible stick6)
(atfinalposition stick6)
(atplace stick8 finallocstick8)
(atfinalposition stick8)
(atplace stick7 finallocstick7)
(atfinalposition stick7)
(fixed cube2)
(fixed stick6)
(fixed stick7)
(fixed stick8)
(positionfree initlocstick12)
(atplace stick12 finallocstick12)
(accessible stick12)
(stacked stick12 stick8)
(stacked stick12 stick7)
(atfinalposition stick12)
(holding robot1 stick14)
(positionfree initlocstick14)
  )
  (:goal 
    (and
      (not (holding robot1 stick14))
(atplace stick14 finallocstick14)
(gripperempty robot1)
(clear stick14)
(accessible stick14)
(stacked stick14 stick9)
(stacked stick14 cube2)
(atfinalposition stick14)
(not (accessible stick9))
(not (accessible cube2))
    ) 
  )
)