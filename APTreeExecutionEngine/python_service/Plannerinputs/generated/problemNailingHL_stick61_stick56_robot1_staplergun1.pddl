(define (problem nailinghl)
  (:domain trussml)
  (:objects 
    ;; Base elements (layer 12, pre-placed)
    stick51 - stick
    stick52 - stick
    stick53 - stick
    stick54 - stick
    cube11 - cube
    cube12 - cube

    ;; Active elements - layer 13
    stick55 - stick
    stick56 - stick
    stick57 - stick
    stick58 - stick
    stick59 - stick

    ;; Active elements - layer 14
    stick60 - stick
    stick61 - stick
    stick62 - stick
    stick63 - stick
    cube13 - cube
    cube14 - cube

    ;; Layers
    layer12 - stack
    layer13 - stack
    layer14 - stack

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
    initlocstick55 - firstposition
    initlocstick56 - firstposition
    initlocstick57 - firstposition
    initlocstick58 - firstposition
    initlocstick59 - firstposition
    initlocstick60 - firstposition
    initlocstick61 - firstposition
    initlocstick62 - firstposition
    initlocstick63 - firstposition
    initloccube13 - firstposition
    initloccube14 - firstposition

    ;; Locations - Final (base + active)
    finallocstick51 - finalposition
    finallocstick52 - finalposition
    finallocstick53 - finalposition
    finallocstick54 - finalposition
    finloccube11 - finalposition
    finloccube12 - finalposition
    finallocstick55 - finalposition
    finallocstick56 - finalposition
    finallocstick57 - finalposition
    finallocstick58 - finalposition
    finallocstick59 - finalposition
    finallocstick60 - finalposition
    finallocstick61 - finalposition
    finallocstick62 - finalposition
    finallocstick63 - finalposition
    finloccube13 - finalposition
    finloccube14 - finalposition
  )
  (:init  
    (robotequipped robot1)
(objectfinalposition stick51 finallocstick51)
(objectfinalposition stick52 finallocstick52)
(objectfinalposition stick53 finallocstick53)
(objectfinalposition stick54 finallocstick54)
(objectfinalposition stick55 finallocstick55)
(objectfinalposition stick56 finallocstick56)
(objectfinalposition stick57 finallocstick57)
(objectfinalposition stick58 finallocstick58)
(objectfinalposition stick59 finallocstick59)
(objectfinalposition stick60 finallocstick60)
(objectfinalposition stick61 finallocstick61)
(objectfinalposition stick62 finallocstick62)
(objectfinalposition stick63 finallocstick63)
(objectfinalposition cube11 finloccube11)
(objectfinalposition cube12 finloccube12)
(objectfinalposition cube13 finloccube13)
(objectfinalposition cube14 finloccube14)
(gripperempty robot1)
(belongstolayer stick51 layer12)
(belongstolayer stick52 layer12)
(belongstolayer stick53 layer12)
(belongstolayer stick54 layer12)
(belongstolayer cube11 layer12)
(belongstolayer cube12 layer12)
(belongstolayer stick55 layer13)
(belongstolayer stick56 layer13)
(belongstolayer stick57 layer13)
(belongstolayer stick58 layer13)
(belongstolayer stick59 layer13)
(belongstolayer stick60 layer14)
(belongstolayer stick61 layer14)
(belongstolayer stick62 layer14)
(belongstolayer stick63 layer14)
(belongstolayer cube13 layer14)
(belongstolayer cube14 layer14)
(clear stick51)
(clear stick52)
(clear stick53)
(clear stick54)
(clear stick55)
(clear stick56)
(clear stick57)
(clear stick58)
(clear stick59)
(clear stick60)
(clear stick61)
(clear stick62)
(clear stick63)
(clear cube11)
(clear cube12)
(clear cube13)
(clear cube14)
(atagent robot1 rpmanipulate)
(attool gripper1 equiplocgripper)
(hastool robot1 staplergun1)
(positionfree equiplocstapler)
(activetool staplergun1)
(atplace stick51 finallocstick51)
(atfinalposition stick51)
(atplace cube11 finloccube11)
(atfinalposition cube11)
(fixed cube11)
(fixed stick51)
(atplace stick52 finallocstick52)
(atfinalposition stick52)
(atplace cube12 finloccube12)
(atfinalposition cube12)
(atplace stick53 finallocstick53)
(atfinalposition stick53)
(atplace stick54 finallocstick54)
(atfinalposition stick54)
(fixed cube12)
(fixed stick52)
(fixed stick53)
(fixed stick54)
(positionfree initlocstick56)
(atplace stick56 finallocstick56)
(stacked stick56 stick51)
(stacked stick56 stick52)
(atfinalposition stick56)
(positionfree initlocstick55)
(atplace stick55 finallocstick55)
(stacked stick55 stick51)
(stacked stick55 cube11)
(atfinalposition stick55)
(nailed stick55 cube11)
(fixed stick55)
(nailed stick55 stick51)
(nailed stick56 stick51)
(fixed stick56)
(nailed stick56 stick52)
(positionfree initlocstick60)
(atplace stick60 finallocstick60)
(accessible stick60)
(stacked stick60 stick56)
(stacked stick60 stick55)
(atfinalposition stick60)
(positionfree initlocstick58)
(atplace stick58 finallocstick58)
(stacked stick58 stick54)
(stacked stick58 stick53)
(atfinalposition stick58)
(positionfree initlocstick57)
(atplace stick57 finallocstick57)
(stacked stick57 stick53)
(stacked stick57 stick52)
(atfinalposition stick57)
(positionfree initlocstick59)
(atplace stick59 finallocstick59)
(stacked stick59 cube12)
(stacked stick59 stick54)
(atfinalposition stick59)
(positionfree initloccube13)
(atplace cube13 finloccube13)
(accessible cube13)
(stacked cube13 stick55)
(stacked cube13 stick56)
(atfinalposition cube13)
(nailed stick57 stick52)
(fixed stick57)
(nailed stick57 stick53)
(nailed stick58 stick53)
(fixed stick58)
(nailed stick58 stick54)
(nailed stick59 cube12)
(fixed stick59)
(nailed stick59 stick54)
(nailed cube13 stick55)
(fixed cube13)
(nailed stick60 stick55)
(fixed stick60)
(nailed stick60 stick56)
(positionfree initlocstick62)
(atplace stick62 finallocstick62)
(accessible stick62)
(stacked stick62 stick58)
(stacked stick62 stick57)
(atfinalposition stick62)
(positionfree initloccube14)
(atplace cube14 finloccube14)
(accessible cube14)
(stacked cube14 stick59)
(stacked cube14 stick57)
(atfinalposition cube14)
(positionfree initlocstick61)
(atplace stick61 finallocstick61)
(accessible stick61)
(stacked stick61 stick57)
(stacked stick61 stick56)
(atfinalposition stick61)
(positionfree initlocstick63)
(atplace stick63 finallocstick63)
(accessible stick63)
(stacked stick63 stick58)
(stacked stick63 stick59)
(atfinalposition stick63)
(nailed cube14 stick59)
(fixed cube14)
  )
  (:goal 
    (and
      (nailed stick61 stick56)
(fixed stick61)
    ) 
  )
)