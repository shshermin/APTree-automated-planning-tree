(define (problem demonstrator)
  (:domain trusshl)
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
    equiplocgripper - equipposition
    equiplocstapler - equipposition

    ;; Robot Positions
    rppickup - rppickup
    rpmanipulate - rpmanipulate
    rptoolchange - rptoolchange

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
    finloccube7 - finalposition
    finloccube8 - finalposition
    finallocstick37 - finalposition
    finallocstick38 - finalposition
    finallocstick39 - finalposition
    finallocstick40 - finalposition
    finallocstick41 - finalposition
    finallocstick42 - finalposition
    finallocstick43 - finalposition
    finallocstick44 - finalposition
    finallocstick45 - finalposition
    finloccube9 - finalposition
    finloccube10 - finalposition
  )

  (:init
    ;; BelongsToLayer
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

    ;; Clear (active + base elements)
    (clear stick33)
    (clear stick34)
    (clear stick35)
    (clear stick36)
    (clear cube7)
    (clear cube8)
    (clear stick37)
    (clear stick38)
    (clear stick39)
    (clear stick40)
    (clear stick41)
    (clear stick42)
    (clear stick43)
    (clear stick44)
    (clear stick45)
    (clear cube9)
    (clear cube10)

    ;; AtPlace - active elements at initial locations
    (atplace stick37 initlocstick37)
    (atplace stick38 initlocstick38)
    (atplace stick39 initlocstick39)
    (atplace stick40 initlocstick40)
    (atplace stick41 initlocstick41)
    (atplace stick42 initlocstick42)
    (atplace stick43 initlocstick43)
    (atplace stick44 initlocstick44)
    (atplace stick45 initlocstick45)
    (atplace cube9 initloccube9)
    (atplace cube10 initloccube10)

    ;; GripperEmpty
    (gripperempty robot1)
    (atagent robot1 rppickup)
    (hastool robot1 gripper1)
    (attool staplergun1 equiplocstapler)

    ;; ObjectFinalPosition (active elements)
    (objectfinalposition stick37 finallocstick37)
    (objectfinalposition stick38 finallocstick38)
    (objectfinalposition stick39 finallocstick39)
    (objectfinalposition stick40 finallocstick40)
    (objectfinalposition stick41 finallocstick41)
    (objectfinalposition stick42 finallocstick42)
    (objectfinalposition stick43 finallocstick43)
    (objectfinalposition stick44 finallocstick44)
    (objectfinalposition stick45 finallocstick45)
    (objectfinalposition cube9 finloccube9)
    (objectfinalposition cube10 finloccube10)

    ;; Base elements (layer 8, pre-placed)
    (fixed stick33)
    (fixed stick34)
    (fixed stick35)
    (fixed stick36)
    (fixed cube7)
    (fixed cube8)
    (atfinalposition stick33)
    (atfinalposition stick34)
    (atfinalposition stick35)
    (atfinalposition stick36)
    (atfinalposition cube7)
    (atfinalposition cube8)
    (atplace stick33 finallocstick33)
    (atplace stick34 finallocstick34)
    (atplace stick35 finallocstick35)
    (atplace stick36 finallocstick36)
    (atplace cube7 finloccube7)
    (atplace cube8 finloccube8)
    (accessible stick33)
    (accessible stick34)
    (accessible stick35)
    (accessible stick36)
    (accessible cube7)
    (accessible cube8)
  )

  (:goal (and
    ;; Stacked - layer 9
    (stacked stick37 cube7)
    (stacked stick37 stick33)
    (stacked stick38 stick33)
    (stacked stick38 stick34)
    (stacked stick39 stick34)
    (stacked stick39 stick35)
    (stacked stick40 stick35)
    (stacked stick40 stick36)
    (stacked stick41 cube8)
    (stacked stick41 stick36)

    ;; Nailed - layer 9
    (nailed stick37 cube7)
    (nailed stick37 stick33)
    (nailed stick38 stick33)
    (nailed stick38 stick34)
    (nailed stick39 stick34)
    (nailed stick39 stick35)
    (nailed stick40 stick35)
    (nailed stick40 stick36)
    (nailed stick41 cube8)
    (nailed stick41 stick36)

    ;; Stacked - layer 10
    (stacked cube9 stick37)
    (stacked cube10 stick41)
    (stacked stick42 stick37)
    (stacked stick42 stick38)
    (stacked stick43 stick38)
    (stacked stick43 stick39)
    (stacked stick44 stick39)
    (stacked stick44 stick40)
    (stacked stick45 stick40)
    (stacked stick45 stick41)

    ;; Nailed - layer 10
    (nailed cube9 stick37)
    (nailed cube10 stick41)
    (nailed stick42 stick37)
    (nailed stick42 stick38)
    (nailed stick43 stick38)
    (nailed stick43 stick39)
    (nailed stick44 stick39)
    (nailed stick44 stick40)
    (nailed stick45 stick40)
    (nailed stick45 stick41)
  ))
)
