(define (problem fit)
  (:domain fit)
  (:objects 
    lp9 - plate
    tp9 - plate
    b49 - beam
    b50 - beam
    b51 - beam
    b52 - beam
    b53 -beam
    b54 -beam
    fp65  - firstposition 
    fp66  - firstposition 
    fp67  - firstposition 
    fp68  - firstposition  
    fp69 - firstposition  
    fp70 - firstposition 
    fp71 - firstposition 
    fp72 - firstposition                     
    pr1  - positiononrail                                                      
    sp9  - stackposition
    r1  - robot
    m9 -  cassette
    lay9 - stack   
  )
  (:init  
   (= (freecapacity lp9) 6)
    (atplace lp9 fp65) 
    (atplace tp9 fp69)
    (atplace b49 fp66)  
    (atplace b50 fp67)
    (atplace b51 fp68)
    (atplace b52 fp70)
     (atplace b53 fp71)
     (atplace b54 fp72)
    (positionfree pr1)
    (positionfree sp9)
    (clear lp9)
    (clear tp9)
    (clear b49)
    (clear b50)
    (clear b51) 
    (clear b52)
    (clear b53)
    (clear b54)
    (belongstolayer b49 lay9) 
    (belongstomodule b49 m9)
    (belongstolayer b50 lay9) 
    (belongstomodule b50 m9)
    (belongstolayer b51 lay9) 
    (belongstomodule b51 m9)
    (belongstolayer b52 lay9) 
    (belongstomodule b52 m9)
    (belongstolayer b53 lay9) 
    (belongstomodule b53 m9)
    (belongstolayer b54 lay9) 
    (belongstomodule b54 m9) 
    (belongstomodule lp9 m9)
    (belongstomodule tp9 m9)
    (vgempty r1)
    
  )


  (:goal 
    (and
(= (freecapacity lp9 ) 0)
(glued lp9)  
(ontop b49 lp9)
(ontop b50 lp9)
(ontop b51 lp9)
(ontop b52 lp9)
(ontop b53 lp9)
(ontop b54 lp9)
(allset lay9 m9)
(glued b49)
(glued b50)
(glued b51)
(glued b52)
(glued b53)
(glued b54)
(nailed b49)
(nailed b50)
(nailed b51)
(nailed b52)
(nailed b53)
(nailed b54)
(ontop tp9 b49)
(nailed tp9)
(cassetteAtStack m9 sp9)

        ) 
  )
)
