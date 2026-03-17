(define (problem changeendeffectorhl)
  (:domain trussml)
  (:objects 
    ;; Base elements (layer 20, pre-placed)
    stick83 - stick
    stick84 - stick
    cube17 - cube

    ;; Active elements - layer 21
    stick85 - stick
    stick86 - stick

    ;; Active elements - layer 22
    stick87 - stick
    cube18 - cube

    ;; Layers
    layer20 - stack
    layer21 - stack
    layer22 - stack

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
    initlocstick85 - firstposition
    initlocstick86 - firstposition
    initlocstick87 - firstposition
    initloccube18 - firstposition

    ;; Locations - Final (base + active)
    finallocstick83 - finalposition
    finallocstick84 - finalposition
    finloccube17 - finalposition
    finallocstick85 - finalposition
    finallocstick86 - finalposition
    finallocstick87 - finalposition
    finloccube18 - finalposition
  )
  (:init  
    (robotequipped robot1)
(objectfinalposition stick83 finallocstick83)
(objectfinalposition stick84 finallocstick84)
(objectfinalposition stick85 finallocstick85)
(objectfinalposition stick86 finallocstick86)
(objectfinalposition stick87 finallocstick87)
(objectfinalposition cube17 finloccube17)
(objectfinalposition cube18 finloccube18)
(gripperempty robot1)
(belongstolayer stick83 layer20)
(belongstolayer stick84 layer20)
(belongstolayer cube17 layer20)
(belongstolayer stick85 layer21)
(belongstolayer stick86 layer21)
(belongstolayer stick87 layer22)
(belongstolayer cube18 layer22)
(clear stick83)
(clear stick84)
(clear stick85)
(clear stick86)
(clear stick87)
(clear cube17)
(clear cube18)
(atplace stick87 initlocstick87)
(atagent robot1 rpmanipulate)
(attool gripper1 equiplocgripper)
(hastool robot1 staplergun1)
(positionfree equiplocstapler)
(activetool staplergun1)
(atplace stick84 finallocstick84)
(atfinalposition stick84)
(atplace cube17 finloccube17)
(atfinalposition cube17)
(atplace stick83 finallocstick83)
(atfinalposition stick83)
(fixed cube17)
(fixed stick83)
(fixed stick84)
(positionfree initlocstick85)
(atplace stick85 finallocstick85)
(stacked stick85 stick83)
(stacked stick85 cube17)
(atfinalposition stick85)
(positionfree initlocstick86)
(atplace stick86 finallocstick86)
(stacked stick86 stick84)
(stacked stick86 stick83)
(atfinalposition stick86)
(nailed stick85 cube17)
(fixed stick85)
(nailed stick86 stick83)
(fixed stick86)
(nailed stick86 stick84)
(nailed stick85 stick83)
(positionfree initloccube18)
(atplace cube18 finloccube18)
(accessible cube18)
(stacked cube18 stick86)
(stacked cube18 stick85)
(atfinalposition cube18)
(nailed cube18 stick85)
(fixed cube18)
  )
  (:goal 
    (and
      (not (hastool robot1 staplergun1))
(hastool robot1 gripper1)
    ) 
  )
)