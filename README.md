# VikingHusbandry

**VikingHusbandry** transforms Valheim's animal domestication and companion systems with active rodeo subdual, 4-state tactical companion stances, chest-to-trough conversion, and anti-chute paddock density control with 100% save-file reversibility.

![VikingHusbandry Rodeo Subdual & Troughs](screenshot_husbandry_rodeo.png)

> [!WARNING]
> **Work in Progress (Pre-Release v0.1.0)**: This mod is in active development and has not been tested extensively. Features, balance, and mechanics may change, and unintended bugs or edge cases may occur. Backup your character and world saves before testing.

---

## How It Works

Animal handling is elevated from passive pit-crouching to responsive, skill-based interactions:

- **Active Rodeo Subdual**: Parrying a wild creature or sneaking undetected creates a grapple opening. Press **`[E]`** on the staggered or unalerted beast to mount its back. Weather the thrashing struggle (10 stamina/sec) to fill the subdual bar. At 100%, the creature collapses into a 20-second **Downed State**—press an active hotbar key (1–8) with favored food to feed it directly and achieve **100% Tamed** in a single encounter.
- **Tactical 4-State Companion Stances**: Interacting with any tamed companion (including commanded Boars) cycles through 4 behavioral stances: *Follow (Aggressive)*, *Follow (Passive / Heel)*, *Stay (Guard Area)*, and *Stay (Passive / Pen)*.
- **Chest-to-Trough Conversion**: Press **`Shift + E`** on any standard wooden chest to toggle it into a livestock **Feeding Trough**. Livestock within 20m path directly to the trough to feed, preventing food despawns and ground clipping without introducing custom prefabs.
- **Anti-Chute Paddock Limits**: Breeding limits evaluate density using horizontal ground distance only, rendering vertical breeding chutes obsolete.

<details>
<summary><b>Click to expand Tactical Companion Stances</b></summary>

Cycle through stances by aiming at a tamed companion and pressing `[E]`:

| Stance | Behavior | Tactical Use |
| :--- | :--- | :--- |
| **Follow (Aggressive)** | Standard vanilla follow; pursues and attacks all hostiles on sight. | Wilderness clearing and open-field combat. |
| **Follow (Passive / Heel)** | Stays within 4m of player; ignores unprovoked hostiles; only retaliates if directly struck. | Safe transport through dangerous biomes without breaking formation. |
| **Stay (Guard Area)** | Anchors to issued position; attacks invaders within 15m; strictly leashes back once threats flee or die. | Defending open base gates and farm perimeters. |
| **Stay (Passive / Pen)** | Strictly anchored; completely ignores wild enemies outside fence walls. | Keeps domestic livestock calm and prevents fence battering. |

</details>

<details>
<summary><b>Click to expand Rodeo Subdual & Favored Diet Reference</b></summary>

| Creature | Grapple Condition | Favored Downed Food (Hotbar Keys 1–8) |
| :--- | :--- | :--- |
| **Boar** | Stagger from timed parry, or unalerted sneak approach | Carrots, Turnips, Onions, Berries, Mushrooms |
| **Wolf** | Stagger from timed parry, or unalerted sneak approach | Raw Meat, Sausages, Deer Meat, Boar Meat |
| **Lox** | Stagger from timed parry, or unalerted sneak approach | Cloudberries, Barley, Flax |
| **Asksvin** | Stagger from timed parry, or unalerted sneak approach | Fiddlehead, Smoke Puff, Vineberry |

*Note: If you run out of stamina and are bucked off during the rodeo struggle, you take minor blunt damage, but the creature **retains all accumulated subdual progress** so you can recover stamina and finish the trial.*

</details>

<details>
<summary><b>Click to expand Feeding Troughs & Paddock Density</b></summary>

- **100% Save-Safe Troughs**: Troughs utilize native `piece_chest_wood`. If the mod is removed, troughs revert back into standard wooden chests with all inventory contents preserved.
- **20m Scent Pathing**: Hungry livestock locate food stored inside troughs or on the ground through fences, consuming 1 item directly from the container.
- **Horizontal Cylinder Capping**: Procreation evaluates nearby creature counts horizontally across a 20m cylinder, ignoring vertical elevation differences ($\Delta y$). Offspring dropped down vertical chutes still count against the paddock limit, preventing infinite breeding server lag.

</details>
