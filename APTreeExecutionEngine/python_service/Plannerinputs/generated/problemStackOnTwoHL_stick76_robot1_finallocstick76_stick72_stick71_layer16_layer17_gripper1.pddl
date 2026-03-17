(define (problem stackontwohl)
  (:domain trussml)
  (:objects 
    ;; Base elements (layer 16, pre-placed)
    stick69 - stick
    stick70 - stick
    stick71 - stick
    stick72 - stick
    cube15 - cube

    ;; Active elements - layer 17
    stick73 - stick
    stick74 - stick
    stick75 - stick
    stick76 - stick

    ;; Active elements - layer 18
    stick77 - stick
    stick78 - stick
    stick79 - stick
    cube16 - cube

    ;; Layers
    layer16 - stack
    layer17 - stack
    layer18 - stack

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
    initlocstick73 - firstposition
    initlocstick74 - firstposition
    initlocstick75 - firstposition
    initlocstick76 - firstposition
    initlocstick77 - firstposition
    initlocstick78 - firstposition
    initlocstick79 - firstposition
    initloccube16 - firstposition

    ;; Locations - Final (base + active)
    finallocstick69 - finalposition
    finallocstick70 - finalposition
    finallocstick71 - finalposition
    finallocstick72 - finalposition
    finloccube15 - finalposition
    finallocstick73 - finalposition
    finallocstick74 - finalposition
    finallocstick75 - finalposition
    finallocstick76 - finalposition
    finallocstick77 - finalposition
    finallocstick78 - finalposition
    finallocstick79 - finalposition
    finloccube16 - finalposition
  )
  (:init  
    (robotequipped robot1)
(objectfinalposition stick69 finallocstick69)
(objectfinalposition stick70 finallocstick70)
(objectfinalposition stick71 finallocstick71)
(objectfinalposition stick72 finallocstick72)
(objectfinalposition stick73 finallocstick73)
(objectfinalposition stick74 finallocstick74)
(objectfinalposition stick75 finallocstick75)
(objectfinalposition stick76 finallocstick76)
(objectfinalposition stick77 finallocstick77)
(objectfinalposition stick78 finallocstick78)
(objectfinalposition stick79 finallocstick79)
(objectfinalposition cube15 finloccube15)
(objectfinalposition cube16 finloccube16)
(positionfree equiplocgripper)
(belongstolayer stick69 layer16)
(belongstolayer stick70 layer16)
(belongstolayer stick71 layer16)
(belongstolayer stick72 layer16)
(belongstolayer cube15 layer16)
(belongstolayer stick73 layer17)
(belongstolayer stick74 layer17)
(belongstolayer stick75 layer17)
(belongstolayer stick76 layer17)
(belongstolayer stick77 layer18)
(belongstolayer stick78 layer18)
(belongstolayer stick79 layer18)
(belongstolayer cube16 layer18)
(hastool robot1 gripper1)
(clear stick69)
(clear stick70)
(clear stick71)
(clear stick72)
(clear stick73)
(clear stick74)
(clear stick75)
(clear stick77)
(clear stick78)
(clear stick79)
(clear cube15)
(clear cube16)
(attool staplergun1 equiplocstapler)
(atplace stick77 initlocstick77)
(atplace stick78 initlocstick78)
(atplace stick79 initlocstick79)
(atplace cube16 initloccube16)
(atagent robot1 rppickup)
(atplace stick71 finallocstick71)
(atfinalposition stick71)
(atplace stick72 finallocstick72)
(accessible stick72)
(atfinalposition stick72)
(atplace stick70 finallocstick70)
(atfinalposition stick70)
(atplace cube15 finloccube15)
(atfinalposition cube15)
(atplace stick69 finallocstick69)
(atfinalposition stick69)
(fixed cube15)
(fixed stick69)
(fixed stick70)
(fixed stick71)
(fixed stick72)
(positionfree initlocstick73)
(atplace stick73 finallocstick73)
(accessible stick73)
(stacked stick73 stick69)
(stacked stick73 cube15)
(atfinalposition stick73)
(positionfree initlocstick74)
(atplace stick74 finallocstick74)
(accessible stick74)
(stacked stick74 stick70)
(stacked stick74 stick69)
(atfinalposition stick74)
(positionfree initlocstick75)
(atplace stick75 finallocstick75)
(accessible stick75)
(stacked stick75 stick70)
(stacked stick75 stick71)
(atfinalposition stick75)
(holding robot1 stick76)
(positionfree initlocstick76)
  )
  (:goal 
    (and
      (not (holding robot1 stick76))
(atplace stick76 finallocstick76)
(gripperempty robot1)
(clear stick76)
(accessible stick76)
(stacked stick76 stick72)
(stacked stick76 stick71)
(atfinalposition stick76)
(not (accessible stick72))
(not (accessible stick71))
    ) 
  )
)