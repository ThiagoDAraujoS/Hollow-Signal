# MEMORY MODEL
## Blackboard (The Virtual DB): 
A completely passive, in-memory repository. 
It has no game logic and doesn't care why data is moving—it simply holds 
the active runtime state in RAM as the absolute master copy of the game session.

## SaveSystem (The Disk Worker): 
The orchestrator of directory operations. 
It is responsible for handling async save/load requests, organizing atomic directory swaps, creating auto-saves, 
and coining directory metadata for the load UI.

## BlackboardClient (The Mediator): 
The crucial bridge between concrete runtime components and the virtual Blackboard. 
It uses RAII principles to bind game entities to Blackboard memory ranges on initialization, 
and desynced/flushes that memory on destruction.

## TrackedBehaviour (The Lifecycle Wrapper): 
An abstract MonoBehaviour that automates the boilerplate of the Mediator. 
It links a component's active scene existence to the Blackboard, pulling data when it enters and disconnecting when it unloads.


## Tracked<T> (The Decorator): 
An elegant variable-level decorator. 
It wraps individual properties (like Health, Gold, or Coordinates) 
and transparently pushes all real-time mutations directly into the Blackboard, 
aligning perfectly with the GameObject's runtime execution.