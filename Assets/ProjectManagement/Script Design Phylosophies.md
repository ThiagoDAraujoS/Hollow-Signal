CRPG Script Design Philosophies
Development Guidelines. Fail-Fast & Assertive Execution Avoid wrapping code in preventive null checks, generic try-catch blocks, or defensive fallbacks unless null is a valid, expected state for that specific pathway. Crash Loud & Crash Early: If an asset is missing, a reference is unbound, or a type mismatch occurs, let the engine throw its native exceptions.
Keep it simple: We're at the very start of a complex project, scripts have to be simple so we can keep testing fast
Pure Explicit Casts Cast raw objects directly to their expected concrete types, unless strictly required by performance constraints.
Code Cleanliness & Structure Do not partition code using #region and #endregion tags. Keep files compact, highly cohesive, and self-documenting. Do not use <summary>, <param>, or <returns> tags.
All documentation comments must use the triple-slash (///) prefix.
/// Moves the character directly to the destination coordinates.
/// Will throw a NullReferenceException instantly if the NavMeshAgent is missing.
public void MoveTo(Vector3 destination) => agent.destination = destination;