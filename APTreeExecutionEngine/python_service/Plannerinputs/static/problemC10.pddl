(define (problem fit)
  (:domain fit)
  (:objects 
    lp10 - plate
    tp10 - plate
    b55 - beam
    b56 - beam
    b57 - beam
    b58 - beam
    b59 -beam
    b60 -beam
    fp73  - firstposition 
    fp74  - firstposition 
    fp75  - firstposition 
    fp76  - firstposition  
    fp77 - firstposition  
    fp78 - firstposition 
    fp79 - firstposition 
    fp80 - firstposition                     
    pr2  - positiononrail                                                      
    sp10  - stackposition
    r1  - robot
    m10 -  cassette
    lay10 - stack   
  )
  (:init  
   (= (freecapacity lp10) 6)
    (atplace lp10 fp73) 
    (atplace tp10 fp77)
    (atplace b55 fp74)  
    (atplace b56 fp75)
    (atplace b57 fp76)
    (atplace b58 fp78)
     (atplace b59 fp79)
     (atplace b60 fp80)
    (positionfree pr2)
    (positionfree sp10)
    (clear lp10)
    (clear tp10)
    (clear b55)
    (clear b56)
    (clear b57) 
    (clear b58)
    (clear b59)
    (clear b60)
    (belongstolayer b55 lay10) 
    (belongstomodule b55 m10)
    (belongstolayer b56 lay10) 
    (belongstomodule b56 m10)
    (belongstolayer b57 lay10) 
    (belongstomodule b57 m10)
    (belongstolayer b58 lay10) 
    (belongstomodule b58 m10)
    (belongstolayer b59 lay10) 
    (belongstomodule b59 m10)
    (belongstolayer b60 lay10) 
    (belongstomodule b60 m10) 
    (belongstomodule lp10 m10)
    (belongstomodule tp10 m10)
    (vgempty r1)
    
  )


  (:goal 
    (and
(= (freecapacity lp10 ) 0)
(glued lp10)  
(ontop b55 lp10)
(ontop b56 lp10)
(ontop b57 lp10)
(ontop b58 lp10)
(ontop b59 lp10)
(ontop b60 lp10)
(allset lay10 m10)
(glued b55)
(glued b56)
(glued b57)
(glued b58)
(glued b59)
(glued b60)
(nailed b55)
(nailed b56)
(nailed b57)
(nailed b58)
(nailed b59)
(nailed b60)
(ontop tp10 b55)
(nailed tp10)
(cassetteAtStack m10 sp10)

        ) 
  )
)
