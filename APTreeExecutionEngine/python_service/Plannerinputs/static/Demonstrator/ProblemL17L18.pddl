(define (problem demonstrator)
  (:domain trusshl)
  (:objects
    ;; Base elements (layer 16, pre-placed)
    stick69 - stick
    stick70 - stick
    stick71 - stick
    stick72 - stick
    cube15 - cube

    ;; Active elements - layer 17
    stick73 - stick
    stick74 - stick
    stick75 - stick
    stick76 - stick

    ;; Active elements - layer 18
    stick77 - stick
    stick78 - stick
    stick79 - stick
    cube16 - cube

    ;; Layers
    layer16 - stack
    layer17 - stack
    layer18 - stack

    ;; Agent
    robot1 - robot

    ;; Tools
    gripper1 - gripper
    staplergun1 - staplergun

    ;; Equip Positions
   ; equiplocgripper - equipposition
   ; equiplocstapler - equipposition

    ;; Robot Positions
   ; rppickup - rppickup
   ; rpmanipulate - rpmanipulate
   ; rptoolchange - rptoolchange

    ;; Locations - Initial (active elements only)
    initlocstick73 - firstposition
    initlocstick74 - firstposition
    initlocstick75 - firstposition
    initlocstick76 - firstposition
    initlocstick77 - firstposition
    initlocstick78 - firstposition
    initlocstick79 - firstposition
    initloccube16 - firstposition

    ;; Locations - Final (base + active)
    finallocstick69 - finalposition
    finallocstick70 - finalposition
    finallocstick71 - finalposition
    finallocstick72 - finalposition
    finalloccube15 - finalposition
    finallocstick73 - finalposition
    finallocstick74 - finalposition
    finallocstick75 - finalposition
    finallocstick76 - finalposition
    finallocstick77 - finalposition
    finallocstick78 - finalposition
    finallocstick79 - finalposition
    finalloccube16 - finalposition
  )

  (:init
    ;; BelongsToLayer
    (belongstolayer stick69 layer16)
    (belongstolayer stick70 layer16)
    (belongstolayer stick71 layer16)
    (belongstolayer stick72 layer16)
    (belongstolayer cube15 layer16)
    (belongstolayer stick73 layer17)
    (belongstolayer stick74 layer17)
    (belongstolayer stick75 layer17)
    (belongstolayer stick76 layer17)
    (belongstolayer stick77 layer18)
    (belongstolayer stick78 layer18)
    (belongstolayer stick79 layer18)
    (belongstolayer cube16 layer18)

    ;; Clear (active + base elements)
    (clear stick69)
    (clear stick70)
    (clear stick71)
    (clear stick72)
    (clear cube15)
    (clear stick73)
    (clear stick74)
    (clear stick75)
    (clear stick76)
    (clear stick77)
    (clear stick78)
    (clear stick79)
    (clear cube16)

    ;; AtPlace - active elements at initial locations
    (atplace stick73 initlocstick73)
    (atplace stick74 initlocstick74)
    (atplace stick75 initlocstick75)
    (atplace stick76 initlocstick76)
    (atplace stick77 initlocstick77)
    (atplace stick78 initlocstick78)
    (atplace stick79 initlocstick79)
    (atplace cube16 initloccube16)

    ;; GripperEmpty
    (gripperempty robot1)
    ;(atagent robot1 rppickup)
    (hastool robot1 gripper1)
    ;(attool staplergun1 equiplocstapler)

    ;; ObjectFinalPosition (active elements)
    (objectfinalposition stick73 finallocstick73)
    (objectfinalposition stick74 finallocstick74)
    (objectfinalposition stick75 finallocstick75)
    (objectfinalposition stick76 finallocstick76)
    (objectfinalposition stick77 finallocstick77)
    (objectfinalposition stick78 finallocstick78)
    (objectfinalposition stick79 finallocstick79)
    (objectfinalposition cube16 finalloccube16)

    ;; Base elements (layer 16, pre-placed)
    (fixed stick69)
    (fixed stick70)
    (fixed stick71)
    (fixed stick72)
    (fixed cube15)
    (atfinalposition stick69)
    (atfinalposition stick70)
    (atfinalposition stick71)
    (atfinalposition stick72)
    (atfinalposition cube15)
    (atplace stick69 finallocstick69)
    (atplace stick70 finallocstick70)
    (atplace stick71 finallocstick71)
    (atplace stick72 finallocstick72)
    (atplace cube15 finalloccube15)
    (accessible stick69)
    (accessible stick70)
    (accessible stick71)
    (accessible stick72)
    (accessible cube15)
  )

  (:goal (and
    ;; Stacked - layer 17
    (stacked stick73 cube15)
    (stacked stick73 stick69)
    (stacked stick74 stick69)
    (stacked stick74 stick70)
    (stacked stick75 stick70)
    (stacked stick75 stick71)
    (stacked stick76 stick71)
    (stacked stick76 stick72)

    ;; Nailed - layer 17
    ;(nailed stick73 cube15)
    ;(nailed stick73 stick69)
    ;(nailed stick74 stick69)
    ;(nailed stick74 stick70)
    ;(nailed stick75 stick70)
    ;(nailed stick75 stick71)
    ;(nailed stick76 stick71)
    ;(nailed stick76 stick72)

    ;; Stacked - layer 18
    (stacked cube16 stick73)
    (stacked stick77 stick73)
    (stacked stick77 stick74)
    (stacked stick78 stick74)
    (stacked stick78 stick75)
    (stacked stick79 stick75)
    (stacked stick79 stick76)

    ;; Nailed - layer 18
    (nailed cube16 stick73)
    (nailed stick77 stick73)
    (nailed stick77 stick74)
    (nailed stick78 stick74)
    (nailed stick78 stick75)
    (nailed stick79 stick75)
    (nailed stick79 stick76)
  ))
)
