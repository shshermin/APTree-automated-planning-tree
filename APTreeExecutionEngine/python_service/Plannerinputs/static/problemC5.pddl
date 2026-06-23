(define (problem fit)
  (:domain fit)
  (:objects 
    lp5 - plate
    tp5 - plate
    b25 - beam
    b26 - beam
    b27 - beam
    b28 - beam
    b29 -beam
    b30 -beam
    fp33  - firstposition 
    fp34  - firstposition 
    fp35  - firstposition 
    fp36  - firstposition  
    fp37 - firstposition  
    fp38 - firstposition 
    fp39 - firstposition 
    fp40 - firstposition                     
    pr1  - positiononrail                                                      
    sp5  - stackposition
    r1  - robot
    m5 -  cassette
    lay5 - stack   
  )
  (:init  
   (= (freecapacity lp5) 6)
    (atplace lp5 fp33) 
    (atplace tp5 fp37)
    (atplace b25 fp34)  
    (atplace b26 fp35)
    (atplace b27 fp36)
    (atplace b28 fp38)
     (atplace b29 fp39)
     (atplace b30 fp40)
    (positionfree pr1)
    (positionfree sp5)
    (clear lp5)
    (clear tp5)
    (clear b25)
    (clear b26)
    (clear b27) 
    (clear b28)
    (clear b29)
    (clear b30)
    (belongstolayer b25 lay5) 
    (belongstomodule b25 m5)
    (belongstolayer b26 lay5) 
    (belongstomodule b26 m5)
    (belongstolayer b27 lay5) 
    (belongstomodule b27 m5)
    (belongstolayer b28 lay5) 
    (belongstomodule b28 m5)
    (belongstolayer b29 lay5) 
    (belongstomodule b29 m5)
    (belongstolayer b30 lay5) 
    (belongstomodule b30 m5) 
    (belongstomodule lp5 m5)
    (belongstomodule tp5 m5)
    (vgempty r1)
    
  )


  (:goal 
    (and
(= (freecapacity lp5 ) 0)
(glued lp5)  
(ontop b25 lp5)
(ontop b26 lp5)
(ontop b27 lp5)
(ontop b28 lp5)
(ontop b29 lp5)
(ontop b30 lp5)
(allset lay5 m5)
(glued b25)
(glued b26)
(glued b27)
(glued b28)
(glued b29)
(glued b30)
(nailed b25)
(nailed b26)
(nailed b27)
(nailed b28)
(nailed b29)
(nailed b30)
(ontop tp5 b25)
(nailed tp5)
(cassetteAtStack m5 sp5)

        ) 
  )
)
