(define (problem pickuphl)
  (:domain trussml)
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
    finalloccube16 - finalposition
    finallocstick80 - finalposition
    finallocstick81 - finalposition
    finallocstick82 - finalposition
    finallocstick83 - finalposition
    finallocstick84 - finalposition
    finalloccube17 - finalposition
    ;; HARDCODED ML-only objects (not in HL problem files)
    equiplocgripper - equipposition
    equiplocstapler - equipposition
    rppickup - rppickup
    rpmanipulate - rpmanipulate
    rptoolchange - rptoolchange
  )
  (:init  
    (robotequipped robot1)
(objectfinalposition stick77 finallocstick77)
(objectfinalposition stick78 finallocstick78)
(objectfinalposition stick79 finallocstick79)
(objectfinalposition stick80 finallocstick80)
(objectfinalposition stick81 finallocstick81)
(objectfinalposition stick82 finallocstick82)
(objectfinalposition stick83 finallocstick83)
(objectfinalposition stick84 finallocstick84)
(objectfinalposition cube16 finalloccube16)
(objectfinalposition cube17 finalloccube17)
(positionfree equiplocgripper)
(gripperempty robot1)
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
(hastool robot1 gripper1)
(clear stick77)
(clear stick78)
(clear stick79)
(clear stick80)
(clear stick81)
(clear stick82)
(clear stick83)
(clear stick84)
(clear cube16)
(clear cube17)
(attool staplergun1 equiplocstapler)
(atplace stick83 initlocstick83)
(atplace stick84 initlocstick84)
(atagent robot1 rpmanipulate)
(atfinalposition cube16)
(atplace cube16 finalloccube16)
(atplace stick77 finallocstick77)
(atfinalposition stick77)
(atplace stick78 finallocstick78)
(atfinalposition stick78)
(atplace stick79 finallocstick79)
(atfinalposition stick79)
(fixed cube16)
(fixed stick77)
(fixed stick78)
(fixed stick79)
(positionfree initlocstick81)
(atplace stick81 finallocstick81)
(accessible stick81)
(stacked stick81 stick77)
(stacked stick81 stick78)
(atfinalposition stick81)
(positionfree initlocstick80)
(atplace stick80 finallocstick80)
(stacked stick80 cube16)
(stacked stick80 stick77)
(atfinalposition stick80)
(positionfree initloccube17)
(atfinalposition cube17)
(atplace cube17 finalloccube17)
(accessible cube17)
(stacked cube17 stick80)
(positionfree initlocstick82)
(atplace stick82 finallocstick82)
(accessible stick82)
(stacked stick82 stick79)
(stacked stick82 stick78)
(atfinalposition stick82)
  )
  (:goal 
    (and
      (holding robot1 stick83)
(not (atplace stick83 initlocstick83))
(not (gripperempty robot1))
(not (clear stick83))
(positionfree initlocstick83)
    ) 
  )
)