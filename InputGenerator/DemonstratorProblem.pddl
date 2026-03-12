(define (problem demonstrator)
  (:domain trusshl)
  (:objects
    ;; Elements - Sticks
    stick1 - stick
    stick2 - stick
    stick3 - stick
    stick4 - stick
    stick5 - stick
    stick6 - stick
    stick7 - stick
    stick8 - stick
    stick9 - stick
    stick10 - stick
    stick11 - stick
    stick12 - stick
    stick13 - stick
    stick14 - stick
    stick15 - stick
    stick16 - stick
    stick17 - stick
    stick18 - stick
    stick19 - stick
    stick20 - stick
    stick21 - stick
    stick22 - stick
    stick23 - stick
    stick24 - stick
    stick25 - stick
    stick26 - stick
    stick27 - stick
    stick28 - stick
    stick29 - stick
    stick30 - stick
    stick31 - stick
    stick32 - stick
    stick33 - stick
    stick34 - stick
    stick35 - stick
    stick36 - stick
    stick37 - stick
    stick38 - stick
    stick39 - stick
    stick40 - stick
    stick41 - stick
    stick42 - stick
    stick43 - stick
    stick44 - stick
    stick45 - stick
    stick46 - stick
    stick47 - stick
    stick48 - stick
    stick49 - stick
    stick50 - stick
    stick51 - stick
    stick52 - stick
    stick53 - stick
    stick54 - stick
    stick55 - stick
    stick56 - stick
    stick57 - stick
    stick58 - stick
    stick59 - stick
    stick60 - stick
    stick61 - stick
    stick62 - stick
    stick63 - stick
    stick64 - stick
    stick65 - stick
    stick66 - stick
    stick67 - stick
    stick68 - stick
    stick69 - stick
    stick70 - stick
    stick71 - stick
    stick72 - stick
    stick73 - stick
    stick74 - stick
    stick75 - stick
    stick76 - stick
    stick77 - stick
    stick78 - stick
    stick79 - stick
    stick80 - stick
    stick81 - stick
    stick82 - stick
    stick83 - stick
    stick84 - stick
    stick85 - stick
    stick86 - stick
    stick87 - stick
    stick88 - stick
    ;; Elements - Cubes
    cube1 - cube
    cube2 - cube
    cube3 - cube
    cube4 - cube
    cube5 - cube
    cube6 - cube
    cube7 - cube
    cube8 - cube
    cube9 - cube
    cube10 - cube
    cube11 - cube
    cube12 - cube
    cube13 - cube
    cube14 - cube
    cube15 - cube
    cube16 - cube
    cube17 - cube
    cube18 - cube
    ;; Layers
    layer1 - stack
    layer2 - stack
    layer3 - stack
    layer4 - stack
    layer5 - stack
    layer6 - stack
    layer7 - stack
    layer8 - stack
    layer9 - stack
    layer10 - stack
    layer11 - stack
    layer12 - stack
    layer13 - stack
    layer14 - stack
    layer15 - stack
    layer16 - stack
    layer17 - stack
    layer18 - stack
    layer19 - stack
    layer20 - stack
    layer21 - stack
    layer22 - stack
    layer23 - stack
    ;; Agent
    robot1 - robot
    ;; Tools
    gripper1 - gripper
    ;; Locations - Initial (first positions)
    initlocstick1 - firstposition
    initlocstick2 - firstposition
    initlocstick3 - firstposition
    initlocstick4 - firstposition
    initloccube1 - firstposition
    initloccube2 - firstposition
    initloccube3 - firstposition
    initloccube4 - firstposition
    ;; Locations - Final positions
    finallocstick1 - finalposition
    finallocstick2 - finalposition
    finallocstick3 - finalposition
    finallocstick4 - finalposition
    finallocstick5 - finalposition
    finallocstick6 - finalposition
    finallocstick7 - finalposition
    finallocstick8 - finalposition
    finallocstick9 - finalposition
    finallocstick10 - finalposition
    finallocstick11 - finalposition
    finallocstick12 - finalposition
    finallocstick13 - finalposition
    finallocstick14 - finalposition
    finallocstick15 - finalposition
    finallocstick16 - finalposition
    finallocstick17 - finalposition
    finallocstick18 - finalposition
    finallocstick19 - finalposition
    finallocstick20 - finalposition
    finallocstick21 - finalposition
    finallocstick22 - finalposition
    finallocstick23 - finalposition
    finallocstick24 - finalposition
    finallocstick25 - finalposition
    finallocstick26 - finalposition
    finallocstick27 - finalposition
    finallocstick28 - finalposition
    finallocstick29 - finalposition
    finallocstick30 - finalposition
    finallocstick31 - finalposition
    finallocstick32 - finalposition
    finallocstick33 - finalposition
    finallocstick34 - finalposition
    finallocstick35 - finalposition
    finallocstick36 - finalposition
    finallocstick37 - finalposition
    finallocstick38 - finalposition
    finallocstick39 - finalposition
    finallocstick40 - finalposition
    finallocstick41 - finalposition
    finallocstick42 - finalposition
    finallocstick43 - finalposition
    finallocstick44 - finalposition
    finallocstick45 - finalposition
    finallocstick46 - finalposition
    finallocstick47 - finalposition
    finallocstick48 - finalposition
    finallocstick49 - finalposition
    finallocstick50 - finalposition
    finallocstick51 - finalposition
    finallocstick52 - finalposition
    finallocstick53 - finalposition
    finallocstick54 - finalposition
    finallocstick55 - finalposition
    finallocstick56 - finalposition
    finallocstick57 - finalposition
    finallocstick58 - finalposition
    finallocstick59 - finalposition
    finallocstick60 - finalposition
    finallocstick61 - finalposition
    finallocstick62 - finalposition
    finallocstick63 - finalposition
    finallocstick64 - finalposition
    finallocstick65 - finalposition
    finallocstick66 - finalposition
    finallocstick67 - finalposition
    finallocstick68 - finalposition
    finallocstick69 - finalposition
    finallocstick70 - finalposition
    finallocstick71 - finalposition
    finallocstick72 - finalposition
    finallocstick73 - finalposition
    finallocstick74 - finalposition
    finallocstick75 - finalposition
    finallocstick76 - finalposition
    finallocstick77 - finalposition
    finallocstick78 - finalposition
    finallocstick79 - finalposition
    finallocstick80 - finalposition
    finallocstick81 - finalposition
    finallocstick82 - finalposition
    finallocstick83 - finalposition
    finallocstick84 - finalposition
    finallocstick85 - finalposition
    finallocstick86 - finalposition
    finallocstick87 - finalposition
    finallocstick88 - finalposition
    finloccube1 - finalposition
    finloccube2 - finalposition
    finloccube3 - finalposition
    finloccube4 - finalposition
    finloccube5 - finalposition
    finloccube6 - finalposition
    finloccube7 - finalposition
    finloccube8 - finalposition
    finloccube9 - finalposition
    finloccube10 - finalposition
    finloccube11 - finalposition
    finloccube12 - finalposition
    finloccube13 - finalposition
    finloccube14 - finalposition
    finloccube15 - finalposition
    finloccube16 - finalposition
    finloccube17 - finalposition
    finloccube18 - finalposition
  )

  (:init
    ;; BelongsToLayer
    (belongstolayer stick1 layer1)
    (belongstolayer stick2 layer1)
    (belongstolayer stick3 layer1)
    (belongstolayer stick4 layer1)
    (belongstolayer stick5 layer1)
    (belongstolayer stick6 layer2)
    (belongstolayer stick7 layer2)
    (belongstolayer stick8 layer2)
    (belongstolayer stick9 layer2)
    (belongstolayer cube1 layer2)
    (belongstolayer cube2 layer2)
    (belongstolayer stick10 layer3)
    (belongstolayer stick11 layer3)
    (belongstolayer stick12 layer3)
    (belongstolayer stick13 layer3)
    (belongstolayer stick14 layer3)
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
    (belongstolayer stick28 layer7)
    (belongstolayer stick29 layer7)
    (belongstolayer stick30 layer7)
    (belongstolayer stick31 layer7)
    (belongstolayer stick32 layer7)
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
    (belongstolayer stick73 layer17)
    (belongstolayer stick74 layer17)
    (belongstolayer stick75 layer17)
    (belongstolayer stick76 layer17)
    (belongstolayer stick77 layer18)
    (belongstolayer stick78 layer18)
    (belongstolayer stick79 layer18)
    (belongstolayer cube16 layer18)
    (belongstolayer stick80 layer19)
    (belongstolayer stick81 layer19)
    (belongstolayer stick82 layer19)
    (belongstolayer stick83 layer20)
    (belongstolayer stick84 layer20)
    (belongstolayer cube17 layer20)
    (belongstolayer stick85 layer21)
    (belongstolayer stick86 layer21)
    (belongstolayer stick87 layer22)
    (belongstolayer cube18 layer22)
    (belongstolayer stick88 layer23)

    ;; Clear predicates
    (clear stick1)
    (clear stick2)
    (clear stick3)
    (clear stick4)
    (clear stick5)
    (clear stick6)
    (clear stick7)
    (clear stick8)
    (clear stick9)
    (clear stick10)
    (clear stick11)
    (clear stick12)
    (clear stick13)
    (clear stick14)
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
    (clear stick28)
    (clear stick29)
    (clear stick30)
    (clear stick31)
    (clear stick32)
    (clear stick33)
    (clear stick34)
    (clear stick35)
    (clear stick36)
    (clear stick37)
    (clear stick38)
    (clear stick39)
    (clear stick40)
    (clear stick41)
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
    (clear stick55)
    (clear stick56)
    (clear stick57)
    (clear stick58)
    (clear stick59)
    (clear stick60)
    (clear stick61)
    (clear stick62)
    (clear stick63)
    (clear stick64)
    (clear stick65)
    (clear stick66)
    (clear stick67)
    (clear stick68)
    (clear stick69)
    (clear stick70)
    (clear stick71)
    (clear stick72)
    (clear stick73)
    (clear stick74)
    (clear stick75)
    (clear stick76)
    (clear stick77)
    (clear stick78)
    (clear stick79)
    (clear stick80)
    (clear stick81)
    (clear stick82)
    (clear stick83)
    (clear stick84)
    (clear stick85)
    (clear stick86)
    (clear stick87)
    (clear stick88)
    (clear cube1)
    (clear cube2)
    (clear cube3)
    (clear cube4)
    (clear cube5)
    (clear cube6)
    (clear cube7)
    (clear cube8)
    (clear cube9)
    (clear cube10)
    (clear cube11)
    (clear cube12)
    (clear cube13)
    (clear cube14)
    (clear cube15)
    (clear cube16)
    (clear cube17)
    (clear cube18)

    ;; PositionFree predicates
    (positionfree initlocstick1)
    (positionfree initlocstick2)
    (positionfree initlocstick3)
    (positionfree initlocstick4)

    ;; AtPlace predicates (elements at initial locations)
    (atplace stick1 initlocstick1)
    (atplace stick2 initlocstick2)
    (atplace stick3 initlocstick3)
    (atplace stick4 initlocstick4)
    (atplace stick5 initlocstick1)
    (atplace stick6 initlocstick2)
    (atplace stick7 initlocstick3)
    (atplace stick8 initlocstick4)
    (atplace stick9 initlocstick1)
    (atplace stick10 initlocstick2)
    (atplace stick11 initlocstick3)
    (atplace stick12 initlocstick4)
    (atplace stick13 initlocstick1)
    (atplace stick14 initlocstick2)
    (atplace stick15 initlocstick3)
    (atplace stick16 initlocstick4)
    (atplace stick17 initlocstick1)
    (atplace stick18 initlocstick2)
    (atplace stick19 initlocstick3)
    (atplace stick20 initlocstick4)
    (atplace stick21 initlocstick1)
    (atplace stick22 initlocstick2)
    (atplace stick23 initlocstick3)
    (atplace stick24 initlocstick4)
    (atplace stick25 initlocstick1)
    (atplace stick26 initlocstick2)
    (atplace stick27 initlocstick3)
    (atplace stick28 initlocstick4)
    (atplace stick29 initlocstick1)
    (atplace stick30 initlocstick2)
    (atplace stick31 initlocstick3)
    (atplace stick32 initlocstick4)
    (atplace stick33 initlocstick1)
    (atplace stick34 initlocstick2)
    (atplace stick35 initlocstick3)
    (atplace stick36 initlocstick4)
    (atplace stick37 initlocstick1)
    (atplace stick38 initlocstick2)
    (atplace stick39 initlocstick3)
    (atplace stick40 initlocstick4)
    (atplace stick41 initlocstick1)
    (atplace stick42 initlocstick2)
    (atplace stick43 initlocstick3)
    (atplace stick44 initlocstick4)
    (atplace stick45 initlocstick1)
    (atplace stick46 initlocstick2)
    (atplace stick47 initlocstick3)
    (atplace stick48 initlocstick4)
    (atplace stick49 initlocstick1)
    (atplace stick50 initlocstick2)
    (atplace stick51 initlocstick3)
    (atplace stick52 initlocstick4)
    (atplace stick53 initlocstick1)
    (atplace stick54 initlocstick2)
    (atplace stick55 initlocstick3)
    (atplace stick56 initlocstick4)
    (atplace stick57 initlocstick1)
    (atplace stick58 initlocstick2)
    (atplace stick59 initlocstick3)
    (atplace stick60 initlocstick4)
    (atplace stick61 initlocstick1)
    (atplace stick62 initlocstick2)
    (atplace stick63 initlocstick3)
    (atplace stick64 initlocstick4)
    (atplace stick65 initlocstick1)
    (atplace stick66 initlocstick2)
    (atplace stick67 initlocstick3)
    (atplace stick68 initlocstick4)
    (atplace stick69 initlocstick1)
    (atplace stick70 initlocstick2)
    (atplace stick71 initlocstick3)
    (atplace stick72 initlocstick4)
    (atplace stick73 initlocstick1)
    (atplace stick74 initlocstick2)
    (atplace stick75 initlocstick3)
    (atplace stick76 initlocstick4)
    (atplace stick77 initlocstick1)
    (atplace stick78 initlocstick2)
    (atplace stick79 initlocstick3)
    (atplace stick80 initlocstick4)
    (atplace stick81 initlocstick1)
    (atplace stick82 initlocstick2)
    (atplace stick83 initlocstick3)
    (atplace stick84 initlocstick4)
    (atplace stick85 initlocstick1)
    (atplace stick86 initlocstick2)
    (atplace stick87 initlocstick3)
    (atplace stick88 initlocstick4)
    (atplace cube1 initloccube1)
    (atplace cube2 initloccube2)
    (atplace cube3 initloccube3)
    (atplace cube4 initloccube4)
    (atplace cube5 initloccube1)
    (atplace cube6 initloccube2)
    (atplace cube7 initloccube3)
    (atplace cube8 initloccube4)
    (atplace cube9 initloccube1)
    (atplace cube10 initloccube2)
    (atplace cube11 initloccube3)
    (atplace cube12 initloccube4)
    (atplace cube13 initloccube1)
    (atplace cube14 initloccube2)
    (atplace cube15 initloccube3)
    (atplace cube16 initloccube4)
    (atplace cube17 initloccube1)
    (atplace cube18 initloccube2)

    ;; GripperEmpty
    (gripperempty robot1)

    ;; ObjectFinalPosition
    (objectfinalposition stick1 finallocstick1)
    (objectfinalposition stick2 finallocstick2)
    (objectfinalposition stick3 finallocstick3)
    (objectfinalposition stick4 finallocstick4)
    (objectfinalposition stick5 finallocstick5)
    (objectfinalposition stick6 finallocstick6)
    (objectfinalposition stick7 finallocstick7)
    (objectfinalposition stick8 finallocstick8)
    (objectfinalposition stick9 finallocstick9)
    (objectfinalposition stick10 finallocstick10)
    (objectfinalposition stick11 finallocstick11)
    (objectfinalposition stick12 finallocstick12)
    (objectfinalposition stick13 finallocstick13)
    (objectfinalposition stick14 finallocstick14)
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
    (objectfinalposition stick28 finallocstick28)
    (objectfinalposition stick29 finallocstick29)
    (objectfinalposition stick30 finallocstick30)
    (objectfinalposition stick31 finallocstick31)
    (objectfinalposition stick32 finallocstick32)
    (objectfinalposition stick33 finallocstick33)
    (objectfinalposition stick34 finallocstick34)
    (objectfinalposition stick35 finallocstick35)
    (objectfinalposition stick36 finallocstick36)
    (objectfinalposition stick37 finallocstick37)
    (objectfinalposition stick38 finallocstick38)
    (objectfinalposition stick39 finallocstick39)
    (objectfinalposition stick40 finallocstick40)
    (objectfinalposition stick41 finallocstick41)
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
    (objectfinalposition stick55 finallocstick55)
    (objectfinalposition stick56 finallocstick56)
    (objectfinalposition stick57 finallocstick57)
    (objectfinalposition stick58 finallocstick58)
    (objectfinalposition stick59 finallocstick59)
    (objectfinalposition stick60 finallocstick60)
    (objectfinalposition stick61 finallocstick61)
    (objectfinalposition stick62 finallocstick62)
    (objectfinalposition stick63 finallocstick63)
    (objectfinalposition stick64 finallocstick64)
    (objectfinalposition stick65 finallocstick65)
    (objectfinalposition stick66 finallocstick66)
    (objectfinalposition stick67 finallocstick67)
    (objectfinalposition stick68 finallocstick68)
    (objectfinalposition stick69 finallocstick69)
    (objectfinalposition stick70 finallocstick70)
    (objectfinalposition stick71 finallocstick71)
    (objectfinalposition stick72 finallocstick72)
    (objectfinalposition stick73 finallocstick73)
    (objectfinalposition stick74 finallocstick74)
    (objectfinalposition stick75 finallocstick75)
    (objectfinalposition stick76 finallocstick76)
    (objectfinalposition stick77 finallocstick77)
    (objectfinalposition stick78 finallocstick78)
    (objectfinalposition stick79 finallocstick79)
    (objectfinalposition stick80 finallocstick80)
    (objectfinalposition stick81 finallocstick81)
    (objectfinalposition stick82 finallocstick82)
    (objectfinalposition stick83 finallocstick83)
    (objectfinalposition stick84 finallocstick84)
    (objectfinalposition stick85 finallocstick85)
    (objectfinalposition stick86 finallocstick86)
    (objectfinalposition stick87 finallocstick87)
    (objectfinalposition stick88 finallocstick88)
    (objectfinalposition cube1 finloccube1)
    (objectfinalposition cube2 finloccube2)
    (objectfinalposition cube3 finloccube3)
    (objectfinalposition cube4 finloccube4)
    (objectfinalposition cube5 finloccube5)
    (objectfinalposition cube6 finloccube6)
    (objectfinalposition cube7 finloccube7)
    (objectfinalposition cube8 finloccube8)
    (objectfinalposition cube9 finloccube9)
    (objectfinalposition cube10 finloccube10)
    (objectfinalposition cube11 finloccube11)
    (objectfinalposition cube12 finloccube12)
    (objectfinalposition cube13 finloccube13)
    (objectfinalposition cube14 finloccube14)
    (objectfinalposition cube15 finloccube15)
    (objectfinalposition cube16 finloccube16)
    (objectfinalposition cube17 finloccube17)
    (objectfinalposition cube18 finloccube18)
  )

  (:goal (and
    ;; Stacked predicates (from spatial analysis - 184 pairs)
    ;; Layer 2
    (stacked cube1 stick1)
    (stacked cube2 stick5)
    (stacked stick6 stick1)
    (stacked stick6 stick2)
    (stacked stick7 stick2)
    (stacked stick7 stick3)
    (stacked stick8 stick3)
    (stacked stick8 stick4)
    (stacked stick9 stick4)
    (stacked stick9 stick5)

    ;; Layer 3
    (stacked stick10 cube1)
    (stacked stick10 stick6)
    (stacked stick11 stick6)
    (stacked stick11 stick7)
    (stacked stick12 stick7)
    (stacked stick12 stick8)
    (stacked stick13 stick8)
    (stacked stick13 stick9)
    (stacked stick14 cube2)
    (stacked stick14 stick9)

    ;; Layer 4
    (stacked cube3 stick10)
    (stacked cube4 stick14)
    (stacked stick15 stick10)
    (stacked stick15 stick11)
    (stacked stick16 stick11)
    (stacked stick16 stick12)
    (stacked stick17 stick12)
    (stacked stick17 stick13)
    (stacked stick18 stick13)
    (stacked stick18 stick14)

    ;; Layer 5
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

    ;; Layer 6
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

    ;; Layer 7
    (stacked stick28 cube5)
    (stacked stick28 stick24)
    (stacked stick29 stick24)
    (stacked stick29 stick25)
    (stacked stick30 stick25)
    (stacked stick30 stick26)
    (stacked stick31 stick26)
    (stacked stick31 stick27)
    (stacked stick32 cube6)
    (stacked stick32 stick27)

    ;; Layer 8
    (stacked cube7 stick28)
    (stacked cube8 stick32)
    (stacked stick33 stick28)
    (stacked stick33 stick29)
    (stacked stick34 stick29)
    (stacked stick34 stick30)
    (stacked stick35 stick30)
    (stacked stick35 stick31)
    (stacked stick36 stick31)
    (stacked stick36 stick32)

    ;; Layer 9
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

    ;; Layer 10
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

    ;; Layer 11
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

    ;; Layer 12
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

    ;; Layer 13
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

    ;; Layer 14
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

    ;; Layer 15
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

    ;; Layer 16
    (stacked cube15 stick64)
    (stacked stick69 stick64)
    (stacked stick69 stick65)
    (stacked stick70 stick65)
    (stacked stick70 stick66)
    (stacked stick71 stick66)
    (stacked stick71 stick67)
    (stacked stick72 stick67)
    (stacked stick72 stick68)

    ;; Layer 17
    (stacked stick73 cube15)
    (stacked stick73 stick69)
    (stacked stick74 stick69)
    (stacked stick74 stick70)
    (stacked stick75 stick70)
    (stacked stick75 stick71)
    (stacked stick76 stick71)
    (stacked stick76 stick72)

    ;; Layer 18
    (stacked cube16 stick73)
    (stacked stick77 stick73)
    (stacked stick77 stick74)
    (stacked stick78 stick74)
    (stacked stick78 stick75)
    (stacked stick79 stick75)
    (stacked stick79 stick76)

    ;; Layer 19
    (stacked stick80 cube16)
    (stacked stick80 stick77)
    (stacked stick81 stick77)
    (stacked stick81 stick78)
    (stacked stick82 stick78)
    (stacked stick82 stick79)

    ;; Layer 20
    (stacked cube17 stick80)
    (stacked stick83 stick80)
    (stacked stick83 stick81)
    (stacked stick84 stick81)
    (stacked stick84 stick82)

    ;; Layer 21
    (stacked stick85 cube17)
    (stacked stick85 stick83)
    (stacked stick86 stick83)
    (stacked stick86 stick84)

    ;; Layer 22
    (stacked cube18 stick85)
    (stacked stick87 stick85)
    (stacked stick87 stick86)

    ;; Layer 23
    (stacked stick88 cube18)
    (stacked stick88 stick87)

    ;; Nailed predicates (from spatial analysis - 184 pairs)
    (nailed cube1 stick1)
    (nailed cube2 stick5)
    (nailed cube3 stick10)
    (nailed cube4 stick14)
    (nailed cube5 stick19)
    (nailed cube6 stick23)
    (nailed cube7 stick28)
    (nailed cube8 stick32)
    (nailed cube9 stick37)
    (nailed cube10 stick41)
    (nailed cube11 stick46)
    (nailed cube12 stick50)
    (nailed cube13 stick55)
    (nailed cube14 stick59)
    (nailed cube15 stick64)
    (nailed cube16 stick73)
    (nailed cube17 stick80)
    (nailed cube18 stick85)
    (nailed stick6 stick1)
    (nailed stick6 stick2)
    (nailed stick7 stick2)
    (nailed stick7 stick3)
    (nailed stick8 stick3)
    (nailed stick8 stick4)
    (nailed stick9 stick4)
    (nailed stick9 stick5)
    (nailed stick10 cube1)
    (nailed stick10 stick6)
    (nailed stick11 stick6)
    (nailed stick11 stick7)
    (nailed stick12 stick7)
    (nailed stick12 stick8)
    (nailed stick13 stick8)
    (nailed stick13 stick9)
    (nailed stick14 cube2)
    (nailed stick14 stick9)
    (nailed stick15 stick10)
    (nailed stick15 stick11)
    (nailed stick16 stick11)
    (nailed stick16 stick12)
    (nailed stick17 stick12)
    (nailed stick17 stick13)
    (nailed stick18 stick13)
    (nailed stick18 stick14)
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
    (nailed stick24 stick19)
    (nailed stick24 stick20)
    (nailed stick25 stick20)
    (nailed stick25 stick21)
    (nailed stick26 stick21)
    (nailed stick26 stick22)
    (nailed stick27 stick22)
    (nailed stick27 stick23)
    (nailed stick28 cube5)
    (nailed stick28 stick24)
    (nailed stick29 stick24)
    (nailed stick29 stick25)
    (nailed stick30 stick25)
    (nailed stick30 stick26)
    (nailed stick31 stick26)
    (nailed stick31 stick27)
    (nailed stick32 cube6)
    (nailed stick32 stick27)
    (nailed stick33 stick28)
    (nailed stick33 stick29)
    (nailed stick34 stick29)
    (nailed stick34 stick30)
    (nailed stick35 stick30)
    (nailed stick35 stick31)
    (nailed stick36 stick31)
    (nailed stick36 stick32)
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
    (nailed stick42 stick37)
    (nailed stick42 stick38)
    (nailed stick43 stick38)
    (nailed stick43 stick39)
    (nailed stick44 stick39)
    (nailed stick44 stick40)
    (nailed stick45 stick40)
    (nailed stick45 stick41)
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
    (nailed stick51 stick46)
    (nailed stick51 stick47)
    (nailed stick52 stick47)
    (nailed stick52 stick48)
    (nailed stick53 stick48)
    (nailed stick53 stick49)
    (nailed stick54 stick49)
    (nailed stick54 stick50)
    (nailed stick55 cube11)
    (nailed stick55 stick51)
    (nailed stick56 stick51)
    (nailed stick56 stick52)
    (nailed stick57 stick52)
    (nailed stick57 stick53)
    (nailed stick58 stick53)
    (nailed stick58 stick54)
    (nailed stick59 cube12)
    (nailed stick59 stick54)
    (nailed stick60 stick55)
    (nailed stick60 stick56)
    (nailed stick61 stick56)
    (nailed stick61 stick57)
    (nailed stick62 stick57)
    (nailed stick62 stick58)
    (nailed stick63 stick58)
    (nailed stick63 stick59)
    (nailed stick64 cube13)
    (nailed stick64 stick60)
    (nailed stick65 stick60)
    (nailed stick65 stick61)
    (nailed stick66 stick61)
    (nailed stick66 stick62)
    (nailed stick67 stick62)
    (nailed stick67 stick63)
    (nailed stick68 cube14)
    (nailed stick68 stick63)
    (nailed stick69 stick64)
    (nailed stick69 stick65)
    (nailed stick70 stick65)
    (nailed stick70 stick66)
    (nailed stick71 stick66)
    (nailed stick71 stick67)
    (nailed stick72 stick67)
    (nailed stick72 stick68)
    (nailed stick73 cube15)
    (nailed stick73 stick69)
    (nailed stick74 stick69)
    (nailed stick74 stick70)
    (nailed stick75 stick70)
    (nailed stick75 stick71)
    (nailed stick76 stick71)
    (nailed stick76 stick72)
    (nailed stick77 stick73)
    (nailed stick77 stick74)
    (nailed stick78 stick74)
    (nailed stick78 stick75)
    (nailed stick79 stick75)
    (nailed stick79 stick76)
    (nailed stick80 cube16)
    (nailed stick80 stick77)
    (nailed stick81 stick77)
    (nailed stick81 stick78)
    (nailed stick82 stick78)
    (nailed stick82 stick79)
    (nailed stick83 stick80)
    (nailed stick83 stick81)
    (nailed stick84 stick81)
    (nailed stick84 stick82)
    (nailed stick85 cube17)
    (nailed stick85 stick83)
    (nailed stick86 stick83)
    (nailed stick86 stick84)
    (nailed stick87 stick85)
    (nailed stick87 stick86)
    (nailed stick88 cube18)
    (nailed stick88 stick87)

    ;; AtPlace predicates (elements at final locations)
    (atplace stick1 finallocstick1)
    (atplace stick2 finallocstick2)
    (atplace stick3 finallocstick3)
    (atplace stick4 finallocstick4)
    (atplace stick5 finallocstick5)
    (atplace stick6 finallocstick6)
    (atplace stick7 finallocstick7)
    (atplace stick8 finallocstick8)
    (atplace stick9 finallocstick9)
    (atplace stick10 finallocstick10)
    (atplace stick11 finallocstick11)
    (atplace stick12 finallocstick12)
    (atplace stick13 finallocstick13)
    (atplace stick14 finallocstick14)
    (atplace stick15 finallocstick15)
    (atplace stick16 finallocstick16)
    (atplace stick17 finallocstick17)
    (atplace stick18 finallocstick18)
    (atplace stick19 finallocstick19)
    (atplace stick20 finallocstick20)
    (atplace stick21 finallocstick21)
    (atplace stick22 finallocstick22)
    (atplace stick23 finallocstick23)
    (atplace stick24 finallocstick24)
    (atplace stick25 finallocstick25)
    (atplace stick26 finallocstick26)
    (atplace stick27 finallocstick27)
    (atplace stick28 finallocstick28)
    (atplace stick29 finallocstick29)
    (atplace stick30 finallocstick30)
    (atplace stick31 finallocstick31)
    (atplace stick32 finallocstick32)
    (atplace stick33 finallocstick33)
    (atplace stick34 finallocstick34)
    (atplace stick35 finallocstick35)
    (atplace stick36 finallocstick36)
    (atplace stick37 finallocstick37)
    (atplace stick38 finallocstick38)
    (atplace stick39 finallocstick39)
    (atplace stick40 finallocstick40)
    (atplace stick41 finallocstick41)
    (atplace stick42 finallocstick42)
    (atplace stick43 finallocstick43)
    (atplace stick44 finallocstick44)
    (atplace stick45 finallocstick45)
    (atplace stick46 finallocstick46)
    (atplace stick47 finallocstick47)
    (atplace stick48 finallocstick48)
    (atplace stick49 finallocstick49)
    (atplace stick50 finallocstick50)
    (atplace stick51 finallocstick51)
    (atplace stick52 finallocstick52)
    (atplace stick53 finallocstick53)
    (atplace stick54 finallocstick54)
    (atplace stick55 finallocstick55)
    (atplace stick56 finallocstick56)
    (atplace stick57 finallocstick57)
    (atplace stick58 finallocstick58)
    (atplace stick59 finallocstick59)
    (atplace stick60 finallocstick60)
    (atplace stick61 finallocstick61)
    (atplace stick62 finallocstick62)
    (atplace stick63 finallocstick63)
    (atplace stick64 finallocstick64)
    (atplace stick65 finallocstick65)
    (atplace stick66 finallocstick66)
    (atplace stick67 finallocstick67)
    (atplace stick68 finallocstick68)
    (atplace stick69 finallocstick69)
    (atplace stick70 finallocstick70)
    (atplace stick71 finallocstick71)
    (atplace stick72 finallocstick72)
    (atplace stick73 finallocstick73)
    (atplace stick74 finallocstick74)
    (atplace stick75 finallocstick75)
    (atplace stick76 finallocstick76)
    (atplace stick77 finallocstick77)
    (atplace stick78 finallocstick78)
    (atplace stick79 finallocstick79)
    (atplace stick80 finallocstick80)
    (atplace stick81 finallocstick81)
    (atplace stick82 finallocstick82)
    (atplace stick83 finallocstick83)
    (atplace stick84 finallocstick84)
    (atplace stick85 finallocstick85)
    (atplace stick86 finallocstick86)
    (atplace stick87 finallocstick87)
    (atplace stick88 finallocstick88)
    (atplace cube1 finloccube1)
    (atplace cube2 finloccube2)
    (atplace cube3 finloccube3)
    (atplace cube4 finloccube4)
    (atplace cube5 finloccube5)
    (atplace cube6 finloccube6)
    (atplace cube7 finloccube7)
    (atplace cube8 finloccube8)
    (atplace cube9 finloccube9)
    (atplace cube10 finloccube10)
    (atplace cube11 finloccube11)
    (atplace cube12 finloccube12)
    (atplace cube13 finloccube13)
    (atplace cube14 finloccube14)
    (atplace cube15 finloccube15)
    (atplace cube16 finloccube16)
    (atplace cube17 finloccube17)
    (atplace cube18 finloccube18)

    ;; AtFinalPosition predicates
    (atfinalposition stick1)
    (atfinalposition stick2)
    (atfinalposition stick3)
    (atfinalposition stick4)
    (atfinalposition stick5)
    (atfinalposition stick6)
    (atfinalposition stick7)
    (atfinalposition stick8)
    (atfinalposition stick9)
    (atfinalposition stick10)
    (atfinalposition stick11)
    (atfinalposition stick12)
    (atfinalposition stick13)
    (atfinalposition stick14)
    (atfinalposition stick15)
    (atfinalposition stick16)
    (atfinalposition stick17)
    (atfinalposition stick18)
    (atfinalposition stick19)
    (atfinalposition stick20)
    (atfinalposition stick21)
    (atfinalposition stick22)
    (atfinalposition stick23)
    (atfinalposition stick24)
    (atfinalposition stick25)
    (atfinalposition stick26)
    (atfinalposition stick27)
    (atfinalposition stick28)
    (atfinalposition stick29)
    (atfinalposition stick30)
    (atfinalposition stick31)
    (atfinalposition stick32)
    (atfinalposition stick33)
    (atfinalposition stick34)
    (atfinalposition stick35)
    (atfinalposition stick36)
    (atfinalposition stick37)
    (atfinalposition stick38)
    (atfinalposition stick39)
    (atfinalposition stick40)
    (atfinalposition stick41)
    (atfinalposition stick42)
    (atfinalposition stick43)
    (atfinalposition stick44)
    (atfinalposition stick45)
    (atfinalposition stick46)
    (atfinalposition stick47)
    (atfinalposition stick48)
    (atfinalposition stick49)
    (atfinalposition stick50)
    (atfinalposition stick51)
    (atfinalposition stick52)
    (atfinalposition stick53)
    (atfinalposition stick54)
    (atfinalposition stick55)
    (atfinalposition stick56)
    (atfinalposition stick57)
    (atfinalposition stick58)
    (atfinalposition stick59)
    (atfinalposition stick60)
    (atfinalposition stick61)
    (atfinalposition stick62)
    (atfinalposition stick63)
    (atfinalposition stick64)
    (atfinalposition stick65)
    (atfinalposition stick66)
    (atfinalposition stick67)
    (atfinalposition stick68)
    (atfinalposition stick69)
    (atfinalposition stick70)
    (atfinalposition stick71)
    (atfinalposition stick72)
    (atfinalposition stick73)
    (atfinalposition stick74)
    (atfinalposition stick75)
    (atfinalposition stick76)
    (atfinalposition stick77)
    (atfinalposition stick78)
    (atfinalposition stick79)
    (atfinalposition stick80)
    (atfinalposition stick81)
    (atfinalposition stick82)
    (atfinalposition stick83)
    (atfinalposition stick84)
    (atfinalposition stick85)
    (atfinalposition stick86)
    (atfinalposition stick87)
    (atfinalposition stick88)
    (atfinalposition cube1)
    (atfinalposition cube2)
    (atfinalposition cube3)
    (atfinalposition cube4)
    (atfinalposition cube5)
    (atfinalposition cube6)
    (atfinalposition cube7)
    (atfinalposition cube8)
    (atfinalposition cube9)
    (atfinalposition cube10)
    (atfinalposition cube11)
    (atfinalposition cube12)
    (atfinalposition cube13)
    (atfinalposition cube14)
    (atfinalposition cube15)
    (atfinalposition cube16)
    (atfinalposition cube17)
    (atfinalposition cube18)

    ;; Fixed predicates
    (fixed stick1)
    (fixed stick2)
    (fixed stick3)
    (fixed stick4)
    (fixed stick5)
    (fixed stick6)
    (fixed stick7)
    (fixed stick8)
    (fixed stick9)
    (fixed stick10)
    (fixed stick11)
    (fixed stick12)
    (fixed stick13)
    (fixed stick14)
    (fixed stick15)
    (fixed stick16)
    (fixed stick17)
    (fixed stick18)
    (fixed stick19)
    (fixed stick20)
    (fixed stick21)
    (fixed stick22)
    (fixed stick23)
    (fixed stick24)
    (fixed stick25)
    (fixed stick26)
    (fixed stick27)
    (fixed stick28)
    (fixed stick29)
    (fixed stick30)
    (fixed stick31)
    (fixed stick32)
    (fixed stick33)
    (fixed stick34)
    (fixed stick35)
    (fixed stick36)
    (fixed stick37)
    (fixed stick38)
    (fixed stick39)
    (fixed stick40)
    (fixed stick41)
    (fixed stick42)
    (fixed stick43)
    (fixed stick44)
    (fixed stick45)
    (fixed stick46)
    (fixed stick47)
    (fixed stick48)
    (fixed stick49)
    (fixed stick50)
    (fixed stick51)
    (fixed stick52)
    (fixed stick53)
    (fixed stick54)
    (fixed stick55)
    (fixed stick56)
    (fixed stick57)
    (fixed stick58)
    (fixed stick59)
    (fixed stick60)
    (fixed stick61)
    (fixed stick62)
    (fixed stick63)
    (fixed stick64)
    (fixed stick65)
    (fixed stick66)
    (fixed stick67)
    (fixed stick68)
    (fixed stick69)
    (fixed stick70)
    (fixed stick71)
    (fixed stick72)
    (fixed stick73)
    (fixed stick74)
    (fixed stick75)
    (fixed stick76)
    (fixed stick77)
    (fixed stick78)
    (fixed stick79)
    (fixed stick80)
    (fixed stick81)
    (fixed stick82)
    (fixed stick83)
    (fixed stick84)
    (fixed stick85)
    (fixed stick86)
    (fixed stick87)
    (fixed stick88)
    (fixed cube1)
    (fixed cube2)
    (fixed cube3)
    (fixed cube4)
    (fixed cube5)
    (fixed cube6)
    (fixed cube7)
    (fixed cube8)
    (fixed cube9)
    (fixed cube10)
    (fixed cube11)
    (fixed cube12)
    (fixed cube13)
    (fixed cube14)
    (fixed cube15)
    (fixed cube16)
    (fixed cube17)
    (fixed cube18)

    ;; GripperEmpty
    (gripperempty robot1)
  ))
)
