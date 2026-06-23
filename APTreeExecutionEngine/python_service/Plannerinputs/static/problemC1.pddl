(define (problem fit)
  (:domain fit)
  (:objects 
    lp1 - plate
    tp1 - plate
    b1 - beam
    b2 - beam
    b3 - beam
    b4 - beam
    b5 -beam
    b6 -beam
    fp1  - firstposition 
    fp2  - firstposition 
    fp3  - firstposition 
    fp4  - firstposition  
    fp5 - firstposition  
    fp6 - firstposition 
    fp7 - firstposition 
    fp8 - firstposition                     
    pr1  - positiononrail                                                      
    sp1  - stackposition
    r1  - robot
    m1 -  cassette
    lay1 - stack   
  )
  (:init  
   (= (freecapacity lp1) 6)
    (atplace lp1 fp1) 
    (atplace tp1 fp5)
    (atplace b1 fp2)  
    (atplace b2 fp3)
    (atplace b3 fp4)
    (atplace b4 fp6)
     (atplace b5 fp7)
     (atplace b6 fp8)
    (positionfree pr1)
    (positionfree sp1)
    (clear lp1)
    (clear tp1)
    (clear b1)
    (clear b2)
    (clear b3) 
    (clear b4)
    (clear b5)
    (clear b6)
    (belongstolayer b1 lay1) 
    (belongstomodule b1 m1)
    (belongstolayer b2 lay1) 
    (belongstomodule b2 m1)
    (belongstolayer b3 lay1) 
    (belongstomodule b3 m1)
    (belongstolayer b4 lay1) 
    (belongstomodule b4 m1)
    (belongstolayer b5 lay1) 
    (belongstomodule b5 m1)
    (belongstolayer b6 lay1) 
    (belongstomodule b6 m1) 
    (belongstomodule lp1 m1)
    (belongstomodule tp1 m1)
    (vgempty r1)
    
  )


  (:goal 
    (and
(= (freecapacity lp1 ) 0)
(glued lp1)  
(ontop b1 lp1)
(ontop b2 lp1)
(ontop b3 lp1)
(ontop b4 lp1)
(ontop b5 lp1)
(ontop b6 lp1)
(allset lay1 m1)
(glued b1)
(glued b2)
(glued b3)
(glued b4)
(glued b5)
(glued b6)
(nailed b1)
(nailed b2)
(nailed b3)
(nailed b4)
(nailed b5)
(nailed b6)
(ontop tp1 b1)
(nailed tp1)
(cassetteAtStack m1 sp1)

        ) 
  )
)