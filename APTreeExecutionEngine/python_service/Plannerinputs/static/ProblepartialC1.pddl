(define (problem fit)
  (:domain fit)
  (:objects 
    lp1 - plate
    b1 - beam
    b2 - beam
    fp1  - firstposition 
    fp2  - firstposition 
    fp3  - firstposition                    
    pr1  - positiononrail                                                      
    r1  - robot
    m1 -  cassette
    lay1 - stack   
  )
  (:init  
   (= (freecapacity lp1) 2)
    (atplace lp1 fp1) 
    (atplace b1 fp2)  
    (atplace b2 fp3)
    (positionfree pr1)
    (clear lp1)
    (clear b1)
    (clear b2)
    (belongstolayer b1 lay1) 
    (belongstomodule b1 m1)
    (belongstolayer b2 lay1) 
    (belongstomodule b2 m1)
    (vgempty r1)
    
  )


  (:goal 
    (and
(= (freecapacity lp1 ) 0)
(atplace lp1 pr1)
(glued lp1)  
(ontop b1 lp1)
(ontop b2 lp1)
;(atplace b1 pr1)
;(atplace b2 pr1)
;(allset lay1 m1)



        ) 
  )
)