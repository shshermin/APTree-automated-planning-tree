(define (problem stackhl)
  (:domain fit)
  (:objects 
    fp33 - firstposition
fp34 - firstposition
fp35 - firstposition
fp36 - firstposition
fp37 - firstposition
fp38 - firstposition
fp39 - firstposition
fp40 - firstposition
fp41 - firstposition
fp42 - firstposition
fp43 - firstposition
fp44 - firstposition
fp45 - firstposition
fp46 - firstposition
fp47 - firstposition
fp48 - firstposition
fp49 - firstposition
fp50 - firstposition
fp51 - firstposition
fp52 - firstposition
fp53 - firstposition
fp54 - firstposition
fp55 - firstposition
fp56 - firstposition
fp57 - firstposition
fp58 - firstposition
fp59 - firstposition
fp60 - firstposition
fp61 - firstposition
fp62 - firstposition
fp63 - firstposition
fp64 - firstposition
pr1 - positiononrail
pr2 - positiononrail
pr3 - positiononrail
pr4 - positiononrail
ep1 - equipposition
ep2 - equipposition
ep3 - equipposition
ep4 - equipposition
r1 - robot
b25 - beam
b26 - beam
b27 - beam
b28 - beam
b29 - beam
b30 - beam
b31 - beam
b32 - beam
b33 - beam
b34 - beam
b35 - beam
b36 - beam
b37 - beam
b38 - beam
b39 - beam
b40 - beam
b41 - beam
b42 - beam
b43 - beam
b44 - beam
b45 - beam
b46 - beam
b47 - beam
b48 - beam
lp5 - plate
lp6 - plate
lp7 - plate
lp8 - plate
tp5 - plate
tp6 - plate
tp7 - plate
tp8 - plate
vg1 - vacgripper
ng1 - nailgripper
gg1 - gluegun
m5 - cassette
m6 - cassette
m7 - cassette
m8 - cassette
lay5 - stack
lay6 - stack
lay7 - stack
lay8 - stack
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
(atagent r1 fp62)
(atplace tp5 fp37)
(atplace b27 fp36)
(atplace b29 fp39)
(atplace tp6 fp45)
(atplace b33 fp44)
(atplace b35 fp47)
(atplace tp7 fp53)
(atplace b39 fp52)
(atplace b41 fp55)
(atplace tp8 fp61)
(atplace b45 fp60)
(atplace b47 fp63)
(clear tp5)
(clear b25)
(clear b26)
(clear b27)
(clear b28)
(clear b29)
(clear b30)
(clear tp6)
(clear b31)
(clear b32)
(clear b33)
(clear b34)
(clear b35)
(clear b36)
(clear tp7)
(clear b37)
(clear b38)
(clear b39)
(clear b40)
(clear b41)
(clear b42)
(clear tp8)
(clear b43)
(clear b44)
(clear b45)
(clear b47)
(clear b48)
(belongstolayer b25 lay5)
(belongstolayer b26 lay5)
(belongstolayer b27 lay5)
(belongstolayer b28 lay5)
(belongstolayer b29 lay5)
(belongstolayer b30 lay5)
(belongstolayer b31 lay6)
(belongstolayer b32 lay6)
(belongstolayer b33 lay6)
(belongstolayer b34 lay6)
(belongstolayer b35 lay6)
(belongstolayer b36 lay6)
(belongstolayer b37 lay7)
(belongstolayer b38 lay7)
(belongstolayer b39 lay7)
(belongstolayer b40 lay7)
(belongstolayer b41 lay7)
(belongstolayer b42 lay7)
(belongstolayer b43 lay8)
(belongstolayer b44 lay8)
(belongstolayer b45 lay8)
(belongstolayer b46 lay8)
(belongstolayer b47 lay8)
(belongstolayer b48 lay8)
(belongstomodule b25 m5)
(belongstomodule b26 m5)
(belongstomodule b27 m5)
(belongstomodule b28 m5)
(belongstomodule b29 m5)
(belongstomodule b30 m5)
(belongstomodule lp5 m5)
(belongstomodule tp5 m5)
(belongstomodule b31 m6)
(belongstomodule b32 m6)
(belongstomodule b33 m6)
(belongstomodule b34 m6)
(belongstomodule b35 m6)
(belongstomodule b36 m6)
(belongstomodule lp6 m6)
(belongstomodule tp6 m6)
(belongstomodule b37 m7)
(belongstomodule b38 m7)
(belongstomodule b39 m7)
(belongstomodule b40 m7)
(belongstomodule b41 m7)
(belongstomodule b42 m7)
(belongstomodule lp7 m7)
(belongstomodule tp7 m7)
(belongstomodule b43 m8)
(belongstomodule b44 m8)
(belongstomodule b45 m8)
(belongstomodule b46 m8)
(belongstomodule b47 m8)
(belongstomodule b48 m8)
(belongstomodule lp8 m8)
(belongstomodule tp8 m8)
(positionfree sp5)
(positionfree sp6)
(positionfree sp7)
(positionfree sp8)
(positionfree sp9)
(positionfree sp10)
(positionfree sp11)
(positionfree sp12)
(hastool r1 vg1)
(robotequipped r1)
(positionfree ep1)
(activetool vg1)
(positionfree fp33)
(atplace lp5 pr1)
(positionfree fp41)
(atplace lp6 pr2)
(positionfree fp49)
(atplace lp7 pr3)
(positionfree fp57)
(atplace lp8 pr4)
(glued lp5)
(glued lp6)
(glued lp7)
(glued lp8)
(positionfree fp34)
(ontop b25 lp5)
(atplace b25 pr1)
(stacked b25)
(positionfree fp42)
(ontop b31 lp6)
(atplace b31 pr2)
(stacked b31)
(positionfree fp50)
(ontop b37 lp7)
(atplace b37 pr3)
(stacked b37)
(positionfree fp58)
(ontop b43 lp8)
(atplace b43 pr4)
(stacked b43)
(positionfree fp35)
(ontop b26 lp5)
(atplace b26 pr1)
(stacked b26)
(positionfree fp43)
(ontop b32 lp6)
(atplace b32 pr2)
(stacked b32)
(positionfree fp51)
(ontop b38 lp7)
(atplace b38 pr3)
(stacked b38)
(positionfree fp59)
(ontop b44 lp8)
(atplace b44 pr4)
(stacked b44)
(positionfree fp40)
(ontop b30 lp5)
(atplace b30 pr1)
(stacked b30)
(positionfree fp48)
(ontop b36 lp6)
(atplace b36 pr2)
(stacked b36)
(positionfree fp56)
(ontop b42 lp7)
(atplace b42 pr3)
(stacked b42)
(positionfree fp64)
(ontop b48 lp8)
(atplace b48 pr4)
(stacked b48)
(positionfree fp38)
(ontop b28 lp5)
(atplace b28 pr1)
(stacked b28)
(positionfree fp46)
(ontop b34 lp6)
(atplace b34 pr2)
(stacked b34)
(positionfree fp54)
(ontop b40 lp7)
(atplace b40 pr3)
(stacked b40)
(holding r1 b46)
(positionfree fp62)
  )
  (:goal 
    (and
      (ontop b46 lp8)
(stacked b46)
(not (holding r1 b46))
(atplace b46 pr4)
(not (clear lp8))
(clear b46)
(vgempty r1)
    ) 
  )
)