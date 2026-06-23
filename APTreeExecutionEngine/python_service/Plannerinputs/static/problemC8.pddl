(define (problem fit)
  (:domain fit)
  (:objects 
    lp8 - plate
    tp8 - plate
    b43 - beam
    b44 - beam
    b45 - beam
    b46 - beam
    b47 -beam
    b48 -beam
    fp57  - firstposition 
    fp58  - firstposition 
    fp59  - firstposition 
    fp60  - firstposition  
    fp61 - firstposition  
    fp62 - firstposition 
    fp63 - firstposition 
    fp64 - firstposition                     
    pr4  - positiononrail                                                      
    sp8  - stackposition
    r1  - robot
    m8 -  cassette
    lay8 - stack   
  )
  (:init  
   (= (freecapacity lp8) 6)
    (atplace lp8 fp57) 
    (atplace tp8 fp61)
    (atplace b43 fp58)  
    (atplace b44 fp59)
    (atplace b45 fp60)
    (atplace b46 fp62)
     (atplace b47 fp63)
     (atplace b48 fp64)
    (positionfree pr4)
    (positionfree sp8)
    (clear lp8)
    (clear tp8)
    (clear b43)
    (clear b44)
    (clear b45) 
    (clear b46)
    (clear b47)
    (clear b48)
    (belongstolayer b43 lay8) 
    (belongstomodule b43 m8)
    (belongstolayer b44 lay8) 
    (belongstomodule b44 m8)
    (belongstolayer b45 lay8) 
    (belongstomodule b45 m8)
    (belongstolayer b46 lay8) 
    (belongstomodule b46 m8)
    (belongstolayer b47 lay8) 
    (belongstomodule b47 m8)
    (belongstolayer b48 lay8) 
    (belongstomodule b48 m8) 
    (belongstomodule lp8 m8)
    (belongstomodule tp8 m8)
    (vgempty r1)
    
  )


  (:goal 
    (and
(= (freecapacity lp8 ) 0)
(glued lp8)  
(ontop b43 lp8)
(ontop b44 lp8)
(ontop b45 lp8)
(ontop b46 lp8)
(ontop b47 lp8)
(ontop b48 lp8)
(allset lay8 m8)
(glued b43)
(glued b44)
(glued b45)
(glued b46)
(glued b47)
(glued b48)
(nailed b43)
(nailed b44)
(nailed b45)
(nailed b46)
(nailed b47)
(nailed b48)
(ontop tp8 b43)
(nailed tp8)
(cassetteAtStack m8 sp8)

        ) 
  )
)
