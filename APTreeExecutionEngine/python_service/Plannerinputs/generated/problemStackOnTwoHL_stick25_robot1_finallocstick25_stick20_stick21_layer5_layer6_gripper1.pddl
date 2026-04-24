(define (problem stackontwohl)
  (:domain trussml)
  (:objects 
    ;; Base elements (layer 4, pre-placed)
    stick15 - stick
    stick16 - stick
    stick17 - stick
    stick18 - stick
    cube3 - cube
    cube4 - cube

    ;; Active elements - layer 5
    stick19 - stick
    stick20 - stick
    stick21 - stick
    stick22 - stick
    stick23 - stick

    ;; Active elements - layer 6
    stick24 - stick
    stick25 - stick
    stick26 - stick
    stick27 - stick
    cube5 - cube
    cube6 - cube

    ;; Layers
    layer4 - stack
    layer5 - stack
    layer6 - stack

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

    ;; robotpositions
    

    ;; Locations - Initial (active elements only)
    initlocstick19 - firstposition
    initlocstick20 - firstposition
    initlocstick21 - firstposition
    initlocstick22 - firstposition
    initlocstick23 - firstposition
    initlocstick24 - firstposition
    initlocstick25 - firstposition
    initlocstick26 - firstposition
    initlocstick27 - firstposition
    initloccube5 - firstposition
    initloccube6 - firstposition

    ;; Locations - Final (base + active)
    finallocstick15 - finalposition
    finallocstick16 - finalposition
    finallocstick17 - finalposition
    finallocstick18 - finalposition
    finalloccube3 - finalposition
    finalloccube4 - finalposition
    finallocstick19 - finalposition
    finallocstick20 - finalposition
    finallocstick21 - finalposition
    finallocstick22 - finalposition
    finallocstick23 - finalposition
    finallocstick24 - finalposition
    finallocstick25 - finalposition
    finallocstick26 - finalposition
    finallocstick27 - finalposition
    finalloccube5 - finalposition
    finalloccube6 - finalposition
    ;; HARDCODED ML-only objects (not in HL problem files)
    equiplocgripper - equipposition
    equiplocstapler - equipposition
    rppickup - rppickup
    rpmanipulate - rpmanipulate
    rptoolchange - rptoolchange
  )
  (:init  
    (robotequipped robot1)
(objectfinalposition stick15 finallocstick15)
(objectfinalposition stick16 finallocstick16)
(objectfinalposition stick17 finallocstick17)
(objectfinalposition stick18 finallocstick18)
(objectfinalposition stick19 finallocstick19)
(objectfinalposition stick20 finallocstick20)
(objectfinalposition stick21 finallocstick21)
(objectfinalposition stick22 finallocstick22)
(objectfinalposition stick23 finallocstick23)
(objectfinalposition stick24 finallocstick24)
(objectfinalposition stick25 finallocstick25)
(objectfinalposition stick26 finallocstick26)
(objectfinalposition stick27 finallocstick27)
(objectfinalposition cube3 finalloccube3)
(objectfinalposition cube4 finalloccube4)
(objectfinalposition cube5 finalloccube5)
(objectfinalposition cube6 finalloccube6)
(positionfree equiplocgripper)
(belongstolayer stick15 layer4)
(belongstolayer stick16 layer4)
(belongstolayer stick17 layer4)
(belongstolayer stick18 layer4)
(belongstolayer cube3 layer4)
(belongstolayer cube4 layer4)
(belongstolayer stick19 layer5)
(belongstolayer stick20 layer5)
(belongstolayer stick21 layer5)
(belongstolayer stick22 layer5)
(belongstolayer stick23 layer5)
(belongstolayer stick24 layer6)
(belongstolayer stick25 layer6)
(belongstolayer stick26 layer6)
(belongstolayer stick27 layer6)
(belongstolayer cube5 layer6)
(belongstolayer cube6 layer6)
(hastool robot1 gripper1)
(clear stick15)
(clear stick16)
(clear stick17)
(clear stick18)
(clear stick19)
(clear stick20)
(clear stick21)
(clear stick22)
(clear stick23)
(clear stick24)
(clear stick26)
(clear stick27)
(clear cube3)
(clear cube4)
(clear cube5)
(clear cube6)
(attool staplergun1 equiplocstapler)
(atplace stick22 initlocstick22)
(atplace stick23 initlocstick23)
(atplace stick26 initlocstick26)
(atplace stick27 initlocstick27)
(atplace cube6 initloccube6)
(atagent robot1 rppickup)
(atplace stick15 finallocstick15)
(atfinalposition stick15)
(atfinalposition cube3)
(atplace cube3 finalloccube3)
(atplace stick16 finallocstick16)
(atfinalposition stick16)
(atfinalposition cube4)
(atplace cube4 finalloccube4)
(accessible cube4)
(atplace stick17 finallocstick17)
(atfinalposition stick17)
(fixed stick17)
(fixed cube3)
(fixed cube4)
(fixed stick15)
(fixed stick16)
(atplace stick18 finallocstick18)
(accessible stick18)
(atfinalposition stick18)
(fixed stick18)
(positionfree initlocstick20)
(atplace stick20 finallocstick20)
(stacked stick20 stick15)
(stacked stick20 stick16)
(atfinalposition stick20)
(positionfree initlocstick19)
(atplace stick19 finallocstick19)
(stacked stick19 cube3)
(stacked stick19 stick15)
(atfinalposition stick19)
(positionfree initlocstick21)
(atplace stick21 finallocstick21)
(accessible stick21)
(stacked stick21 stick16)
(stacked stick21 stick17)
(atfinalposition stick21)
(positionfree initlocstick24)
(atplace stick24 finallocstick24)
(accessible stick24)
(stacked stick24 stick20)
(stacked stick24 stick19)
(atfinalposition stick24)
(positionfree initloccube5)
(atfinalposition cube5)
(atplace cube5 finalloccube5)
(accessible cube5)
(stacked cube5 stick19)
(holding robot1 stick25)
(positionfree initlocstick25)
  )
  (:goal 
    (and
      (not (holding robot1 stick25))
(atplace stick25 finallocstick25)
(gripperempty robot1)
(clear stick25)
(accessible stick25)
(stacked stick25 stick20)
(stacked stick25 stick21)
(atfinalposition stick25)
(not (accessible stick20))
(not (accessible stick21))
    ) 
  )
)