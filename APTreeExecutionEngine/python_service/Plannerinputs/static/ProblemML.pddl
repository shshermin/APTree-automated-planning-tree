(define (problem fit)
  (:domain fit)
  (:objects 
    lp1 - plate
    tp1 - plate
    b1 - beam
    b2 - beam
    b3 - beam
    vg1  - vaccumgripper
    gg1 - gluegun
    ng1 - nailgripper
    fp1  - firstposition 
    fp2  - firstposition 
    fp3  - firstposition 
    fp4  - firstposition  
    fp5 - firstposition                       
    pr2  - positiononrail                                                      
    ep1  - equipposition
    ep2  - equipposition
    ep3  - equipposition
    r1  - robot

   
  )
  (:init  
    (atagent r1 pr2) 
    (atplace lp1 fp1) 
    (positionfree pr2)
    (attool vg1 ep1) 
    (attool gg1 ep2)
    (attool ng1 ep3)
    (clear lp1)  
    (vgempty r1) 
  )


  (:goal (and

  (holding r1 lp1)
        ) 
  )
)