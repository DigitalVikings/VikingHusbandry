# Changelog

All notable changes to **VikingHusbandry** will be documented in this file.

## [0.2.2] - 2026-10-29

### Added
- **Offspring Stance Inheritance**: Newborn animals and maturing livestock automatically inherit their parent's companion stance and pen boundaries.
- **3D Feed Trough Alpha Tufts**: Rendered dynamic straw fringe and tuft alpha shaders for the procedural Norse feed trough model.

### Changed
- **Livestock Default Stance**: Tamed animals default to `StayGuard` upon taming to maintain stable pen boundaries rather than following.

## [0.2.1] - 2026-10-27

### Added
- **Authentic Downed Ragdoll Collapse**: Downed animals naturally collapse onto the terrain with physics ragdoll settling while remaining feedable and interactive.
- **Tether Leash Subdual**: Leashing wild boars triggers a bidirectional tug-of-war taut cord with active player dragging and resistance.

### Fixed
- **Ragdoll Interaction Alignment**: Fixed visual and physics desync between downed animal ragdolls and interaction colliders.
- **Line Renderer North Offset**: Fixed phantom secondary tether line pointing north by strictly truncating unused buffer vertices.
- **Subdual Recovery State**: Fixed subdual progress persisting at 100% when a downed animal recovers untamed.
- **Creature AI Loop Guard**: Prevented null reference exceptions when updating uninitialized or destroyed monster instances.

## [0.2.0] - 2026-10-25

### Added
- **Procedural 3D Feeding Trough**: Toggling standard wooden chests (`Shift + E`) displays a procedural Norse feed trough model with real-time straw and feed mound fill tracking.
- **Dynamic Rodeo Struggle Dynamics**: Creature subdual mounting now features violent bucking impulse decay and procedural pitch, roll, and yaw struggle weaving.

## [0.1.0] - 2026-10-25

### Added
- **Active Rodeo Subdual Taming**: Mount staggered or unalerted wild beasts (`[E]`) and withstand procedural bucking to subdue and tame them directly.
- **Procedural Feeding Troughs**: Convert standard wooden chests into livestock feeding troughs (`Shift + E`) with custom 3D trough visuals and feed-level tracking.
- **Tactical 4-State Companion Commands**: Cycle companions through four distinct behavior modes: Follow (Aggressive), Follow (Heel), Stay (Guard Area), and Stay (Pen).
- **Anti-Chute Paddock Limits**: Breeding limits evaluate density using horizontal ground distance only, making vertical breeding chutes obsolete.
- **Save-Safe Vanilla Reversibility**: Trough conversions and companion states persist cleanly in ZDOs without altering world save schemas.
