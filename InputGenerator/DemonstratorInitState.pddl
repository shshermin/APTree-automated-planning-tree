(define (problem demonstrator-init)
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
)
