
(define (domain trusshl) 
(:requirements 
  :adl
  :typing
)
  (:types 
    
    firstposition finalposition equipposition - location      

    rppickup rpmanipulate rptoolchange - robotposition                                                              
                                                       
    table stick cube - element
   
    stack -layer

    demo - module 

    robot - agent    

    gripper staplergun - tool                               
  )
 
  (:predicates
   
    (atagent ?client - robot ?pp - robotposition) 
    (hastool ?client - robot ?tool - tool)
    (attool ?tool - tool ?ep - equipposition)
    (atplace ?obj - element ?p - location) ; object is at position p                                                                                              
    (holding ?client - robot ?obj - element)      ; a robot is holding an object
    (clear ?obj - element)   ;an object is clear
    (gripperempty ?client - robot) ; gripper is empty (not holding any object)
    (glued ?obj - element) ; an object is glued
    (nailed ?obj1 - element ?obj2 - element)
    (fixed ?obj - element)   ;an object is fixed
    (positionfree ?pos - location)
   ; (allset ?lay - layer ?mod - module) 
    (belongstolayer ?obj - element ?lay - layer)
    ;(belongstomodule ?obj - element ?mod - module) 
    (stacked ?obj1 - element ?obj2 - element) 
    (atfinalposition ?obj - element)
    (objectfinalposition ?obj - element ?pos - finalposition)
    (accessible ?obj - element)  ;an object can be nailed (nothing stacked on top)
)

    ;robot grabs an object from the table
    (:action pickUpHL
    :parameters (?obj - element ?p - location  ?client - robot ?g - gripper)
    
    :precondition (and    
      (gripperempty ?client)
      (hastool ?client ?g)                            
      (atplace ?obj ?p)    
      (not (holding ?client ?obj))  
      (not (positionfree ?p)) 
      (not (atfinalposition ?obj))
      (clear ?obj)    
      (not (fixed ?obj))
                  )
     
    :effect  (   and
                 (holding ?client ?obj)
                 (not(atplace ?obj ?p))
                 (not(gripperempty ?client))
                 (not(clear ?obj))
                 (positionfree ?p)
             )
    )

     ;robot stacks one element on another element
     (:action stackHL
    :parameters (?stackingobject - element ?existingobject - element  ?client - robot ?objposition - location ?g - gripper)
    
    :precondition (and 
      (not (gripperempty ?client))  
      (holding ?client ?stackingobject)
      (hastool ?client ?g) 
      (atfinalposition ?existingobject)
      (not (atplace ?stackingobject ?objposition))
      (objectfinalposition ?stackingobject ?objposition)
                  )
     
    :effect (and            
      (not (holding ?client ?stackingobject))     
      (atfinalposition ?stackingobject)
      (atplace ?stackingobject ?objposition)  
      (gripperempty ?client)  
      (clear ?stackingobject)
      (not (accessible ?existingobject))
      (accessible ?stackingobject)
      (stacked ?stackingobject ?existingobject)
            )
    
    )

     ;robot stacks one element on top of two elements
    (:action stackOnTwoHL
    :parameters (?stackingobj - stick ?client - robot ?objposition - location ?existingobj1 - element ?existingobj2 - element ?layer1 - layer ?layer2 - layer ?g - gripper)
    :precondition (and
        (not (gripperempty ?client))  
        (holding ?client ?stackingobj)
        (hastool ?client ?g) 
        (atfinalposition ?existingobj2)  
        (atfinalposition ?existingobj1)  
        (clear ?existingobj1)
        (clear ?existingobj2)
        (belongstolayer ?stackingobj ?layer2)
        (belongstolayer ?existingobj1 ?layer1)
        (belongstolayer ?existingobj2 ?layer1)
        (not (= ?layer1 ?layer2))
        (objectfinalposition ?stackingobj ?objposition)
                )
    
    :effect (and
        (not (holding ?client ?stackingobj))
        (atplace ?stackingobj ?objposition)
        (gripperempty ?client)
        (clear ?stackingobj)
        (accessible ?stackingobj)
        (stacked ?stackingobj ?existingobj1)
        (stacked ?stackingobj ?existingobj2)
        (atfinalposition ?stackingobj)
        (not (accessible ?existingobj1))
        (not (accessible ?existingobj2))
        )
    )

    ;robot nails one element to another
      (:action nailingHL
        :parameters (?obj1 - element ?obj2 - element  ?client - robot ?s - staplergun)
        :precondition (and 
        (gripperempty ?client)
        (hastool ?client ?s)
        (atfinalposition ?obj2)
        (atfinalposition ?obj1)
        (accessible ?obj1)
        (not (nailed ?obj1 ?obj2))
        )

        :effect  (and
        (nailed ?obj1 ?obj2)   
        (fixed ?obj1)
        )           
    )

    ;robot changes its end effector
    (:action changeEndeffectorHL
        :parameters (?client - robot ?oldtool - tool ?newtool - tool)
        :precondition (and
            (hastool ?client ?oldtool)
            (gripperempty ?client)
        )
        :effect (and
            (not (hastool ?client ?oldtool))
            (hastool ?client ?newtool)
        )
    )
      
    
)
