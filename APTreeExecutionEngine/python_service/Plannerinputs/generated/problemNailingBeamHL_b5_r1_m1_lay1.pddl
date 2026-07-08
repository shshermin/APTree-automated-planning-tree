(define (problem nailingbeamhl)
  (:domain fit)
  (:objects 
    fp1 - firstposition
fp2 - firstposition
fp3 - firstposition
fp4 - firstposition
fp5 - firstposition
fp6 - firstposition
fp7 - firstposition
fp8 - firstposition
fp9 - firstposition
fp10 - firstposition
fp11 - firstposition
fp12 - firstposition
fp13 - firstposition
fp14 - firstposition
fp15 - firstposition
fp16 - firstposition
fp17 - firstposition
fp18 - firstposition
fp19 - firstposition
fp20 - firstposition
fp21 - firstposition
fp22 - firstposition
fp23 - firstposition
fp24 - firstposition
fp25 - firstposition
fp26 - firstposition
fp27 - firstposition
fp28 - firstposition
fp29 - firstposition
fp30 - firstposition
fp31 - firstposition
fp32 - firstposition
pr1 - positiononrail
pr2 - positiononrail
pr3 - positiononrail
pr4 - positiononrail
ep1 - equipposition
ep2 - equipposition
ep3 - equipposition
ep4 - equipposition
r1 - robot
b1 - beam
b2 - beam
b3 - beam
b4 - beam
b5 - beam
b6 - beam
b7 - beam
b8 - beam
b9 - beam
b10 - beam
b11 - beam
b12 - beam
b13 - beam
b14 - beam
b15 - beam
b16 - beam
b17 - beam
b18 - beam
b19 - beam
b20 - beam
b21 - beam
b22 - beam
b23 - beam
b24 - beam
lp1 - plate
lp2 - plate
lp3 - plate
lp4 - plate
tp1 - plate
tp2 - plate
tp3 - plate
tp4 - plate
vg1 - vacgripper
ng1 - nailgripper
gg1 - gluegun
m1 - cassette
m2 - cassette
m3 - cassette
m4 - cassette
lay1 - stack
lay2 - stack
lay3 - stack
lay4 - stack
sp1 - stackposition
sp2 - stackposition
sp3 - stackposition
sp4 - stackposition
  )
  (:init  
    (atplace tp1 fp5)
(atplace lp2 fp9)
(atplace tp2 fp13)
(atplace b7 fp10)
(atplace b8 fp11)
(atplace b9 fp12)
(atplace b10 fp14)
(atplace b11 fp15)
(atplace b12 fp16)
(atplace lp3 fp17)
(atplace tp3 fp21)
(atplace b13 fp18)
(atplace b14 fp19)
(atplace b15 fp20)
(atplace b16 fp22)
(atplace b17 fp23)
(atplace b18 fp24)
(atplace lp4 fp25)
(atplace tp4 fp29)
(atplace b19 fp26)
(atplace b20 fp27)
(atplace b21 fp28)
(atplace b22 fp30)
(atplace b23 fp31)
(atplace b24 fp32)
(clear tp1)
(clear b1)
(clear b2)
(clear b3)
(clear b4)
(clear b5)
(clear b6)
(clear lp2)
(clear tp2)
(clear b7)
(clear b8)
(clear b9)
(clear b10)
(clear b11)
(clear b12)
(clear lp3)
(clear tp3)
(clear b13)
(clear b14)
(clear b15)
(clear b16)
(clear b17)
(clear b18)
(clear lp4)
(clear tp4)
(clear b19)
(clear b20)
(clear b21)
(clear b22)
(clear b23)
(clear b24)
(attool vg1 ep1)
(attool gg1 ep2)
(atagent r1 pr1)
(vgempty r1)
(belongstolayer b1 lay1)
(belongstolayer b2 lay1)
(belongstolayer b3 lay1)
(belongstolayer b4 lay1)
(belongstolayer b5 lay1)
(belongstolayer b6 lay1)
(belongstolayer b7 lay2)
(belongstolayer b8 lay2)
(belongstolayer b9 lay2)
(belongstolayer b10 lay2)
(belongstolayer b11 lay2)
(belongstolayer b12 lay2)
(belongstolayer b13 lay3)
(belongstolayer b14 lay3)
(belongstolayer b15 lay3)
(belongstolayer b16 lay3)
(belongstolayer b17 lay3)
(belongstolayer b18 lay3)
(belongstolayer b19 lay4)
(belongstolayer b20 lay4)
(belongstolayer b21 lay4)
(belongstolayer b22 lay4)
(belongstolayer b23 lay4)
(belongstolayer b24 lay4)
(belongstomodule b1 m1)
(belongstomodule b2 m1)
(belongstomodule b3 m1)
(belongstomodule b4 m1)
(belongstomodule b5 m1)
(belongstomodule b6 m1)
(belongstomodule lp1 m1)
(belongstomodule tp1 m1)
(belongstomodule b7 m2)
(belongstomodule b8 m2)
(belongstomodule b9 m2)
(belongstomodule b10 m2)
(belongstomodule b11 m2)
(belongstomodule b12 m2)
(belongstomodule lp2 m2)
(belongstomodule tp2 m2)
(belongstomodule b13 m3)
(belongstomodule b14 m3)
(belongstomodule b15 m3)
(belongstomodule b16 m3)
(belongstomodule b17 m3)
(belongstomodule b18 m3)
(belongstomodule lp3 m3)
(belongstomodule tp3 m3)
(belongstomodule b19 m4)
(belongstomodule b20 m4)
(belongstomodule b21 m4)
(belongstomodule b22 m4)
(belongstomodule b23 m4)
(belongstomodule b24 m4)
(belongstomodule lp4 m4)
(belongstomodule tp4 m4)
(positionfree pr2)
(positionfree pr3)
(positionfree pr4)
(positionfree sp1)
(positionfree sp2)
(positionfree sp3)
(positionfree sp4)
(robotequipped r1)
(positionfree fp1)
(atplace lp1 pr1)
(glued lp1)
(positionfree fp2)
(ontop b1 lp1)
(atplace b1 pr1)
(stacked b1)
(positionfree fp3)
(ontop b2 lp1)
(atplace b2 pr1)
(stacked b2)
(positionfree fp8)
(ontop b6 lp1)
(atplace b6 pr1)
(stacked b6)
(positionfree fp6)
(ontop b4 lp1)
(atplace b4 pr1)
(stacked b4)
(positionfree fp4)
(ontop b3 lp1)
(atplace b3 pr1)
(stacked b3)
(positionfree fp7)
(ontop b5 lp1)
(atplace b5 pr1)
(stacked b5)
(hastool r1 ng1)
(positionfree ep3)
(activetool ng1)
(nailed b2)
(nailed b4)
(nailed b3)
  )
  (:goal 
    (and
      (nailed b5)
    ) 
  )
)