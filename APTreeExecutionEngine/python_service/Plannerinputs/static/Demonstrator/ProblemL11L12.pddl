(define (problem demonstrator)
  (:domain trusshl)
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
    equiplocgripper - equipposition
    equiplocstapler - equipposition

    ;; Robot Positions
    rppickup - rppickup
    rpmanipulate - rpmanipulate
    rptoolchange - rptoolchange

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
    finloccube9 - finalposition
    finloccube10 - finalposition
    finallocstick46 - finalposition
    finallocstick47 - finalposition
    finallocstick48 - finalposition
    finallocstick49 - finalposition
    finallocstick50 - finalposition
    finallocstick51 - finalposition
    finallocstick52 - finalposition
    finallocstick53 - finalposition
    finallocstick54 - finalposition
    finloccube11 - finalposition
    finloccube12 - finalposition
  )

  (:init
    ;; BelongsToLayer
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

    ;; Clear (active + base elements)
    (clear stick42)
    (clear stick43)
    (clear stick44)
    (clear stick45)
    (clear cube9)
    (clear cube10)
    (clear stick46)
    (clear stick47)
    (clear stick48)
    (clear stick49)
    (clear stick50)
    (clear stick51)
    (clear stick52)
    (clear stick53)
    (clear stick54)
    (clear cube11)
    (clear cube12)

    ;; AtPlace - active elements at initial locations
    (atplace stick46 initlocstick46)
    (atplace stick47 initlocstick47)
    (atplace stick48 initlocstick48)
    (atplace stick49 initlocstick49)
    (atplace stick50 initlocstick50)
    (atplace stick51 initlocstick51)
    (atplace stick52 initlocstick52)
    (atplace stick53 initlocstick53)
    (atplace stick54 initlocstick54)
    (atplace cube11 initloccube11)
    (atplace cube12 initloccube12)

    ;; GripperEmpty
    (gripperempty robot1)
    (atagent robot1 rppickup)
    (hastool robot1 gripper1)
    (attool staplergun1 equiplocstapler)

    ;; ObjectFinalPosition (active elements)
    (objectfinalposition stick46 finallocstick46)
    (objectfinalposition stick47 finallocstick47)
    (objectfinalposition stick48 finallocstick48)
    (objectfinalposition stick49 finallocstick49)
    (objectfinalposition stick50 finallocstick50)
    (objectfinalposition stick51 finallocstick51)
    (objectfinalposition stick52 finallocstick52)
    (objectfinalposition stick53 finallocstick53)
    (objectfinalposition stick54 finallocstick54)
    (objectfinalposition cube11 finloccube11)
    (objectfinalposition cube12 finloccube12)

    ;; Base elements (layer 10, pre-placed)
    (fixed stick42)
    (fixed stick43)
    (fixed stick44)
    (fixed stick45)
    (fixed cube9)
    (fixed cube10)
    (atfinalposition stick42)
    (atfinalposition stick43)
    (atfinalposition stick44)
    (atfinalposition stick45)
    (atfinalposition cube9)
    (atfinalposition cube10)
    (atplace stick42 finallocstick42)
    (atplace stick43 finallocstick43)
    (atplace stick44 finallocstick44)
    (atplace stick45 finallocstick45)
    (atplace cube9 finloccube9)
    (atplace cube10 finloccube10)
    (accessible stick42)
    (accessible stick43)
    (accessible stick44)
    (accessible stick45)
    (accessible cube9)
    (accessible cube10)
  )

  (:goal (and
    ;; Stacked - layer 11
    (stacked stick46 cube9)
    (stacked stick46 stick42)
    (stacked stick47 stick42)
    (stacked stick47 stick43)
    (stacked stick48 stick43)
    (stacked stick48 stick44)
    (stacked stick49 stick44)
    (stacked stick49 stick45)
    (stacked stick50 cube10)
    (stacked stick50 stick45)

    ;; Nailed - layer 11
    (nailed stick46 cube9)
    (nailed stick46 stick42)
    (nailed stick47 stick42)
    (nailed stick47 stick43)
    (nailed stick48 stick43)
    (nailed stick48 stick44)
    (nailed stick49 stick44)
    (nailed stick49 stick45)
    (nailed stick50 cube10)
    (nailed stick50 stick45)

    ;; Stacked - layer 12
    (stacked cube11 stick46)
    (stacked cube12 stick50)
    (stacked stick51 stick46)
    (stacked stick51 stick47)
    (stacked stick52 stick47)
    (stacked stick52 stick48)
    (stacked stick53 stick48)
    (stacked stick53 stick49)
    (stacked stick54 stick49)
    (stacked stick54 stick50)

    ;; Nailed - layer 12
    (nailed cube11 stick46)
    (nailed cube12 stick50)
    (nailed stick51 stick46)
    (nailed stick51 stick47)
    (nailed stick52 stick47)
    (nailed stick52 stick48)
    (nailed stick53 stick48)
    (nailed stick53 stick49)
    (nailed stick54 stick49)
    (nailed stick54 stick50)
  ))
)
