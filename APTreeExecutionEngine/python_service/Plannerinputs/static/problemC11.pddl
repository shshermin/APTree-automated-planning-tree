(define (problem fit)
  (:domain fit)
  (:objects 
    lp11 - plate
    tp11 - plate
    b61 - beam
    b62 - beam
    b63 - beam
    b64 - beam
    b65 -beam
    b66 -beam
    fp81  - firstposition 
    fp82  - firstposition 
    fp83  - firstposition 
    fp84  - firstposition  
    fp85 - firstposition  
    fp86 - firstposition 
    fp87 - firstposition 
    fp88 - firstposition                     
    pr3  - positiononrail                                                      
    sp11  - stackposition
    r1  - robot
    m11 -  cassette
    lay11 - stack   
  )
  (:init  
   (= (freecapacity lp11) 6)
    (atplace lp11 fp81) 
    (atplace tp11 fp85)
    (atplace b61 fp82)  
    (atplace b62 fp83)
    (atplace b63 fp84)
    (atplace b64 fp86)
     (atplace b65 fp87)
     (atplace b66 fp88)
    (positionfree pr3)
    (positionfree sp11)
    (clear lp11)
    (clear tp11)
    (clear b61)
    (clear b62)
    (clear b63) 
    (clear b64)
    (clear b65)
    (clear b66)
    (belongstolayer b61 lay11) 
    (belongstomodule b61 m11)
    (belongstolayer b62 lay11) 
    (belongstomodule b62 m11)
    (belongstolayer b63 lay11) 
    (belongstomodule b63 m11)
    (belongstolayer b64 lay11) 
    (belongstomodule b64 m11)
    (belongstolayer b65 lay11) 
    (belongstomodule b65 m11)
    (belongstolayer b66 lay11) 
    (belongstomodule b66 m11) 
    (belongstomodule lp11 m11)
    (belongstomodule tp11 m11)
    (vgempty r1)
    
  )


  (:goal 
    (and
(= (freecapacity lp11 ) 0)
(glued lp11)  
(ontop b61 lp11)
(ontop b62 lp11)
(ontop b63 lp11)
(ontop b64 lp11)
(ontop b65 lp11)
(ontop b66 lp11)
(allset lay11 m11)
(glued b61)
(glued b62)
(glued b63)
(glued b64)
(glued b65)
(glued b66)
(nailed b61)
(nailed b62)
(nailed b63)
(nailed b64)
(nailed b65)
(nailed b66)
(ontop tp11 b61)
(nailed tp11)
(cassetteAtStack m11 sp11)

        ) 
  )
)
