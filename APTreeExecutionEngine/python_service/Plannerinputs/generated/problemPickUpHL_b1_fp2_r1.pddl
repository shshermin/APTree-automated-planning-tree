(define (problem pickuphl)
  (:domain fit)
  (:objects 
    fp1 - firstposition
fp2 - firstposition
fp3 - firstposition
pr1 - positiononrail
pr2 - positiononrail
ep1 - equipposition
ep2 - equipposition
ep3 - equipposition
r1 - robot
b1 - beam
b2 - beam
lp1 - plate
vg1 - vacgripper
ng1 - nailgripper
gg1 - gluegun
m1 - cassette
lay1 - stack
  )
  (:init  
    (atplace b1 fp2)
(clear b1)
(clear b2)
(attool gg1 ep2)
(attool ng1 ep3)
(atagent r1 pr1)
(vgempty r1)
(belongstolayer b1 lay1)
(belongstolayer b2 lay1)
(belongstomodule b1 m1)
(belongstomodule b2 m1)
(belongstomodule lp1 m1)
(positionfree pr2)
(hastool r1 vg1)
(robotequipped r1)
(positionfree ep1)
(activetool vg1)
(positionfree fp1)
(atplace lp1 pr1)
(glued lp1)
(positionfree fp3)
(ontop b2 lp1)
(atplace b2 pr1)
(stacked b2)
  )
  (:goal 
    (and
      (holding r1 b1)
(not (atplace b1 fp2))
(not (clear b1))
(positionfree fp2)
(not (vgempty r1))
    ) 
  )
)