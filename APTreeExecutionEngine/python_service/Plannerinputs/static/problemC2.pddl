(define (problem fit)
  (:domain fit)
  (:objects 
    lp2 - plate
    tp2 - plate
    b7 - beam
    b8 - beam
    b9 - beam
    b10 - beam
    b11 -beam
    b12 -beam
    fp9  - firstposition 
    fp10  - firstposition 
    fp11  - firstposition 
    fp12  - firstposition  
    fp13 - firstposition  
    fp14 - firstposition 
    fp15 - firstposition 
    fp16 - firstposition                     
    pr2  - positiononrail                                                      
    r1  - robot
    m2 -  cassette
    lay1 - stack   
  )
  (:init  
   (= (freecapacity lp2) 6)
    (atplace lp2 fp9) 
    (atplace tp2 fp13)
    (atplace b7 fp10)  
    (atplace b8 fp11)
    (atplace b9 fp12)
    (atplace b10 fp14)
     (atplace b11 fp15)
     (atplace b12 fp16)
    (positionfree pr2)
    (clear lp2)
    (clear tp2)
    (clear b7)
    (clear b8)
    (clear b9) 
    (clear b10)
    (clear b11)
    (clear b12)
    (belongstolayer b7 lay1) 
    (belongstomodule b7 m2)
    (belongstolayer b8 lay1) 
    (belongstomodule b8 m2)
    (belongstolayer b9 lay1) 
    (belongstomodule b9 m2)
    (belongstolayer b10 lay1) 
    (belongstomodule b10 m2)
    (belongstolayer b11 lay1) 
    (belongstomodule b11 m2)
    (belongstolayer b12 lay1) 
    (belongstomodule b12 m2) 
    (vgempty r1)

  )


  (:goal 
    (and
(= (freecapacity lp2 ) 0)
(atplace lp2 pr2)
(glued lp2)  
(ontop b7 lp2)
(ontop b8 lp2)
(ontop b9 lp2)
(ontop b10 lp2)
(ontop b11 lp2)
(ontop b12 lp2)
(atplace b7 pr2)
(atplace b8 pr2)
(atplace b9 pr2)
(atplace b10 pr2)
(atplace b11 pr2)
(atplace b12 pr2)
(allset lay1 m2)
(glued b7)
(glued b8)
(glued b9)
(glued b10)
(glued b11)
(glued b12)
(nailed b7)
(nailed b8)
(nailed b9)
(nailed b10)
(nailed b11)
(nailed b12)
(ontop tp2 b7)
(atplace tp2 pr2)
(nailed tp2)

        ) 
  )
)