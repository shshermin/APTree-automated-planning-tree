(define (problem fit)
  (:domain fit)
  (:objects 
    lp4 - plate
    tp4 - plate
    b19 - beam
    b20 - beam
    b21 - beam
    b22 - beam
    b23 -beam
    b24 -beam
    fp25  - firstposition 
    fp26  - firstposition 
    fp27  - firstposition 
    fp28  - firstposition  
    fp29 - firstposition  
    fp30 - firstposition 
    fp31 - firstposition 
    fp32 - firstposition                     
    pr4  - positiononrail                                                      
    sp4  - stackposition
    r1  - robot
    m4 -  cassette
    lay4 - stack   
  )
  (:init  
   (= (freecapacity lp4) 6)
    (atplace lp4 fp25) 
    (atplace tp4 fp29)
    (atplace b19 fp26)  
    (atplace b20 fp27)
    (atplace b21 fp28)
    (atplace b22 fp30)
     (atplace b23 fp31)
     (atplace b24 fp32)
    (positionfree pr4)
    (positionfree sp4)
    (clear lp4)
    (clear tp4)
    (clear b19)
    (clear b20)
    (clear b21) 
    (clear b22)
    (clear b23)
    (clear b24)
    (belongstolayer b19 lay4) 
    (belongstomodule b19 m4)
    (belongstolayer b20 lay4) 
    (belongstomodule b20 m4)
    (belongstolayer b21 lay4) 
    (belongstomodule b21 m4)
    (belongstolayer b22 lay4) 
    (belongstomodule b22 m4)
    (belongstolayer b23 lay4) 
    (belongstomodule b23 m4)
    (belongstolayer b24 lay4) 
    (belongstomodule b24 m4) 
    (belongstomodule lp4 m4)
    (belongstomodule tp4 m4)
    (vgempty r1)
  )


  (:goal 
    (and
(= (freecapacity lp4 ) 0)
(glued lp4)  
(ontop b19 lp4)
(ontop b20 lp4)
(ontop b21 lp4)
(ontop b22 lp4)
(ontop b23 lp4)
(ontop b24 lp4)
(allset lay4 m4)
(glued b19)
(glued b20)
(glued b21)
(glued b22)
(glued b23)
(glued b24)
(nailed b19)
(nailed b20)
(nailed b21)
(nailed b22)
(nailed b23)
(nailed b24)
(ontop tp4 b19)
(nailed tp4)
(cassetteAtStack m4 sp4)

        ) 
  )
)