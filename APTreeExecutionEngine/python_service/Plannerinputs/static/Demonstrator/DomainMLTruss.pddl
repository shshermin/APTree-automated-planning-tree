
(define (domain trussml) 
(:requirements 
  :adl
  :typing
)

  (:types 
    
    firstposition finalposition equipposition - location    

    rppickup rpmanipulate rptoolchange - robotposition                                                            
                                                       
    table stick cube - element
   
    stack -layer

    ;demo - module 

    robot - agent    

    gripper staplergun - tool                    
  )
 
  (:predicates
    (atagent ?client - robot ?pp - robotposition)   ;robot is at position pp
    (attool ?tool - tool ?ep - equipposition)  ;tool is at an equip position
    (hastool ?client - robot ?tool - tool)     ;a robot is equipped with a tool              
    (robotequipped ?client - robot)      ;a robot is not wquipped with a tool                    
    (activetool ?tool - tool)          ; a tool is active            
    (holding ?client - robot ?obj - element)      ; a robot is holding an object
    (clear ?obj - element)   ;an object is clear
    (gripperempty ?client - robot); vaccum gripper is empty (not holding any object)
    (nailed ?obj1 - element ?obj2 - element)
    (fixed ?obj - element)   ;an object is fixed
    (positionfree ?pos - location)
    (belongstolayer ?obj - element ?lay - layer)
    ;(belongstomodule ?obj - element ?mod - module) 
    (stacked ?obj1 - element ?obj2 - element) 
    (atfinalposition ?obj - element)
    (objectfinalposition ?obj - element ?pos - finalposition)
    (atplace ?obj - element ?p - location) ; object is at position p
    (accessible ?obj - element)  ;an object can be nailed (nothing stacked on top)
    
      
)

 ;robot travels from one location to another
    (:action travelML
    :parameters (?client - robot ?from  - robotposition  ?to - robotposition)
    
    :precondition (and
      (atagent ?client ?from)                        
      (not (= ?from ?to))                          
    )
    
    :effect (and
    (not (atagent ?client ?from))                          
    (atagent ?client ?to)                          
        )
    
    )

  ;robot equips the endeffector
    (:action equipeML
    :parameters (?client - robot ?too - tool ?rp - rptoolchange ?ep - equipposition)
    
    :precondition (and
      (attool ?too ?ep)
      (not(robotequipped ?client))
      (atagent ?client ?rp)
      (not(positionfree ?ep))                        
    )   
    :effect (and
    
      (hastool ?client ?too)
      (robotequipped ?client)
      (not (attool ?too ?ep))
      (positionfree ?ep)    
    )
    )
    ;robot puts the endeffector down
      (:action deequipML
      :parameters (?client - robot ?too - tool ?ep - equipposition ?rp - rptoolchange) 
      :precondition (and
      (atagent ?client ?rp)
      (hastool ?client ?too)
      (not (activetool ?too))
      (not (attool ?too ?ep))
      (robotequipped ?client)
      (positionfree ?ep)
      
                          )
      
      :effect  (and 
      (attool ?too ?ep) 
      (not(robotequipped ?client))    
      (not (hastool ?client ?too))
      (not (positionfree ?ep))
      
      )
      )       
  
  
    ;turns on the tool (end-effector)
    (:action initializeML
    :parameters (?client - robot ?too - tool)
  
    :precondition (and
      
      (robotequipped ?client) 
      (hastool ?client ?too)
      (not (activetool ?too)) 
    )
    :effect (and
      (activetool ?too)  
    )  
            
    )
      ;turns of the tool
      (:action closetoolML
      :parameters (?client - robot ?too - tool)
      :precondition (and 
      (activetool ?too)
      (hastool ?client ?too)
      (gripperempty ?client) 
      (robotequipped ?client) 
      
      )
      :effect  (and
      (not (activetool ?too))    )
    )
    ;robot grabs an object from the table
    (:action pickUpML
    :parameters (?obj - element ?p - location ?client - robot ?vg - gripper ?rp - rppickup)
    
    :precondition (and    
      (hastool ?client ?vg)            
      ;(activetool ?vg)            
      (atplace ?obj ?p)
      (atagent ?client ?rp)    
      (gripperempty ?client)           
      (not (positionfree ?p)) 
      (clear ?obj)    
      (not(fixed ?obj))
                        )
      
    :effect   (and
              (holding ?client ?obj)
              (not(atplace ?obj ?p))
              (not(gripperempty ?client))
              (not(clear ?obj))
              (positionfree ?p)
            )
    )
      

      (:action stackML ; for stacking one object on another object 
    :parameters (?stackingobject - element ?existingobject - element ?client - robot ?gripper - gripper ?objposition - finalposition ?robotposition - rpmanipulate)
    
    :precondition (and
      (not(gripperempty ?client))
      (atagent ?client ?robotposition)
      (hastool ?client ?gripper)
      ;(activetool ?gripper)
      (holding ?client ?stackingobject)

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
      
      (:action stackOnTwoML ; for stacking one object on two objects
    :parameters (?stackingobj - stick ?client - robot ?objposition - finalposition ?robotpos - rpmanipulate  ?existingobj1 - element ?existingobj2 - element ?vg - gripper ?layer1 - layer ?layer2 - layer)
    :precondition (and 
      (not (gripperempty ?client))  
      (holding ?client ?stackingobj) 
      (atfinalposition ?existingobj2)  
      (atfinalposition ?existingobj1)  
      ;(activetool ?vg) 
       (hastool ?client ?vg)
      (atagent ?client ?robotpos)
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

    
    

      (:action nailingML
        :parameters (?obj1 - element ?obj2 - element  ?client - robot ?ng - staplergun ?rp - rpmanipulate)
        :precondition (and 
        (atagent ?client ?rp)
        (atfinalposition ?obj2)
        (atfinalposition ?obj1)
        (accessible ?obj1)
        (activetool ?ng)
        (hastool ?client ?ng)
        (not (nailed ?obj1 ?obj2))
        )

        :effect   (and
        (nailed ?obj1 ?obj2)   
        (fixed ?obj1)
        
        )   
    )
      
    
)