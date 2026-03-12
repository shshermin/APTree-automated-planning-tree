(define (problem demonstrator-goal)
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

  (:goal (and
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
