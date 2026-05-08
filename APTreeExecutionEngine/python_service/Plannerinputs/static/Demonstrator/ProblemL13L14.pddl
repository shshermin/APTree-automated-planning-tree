(define (problem demonstrator)
  (:domain trusshl)
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
    ;equiplocgripper - equipposition
    ;equiplocstapler - equipposition

    ;; Robot Positions
    ;rppickup - rppickup
    ;rpmanipulate - rpmanipulate
    ;rptoolchange - rptoolchange

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
    finalloccube11 - finalposition
    finalloccube12 - finalposition
    finallocstick55 - finalposition
    finallocstick56 - finalposition
    finallocstick57 - finalposition
    finallocstick58 - finalposition
    finallocstick59 - finalposition
    finallocstick60 - finalposition
    finallocstick61 - finalposition
    finallocstick62 - finalposition
    finallocstick63 - finalposition
    finalloccube13 - finalposition
    finalloccube14 - finalposition
  )

  (:init
    ;; BelongsToLayer
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

    ;; Clear (active + base elements)
    (clear stick51)
    (clear stick52)
    (clear stick53)
    (clear stick54)
    (clear cube11)
    (clear cube12)
    (clear stick55)
    (clear stick56)
    (clear stick57)
    (clear stick58)
    (clear stick59)
    (clear stick60)
    (clear stick61)
    (clear stick62)
    (clear stick63)
    (clear cube13)
    (clear cube14)

    ;; AtPlace - active elements at initial locations
    (atplace stick55 initlocstick55)
    (atplace stick56 initlocstick56)
    (atplace stick57 initlocstick57)
    (atplace stick58 initlocstick58)
    (atplace stick59 initlocstick59)
    (atplace stick60 initlocstick60)
    (atplace stick61 initlocstick61)
    (atplace stick62 initlocstick62)
    (atplace stick63 initlocstick63)
    (atplace cube13 initloccube13)
    (atplace cube14 initloccube14)

    ;; GripperEmpty
    (gripperempty robot1)
    ;(atagent robot1 rppickup)
    (hastool robot1 gripper1)
    ;(attool staplergun1 equiplocstapler)

    ;; ObjectFinalPosition (active elements)
    (objectfinalposition stick55 finallocstick55)
    (objectfinalposition stick56 finallocstick56)
    (objectfinalposition stick57 finallocstick57)
    (objectfinalposition stick58 finallocstick58)
    (objectfinalposition stick59 finallocstick59)
    (objectfinalposition stick60 finallocstick60)
    (objectfinalposition stick61 finallocstick61)
    (objectfinalposition stick62 finallocstick62)
    (objectfinalposition stick63 finallocstick63)
    (objectfinalposition cube13 finalloccube13)
    (objectfinalposition cube14 finalloccube14)

    ;; Base elements (layer 12, pre-placed)
    (fixed stick51)
    (fixed stick52)
    (fixed stick53)
    (fixed stick54)
    (fixed cube11)
    (fixed cube12)
    (atfinalposition stick51)
    (atfinalposition stick52)
    (atfinalposition stick53)
    (atfinalposition stick54)
    (atfinalposition cube11)
    (atfinalposition cube12)
    (atplace stick51 finallocstick51)
    (atplace stick52 finallocstick52)
    (atplace stick53 finallocstick53)
    (atplace stick54 finallocstick54)
    (atplace cube11 finalloccube11)
    (atplace cube12 finalloccube12)
    (accessible stick51)
    (accessible stick52)
    (accessible stick53)
    (accessible stick54)
    (accessible cube11)
    (accessible cube12)
  )

  (:goal (and
    ;; Stacked - layer 13
    (stacked stick55 cube11)
    (stacked stick55 stick51)
    (stacked stick56 stick51)
    (stacked stick56 stick52)
    (stacked stick57 stick52)
    (stacked stick57 stick53)
    (stacked stick58 stick53)
    (stacked stick58 stick54)
    (stacked stick59 cube12)
    (stacked stick59 stick54)

    ;; Nailed - layer 13
    ;(nailed stick55 cube11)
    ;(nailed stick55 stick51)
    ;(nailed stick56 stick51)
    ;(nailed stick56 stick52)
    ;(nailed stick57 stick52)
    ;;(nailed stick57 stick53)
    ;(nailed stick58 stick53)
    ;(nailed stick58 stick54)
    ;(nailed stick59 cube12)
    ;(nailed stick59 stick54)

    ;; Stacked - layer 14
    (stacked cube13 stick55)
    (stacked cube14 stick59)
    (stacked stick60 stick55)
    (stacked stick60 stick56)
    (stacked stick61 stick56)
    (stacked stick61 stick57)
    (stacked stick62 stick57)
    (stacked stick62 stick58)
    (stacked stick63 stick58)
    (stacked stick63 stick59)

    ;; Nailed - layer 14
    ;(nailed cube13 stick55)
    ;(nailed cube14 stick59)
    ;(nailed stick60 stick55)
    ;(nailed stick60 stick56)
    ;(nailed stick61 stick56)
    ;(nailed stick61 stick57)
    ;(nailed stick62 stick57)
    ;(nailed stick62 stick58)
    ;(nailed stick63 stick58)
    ;(nailed stick63 stick59)
  ))
)
