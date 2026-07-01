(define (problem stackonmultiplehl)
  (:domain fit)
  (:objects 
    fp65 - firstposition
fp66 - firstposition
fp67 - firstposition
fp68 - firstposition
fp69 - firstposition
fp70 - firstposition
fp71 - firstposition
fp72 - firstposition
fp73 - firstposition
fp74 - firstposition
fp75 - firstposition
fp76 - firstposition
fp77 - firstposition
fp78 - firstposition
fp79 - firstposition
fp80 - firstposition
fp81 - firstposition
fp82 - firstposition
fp83 - firstposition
fp84 - firstposition
fp85 - firstposition
fp86 - firstposition
fp87 - firstposition
fp88 - firstposition
fp89 - firstposition
fp90 - firstposition
fp91 - firstposition
fp92 - firstposition
fp93 - firstposition
fp94 - firstposition
fp95 - firstposition
fp96 - firstposition
pr1 - positiononrail
pr2 - positiononrail
pr3 - positiononrail
pr4 - positiononrail
ep1 - equipposition
ep2 - equipposition
ep3 - equipposition
ep4 - equipposition
r1 - robot
b49 - beam
b50 - beam
b51 - beam
b52 - beam
b53 - beam
b54 - beam
b55 - beam
b56 - beam
b57 - beam
b58 - beam
b59 - beam
b60 - beam
b61 - beam
b62 - beam
b63 - beam
b64 - beam
b65 - beam
b66 - beam
b67 - beam
b68 - beam
b69 - beam
b70 - beam
b71 - beam
b72 - beam
lp9 - plate
lp10 - plate
lp11 - plate
lp12 - plate
tp9 - plate
tp10 - plate
tp11 - plate
tp12 - plate
vg1 - vacgripper
ng1 - nailgripper
gg1 - gluegun
m9 - cassette
m10 - cassette
m11 - cassette
m12 - cassette
lay9 - stack
lay10 - stack
lay11 - stack
lay12 - stack
sp1 - stackposition
sp2 - stackposition
sp3 - stackposition
sp4 - stackposition
sp5 - stackposition
sp6 - stackposition
sp7 - stackposition
sp8 - stackposition
sp9 - stackposition
sp10 - stackposition
sp11 - stackposition
sp12 - stackposition

  )
  (:init  
    (attool gg1 ep2)
(attool ng1 ep3)
(atagent r1 fp93)
(clear tp9)
(clear b50)
(clear b51)
(clear b52)
(clear b53)
(clear b54)
(clear tp10)
(clear b56)
(clear b57)
(clear b58)
(clear b59)
(clear b60)
(clear tp11)
(clear b62)
(clear b63)
(clear b64)
(clear b65)
(clear b66)
(clear b67)
(clear b68)
(clear b69)
(clear b70)
(clear b71)
(clear b72)
(belongstolayer b49 lay9)
(belongstolayer b50 lay9)
(belongstolayer b51 lay9)
(belongstolayer b52 lay9)
(belongstolayer b53 lay9)
(belongstolayer b54 lay9)
(belongstolayer b55 lay10)
(belongstolayer b56 lay10)
(belongstolayer b57 lay10)
(belongstolayer b58 lay10)
(belongstolayer b59 lay10)
(belongstolayer b60 lay10)
(belongstolayer b61 lay11)
(belongstolayer b62 lay11)
(belongstolayer b63 lay11)
(belongstolayer b64 lay11)
(belongstolayer b65 lay11)
(belongstolayer b66 lay11)
(belongstolayer b67 lay12)
(belongstolayer b68 lay12)
(belongstolayer b69 lay12)
(belongstolayer b70 lay12)
(belongstolayer b71 lay12)
(belongstolayer b72 lay12)
(belongstomodule b49 m9)
(belongstomodule b50 m9)
(belongstomodule b51 m9)
(belongstomodule b52 m9)
(belongstomodule b53 m9)
(belongstomodule b54 m9)
(belongstomodule lp9 m9)
(belongstomodule tp9 m9)
(belongstomodule b55 m10)
(belongstomodule b56 m10)
(belongstomodule b57 m10)
(belongstomodule b58 m10)
(belongstomodule b59 m10)
(belongstomodule b60 m10)
(belongstomodule lp10 m10)
(belongstomodule tp10 m10)
(belongstomodule b61 m11)
(belongstomodule b62 m11)
(belongstomodule b63 m11)
(belongstomodule b64 m11)
(belongstomodule b65 m11)
(belongstomodule b66 m11)
(belongstomodule lp11 m11)
(belongstomodule tp11 m11)
(belongstomodule b67 m12)
(belongstomodule b68 m12)
(belongstomodule b69 m12)
(belongstomodule b70 m12)
(belongstomodule b71 m12)
(belongstomodule b72 m12)
(belongstomodule lp12 m12)
(belongstomodule tp12 m12)
(positionfree sp9)
(positionfree sp10)
(positionfree sp11)
(positionfree sp12)
(hastool r1 vg1)
(robotequipped r1)
(positionfree ep1)
(activetool vg1)
(positionfree fp65)
(atplace lp9 pr1)
(positionfree fp73)
(atplace lp10 pr2)
(positionfree fp81)
(atplace lp11 pr3)
(positionfree fp89)
(atplace lp12 pr4)
(glued lp9)
(glued lp10)
(glued lp11)
(glued lp12)
(positionfree fp66)
(ontop b49 lp9)
(atplace b49 pr1)
(stacked b49)
(positionfree fp67)
(ontop b50 lp9)
(atplace b50 pr1)
(stacked b50)
(positionfree fp72)
(ontop b54 lp9)
(atplace b54 pr1)
(stacked b54)
(positionfree fp70)
(ontop b52 lp9)
(atplace b52 pr1)
(stacked b52)
(positionfree fp68)
(ontop b51 lp9)
(atplace b51 pr1)
(stacked b51)
(positionfree fp71)
(ontop b53 lp9)
(atplace b53 pr1)
(stacked b53)
(positionfree fp74)
(ontop b55 lp10)
(atplace b55 pr2)
(stacked b55)
(positionfree fp75)
(ontop b56 lp10)
(atplace b56 pr2)
(stacked b56)
(positionfree fp80)
(ontop b60 lp10)
(atplace b60 pr2)
(stacked b60)
(positionfree fp78)
(ontop b58 lp10)
(atplace b58 pr2)
(stacked b58)
(positionfree fp76)
(ontop b57 lp10)
(atplace b57 pr2)
(stacked b57)
(positionfree fp79)
(ontop b59 lp10)
(atplace b59 pr2)
(stacked b59)
(positionfree fp82)
(ontop b61 lp11)
(atplace b61 pr3)
(stacked b61)
(positionfree fp83)
(ontop b62 lp11)
(atplace b62 pr3)
(stacked b62)
(positionfree fp88)
(ontop b66 lp11)
(atplace b66 pr3)
(stacked b66)
(positionfree fp86)
(ontop b64 lp11)
(atplace b64 pr3)
(stacked b64)
(positionfree fp84)
(ontop b63 lp11)
(atplace b63 pr3)
(stacked b63)
(positionfree fp87)
(ontop b65 lp11)
(atplace b65 pr3)
(stacked b65)
(positionfree fp90)
(ontop b67 lp12)
(atplace b67 pr4)
(stacked b67)
(positionfree fp91)
(ontop b68 lp12)
(atplace b68 pr4)
(stacked b68)
(positionfree fp96)
(ontop b72 lp12)
(atplace b72 pr4)
(stacked b72)
(positionfree fp94)
(ontop b70 lp12)
(atplace b70 pr4)
(stacked b70)
(positionfree fp92)
(ontop b69 lp12)
(atplace b69 pr4)
(stacked b69)
(positionfree fp95)
(ontop b71 lp12)
(atplace b71 pr4)
(stacked b71)
(nailed b50)
(nailed b52)
(nailed b51)
(nailed b53)
(nailed b49)
(nailed b54)
(nailed b56)
(nailed b58)
(nailed b57)
(nailed b59)
(nailed b55)
(nailed b60)
(nailed b62)
(nailed b64)
(nailed b63)
(nailed b65)
(nailed b61)
(nailed b66)
(nailed b68)
(nailed b70)
(nailed b69)
(nailed b71)
(nailed b67)
(nailed b72)
(glued b49)
(glued b50)
(glued b53)
(glued b54)
(glued b52)
(glued b51)
(glued b55)
(glued b56)
(glued b59)
(glued b60)
(glued b58)
(glued b57)
(glued b61)
(glued b62)
(glued b65)
(glued b66)
(glued b64)
(glued b63)
(glued b67)
(glued b68)
(glued b71)
(glued b72)
(glued b70)
(glued b69)
(positionfree fp69)
(ontop tp9 b49)
(atplace tp9 pr1)
(stacked tp9)
(positionfree fp77)
(ontop tp10 b55)
(atplace tp10 pr2)
(stacked tp10)
(positionfree fp85)
(ontop tp11 b61)
(atplace tp11 pr3)
(stacked tp11)
(holding r1 tp12)
(positionfree fp93)
  )
  (:goal 
    (and
      (atplace tp12 pr4)
(vgempty r1)
(clear tp12)
    ) 
  )
)