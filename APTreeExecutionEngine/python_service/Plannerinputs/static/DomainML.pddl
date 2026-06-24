
(define (domain fit) 
(:requirements 
  :adl
  :typing
)

  (:types 
    
    equipposition firstposition positiononrail stackposition - location     
                                         
    vacgripper nailgripper gluegun - tool 
                                       
    plate beam - element

    stack -layer

    cassette - module 
    
    robot - agent                  
  )
 
  (:predicates
    (atagent ?client - robot ?pp - location)   ;robot is at position pp
    (atplace ?obj - element ?p - location) ; object is at position p
    (attool ?tool - tool ?ep - equipposition)  ;tool is at an equip position
    (hastool ?client - robot ?tool - tool)     ;a robot is equipped with a tool              
    (robotequipped ?client - robot)      ;a robot is not wquipped with a tool                    
    (activetool ?tool - tool)          ; a tool is active            
    (holding ?client - robot ?obj - element)      ; a robot is holding an object
    (clear ?obj - element)   ;an object is clear
    (ontop ?obj1 - element ?obj2 - element); object one is on top of object 2
    (vgempty ?client - robot); vaccum gripper is empty (not holding any object)
    (glued ?obj - element); an object is glued
    (nailed ?obj - element)   ;an object is nailed
    (positionfree ?pos - location)
    (allset ?lay - layer ?mod - module) 
    (belongstolayer ?obj - element ?lay - layer)
    (belongstomodule ?obj - element ?mod - module) 
    (stacked ?obj - element) 
    (cassetteAtStack ?mod - module ?sp - stackposition)
      
)

 ;robot travels from one location to another
    (:action travelML
    :parameters (?client - robot ?from  - location   ?to - location)
    
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
    :parameters (?client - robot ?too - tool ?ep - equipposition)
    
    :precondition (and
      (attool ?too ?ep)                          
      (not(robotequipped ?client))                   
      (atagent ?client ?ep)   
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
      :parameters (?client - robot ?too - tool ?ep - equipposition) 
      :precondition (and
      (atagent ?client ?ep)
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
    :effect 
    
      (activetool ?too)    
            
    )
      ;turns of the tool
      (:action closetoolML
      :parameters (?client - robot ?too - tool)
      :precondition (and 
      (activetool ?too)
      (vgempty ?client) 
      
      )
      :effect   
      (not (activetool ?too))      
  
    )
    ;robot grabs an object from the table
    (:action pickUpML
    :parameters (?obj - element ?p - location   ?client - robot ?vg - vacgripper)
    
    :precondition (and    
      (hastool ?client ?vg)            
      (activetool ?vg)            
      (atplace ?obj ?p)
      (atagent ?client ?p)    
      (vgempty ?client)          
      (not (holding ?client ?obj))  
      (not (positionfree ?p)) 
      (clear ?obj)    
      (not(glued ?obj))
      (not (nailed ?obj))
                        )
      
    :effect   (   and
              (holding ?client ?obj)
              (not(atplace ?obj ?p))
              (not(vgempty ?client))
              (not(clear ?obj))
              (positionfree ?p)
            )
    )
    ;robot places an object on the table
      (:action placeML
    :parameters (?obj - element ?p -  location   ?client - robot ?vg - vacgripper)
    
    :precondition (and
      (not(vgempty ?client))
      (holding ?client ?obj)  
      (atagent ?client ?p)    
      (activetool ?vg)  
      (not(clear ?obj)) 
      (positionfree ?p)    
                        )
      
    :effect (and
      (atplace ?obj ?p)      
      (not (holding ?client ?obj))
      (vgempty ?client) 
      (clear ?obj)
      (not(positionfree ?p))
            )      
  )
      

      (:action stackML ; for placing object 1 on object 2 based on their capacity
    :parameters (?obj1 - element ?obj2 - element ?client - robot ?vg - vacgripper ?pr - positiononrail)
    
    :precondition (and
      (not(vgempty ?client))
      (holding ?client ?obj1)  
      (atagent ?client ?pr)    
      (activetool ?vg)  
      (atplace ?obj2 ?pr)    
      (not (atplace ?obj1 ?pr)) 
      (not(positionfree ?pr)) 
                        )
      
    :effect (and  
              
      (ontop ?obj1 ?obj2)      
      (not (holding ?client ?obj1))    
      (atplace ?obj1 ?pr)  
      (vgempty ?client)  
      (not (clear ?obj2))  
      (clear ?obj1)
      (stacked ?obj1)
            )
    
    )

    



      (:action gluingML
        :parameters (?obj - element ?p - positiononrail ?client - robot ?gg - gluegun)
        :precondition (and 
    
        (atagent ?client ?p)
        (atplace ?obj ?p) 
        (clear ?obj)      
        (activetool ?gg)
        (not (glued ?obj))
        )

        :effect   
        (glued ?obj)
              
    )
    

      (:action nailingML
        :parameters (?obj - element ?p - positiononrail ?client - robot ?ng - nailgripper)
        :precondition (and 
        (atagent ?client ?p)
        (atplace ?obj ?p)
        (clear ?obj)
        (activetool ?ng)
        (not (nailed ?obj))
        )

        :effect   
        (nailed ?obj)         
    )

    ;robot picks up an assembled cassette by grabbing its lower plate
    (:action pickUpCassetteML
    :parameters (?lp - plate ?mod - cassette ?lay - stack
                 ?p - location ?client - robot ?vg - vacgripper)

    :precondition (and
        (hastool ?client ?vg)
        (activetool ?vg)
        (vgempty ?client)
        (atagent ?client ?p)
        (atplace ?lp ?p)
        (not (positionfree ?p))
        (belongstomodule ?lp ?mod)
        (allset ?lay ?mod)
        (not (holding ?client ?lp))
    )

    :effect (and
        (holding ?client ?lp)
        (not (atplace ?lp ?p))
        (not (vgempty ?client))
        (positionfree ?p)
    )
    )

    ;robot places an assembled cassette at a stack position
    (:action placeCassetteML
    :parameters (?lp - plate ?mod - cassette
                 ?sp - stackposition ?client - robot ?vg - vacgripper)

    :precondition (and
        (hastool ?client ?vg)
        (activetool ?vg)
        (holding ?client ?lp)
        (atagent ?client ?sp)
        (belongstomodule ?lp ?mod)
        (positionfree ?sp)
    )

    :effect (and
        (atplace ?lp ?sp)
        (not (holding ?client ?lp))
        (vgempty ?client)
        (not (positionfree ?sp))
        (cassetteAtStack ?mod ?sp)
    )
    )
)