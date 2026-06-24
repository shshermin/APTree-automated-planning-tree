(define (problem fit)
  (:domain fit)
  (:objects 
    lp12 - plate
    tp12 - plate
    b67 - beam
    b68 - beam
    b69 - beam
    b70 - beam
    b71 -beam
    b72 -beam
    fp89  - firstposition 
    fp90  - firstposition 
    fp91  - firstposition 
    fp92  - firstposition  
    fp93 - firstposition  
    fp94 - firstposition 
    fp95 - firstposition 
    fp96 - firstposition                     
    pr4  - positiononrail                                                      
    sp12  - stackposition
    r1  - robot
    m12 -  cassette
    lay12 - stack   
  )
  (:init  
   (= (freecapacity lp12) 6)
    (atplace lp12 fp89) 
    (atplace tp12 fp93)
    (atplace b67 fp90)  
    (atplace b68 fp91)
    (atplace b69 fp92)
    (atplace b70 fp94)
     (atplace b71 fp95)
     (atplace b72 fp96)
    (positionfree pr4)
    (positionfree sp12)
    (clear lp12)
    (clear tp12)
    (clear b67)
    (clear b68)
    (clear b69) 
    (clear b70)
    (clear b71)
    (clear b72)
    (belongstolayer b67 lay12) 
    (belongstomodule b67 m12)
    (belongstolayer b68 lay12) 
    (belongstomodule b68 m12)
    (belongstolayer b69 lay12) 
    (belongstomodule b69 m12)
    (belongstolayer b70 lay12) 
    (belongstomodule b70 m12)
    (belongstolayer b71 lay12) 
    (belongstomodule b71 m12)
    (belongstolayer b72 lay12) 
    (belongstomodule b72 m12) 
    (belongstomodule lp12 m12)
    (belongstomodule tp12 m12)
    (vgempty r1)
    
  )


  (:goal 
    (and
(= (freecapacity lp12 ) 0)
(glued lp12)  
(ontop b67 lp12)
(ontop b68 lp12)
(ontop b69 lp12)
(ontop b70 lp12)
(ontop b71 lp12)
(ontop b72 lp12)
(allset lay12 m12)
(glued b67)
(glued b68)
(glued b69)
(glued b70)
(glued b71)
(glued b72)
(nailed b67)
(nailed b68)
(nailed b69)
(nailed b70)
(nailed b71)
(nailed b72)
(ontop tp12 b67)
(nailed tp12)
(cassetteAtStack m12 sp12)

        ) 
  )
)
