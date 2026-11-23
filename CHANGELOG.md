# Changelog

All notable changes to **VikingHusbandry** will be documented in this file.

## [0.2.4] - 2026-11-23

### Changed
- **Server-Authoritative Configs**: Marked balance and gameplay settings as server-authoritative (`AdminOnly`) for dedicated server parity.
- **Trough Interaction Key**: Updated feeding trough toggle documentation to `Alt + E`.

### Fixed
- **Rodeo Dismount Cleanup**: Properly cleans up active rodeo grapple state if either the rider or target creature dies or despawns mid-rodeo.

## [0.2.3] - 2026-11-03

### Changed
- **Downed Animal Animation**: Replaced duplicate ragdoll instantiation with native sleeping pose animation, preserving creature renderers and preventing premature loot drops.
- **Trough Fattening Integration**: Enhanced livestock feeding troughs with improved food visualizers and carcass yield scaling upon maturity.

## [0.2.2] - 2026-10-29

### Added
- **Wolf Ground Tethering**: Wolves now use the ground tether leash subdual mechanic instead of ride mounting, matching boars.
- **Offspring Stance Inheritance**: Newborn animals and maturing livestock inherit their parent's companion stance and pen boundaries.
- **Livestock Fattening**: Tamed animals gain fattening bonuses from automated trough feeding, increasing slaughter loot yield and trophy drop chances with diminishing returns.
- **50/50 Partner Star Inheritance**: Offspring now have a 50% chance to inherit the star tier of the nearby breeding partner instead of exclusively copying the mother.

### Changed
- **Trough Toggle Keybind**: Replaced `Shift+E` with `Alt+E` (or Gamepad `LB+E`) to prevent sprinting up to chests from converting them.
- **Trough Straw Visuals**: Swapped coarse roofing tiles with high-density straw bedding textures and eliminated artifacting geometry on the wooden frame.
- **Livestock Default Stance**: Tamed animals default to `StayGuard` upon taming to maintain stable pen boundaries rather than following.

### Fixed
- **AI Locomotion Heap Allocation**: Replaced reflective invocations with zero-allocation delegates in AIHelper, eliminating per-frame memory churn during rodeo subdual and herding.

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
