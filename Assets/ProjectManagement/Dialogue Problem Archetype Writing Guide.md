# Dialogue Problem Archetype Writing Guide

This document defines how narrative designers and level writers author `.dialog` files using the **Archetype-Driven Problem Resolution System**.

---

## 1. Design Philosophy: Lightweight & Data-Driven

### 1.1 The Problem with Legacy Dialogue Scripting
In traditional scripting, every recurring obstacle (a locked door, a security terminal, an encrypted safe, a hostile sentry) required manual branching and redundant text:
- Writing separate choices for *Pick lock*, *Bypass circuit*, *Smash with crowbar*, *Force latch*.
- Hardcoding DC values for every approach directly in `.dialog`.
- Writing manual success and failure lines for every interaction.
- Hand-wiring perk requirement checks across hundreds of knots.

This resulted in bloated dialogue files that were tedious to maintain and impossible to rebalance centrally.

### 1.2 The Archetype Solution
In Hollow Signal, all recurring problem definitions, approaches, and default quips live externally in **Google Sheets / Game Databases**. 

The `.dialog` script only needs to specify:
1. **The Problem Archetype** (e.g., `Door`, `Safe`, `Terminal`, `Chest`).
2. **The Base Difficulty Level** (e.g., `5`).
3. **Where to go on Success** (`- SUCCESS -> TargetKnot`).
4. **Where to go on Failure** (`- FAILURE -> TargetKnot`).

The dialogue engine automatically queries the database, displays the eligible approaches, calculates the target DCs, checks player perks, renders roll transcripts, and plays default quips.

---

## 2. Standard Problem Knot Syntax

### 2.1 The Standard Minimalist Pattern (Recommended)
This is what 90% of your problem knots will look like:

```text
=== KNOT: SecurityBulkhead ===
NARRATOR: A heavy hydraulic blast door seals the maintenance shaft.
~ Problem(Door: 5)
- SUCCESS ->
    -> SectorB_Hallway
- FAILURE ->
    -> SectorB_LockedOut
```

#### What Happens Under the Hood:
1. The narrator line displays.
2. The UI queries the `Door` archetype from the database.
3. Every approach registered for a `Door` (e.g., `Pick`, `Bypass`, `Force`, `Smash`) is evaluated:
   - Hidden approaches requiring missing perks are omitted (`HiddenWhenLocked`).
   - Visible approaches requiring missing perks are grayed out with tooltips (`ShownWhenLocked`).
4. The player picks their approach, rolls dice, and attempts the Cypher test.
5. If successful, the character's default action success quip is displayed, and the dialogue segues immediately to `SectorB_Hallway`.
6. If failed, the character's failure quip is displayed, and the dialogue segues to `SectorB_LockedOut`.

---

### 2.2 Adding Custom Dialogue Lines (Optional)
If a specific story encounter requires unique exposition before branching, you can optionally include lines under `- SUCCESS ->` or `- FAILURE ->`. Any written lines take precedence over the generic action quips:

```text
=== KNOT: ExperimentalVault ===
NARRATOR: The vault dial is frozen in liquid nitrogen.
~ Problem(Safe: 6)
- SUCCESS ->
    VAULT_TECH: The locking tumblers shattered cleanly. We're in!
    -> VaultInterior
- FAILURE ->
    VAULT_TECH: Dammit! The cold welded the mechanism shut permanently.
    -> VaultFailed
```

---

### 2.3 One-Line Knot Transitions
If no intermediate text is needed, the divert arrow can be placed directly on the branch header:

```text
=== KNOT: MedicalLocker ===
NARRATOR: A reinforced narcotic cabinet.
~ Problem(Locker: 3)
- SUCCESS -> LockerOpen
- FAILURE -> LockerIntact
```

---

## 3. Difficulty & Cypher Scaling

Difficulty in `.dialog` files is expressed in **Cypher System Levels** (integers, typically 1 to 10), **NOT raw DCs**.

$$\text{Final DC} = (\text{Problem Base Level} + \text{Approach Level Offset}) \times 3$$

### Example: `~ Problem(Door: 5)`
Base problem difficulty is **Level 5**:

| Approach | Sheet `levelOffset` | Effective Level | Calculated Target DC |
| :--- | :---: | :---: | :---: |
| **Pick** (Finesse test) | `0` | Level 5 | **15** |
| **Bypass** (Tech test) | `-1` | Level 4 | **12** |
| **Smash** (Brute Force test) | `+1` | Level 6 | **18** |

*Writers do not calculate or hardcode DCs.* If a designer later rebalances `Smash` in the spreadsheet from `+1` to `+2`, all doors in the game automatically update without touching `.dialog` files.

---

## 4. Perk Gating & Availability

Perk requirements are configured in the archetype database, not in the dialogue script:

- **`HiddenWhenLocked`**: If an approach requires a perk (e.g., *Master Locksmith*) and the player does not have it, the option is completely invisible.
- **`ShownWhenLocked`**: The option appears on screen disabled (grayed out) with a hover tooltip explaining the missing requirement (e.g., *Requires: Arc Welder*).

---

## 5. Complete Before & After Comparison

### Old Legacy Way (Handcrafted Knot)
```text
=== KNOT: OldDoorKnot ===
NARRATOR: A heavy blast door is locked tight.
* [Pick the physical tumblers] -> PickDoor
* [Bypass the electronic keypad] -> BypassDoor
* [Smash the latch with a hammer] -> SmashDoor
+ [Walk away] -> Hallway

=== KNOT: PickDoor ===
~ SkillCheck(Lockpick, 15)
- SUCCESS ->
    NARRATOR: Tumblers click open.
    -> Hallway
- FAILURE ->
    NARRATOR: The pick snapped in the lock.
    -> Hallway

=== KNOT: BypassDoor ===
~ SkillCheck(Electronics, 12)
- SUCCESS ->
    NARRATOR: The keypad sparks and unlocks.
    -> Hallway
- FAILURE ->
    NARRATOR: Alarm tripped!
    -> Hallway

=== KNOT: SmashDoor ===
~ SkillCheck(Athletics, 18)
- SUCCESS ->
    NARRATOR: The door gives way.
    -> Hallway
- FAILURE ->
    NARRATOR: Your hands sting, door intact.
    -> Hallway
```
*Total: 34 lines of boilerplate script across 4 separate knots.*

---

### New Archetype Way
```text
=== KNOT: BulkheadDoor ===
NARRATOR: A heavy blast door is locked tight.
~ Problem(Door: 5)
- SUCCESS -> Hallway
- FAILURE -> BulkheadLocked
```
*Total: 5 lines of lightweight, balanced, data-driven narrative script.*

---

## 6. Future Syntax Roadmap (Post-MVP)

In a future iteration, writers will be able to override specific approaches for unique instances directly within the `.dialog` script:

```text
// FUTURE SYNTAX (NOT YET ACTIVE IN CURRENT MVP)
=== KNOT: SpecialVault ===
NARRATOR: An experimental reinforced chamber.
~ Problem(Door: 5)
    - Smash: x            // Disables the Smash option for this specific door
    - Hack: 6             // Overrides the Hack approach to Level 6 (DC 18)
    - SuperLockpick: 4:shown // Adds a custom approach at Level 4, shown even if locked
- SUCCESS -> VaultOpen
- FAILURE -> VaultLocked
```

For the current version, write your knots using the standard `~ Problem(ArchetypeId: Level)` syntax.
