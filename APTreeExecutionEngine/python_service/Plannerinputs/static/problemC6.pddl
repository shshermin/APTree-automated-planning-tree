(define (problem fit)
  (:domain fit)
  (:objects 
    lp6 - plate
    tp6 - plate
    b31 - beam
    b32 - beam
    b33 - beam
    b34 - beam
    b35 -beam
    b36 -beam
    fp41  - firstposition 
    fp42  - firstposition 
    fp43  - firstposition 
    fp44  - firstposition  
    fp45 - firstposition  
    fp46 - firstposition 
    fp47 - firstposition 
    fp48 - firstposition                     
    pr2  - positiononrail                                                      
    sp6  - stackposition
    r1  - robot
    m6 -  cassette
    lay6 - stack   
  )
  (:init  
   (= (freecapacity lp6) 6)
    (atplace lp6 fp41) 
    (atplace tp6 fp45)
    (atplace b31 fp42)  
    (atplace b32 fp43)
    (atplace b33 fp44)
    (atplace b34 fp46)
     (atplace b35 fp47)
     (atplace b36 fp48)
    (positionfree pr2)
    (positionfree sp6)
    (clear lp6)
    (clear tp6)
    (clear b31)
    (clear b32)
    (clear b33) 
    (clear b34)
    (clear b35)
    (clear b36)
    (belongstolayer b31 lay6) 
    (belongstomodule b31 m6)
    (belongstolayer b32 lay6) 
    (belongstomodule b32 m6)
    (belongstolayer b33 lay6) 
    (belongstomodule b33 m6)
    (belongstolayer b34 lay6) 
    (belongstomodule b34 m6)
    (belongstolayer b35 lay6) 
    (belongstomodule b35 m6)
    (belongstolayer b36 lay6) 
    (belongstomodule b36 m6) 
    (belongstomodule lp6 m6)
    (belongstomodule tp6 m6)
    (vgempty r1)
    
  )


  (:goal 
    (and
(= (freecapacity lp6 ) 0)
(glued lp6)  
(ontop b31 lp6)
(ontop b32 lp6)
(ontop b33 lp6)
(ontop b34 lp6)
(ontop b35 lp6)
(ontop b36 lp6)
(allset lay6 m6)
(glued b31)
(glued b32)
(glued b33)
(glued b34)
(glued b35)
(glued b36)
(nailed b31)
(nailed b32)
(nailed b33)
(nailed b34)
(nailed b35)
(nailed b36)
(ontop tp6 b31)
(nailed tp6)
(cassetteAtStack m6 sp6)

        ) 
  )
)
