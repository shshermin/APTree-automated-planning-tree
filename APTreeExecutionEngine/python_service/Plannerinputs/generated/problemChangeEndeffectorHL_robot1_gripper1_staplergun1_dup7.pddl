(define (problem changeendeffectorhl)
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
    equiplocgripper - equipposition
    equiplocstapler - equipposition

    ;; Robot Positions
    rppickup - rppickup
    rpmanipulate - rpmanipulate
    rptoolchange - rptoolchange

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
    finloccube3 - finalposition
    finloccube4 - finalposition
    finallocstick19 - finalposition
    finallocstick20 - finalposition
    finallocstick21 - finalposition
    finallocstick22 - finalposition
    finallocstick23 - finalposition
    finallocstick24 - finalposition
    finallocstick25 - finalposition
    finallocstick26 - finalposition
    finallocstick27 - finalposition
    finloccube5 - finalposition
    finloccube6 - finalposition
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
(objectfinalposition cube3 finloccube3)
(objectfinalposition cube4 finloccube4)
(objectfinalposition cube5 finloccube5)
(objectfinalposition cube6 finloccube6)
(positionfree equiplocgripper)
(gripperempty robot1)
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
(clear stick25)
(clear stick26)
(clear stick27)
(clear cube3)
(clear cube4)
(clear cube5)
(clear cube6)
(attool staplergun1 equiplocstapler)
(atplace stick25 initlocstick25)
(atplace stick26 initlocstick26)
(atplace stick27 initlocstick27)
(atplace cube6 initloccube6)
(atagent robot1 rpmanipulate)
(atplace stick15 finallocstick15)
(atfinalposition stick15)
(atplace cube3 finloccube3)
(atfinalposition cube3)
(fixed cube3)
(fixed stick15)
(atplace stick16 finallocstick16)
(atfinalposition stick16)
(atplace cube4 finloccube4)
(atfinalposition cube4)
(atplace stick17 finallocstick17)
(atfinalposition stick17)
(atplace stick18 finallocstick18)
(atfinalposition stick18)
(fixed cube4)
(fixed stick16)
(fixed stick17)
(fixed stick18)
(positionfree initlocstick20)
(atplace stick20 finallocstick20)
(stacked stick20 stick15)
(stacked stick20 stick16)
(atfinalposition stick20)
(positionfree initlocstick19)
(atplace stick19 finallocstick19)
(stacked stick19 stick15)
(stacked stick19 cube3)
(atfinalposition stick19)
(nailed stick20 stick15)
(fixed stick20)
(nailed stick19 cube3)
(fixed stick19)
(nailed stick19 stick15)
(nailed stick20 stick16)
(positionfree initlocstick24)
(atplace stick24 finallocstick24)
(accessible stick24)
(stacked stick24 stick20)
(stacked stick24 stick19)
(atfinalposition stick24)
(positionfree initlocstick22)
(atplace stick22 finallocstick22)
(accessible stick22)
(stacked stick22 stick18)
(stacked stick22 stick17)
(atfinalposition stick22)
(positionfree initlocstick21)
(atplace stick21 finallocstick21)
(accessible stick21)
(stacked stick21 stick17)
(stacked stick21 stick16)
(atfinalposition stick21)
(positionfree initlocstick23)
(atplace stick23 finallocstick23)
(accessible stick23)
(stacked stick23 cube4)
(stacked stick23 stick18)
(atfinalposition stick23)
(positionfree initloccube5)
(atplace cube5 finloccube5)
(accessible cube5)
(stacked cube5 stick19)
(stacked cube5 stick20)
(atfinalposition cube5)
  )
  (:goal 
    (and
      (not (hastool robot1 gripper1))
(hastool robot1 staplergun1)
    ) 
  )
)