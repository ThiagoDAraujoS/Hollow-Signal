# Mastery & Problem Deduction Architecture

## 1. Overview & Core Philosophy

Hollow Signal is a story-driven tactical CRPG where traditional metagaming is discarded in favor of **narrative deduction and character identity**.

In traditional CRPGs, players read sterile UI statistics like `[Lockpick 65% - DC 14]` and optimize raw numbers. In Hollow Signal, **raw skills are a black box**. Players interact with the world through intuitive roleplaying deduction:
1. Players read a colorful description of a physical obstacle.
2. Players choose an approach based on the background and competencies of their characters.
3. Behind the scenes, the game evaluates hidden skill proficiencies granted by the player's **Masteries**.
4. The resolution explicitly reveals **which Mastery aided the character** and delivers a **tailored method quip** detailing how their specific background solved the obstacle.

---

## 2. The Triad: Masteries, Skills, and Problem Archetypes

```
┌─────────────────────────────────┐               ┌─────────────────────────────────┐
│        CHARACTER SHEET          │               │       WORLD OBSTACLE / KNOT     │
│                                 │               │                                 │
│  Selected Masteries             │               │  Physical Obstacle Description  │
│  (e.g., Dockyard Boiler-Hauler) │               │  (e.g., Seized Bulkhead Hatch)  │
│               │                 │               │               │                 │
│               ▼                 │               │               ▼                 │
│    [Hidden Skill Bonuses]       │               │    [Problem Archetypes]         │
│  • FixMachinery: +2             │               │  • Approach A: Brute Force      │
│  • Smash:        +3             │               │    (LiftHeavyObjects, Smash)    │
│  • LiftHeavy:    +3             │               │  • Approach B: Engineering      │
│                                 │               │    (FixMachinery, Tinker)       │
└───────────────┬─────────────────┘               └───────────────┬─────────────────┘
                │                                                 │
                └───────────────────────┬─────────────────────────┘
                                        ▼
                         [THE DEDUCTION & RESOLUTION]
                         1. Player picks Approach A (Brute Force)
                         2. System queries character sheet for highest matching skill
                         3. Best Skill found: Smash (+3 bonus from Dockyard Boiler-Hauler)
                         4. Roll d20 + 3 vs DC 13
                         5. Reveal Mastery & Method Quip
```

### 2.1 The Black Box: Hidden Skills (~100 Skills)
- The game maintains an extensive enumerated skill list (`Data.Skill`) covering fine nuances (e.g., `BreakArcaneCircuits`, `FixMachinery`, `PickLocks`, `Smash`, `ShatterArmor`, `NavigateBureaucracy`, `ParleyUnderground`, etc.).
- **Players never assign points directly to these skills.** The raw skills exist solely as internal mechanics to prevent min-maxing and preserve narrative surprise.

### 2.2 The Player's Lens: Masteries (`Data.Mastery`)
- Players acquire **Masteries** during character creation and progression.
- Each Mastery is a flavorful, lore-rich identity representing past occupations, cultural upbringings, or specialized physical training (e.g., *Dockyard Boiler-Hauler*, *Trench Runner*, *Scavenger's Eye*).
- Under the hood, each Mastery ScriptableObject grants stacking bonuses (`associatedSkills`) or penalties (`penalizedSkills`) to specific hidden skills.

### 2.3 The World's Obstacles: Problem Archetypes
- Physical challenges, puzzles, and dialogue obstacles are tagged with **Problem Archetypes** (defined in `Archetypes.csv`).
- Each Archetype specifies:
  - **Applicable Skills**: A collection of hidden skills that could logically overcome the obstacle.
  - **Per-Skill Success Quips**: Evocative narrative outcomes detailing the exact method used (e.g., brute force smashing vs. delicate lockpicking).
  - **Per-Skill Failure Quips**: Evocative failure descriptions hinting at what went wrong.

---

## 3. Moment-to-Moment Dialogue Flow

### Beat 1: The Problem Setup (Transcript)
The dialogue window presents an evocative, sensory description of the situation without revealing mechanical formulas.
> *"The emergency isolation gate has slammed shut. A hydraulic control box hangs off the wall by a single rusted bracket, while the heavy gear spindle on the doorframe is visibly jammed with bent rebar."*

### Beat 2: Bifurcated Physical Approaches (Choice Area)
Instead of a single "Roll Skill" option, choices represent **distinct physical intents** mapped to separate Problem Archetypes.
- **`[1] [BRUTE FORCE] Use a heavy pry-bar to snap the buckled rebar.`**  
  *(Maps to Archetype: `ClearObstruction` $\rightarrow$ Skills: `LiftHeavyObjects`, `Smash`)*
- **`[2] [ENGINEERING] Bypass the hydraulic valves inside the wall box.`**  
  *(Maps to Archetype: `RustedMechanism` $\rightarrow$ Skills: `FixMachinery`, `Tinker`)*
- **`[3] [RETREAT] Step away from the gate.`**

> **Design Rule:** The UI **never** spoils which Mastery will assist the player prior to clicking. The player must deduce which approach aligns with their party's identities.

### Beat 3: The Resolution & Mastery Reveal
When the player commits to an option:
1. The system evaluates the chosen Archetype against the acting hero's `CharacterSheet`.
2. It identifies the highest applicable skill bonus and traces which **active Mastery** provided it.
3. The roll is calculated: `d20 + SkillBonus + ItemBonus vs TargetDC`.
4. The outcome block is appended directly into the transcript:

#### Success Example:
```text
<b><color=#143447>[ACTION: BRUTE FORCE]</color></b>
Leverage applied to the seized bulkhead spindle.

<b><color=#143447>:: RELEVANT MASTERY: DOCKYARD BOILER-HAULER ::</color></b>
<i><color=#38322B>Years of dislodging frozen valves in the lower shipyards guide your hands to the weakest weld.</color></i>

<b><color=#114A27>[PASSED] // Roll: 12 + 3 = 15 vs DC 13</color></b>

<color=#1C1814>"You bypass the rust entirely by forcing the mechanism into place with a brutal strike. The pipe snaps with a sharp crack, and the heavy lever slams shut."</color>
```

#### Failure Example (No Relevant Mastery):
```text
<b><color=#7D1609>[ACTION: ENGINEERING]</color></b>
Attempting to bleed the pneumatic back-pressure line.

<b><color=#7D1609>:: NO APPLICABLE MASTERY ::</color></b>
<i><color=#38322B>The tangle of corroded brass tubing and obscure pressure gauges is completely foreign to your background.</color></i>

<b><color=#7D1609>[FAILED] // Roll: 6 + 0 = 6 vs DC 14</color></b>

<color=#1C1814>"The rusted parts grind violently against each other, completely seizing up despite your efforts. The valve wheel shears off in your palm."</color>
```

---

## 4. Key Takeaways for Future Systems
- **Character Sheet Responsibility**: `CharacterSheet` must provide a query method `EvaluateArchetype(ProblemArchetype archetype, out Skill bestSkill, out Mastery contributingMastery, out int totalBonus)`.
- **Dialogue Knot Responsibility**: Choices store an archetype reference and target DC rather than hardcoded skill enums.
- **UI Responsibility**: Format the reveal using the high-contrast dieselpunk palette, avoiding Unicode symbols not present in typewriter fonts.
