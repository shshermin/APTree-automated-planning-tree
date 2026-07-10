(define (problem placehl)
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
(atplace b2 fp3)
(clear b1)
(clear b2)
(attool gg1 ep2)
(attool ng1 ep3)
(atagent r1 fp1)
(belongstolayer b1 lay1)
(belongstolayer b2 lay1)
(belongstomodule b1 m1)
(belongstomodule b2 m1)
(belongstomodule lp1 m1)
(positionfree pr1)
(positionfree pr2)
(hastool r1 vg1)
(robotequipped r1)
(positionfree ep1)
(activetool vg1)
(holding r1 lp1)
(positionfree fp1)
  )
  (:goal 
    (and
      (atplace lp1 pr1)
(not (holding r1 lp1))
(clear lp1)
(not (positionfree pr1))
(vgempty r1)
    ) 
  )
)