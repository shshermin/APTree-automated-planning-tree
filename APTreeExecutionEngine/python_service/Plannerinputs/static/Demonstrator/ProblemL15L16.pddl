(define (problem demonstrator)
  (:domain trusshl)
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
  )

  (:init
    ;; BelongsToLayer
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

    ;; Clear (active + base elements)
    (clear stick60)
    (clear stick61)
    (clear stick62)
    (clear stick63)
    (clear cube13)
    (clear cube14)
    (clear stick64)
    (clear stick65)
    (clear stick66)
    (clear stick67)
    (clear stick68)
    (clear stick69)
    (clear stick70)
    (clear stick71)
    (clear stick72)
    (clear cube15)

    ;; AtPlace - active elements at initial locations
    (atplace stick64 initlocstick64)
    (atplace stick65 initlocstick65)
    (atplace stick66 initlocstick66)
    (atplace stick67 initlocstick67)
    (atplace stick68 initlocstick68)
    (atplace stick69 initlocstick69)
    (atplace stick70 initlocstick70)
    (atplace stick71 initlocstick71)
    (atplace stick72 initlocstick72)
    (atplace cube15 initloccube15)

    ;; GripperEmpty
    (gripperempty robot1)
    ;(atagent robot1 rppickup)
    (hastool robot1 gripper1)
    ;(attool staplergun1 equiplocstapler)

    ;; ObjectFinalPosition (active elements)
    (objectfinalposition stick64 finallocstick64)
    (objectfinalposition stick65 finallocstick65)
    (objectfinalposition stick66 finallocstick66)
    (objectfinalposition stick67 finallocstick67)
    (objectfinalposition stick68 finallocstick68)
    (objectfinalposition stick69 finallocstick69)
    (objectfinalposition stick70 finallocstick70)
    (objectfinalposition stick71 finallocstick71)
    (objectfinalposition stick72 finallocstick72)
    (objectfinalposition cube15 finalloccube15)

    ;; Base elements (layer 14, pre-placed)
    (fixed stick60)
    (fixed stick61)
    (fixed stick62)
    (fixed stick63)
    (fixed cube13)
    (fixed cube14)
    (atfinalposition stick60)
    (atfinalposition stick61)
    (atfinalposition stick62)
    (atfinalposition stick63)
    (atfinalposition cube13)
    (atfinalposition cube14)
    (atplace stick60 finallocstick60)
    (atplace stick61 finallocstick61)
    (atplace stick62 finallocstick62)
    (atplace stick63 finallocstick63)
    (atplace cube13 finalloccube13)
    (atplace cube14 finalloccube14)
    (accessible stick60)
    (accessible stick61)
    (accessible stick62)
    (accessible stick63)
    (accessible cube13)
    (accessible cube14)
  )

  (:goal (and
    ;; Stacked - layer 15
    (stacked stick64 cube13)
    (stacked stick64 stick60)
    (stacked stick65 stick60)
    (stacked stick65 stick61)
    (stacked stick66 stick61)
    (stacked stick66 stick62)
    (stacked stick67 stick62)
    (stacked stick67 stick63)
    (stacked stick68 cube14)
    (stacked stick68 stick63)

    ;; Nailed - layer 15
    ;(nailed stick64 cube13)
    ;(nailed stick64 stick60)
    ;(nailed stick65 stick60)
    ;(nailed stick65 stick61)
    ;(nailed stick66 stick61)
    ;(nailed stick66 stick62)
    ;(nailed stick67 stick62)
    ;(nailed stick67 stick63)
    ;(nailed stick68 cube14)
    ;(nailed stick68 stick63)

    ;; Stacked - layer 16
    (stacked cube15 stick64)
    (stacked stick69 stick64)
    (stacked stick69 stick65)
    (stacked stick70 stick65)
    (stacked stick70 stick66)
    (stacked stick71 stick66)
    (stacked stick71 stick67)
    (stacked stick72 stick67)
    (stacked stick72 stick68)

    ;; Nailed - layer 16
    (nailed cube15 stick64)
    (nailed stick69 stick64)
    (nailed stick69 stick65)
    (nailed stick70 stick65)
    (nailed stick70 stick66)
    (nailed stick71 stick66)
    (nailed stick71 stick67)
    (nailed stick72 stick67)
    (nailed stick72 stick68)
  ))
)
