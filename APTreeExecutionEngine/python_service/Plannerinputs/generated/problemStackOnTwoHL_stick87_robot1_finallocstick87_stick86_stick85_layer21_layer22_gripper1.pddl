(define (problem stackontwohl)
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
    ;equiplocgripper - equipposition
   ; equiplocstapler - equipposition

    ;; Robot Positions
    ;rppickup - rppickup
    ;rpmanipulate - rpmanipulate
    ;rptoolchange - rptoolchange

    ;; Locations - Initial (active elements only)
    initlocstick85 - firstposition
    initlocstick86 - firstposition
    initlocstick87 - firstposition
    initloccube18 - firstposition

    ;; Locations - Final (base + active)
    finallocstick83 - finalposition
    finallocstick84 - finalposition
    finalloccube17 - finalposition
    finallocstick85 - finalposition
    finallocstick86 - finalposition
    finallocstick87 - finalposition
    finalloccube18 - finalposition
    ;; HARDCODED ML-only objects (not in HL problem files)
    equiplocgripper - equipposition
    equiplocstapler - equipposition
    rppickup - rppickup
    rpmanipulate - rpmanipulate
    rptoolchange - rptoolchange
  )
  (:init  
    (objectfinalposition stick83 finallocstick83)
(objectfinalposition stick84 finallocstick84)
(objectfinalposition stick85 finallocstick85)
(objectfinalposition stick86 finallocstick86)
(objectfinalposition stick87 finallocstick87)
(objectfinalposition cube17 finalloccube17)
(positionfree equiplocgripper)
(belongstolayer stick83 layer20)
(belongstolayer stick84 layer20)
(belongstolayer cube17 layer20)
(belongstolayer stick85 layer21)
(belongstolayer stick86 layer21)
(belongstolayer stick87 layer22)
(hastool robot1 gripper1)
(clear stick83)
(clear stick84)
(clear stick86)
(clear cube17)
(attool staplergun1 equiplocstapler)
(atplace stick86 initlocstick86)
(atagent robot1 rppickup)
(atfinalposition cube17)
(atplace cube17 finalloccube17)
(accessible cube17)
(atplace stick83 finallocstick83)
(accessible stick83)
(atfinalposition stick83)
(atplace stick84 finallocstick84)
(accessible stick84)
(atfinalposition stick84)
(fixed cube17)
(fixed stick83)
(fixed stick84)
(holding robot1 stick87)
(positionfree initlocstick87)
(holding robot1 stick85)
(positionfree initlocstick85)
  )
  (:goal 
    (and
      (not (holding robot1 stick87))
(atplace stick87 finallocstick87)
(gripperempty robot1)
(clear stick87)
(accessible stick87)
(stacked stick87 stick86)
(stacked stick87 stick85)
(atfinalposition stick87)
(not (accessible stick86))
(not (accessible stick85))
    ) 
  )
)