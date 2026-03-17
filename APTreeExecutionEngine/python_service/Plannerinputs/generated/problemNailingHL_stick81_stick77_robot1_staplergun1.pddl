(define (problem nailinghl)
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
    equiplocgripper - equipposition
    equiplocstapler - equipposition

    ;; Robot Positions
    rppickup - rppickup
    rpmanipulate - rpmanipulate
    rptoolchange - rptoolchange

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
    (robotequipped robot1)
(objectfinalposition stick77 finallocstick77)
(objectfinalposition stick78 finallocstick78)
(objectfinalposition stick79 finallocstick79)
(objectfinalposition stick80 finallocstick80)
(objectfinalposition stick81 finallocstick81)
(objectfinalposition stick82 finallocstick82)
(objectfinalposition stick83 finallocstick83)
(objectfinalposition stick84 finallocstick84)
(objectfinalposition cube16 finloccube16)
(objectfinalposition cube17 finloccube17)
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
(atplace stick83 initlocstick83)
(atplace stick84 initlocstick84)
(atplace cube17 initloccube17)
(atagent robot1 rpmanipulate)
(attool gripper1 equiplocgripper)
(hastool robot1 staplergun1)
(positionfree equiplocstapler)
(activetool staplergun1)
(atplace stick79 finallocstick79)
(atfinalposition stick79)
(atplace stick77 finallocstick77)
(atfinalposition stick77)
(atplace cube16 finloccube16)
(atfinalposition cube16)
(atplace stick78 finallocstick78)
(atfinalposition stick78)
(fixed stick79)
(fixed cube16)
(fixed stick77)
(fixed stick78)
(positionfree initlocstick80)
(atplace stick80 finallocstick80)
(accessible stick80)
(stacked stick80 stick77)
(stacked stick80 cube16)
(atfinalposition stick80)
(positionfree initlocstick81)
(atplace stick81 finallocstick81)
(accessible stick81)
(stacked stick81 stick77)
(stacked stick81 stick78)
(atfinalposition stick81)
(positionfree initlocstick82)
(atplace stick82 finallocstick82)
(accessible stick82)
(stacked stick82 stick78)
(stacked stick82 stick79)
(atfinalposition stick82)
(nailed stick81 stick78)
(fixed stick81)
(nailed stick80 cube16)
(fixed stick80)
(nailed stick80 stick77)
  )
  (:goal 
    (and
      (nailed stick81 stick77)
(fixed stick81)
    ) 
  )
)