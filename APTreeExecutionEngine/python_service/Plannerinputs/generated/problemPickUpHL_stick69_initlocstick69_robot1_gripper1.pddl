(define (problem pickuphl)
  (:domain trussml)
  (:objects 
    ;; Base elements (layer 14, pre-placed)
    stick60 - stick
    stick61 - stick
    stick62 - stick
    stick63 - stick
    cube13 - cube
    cube14 - cube

    ;; Active elements - layer 15
    stick64 - stick
    stick65 - stick
    stick66 - stick
    stick67 - stick
    stick68 - stick

    ;; Active elements - layer 16
    stick69 - stick
    stick70 - stick
    stick71 - stick
    stick72 - stick
    cube15 - cube

    ;; Layers
    layer14 - stack
    layer15 - stack
    layer16 - stack

    ;; Agent
    robot1 - robot

    ;; Tools
    gripper1 - gripper
    staplergun1 - staplergun

    ;; Equip Positions
   ; equiplocgripper - equipposition
    ;equiplocstapler - equipposition

    ;; Robot Positions
   ; rppickup - rppickup
    ;rpmanipulate - rpmanipulate
    ;rptoolchange - rptoolchange

    ;; Locations - Initial (active elements only)
    initlocstick64 - firstposition
    initlocstick65 - firstposition
    initlocstick66 - firstposition
    initlocstick67 - firstposition
    initlocstick68 - firstposition
    initlocstick69 - firstposition
    initlocstick70 - firstposition
    initlocstick71 - firstposition
    initlocstick72 - firstposition
    initloccube15 - firstposition

    ;; Locations - Final (base + active)
    finallocstick60 - finalposition
    finallocstick61 - finalposition
    finallocstick62 - finalposition
    finallocstick63 - finalposition
    finalloccube13 - finalposition
    finalloccube14 - finalposition
    finallocstick64 - finalposition
    finallocstick65 - finalposition
    finallocstick66 - finalposition
    finallocstick67 - finalposition
    finallocstick68 - finalposition
    finallocstick69 - finalposition
    finallocstick70 - finalposition
    finallocstick71 - finalposition
    finallocstick72 - finalposition
    finalloccube15 - finalposition
    ;; HARDCODED ML-only objects (not in HL problem files)
    equiplocgripper - equipposition
    equiplocstapler - equipposition
    rppickup - rppickup
    rpmanipulate - rpmanipulate
    rptoolchange - rptoolchange
  )
  (:init  
    (robotequipped robot1)
(objectfinalposition stick60 finallocstick60)
(objectfinalposition stick61 finallocstick61)
(objectfinalposition stick62 finallocstick62)
(objectfinalposition stick63 finallocstick63)
(objectfinalposition stick64 finallocstick64)
(objectfinalposition stick65 finallocstick65)
(objectfinalposition stick66 finallocstick66)
(objectfinalposition stick67 finallocstick67)
(objectfinalposition stick68 finallocstick68)
(objectfinalposition stick69 finallocstick69)
(objectfinalposition stick70 finallocstick70)
(objectfinalposition stick71 finallocstick71)
(objectfinalposition stick72 finallocstick72)
(objectfinalposition cube13 finalloccube13)
(objectfinalposition cube14 finalloccube14)
(objectfinalposition cube15 finalloccube15)
(positionfree equiplocgripper)
(gripperempty robot1)
(belongstolayer stick60 layer14)
(belongstolayer stick61 layer14)
(belongstolayer stick62 layer14)
(belongstolayer stick63 layer14)
(belongstolayer cube13 layer14)
(belongstolayer cube14 layer14)
(belongstolayer stick64 layer15)
(belongstolayer stick65 layer15)
(belongstolayer stick66 layer15)
(belongstolayer stick67 layer15)
(belongstolayer stick68 layer15)
(belongstolayer stick69 layer16)
(belongstolayer stick70 layer16)
(belongstolayer stick71 layer16)
(belongstolayer stick72 layer16)
(belongstolayer cube15 layer16)
(hastool robot1 gripper1)
(clear stick60)
(clear stick61)
(clear stick62)
(clear stick63)
(clear stick64)
(clear stick65)
(clear stick66)
(clear stick67)
(clear stick68)
(clear stick69)
(clear stick70)
(clear stick71)
(clear stick72)
(clear cube13)
(clear cube14)
(clear cube15)
(attool staplergun1 equiplocstapler)
(atplace stick66 initlocstick66)
(atplace stick67 initlocstick67)
(atplace stick68 initlocstick68)
(atplace stick69 initlocstick69)
(atplace stick70 initlocstick70)
(atplace stick71 initlocstick71)
(atplace stick72 initlocstick72)
(atplace cube15 initloccube15)
(atagent robot1 rpmanipulate)
(atplace stick60 finallocstick60)
(atfinalposition stick60)
(atplace stick61 finallocstick61)
(atfinalposition stick61)
(atfinalposition cube14)
(atplace cube14 finalloccube14)
(accessible cube14)
(atplace stick62 finallocstick62)
(accessible stick62)
(atfinalposition stick62)
(atfinalposition cube13)
(atplace cube13 finalloccube13)
(atplace stick63 finallocstick63)
(accessible stick63)
(atfinalposition stick63)
(positionfree initlocstick65)
(atplace stick65 finallocstick65)
(accessible stick65)
(stacked stick65 stick61)
(stacked stick65 stick60)
(atfinalposition stick65)
(positionfree initlocstick64)
(atplace stick64 finallocstick64)
(accessible stick64)
(stacked stick64 cube13)
(stacked stick64 stick60)
(atfinalposition stick64)
  )
  (:goal 
    (and
      (holding robot1 stick69)
(not (atplace stick69 initlocstick69))
(not (gripperempty robot1))
(not (clear stick69))
(positionfree initlocstick69)
    ) 
  )
)