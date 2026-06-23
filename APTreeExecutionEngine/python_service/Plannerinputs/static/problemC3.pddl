(define (problem fit)
  (:domain fit)
  (:objects 
    lp3 - plate
    tp3 - plate
    b13 - beam
    b14 - beam
    b15 - beam
    b16 - beam
    b17 -beam
    b18 -beam
    fp17  - firstposition 
    fp18  - firstposition 
    fp19  - firstposition 
    fp20  - firstposition  
    fp21 - firstposition  
    fp22 - firstposition 
    fp23 - firstposition 
    fp24 - firstposition                     
    pr3  - positiononrail                                                      
    sp3  - stackposition
    r1  - robot
    m3 -  cassette
    lay3 - stack   
  )
  (:init  
   (= (freecapacity lp3) 6)
    (atplace lp3 fp17) 
    (atplace tp3 fp21)
    (atplace b13 fp18)  
    (atplace b14 fp19)
    (atplace b15 fp20)
    (atplace b16 fp22)
     (atplace b17 fp23)
     (atplace b18 fp24)
    (positionfree pr3)
    (positionfree sp3)
    (clear lp3)
    (clear tp3)
    (clear b13)
    (clear b14)
    (clear b15) 
    (clear b16)
    (clear b17)
    (clear b18)
    (belongstolayer b13 lay3) 
    (belongstomodule b13 m3)
    (belongstolayer b14 lay3) 
    (belongstomodule b14 m3)
    (belongstolayer b15 lay3) 
    (belongstomodule b15 m3)
    (belongstolayer b16 lay3) 
    (belongstomodule b16 m3)
    (belongstolayer b17 lay3) 
    (belongstomodule b17 m3)
    (belongstolayer b18 lay3) 
    (belongstomodule b18 m3) 
    (belongstomodule lp3 m3)
    (belongstomodule tp3 m3)
    (vgempty r1)

  )


  (:goal 
    (and
(= (freecapacity lp3 ) 0)
(glued lp3)  
(ontop b13 lp3)
(ontop b14 lp3)
(ontop b15 lp3)
(ontop b16 lp3)
(ontop b17 lp3)
(ontop b18 lp3)
(allset lay3 m3)
(glued b13)
(glued b14)
(glued b15)
(glued b16)
(glued b17)
(glued b18)
(nailed b13)
(nailed b14)
(nailed b15)
(nailed b16)
(nailed b17)
(nailed b18)
(ontop tp3 b13)
(nailed tp3)
(cassetteAtStack m3 sp3)

        ) 
  )
)