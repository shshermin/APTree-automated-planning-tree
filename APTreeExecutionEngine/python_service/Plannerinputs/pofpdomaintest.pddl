(define (domain fit)
(:requirements
  :typing
  :durative-actions
  :negative-preconditions
  :equality
)

  (:types
    equipposition firstposition positiononrail stackposition - location
    vacgripper nailgripper gluegun - tool
    plate beam - element
    stack - layer
    cassette - module
    robot - agent
  )

  (:predicates
    (atagent ?client - robot ?pp - location)        ; robot is at position pp
    (atplace ?obj - element ?p - location)          ; object is at position p
    (attool ?tool - tool ?ep - equipposition)       ; tool is at an equip position
    (hastool ?client - robot ?tool - tool)          ; robot is equipped with a tool
    (robotequipped ?client - robot)                 ; robot is equipped with some tool
    (activetool ?tool - tool)                       ; tool is active
    (holding ?client - robot ?obj - element)        ; robot is holding an object
    (clear ?obj - element)                          ; object is clear (nothing on top)
    (ontop ?obj1 - element ?obj2 - element)         ; obj1 is on top of obj2
    (vgempty ?client - robot)                       ; vacuum gripper is empty
    (glued ?obj - element)
    (nailed ?obj - element)
    (positionfree ?pos - location)
    (allset ?lay - layer ?mod - module)
    (belongstolayer ?obj - element ?lay - layer)
    (belongstomodule ?obj - element ?mod - module)
    (stacked ?obj - element)
    (cassetteAtStack ?mod - module ?sp - stackposition)
  )

  ;; robot travels from one location to another
  (:durative-action travelML
    :parameters (?client - robot ?from - location ?to - location)
    :duration (= ?duration 5)
    :condition (and
      (at start (atagent ?client ?from))
      (at start (not (= ?from ?to)))
    )
    :effect (and
      (at start (not (atagent ?client ?from)))
      (at end   (atagent ?client ?to))
    )
  )

  ;; robot equips an end-effector
  (:durative-action equipeML
    :parameters (?client - robot ?too - tool ?ep - equipposition)
    :duration (= ?duration 2)
    :condition (and
      (at start (attool ?too ?ep))
      (at start (not (robotequipped ?client)))
      (at start (not (positionfree ?ep)))
      (over all (atagent ?client ?ep))
    )
    :effect (and
      (at start (not (attool ?too ?ep)))
      (at end   (hastool ?client ?too))
      (at end   (robotequipped ?client))
      (at end   (positionfree ?ep))
    )
  )

  ;; robot puts the end-effector down
  (:durative-action deequipML
    :parameters (?client - robot ?too - tool ?ep - equipposition)
    :duration (= ?duration 2)
    :condition (and
      (at start (hastool ?client ?too))
      (at start (not (activetool ?too)))
      (at start (not (attool ?too ?ep)))
      (at start (robotequipped ?client))
      (at start (positionfree ?ep))
      (over all (atagent ?client ?ep))
    )
    :effect (and
      (at start (not (hastool ?client ?too)))
      (at start (not (robotequipped ?client)))
      (at start (not (positionfree ?ep)))
      (at end   (attool ?too ?ep))
    )
  )

  ;; turns on the tool (end-effector)
  (:durative-action initializeML
    :parameters (?client - robot ?too - tool)
    :duration (= ?duration 1)
    :condition (and
      (at start (robotequipped ?client))
      (at start (hastool ?client ?too))
      (at start (not (activetool ?too)))
    )
    :effect (and
      (at end (activetool ?too))
    )
  )

  ;; turns off the tool
  (:durative-action closetoolML
    :parameters (?client - robot ?too - tool)
    :duration (= ?duration 1)
    :condition (and
      (at start (activetool ?too))
      (at start (vgempty ?client))
    )
    :effect (and
      (at end (not (activetool ?too)))
    )
  )

  ;; robot grabs an object from a location
  (:durative-action pickUpML
    :parameters (?obj - element ?p - location ?client - robot ?vg - vacgripper)
    :duration (= ?duration 3)
    :condition (and
      (at start (atplace ?obj ?p))
      (at start (vgempty ?client))
      (at start (not (holding ?client ?obj)))
      (at start (not (positionfree ?p)))
      (at start (clear ?obj))
      (at start (not (glued ?obj)))
      (at start (not (nailed ?obj)))
      (over all (hastool ?client ?vg))
      (over all (activetool ?vg))
      (over all (atagent ?client ?p))
    )
    :effect (and
      (at start (not (atplace ?obj ?p)))
      (at start (not (vgempty ?client)))
      (at start (not (clear ?obj)))
      (at start (positionfree ?p))
      (at end   (holding ?client ?obj))
    )
  )

  ;; robot places an object at a location
  (:durative-action placeML
    :parameters (?obj - element ?p - location ?client - robot ?vg - vacgripper)
    :duration (= ?duration 3)
    :condition (and
      (at start (not (vgempty ?client)))
      (at start (holding ?client ?obj))
      (at start (not (clear ?obj)))
      (at start (positionfree ?p))
      (over all (atagent ?client ?p))
      (over all (activetool ?vg))
    )
    :effect (and
      (at start (not (holding ?client ?obj)))
      (at start (vgempty ?client))
      (at start (not (positionfree ?p)))
      (at end   (atplace ?obj ?p))
      (at end   (clear ?obj))
    )
  )

  ;; place obj1 on top of obj2 on a rail position
  (:durative-action stackML
    :parameters (?obj1 - element ?obj2 - element ?client - robot ?vg - vacgripper ?pr - positiononrail)
    :duration (= ?duration 4)
    :condition (and
      (at start (not (vgempty ?client)))
      (at start (holding ?client ?obj1))
      (at start (atplace ?obj2 ?pr))
      (at start (not (atplace ?obj1 ?pr)))
      (at start (not (positionfree ?pr)))
      (over all (atagent ?client ?pr))
      (over all (activetool ?vg))
    )
    :effect (and
      (at start (not (holding ?client ?obj1)))
      (at start (vgempty ?client))
      (at start (not (clear ?obj2)))
      (at end   (ontop ?obj1 ?obj2))
      (at end   (atplace ?obj1 ?pr))
      (at end   (clear ?obj1))
      (at end   (stacked ?obj1))
    )
  )

  (:durative-action gluingML
    :parameters (?obj - element ?p - positiononrail ?client - robot ?gg - gluegun)
    :duration (= ?duration 8)
    :condition (and
      (at start (not (glued ?obj)))
      (over all (atagent ?client ?p))
      (over all (atplace ?obj ?p))
      (over all (clear ?obj))
      (over all (activetool ?gg))
    )
    :effect (and
      (at end (glued ?obj))
    )
  )

  (:durative-action nailingML
    :parameters (?obj - element ?p - positiononrail ?client - robot ?ng - nailgripper)
    :duration (= ?duration 8)
    :condition (and
      (at start (not (nailed ?obj)))
      (over all (atagent ?client ?p))
      (over all (atplace ?obj ?p))
      (over all (clear ?obj))
      (over all (activetool ?ng))
    )
    :effect (and
      (at end (nailed ?obj))
    )
  )

  ;; robot picks up an assembled cassette by grabbing its lower plate
  (:durative-action pickUpCassetteML
    :parameters (?lp - plate ?mod - cassette ?lay - stack
                 ?p - location ?client - robot ?vg - vacgripper)
    :duration (= ?duration 3)
    :condition (and
      (at start (vgempty ?client))
      (at start (atplace ?lp ?p))
      (at start (not (positionfree ?p)))
      (at start (not (holding ?client ?lp)))
      (at start (belongstomodule ?lp ?mod))
      (at start (allset ?lay ?mod))
      (over all (hastool ?client ?vg))
      (over all (activetool ?vg))
      (over all (atagent ?client ?p))
    )
    :effect (and
      (at start (not (atplace ?lp ?p)))
      (at start (not (vgempty ?client)))
      (at start (positionfree ?p))
      (at end   (holding ?client ?lp))
    )
  )

  ;; robot places an assembled cassette at a stack position
  (:durative-action placeCassetteML
    :parameters (?lp - plate ?mod - cassette
                 ?sp - stackposition ?client - robot ?vg - vacgripper)
    :duration (= ?duration 3)
    :condition (and
      (at start (holding ?client ?lp))
      (at start (positionfree ?sp))
      (at start (belongstomodule ?lp ?mod))
      (over all (hastool ?client ?vg))
      (over all (activetool ?vg))
      (over all (atagent ?client ?sp))
    )
    :effect (and
      (at start (not (holding ?client ?lp)))
      (at start (vgempty ?client))
      (at start (not (positionfree ?sp)))
      (at end   (atplace ?lp ?sp))
      (at end   (cassetteAtStack ?mod ?sp))
    )
  )
)