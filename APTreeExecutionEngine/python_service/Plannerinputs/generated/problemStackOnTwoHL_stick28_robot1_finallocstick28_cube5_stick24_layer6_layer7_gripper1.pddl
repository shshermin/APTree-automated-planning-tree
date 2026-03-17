(define (problem stackontwohl)
  (:domain trussml)
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
    (robotequipped robot1)
(objectfinalposition stick24 finallocstick24)
(objectfinalposition stick25 finallocstick25)
(objectfinalposition stick26 finallocstick26)
(objectfinalposition stick27 finallocstick27)
(objectfinalposition stick28 finallocstick28)
(objectfinalposition stick29 finallocstick29)
(objectfinalposition stick30 finallocstick30)
(objectfinalposition stick31 finallocstick31)
(objectfinalposition stick32 finallocstick32)
(objectfinalposition stick33 finallocstick33)
(objectfinalposition stick34 finallocstick34)
(objectfinalposition stick35 finallocstick35)
(objectfinalposition stick36 finallocstick36)
(objectfinalposition cube5 finloccube5)
(objectfinalposition cube6 finloccube6)
(objectfinalposition cube7 finloccube7)
(objectfinalposition cube8 finloccube8)
(positionfree equiplocgripper)
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
(hastool robot1 gripper1)
(clear stick24)
(clear stick25)
(clear stick26)
(clear stick27)
(clear stick29)
(clear stick30)
(clear stick31)
(clear stick32)
(clear stick33)
(clear stick34)
(clear stick35)
(clear stick36)
(clear cube5)
(clear cube6)
(clear cube7)
(clear cube8)
(attool staplergun1 equiplocstapler)
(atplace stick30 initlocstick30)
(atplace stick31 initlocstick31)
(atplace stick32 initlocstick32)
(atplace stick33 initlocstick33)
(atplace stick34 initlocstick34)
(atplace stick35 initlocstick35)
(atplace stick36 initlocstick36)
(atplace cube7 initloccube7)
(atplace cube8 initloccube8)
(atagent robot1 rppickup)
(atplace stick24 finallocstick24)
(atfinalposition stick24)
(atplace cube5 finloccube5)
(accessible cube5)
(atfinalposition cube5)
(fixed cube5)
(fixed stick24)
(atplace stick25 finallocstick25)
(atfinalposition stick25)
(atplace cube6 finloccube6)
(accessible cube6)
(atfinalposition cube6)
(atplace stick26 finallocstick26)
(accessible stick26)
(atfinalposition stick26)
(atplace stick27 finallocstick27)
(accessible stick27)
(atfinalposition stick27)
(fixed cube6)
(fixed stick25)
(fixed stick26)
(fixed stick27)
(positionfree initlocstick29)
(atplace stick29 finallocstick29)
(accessible stick29)
(stacked stick29 stick24)
(stacked stick29 stick25)
(atfinalposition stick29)
(holding robot1 stick28)
(positionfree initlocstick28)
  )
  (:goal 
    (and
      (not (holding robot1 stick28))
(atplace stick28 finallocstick28)
(gripperempty robot1)
(clear stick28)
(accessible stick28)
(stacked stick28 cube5)
(stacked stick28 stick24)
(atfinalposition stick28)
(not (accessible cube5))
(not (accessible stick24))
    ) 
  )
)