(define (domain fit) 

  (:types 
      
    firstposition positiononrail stackposition - location                                                                
                                                       
    plate beam - element
   
    stack -layer

    cassette - module 

    robot -agent    

    vacgripper -tool                               
  )
 
  (:predicates
   
    (atplace ?obj - element ?place - location)                                                                                               
    (holding ?client - robot ?obj - element)        
    (clear ?obj - element)    
    (ontop ?obj1 - element ?obj2 - element)
    (allset ?lay - layer ?mod - module) 
    (belongstolayer ?obj - element ?lay - layer)
    (belongstomodule ?obj - element ?mod - module)   
    (positionfree ?pos - location)
    (stacked ?obj - element)     
    (glued ?obj - element) 
    (nailed ?obj - element)   
    (vgempty ?vg - tool)
)

  ; assigns the number of element that can be stacked on top of this module
  (:functions   
    (freecapacity ?plate - element) 
  )
    
    ;robot grabs an object from the table
    (:action pickUpHL
    :parameters (?obj - element ?p - location  ?client - robot )
    
    :precondition (and    
      (vgempty ?client)                            
      (atplace ?obj ?p)    
      (not (holding ?client ?obj))  
      (not (positionfree ?p)) 
      (clear ?obj)    
      (not (stacked ?obj))
                  )
     
    :effect  (   and
                 (holding ?client ?obj)
                 (not(atplace ?obj ?p))
                 (not(clear ?obj))
                 (positionfree ?p)
                 (not (vgempty ?client))
             )
    )


     (:action placeHl 
    :parameters (?obj - element ?p -  location   ?client - robot )
    
    :precondition (and
      (not (vgempty ?client))
      (holding ?client ?obj) 
      (not(clear ?obj)) 
      (positionfree ?p)     
                  )
     
    :effect (and
      (atplace ?obj ?p)        
      (not (holding ?client ?obj))
      (clear ?obj)
      (not(positionfree ?p))  
      (vgempty ?client)
     )        
  )
     


      
 ;maybe I can create one without numbers and just with forall (beam) when beam belong to certain layer and certain module, check if all have certain predicate 
     ;robot stacks one object on top of the other object
     (:action stackHL ; for placing object 1 on object 2 based on their capacity
    :parameters (?obj1 - element ?obj2 - element  ?client - robot ?pr - positiononrail ?lay - layer ?mod - module)
    
    :precondition (and 
      (not (vgempty ?client))  
      (holding ?client ?obj1) 
      (atplace ?obj2 ?pr)    
      (>= (freecapacity ?obj2) 0)
                  )
     
    :effect (and            
      (decrease (freecapacity ?obj2) 1)
      (ontop ?obj1 ?obj2)   
      (stacked ?obj1)    
      (not (holding ?client ?obj1))     
      (atplace ?obj1 ?pr)           
      (not (clear ?obj2))  
      (clear ?obj1)
      (vgempty ?client)
      (when (<= (freecapacity ?obj2 ) 1) 
      (allset ?lay ?mod))
            )
    
    )
     ;robot stacks one biggers object on top of multiple element
    (:action stackonmultipleHL
    :parameters (?plate - plate ?client - robot ?p -positiononrail  ?mod - module ?lay -layer)
    :precondition (and
        (allset ?lay ?mod)
        (holding ?client ?plate)
        (not (atplace ?plate ?p))
        (not (vgempty ?client)) 
              
                )
    
    :effect (and
     
        (forall (?beam - beam) 
        (when (and (belongstolayer ?beam ?lay) (belongstomodule ?beam ?mod))
          (and (ontop ?plate ?beam)(not(clear ?beam)))        
          )                          
        ) 
        (atplace ?plate ?p) 
        (vgempty ?client) 
        (clear ?plate)
                           
        )
 
    )


;robot pours glue on top of an object
; need to fix it for multiple guing on the low plate! we can introduce glue locations
     (:action gluingPLateHL
        :parameters (?obj - plate ?p - positiononrail ?client - robot)
        :precondition (and 
        (vgempty ?client)
        (atplace ?obj ?p) 
        (clear ?obj)      
        (not (glued ?obj))  
        )

        :effect  
        (glued ?obj)
               
    )
       (:action gluingBeamHL
        :parameters (?obj - beam ?p - positiononrail ?client - robot ?mod - module ?lay -layer )
        :precondition (and 
        (vgempty ?client)
        (atplace ?obj ?p) 
        (clear ?obj)      
        (not (glued ?obj))  
        (allset ?lay ?mod)
        )

        :effect  
        (glued ?obj)
               
    )
    
;robot nail an object
    ;works fine for now
      (:action nailingHL
<<<<<<< HEAD
        :parameters (?obj - plate ?p - positiononrail ?client - robot )
=======
        :parameters (?obj - element ?p - positiononrail ?client - robot )
>>>>>>> 59d884f (Update action node colors to pink, flow nodes to blue, rename sidebar title to APTree)
        :precondition (and 
        (vgempty ?client)
        (atplace ?obj ?p)
        (clear ?obj)
        (not (nailed ?obj))
        )

        :effect  
        (nailed ?obj)              
    )
<<<<<<< HEAD
      
          (:action nailingBeamHL
        :parameters (?obj - beam ?p - positiononrail ?client - robot ?mod - module ?lay - layer)
        :precondition (and 
        (vgempty ?client)
        (atplace ?obj ?p)
        (clear ?obj)
        (not (nailed ?obj))
        (glued ?obj)
        (allset ?lay ?mod)
        )

        :effect  
        (nailed ?obj)              
    )
=======
>>>>>>> 59d884f (Update action node colors to pink, flow nodes to blue, rename sidebar title to APTree)
)
     
  
   
     
  
  
  

  