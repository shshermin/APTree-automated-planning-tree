(define (problem pickuphl)
  (:domain trussml)
  (:objects 
    ;; Base elements (layer 8, pre-placed)
    stick33 - stick
    stick34 - stick
    stick35 - stick
    stick36 - stick
    cube7 - cube
    cube8 - cube

    ;; Active elements - layer 9
    stick37 - stick
    stick38 - stick
    stick39 - stick
    stick40 - stick
    stick41 - stick

    ;; Active elements - layer 10
    stick42 - stick
    stick43 - stick
    stick44 - stick
    stick45 - stick
    cube9 - cube
    cube10 - cube

    ;; Layers
    layer8 - stack
    layer9 - stack
    layer10 - stack

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
    initlocstick37 - firstposition
    initlocstick38 - firstposition
    initlocstick39 - firstposition
    initlocstick40 - firstposition
    initlocstick41 - firstposition
    initlocstick42 - firstposition
    initlocstick43 - firstposition
    initlocstick44 - firstposition
    initlocstick45 - firstposition
    initloccube9 - firstposition
    initloccube10 - firstposition

    ;; Locations - Final (base + active)
    finallocstick33 - finalposition
    finallocstick34 - finalposition
    finallocstick35 - finalposition
    finallocstick36 - finalposition
    finalloccube7 - finalposition
    finalloccube8 - finalposition
    finallocstick37 - finalposition
    finallocstick38 - finalposition
    finallocstick39 - finalposition
    finallocstick40 - finalposition
    finallocstick41 - finalposition
    finallocstick42 - finalposition
    finallocstick43 - finalposition
    finallocstick44 - finalposition
    finallocstick45 - finalposition
    finalloccube9 - finalposition
    finalloccube10 - finalposition
    ;; HARDCODED ML-only objects (not in HL problem files)
    equiplocgripper - equipposition
    equiplocstapler - equipposition
    rppickup - rppickup
    rpmanipulate - rpmanipulate
    rptoolchange - rptoolchange
  )
  (:init  
    (robotequipped robot1)
(objectfinalposition stick33 finallocstick33)
(objectfinalposition stick34 finallocstick34)
(objectfinalposition stick35 finallocstick35)
(objectfinalposition stick36 finallocstick36)
(objectfinalposition stick37 finallocstick37)
(objectfinalposition stick38 finallocstick38)
(objectfinalposition stick39 finallocstick39)
(objectfinalposition stick40 finallocstick40)
(objectfinalposition stick41 finallocstick41)
(objectfinalposition stick42 finallocstick42)
(objectfinalposition stick43 finallocstick43)
(objectfinalposition stick44 finallocstick44)
(objectfinalposition stick45 finallocstick45)
(objectfinalposition cube7 finalloccube7)
(objectfinalposition cube8 finalloccube8)
(objectfinalposition cube9 finalloccube9)
(objectfinalposition cube10 finalloccube10)
(positionfree equiplocgripper)
(gripperempty robot1)
(belongstolayer stick33 layer8)
(belongstolayer stick34 layer8)
(belongstolayer stick35 layer8)
(belongstolayer stick36 layer8)
(belongstolayer cube7 layer8)
(belongstolayer cube8 layer8)
(belongstolayer stick37 layer9)
(belongstolayer stick38 layer9)
(belongstolayer stick39 layer9)
(belongstolayer stick40 layer9)
(belongstolayer stick41 layer9)
(belongstolayer stick42 layer10)
(belongstolayer stick43 layer10)
(belongstolayer stick44 layer10)
(belongstolayer stick45 layer10)
(belongstolayer cube9 layer10)
(belongstolayer cube10 layer10)
(hastool robot1 gripper1)
(clear stick33)
(clear stick34)
(clear stick35)
(clear stick36)
(clear stick37)
(clear stick38)
(clear stick39)
(clear stick40)
(clear stick41)
(clear stick42)
(clear stick43)
(clear stick44)
(clear stick45)
(clear cube7)
(clear cube8)
(clear cube9)
(clear cube10)
(attool staplergun1 equiplocstapler)
(atplace stick40 initlocstick40)
(atplace stick41 initlocstick41)
(atplace stick42 initlocstick42)
(atplace stick43 initlocstick43)
(atplace stick44 initlocstick44)
(atplace stick45 initlocstick45)
(atplace cube9 initloccube9)
(atplace cube10 initloccube10)
(atagent robot1 rpmanipulate)
(atplace stick33 finallocstick33)
(atfinalposition stick33)
(atplace stick35 finallocstick35)
(atfinalposition stick35)
(atplace stick34 finallocstick34)
(atfinalposition stick34)
(atfinalposition cube8)
(atplace cube8 finalloccube8)
(accessible cube8)
(atfinalposition cube7)
(atplace cube7 finalloccube7)
(atplace stick36 finallocstick36)
(accessible stick36)
(atfinalposition stick36)
(fixed cube7)
(fixed cube8)
(fixed stick33)
(fixed stick34)
(fixed stick35)
(fixed stick36)
(positionfree initlocstick38)
(atplace stick38 finallocstick38)
(accessible stick38)
(stacked stick38 stick33)
(stacked stick38 stick34)
(atfinalposition stick38)
(positionfree initlocstick37)
(atplace stick37 finallocstick37)
(accessible stick37)
(stacked stick37 cube7)
(stacked stick37 stick33)
(atfinalposition stick37)
(positionfree initlocstick39)
(atplace stick39 finallocstick39)
(accessible stick39)
(stacked stick39 stick34)
(stacked stick39 stick35)
(atfinalposition stick39)
  )
  (:goal 
    (and
      (holding robot1 stick40)
(not (atplace stick40 initlocstick40))
(not (gripperempty robot1))
(not (clear stick40))
(positionfree initlocstick40)
    ) 
  )
)