(define (problem pickuphl)
  (:domain trussml)
  (:objects 
    ;; Base elements (layer 10, pre-placed)
    stick42 - stick
    stick43 - stick
    stick44 - stick
    stick45 - stick
    cube9 - cube
    cube10 - cube

    ;; Active elements - layer 11
    stick46 - stick
    stick47 - stick
    stick48 - stick
    stick49 - stick
    stick50 - stick

    ;; Active elements - layer 12
    stick51 - stick
    stick52 - stick
    stick53 - stick
    stick54 - stick
    cube11 - cube
    cube12 - cube

    ;; Layers
    layer10 - stack
    layer11 - stack
    layer12 - stack

    ;; Agent
    robot1 - robot

    ;; Tools
    gripper1 - gripper
    staplergun1 - staplergun

    ;; Equip Positions
    ;equiplocgripper - equipposition
    ;equiplocstapler - equipposition

    ;; Robot Positions
   ; rppickup - rppickup
   ; rpmanipulate - rpmanipulate
    ;rptoolchange - rptoolchange

    ;; Locations - Initial (active elements only)
    initlocstick46 - firstposition
    initlocstick47 - firstposition
    initlocstick48 - firstposition
    initlocstick49 - firstposition
    initlocstick50 - firstposition
    initlocstick51 - firstposition
    initlocstick52 - firstposition
    initlocstick53 - firstposition
    initlocstick54 - firstposition
    initloccube11 - firstposition
    initloccube12 - firstposition

    ;; Locations - Final (base + active)
    finallocstick42 - finalposition
    finallocstick43 - finalposition
    finallocstick44 - finalposition
    finallocstick45 - finalposition
    finalloccube9 - finalposition
    finalloccube10 - finalposition
    finallocstick46 - finalposition
    finallocstick47 - finalposition
    finallocstick48 - finalposition
    finallocstick49 - finalposition
    finallocstick50 - finalposition
    finallocstick51 - finalposition
    finallocstick52 - finalposition
    finallocstick53 - finalposition
    finallocstick54 - finalposition
    finalloccube11 - finalposition
    finalloccube12 - finalposition
    ;; HARDCODED ML-only objects (not in HL problem files)
    equiplocgripper - equipposition
    equiplocstapler - equipposition
    rppickup - rppickup
    rpmanipulate - rpmanipulate
    rptoolchange - rptoolchange
  )
  (:init  
    (robotequipped robot1)
(objectfinalposition stick42 finallocstick42)
(objectfinalposition stick43 finallocstick43)
(objectfinalposition stick44 finallocstick44)
(objectfinalposition stick45 finallocstick45)
(objectfinalposition stick46 finallocstick46)
(objectfinalposition stick47 finallocstick47)
(objectfinalposition stick48 finallocstick48)
(objectfinalposition stick49 finallocstick49)
(objectfinalposition stick50 finallocstick50)
(objectfinalposition stick51 finallocstick51)
(objectfinalposition stick52 finallocstick52)
(objectfinalposition stick53 finallocstick53)
(objectfinalposition stick54 finallocstick54)
(objectfinalposition cube9 finalloccube9)
(objectfinalposition cube10 finalloccube10)
(objectfinalposition cube11 finalloccube11)
(objectfinalposition cube12 finalloccube12)
(positionfree equiplocgripper)
(gripperempty robot1)
(belongstolayer stick42 layer10)
(belongstolayer stick43 layer10)
(belongstolayer stick44 layer10)
(belongstolayer stick45 layer10)
(belongstolayer cube9 layer10)
(belongstolayer cube10 layer10)
(belongstolayer stick46 layer11)
(belongstolayer stick47 layer11)
(belongstolayer stick48 layer11)
(belongstolayer stick49 layer11)
(belongstolayer stick50 layer11)
(belongstolayer stick51 layer12)
(belongstolayer stick52 layer12)
(belongstolayer stick53 layer12)
(belongstolayer stick54 layer12)
(belongstolayer cube11 layer12)
(belongstolayer cube12 layer12)
(hastool robot1 gripper1)
(clear stick42)
(clear stick43)
(clear stick44)
(clear stick45)
(clear stick46)
(clear stick47)
(clear stick48)
(clear stick49)
(clear stick50)
(clear stick51)
(clear stick52)
(clear stick53)
(clear stick54)
(clear cube9)
(clear cube10)
(clear cube11)
(clear cube12)
(attool staplergun1 equiplocstapler)
(atplace stick49 initlocstick49)
(atplace stick50 initlocstick50)
(atplace stick51 initlocstick51)
(atplace stick52 initlocstick52)
(atplace stick53 initlocstick53)
(atplace stick54 initlocstick54)
(atplace cube11 initloccube11)
(atplace cube12 initloccube12)
(atagent robot1 rpmanipulate)
(atplace stick42 finallocstick42)
(atfinalposition stick42)
(atfinalposition cube9)
(atplace cube9 finalloccube9)
(atplace stick43 finallocstick43)
(atfinalposition stick43)
(atfinalposition cube10)
(atplace cube10 finalloccube10)
(accessible cube10)
(atplace stick44 finallocstick44)
(atfinalposition stick44)
(fixed stick43)
(fixed stick44)
(fixed cube9)
(fixed cube10)
(fixed stick42)
(atplace stick45 finallocstick45)
(accessible stick45)
(atfinalposition stick45)
(fixed stick45)
(positionfree initlocstick47)
(atplace stick47 finallocstick47)
(accessible stick47)
(stacked stick47 stick42)
(stacked stick47 stick43)
(atfinalposition stick47)
(positionfree initlocstick46)
(atplace stick46 finallocstick46)
(accessible stick46)
(stacked stick46 cube9)
(stacked stick46 stick42)
(atfinalposition stick46)
(positionfree initlocstick48)
(atplace stick48 finallocstick48)
(accessible stick48)
(stacked stick48 stick43)
(stacked stick48 stick44)
(atfinalposition stick48)
  )
  (:goal 
    (and
      (holding robot1 stick51)
(not (atplace stick51 initlocstick51))
(not (gripperempty robot1))
(not (clear stick51))
(positionfree initlocstick51)
    ) 
  )
)