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
    ;equiplocgripper - equipposition
    ;equiplocstapler - equipposition

    ;; Robot Positions
    ;rppickup - rppickup
    ;rpmanipulate - rpmanipulate
    ;rptoolchange - rptoolchange

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
    finalloccube1 - finalposition
    finalloccube2 - finalposition
    finallocstick10 - finalposition
    finallocstick11 - finalposition
    finallocstick12 - finalposition
    finallocstick13 - finalposition
    finallocstick14 - finalposition
    finallocstick15 - finalposition
    finallocstick16 - finalposition
    finallocstick17 - finalposition
    finallocstick18 - finalposition
    finalloccube3 - finalposition
    finalloccube4 - finalposition
    ;; HARDCODED ML-only objects (not in HL problem files)
    equiplocgripper - equipposition
    equiplocstapler - equipposition
    rppickup - rppickup
    rpmanipulate - rpmanipulate
    rptoolchange - rptoolchange
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
(objectfinalposition cube1 finalloccube1)
(objectfinalposition cube2 finalloccube2)
(objectfinalposition cube3 finalloccube3)
(objectfinalposition cube4 finalloccube4)
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
(clear stick14)
(clear stick16)
(clear stick17)
(clear stick18)
(clear cube1)
(clear cube2)
(clear cube3)
(clear cube4)
(attool staplergun1 equiplocstapler)
(atplace stick13 initlocstick13)
(atplace stick14 initlocstick14)
(atplace stick16 initlocstick16)
(atplace stick17 initlocstick17)
(atplace stick18 initlocstick18)
(atplace cube3 initloccube3)
(atplace cube4 initloccube4)
(atagent robot1 rppickup)
(atfinalposition cube2)
(atplace cube2 finalloccube2)
(accessible cube2)
(atplace stick6 finallocstick6)
(atfinalposition stick6)
(atplace stick7 finallocstick7)
(atfinalposition stick7)
(atfinalposition cube1)
(atplace cube1 finalloccube1)
(atplace stick8 finallocstick8)
(atfinalposition stick8)
(atplace stick9 finallocstick9)
(accessible stick9)
(atfinalposition stick9)
(fixed cube1)
(fixed cube2)
(fixed stick6)
(fixed stick7)
(fixed stick8)
(fixed stick9)
(positionfree initlocstick11)
(atplace stick11 finallocstick11)
(accessible stick11)
(stacked stick11 stick6)
(stacked stick11 stick7)
(atfinalposition stick11)
(positionfree initlocstick10)
(atplace stick10 finallocstick10)
(accessible stick10)
(stacked stick10 cube1)
(stacked stick10 stick6)
(atfinalposition stick10)
(positionfree initlocstick12)
(atplace stick12 finallocstick12)
(accessible stick12)
(stacked stick12 stick7)
(stacked stick12 stick8)
(atfinalposition stick12)
(holding robot1 stick15)
(positionfree initlocstick15)
  )
  (:goal 
    (and
      (not (holding robot1 stick15))
(atplace stick15 finallocstick15)
(gripperempty robot1)
(clear stick15)
(accessible stick15)
(stacked stick15 stick11)
(stacked stick15 stick10)
(atfinalposition stick15)
(not (accessible stick11))
(not (accessible stick10))
    ) 
  )
)