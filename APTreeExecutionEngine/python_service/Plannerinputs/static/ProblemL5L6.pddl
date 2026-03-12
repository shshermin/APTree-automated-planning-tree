(define (problem demonstrator)
  (:domain trusshl)
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
    ;; BelongsToLayer
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

    ;; Clear (active + base elements)
    (clear stick15)
    (clear stick16)
    (clear stick17)
    (clear stick18)
    (clear cube3)
    (clear cube4)
    (clear stick19)
    (clear stick20)
    (clear stick21)
    (clear stick22)
    (clear stick23)
    (clear stick24)
    (clear stick25)
    (clear stick26)
    (clear stick27)
    (clear cube5)
    (clear cube6)

    ;; AtPlace - active elements at initial locations
    (atplace stick19 initlocstick19)
    (atplace stick20 initlocstick20)
    (atplace stick21 initlocstick21)
    (atplace stick22 initlocstick22)
    (atplace stick23 initlocstick23)
    (atplace stick24 initlocstick24)
    (atplace stick25 initlocstick25)
    (atplace stick26 initlocstick26)
    (atplace stick27 initlocstick27)
    (atplace cube5 initloccube5)
    (atplace cube6 initloccube6)

    ;; GripperEmpty
    (gripperempty robot1)
    (hastool robot1 gripper1)

    ;; ObjectFinalPosition (active elements)
    (objectfinalposition stick19 finallocstick19)
    (objectfinalposition stick20 finallocstick20)
    (objectfinalposition stick21 finallocstick21)
    (objectfinalposition stick22 finallocstick22)
    (objectfinalposition stick23 finallocstick23)
    (objectfinalposition stick24 finallocstick24)
    (objectfinalposition stick25 finallocstick25)
    (objectfinalposition stick26 finallocstick26)
    (objectfinalposition stick27 finallocstick27)
    (objectfinalposition cube5 finloccube5)
    (objectfinalposition cube6 finloccube6)

    ;; Base elements (layer 4, pre-placed)
    (fixed stick15)
    (fixed stick16)
    (fixed stick17)
    (fixed stick18)
    (fixed cube3)
    (fixed cube4)
    (atfinalposition stick15)
    (atfinalposition stick16)
    (atfinalposition stick17)
    (atfinalposition stick18)
    (atfinalposition cube3)
    (atfinalposition cube4)
    (atplace stick15 finallocstick15)
    (atplace stick16 finallocstick16)
    (atplace stick17 finallocstick17)
    (atplace stick18 finallocstick18)
    (atplace cube3 finloccube3)
    (atplace cube4 finloccube4)
    (accessible stick15)
    (accessible stick16)
    (accessible stick17)
    (accessible stick18)
    (accessible cube3)
    (accessible cube4)
  )

  (:goal (and
    ;; Stacked - layer 5
    (stacked stick19 cube3)
    (stacked stick19 stick15)
    (stacked stick20 stick15)
    (stacked stick20 stick16)
    (stacked stick21 stick16)
    (stacked stick21 stick17)
    (stacked stick22 stick17)
    (stacked stick22 stick18)
    (stacked stick23 cube4)
    (stacked stick23 stick18)

    ;; Nailed - layer 5
    (nailed stick19 cube3)
    (nailed stick19 stick15)
    (nailed stick20 stick15)
    (nailed stick20 stick16)
    (nailed stick21 stick16)
    (nailed stick21 stick17)
    (nailed stick22 stick17)
    (nailed stick22 stick18)
    (nailed stick23 cube4)
    (nailed stick23 stick18)

    ;; Stacked - layer 6
    (stacked cube5 stick19)
    (stacked cube6 stick23)
    (stacked stick24 stick19)
    (stacked stick24 stick20)
    (stacked stick25 stick20)
    (stacked stick25 stick21)
    (stacked stick26 stick21)
    (stacked stick26 stick22)
    (stacked stick27 stick22)
    (stacked stick27 stick23)

    ;; Nailed - layer 6
    (nailed cube5 stick19)
    (nailed cube6 stick23)
    (nailed stick24 stick19)
    (nailed stick24 stick20)
    (nailed stick25 stick20)
    (nailed stick25 stick21)
    (nailed stick26 stick21)
    (nailed stick26 stick22)
    (nailed stick27 stick22)
    (nailed stick27 stick23)
  ))
)
