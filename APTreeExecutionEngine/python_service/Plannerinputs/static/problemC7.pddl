(define (problem fit)
  (:domain fit)
  (:objects 
    lp7 - plate
    tp7 - plate
    b37 - beam
    b38 - beam
    b39 - beam
    b40 - beam
    b41 -beam
    b42 -beam
    fp49  - firstposition 
    fp50  - firstposition 
    fp51  - firstposition 
    fp52  - firstposition  
    fp53 - firstposition  
    fp54 - firstposition 
    fp55 - firstposition 
    fp56 - firstposition                     
    pr3  - positiononrail                                                      
    sp7  - stackposition
    r1  - robot
    m7 -  cassette
    lay7 - stack   
  )
  (:init  
   (= (freecapacity lp7) 6)
    (atplace lp7 fp49) 
    (atplace tp7 fp53)
    (atplace b37 fp50)  
    (atplace b38 fp51)
    (atplace b39 fp52)
    (atplace b40 fp54)
     (atplace b41 fp55)
     (atplace b42 fp56)
    (positionfree pr3)
    (positionfree sp7)
    (clear lp7)
    (clear tp7)
    (clear b37)
    (clear b38)
    (clear b39) 
    (clear b40)
    (clear b41)
    (clear b42)
    (belongstolayer b37 lay7) 
    (belongstomodule b37 m7)
    (belongstolayer b38 lay7) 
    (belongstomodule b38 m7)
    (belongstolayer b39 lay7) 
    (belongstomodule b39 m7)
    (belongstolayer b40 lay7) 
    (belongstomodule b40 m7)
    (belongstolayer b41 lay7) 
    (belongstomodule b41 m7)
    (belongstolayer b42 lay7) 
    (belongstomodule b42 m7) 
    (belongstomodule lp7 m7)
    (belongstomodule tp7 m7)
    (vgempty r1)
    
  )


  (:goal 
    (and
(= (freecapacity lp7 ) 0)
(glued lp7)  
(ontop b37 lp7)
(ontop b38 lp7)
(ontop b39 lp7)
(ontop b40 lp7)
(ontop b41 lp7)
(ontop b42 lp7)
(allset lay7 m7)
(glued b37)
(glued b38)
(glued b39)
(glued b40)
(glued b41)
(glued b42)
(nailed b37)
(nailed b38)
(nailed b39)
(nailed b40)
(nailed b41)
(nailed b42)
(ontop tp7 b37)
(nailed tp7)
(cassetteAtStack m7 sp7)

        ) 
  )
)
