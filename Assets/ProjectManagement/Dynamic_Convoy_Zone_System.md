# Dynamic Convoy Zone System — Architecture Specification

> **Project:** Hollow Signal (Wasteland Roadtrip CRPG)  
> **Module:** Tactical Zone Combat / Dynamic Road Skirmish Extension  
> **Base Architecture:** Extends `TacticalZone` & `TacticalSlot` (Voronoi Graph)

---

## 1. System Overview

Road battles do not take place on a static world map. Instead, combat takes place on a **Relative Convoy Graph**. 
All combatants travel forward simultaneously at high speed. The spatial zones are anchored to the vehicles themselves, moving through a scrolling visual road background.

```
                    [ RELATIVE CONVOY TOPOLOGY ]

                         [ FORWARD ROAD ]
                     (Roadblocks, Debris, Mines)
                                 ▲
                                 │
     [ LEFT FLANK ]    ◄─── [ THE WAR RIG ] ───►   [ RIGHT FLANK ]
 (Enemy Technical Car)         (Player)           (Enemy Outriders)
                                 │
                                 ▼
                        [ REAR CHASE ZONE ]
                       (Trailing Biker Pack)
```

---

## 2. The War Rig Interior & Exterior Zones

The Player's War Rig is a self-contained multi-zone node cluster:

```
┌──────────────────────────────────────────────────────────────────┐
│                     THE WAR RIG (PLAYER BASE)                    │
│                                                                  │
│  ┌──────────────────────┐              ┌──────────────────────┐  │
│  │   [ZONE: THE HOOD]   │              │   [ZONE: THE ROOF]   │  │
│  │ • 1 Tactical Slot    │◄────────────►│ • 2 Tactical Slots   │  │
│  │ • Exposed (No Cover) │              │ • Swivel Turret      │  │
│  │ • Engine Emergency Fix│              │ • High Ground (+1)   │  │
│  └──────────┬───────────┘              └──────────┬───────────┘  │
│             │                                     │              │
│             ▼                                     ▼              │
│  ┌──────────────────────┐              ┌──────────────────────┐  │
│  │  [ZONE: THE CABIN]   │              │ [ZONE: THE FLATBED]  │  │
│  │ • 2 Tactical Slots   │◄────────────►│ • 2 Tactical Slots   │  │
│  │ • High Cover         │              │ • Low Cover          │  │
│  │ • Steering Console   │              │ • Drop Mines/Harpoon │  │
│  └──────────────────────┘              └──────────────────────┘  │
└──────────────────────────────────────────────────────────────────┘
```

### Zone Breakdown:

| Zone Name | Slots | Cover | Contextual Interactables | Tactical Purpose |
| :--- | :---: | :---: | :--- | :--- |
| **`Rig_Cabin`** | 2 | **High Cover** | • *Steering Wheel Console* (Driver only)<br>• *Radio / Dash Gauges* | Safest zone. Pilot steers; Medic patches up wounded; Driver issues ram/evasion commands. |
| **`Rig_Roof`** | 2 | **Low Cover** | • *Swivel Machine Gun / Turret*<br>• *Spotter Vantage Point* | Offense hub. Snipers and heavy gunners get $+1$ to hit and clean line of sight to all flanking vehicles. |
| **`Rig_Flatbed`** | 2 | **Medium Cover** | • *Harpoon Winch*<br>• *Mine Chute / Grenade Basket* | Rear defense. Demolitions tosses bombs backward; Harpooner anchors pursuing buggies. Boarding entry point from Rear Chase. |
| **`Rig_Hood`** | 1 | **Exposed** | • *Radiator Cap / Supercharger* | Emergency repairs (cooling engine under fire); kicking off boarders climbing the front grille. |

---

## 3. Dynamic Enemy Vehicle Zones & Adjacency

Enemy vehicles are modular sub-graphs that dock into **Relative Convoys Slots** (`FlankLeft`, `FlankRight`, `RearChase`, `HeadOn`).

### Example: Enemy War Buggy (`Enemy_Buggy_01`)
* **`Buggy_Cabin` (1 Slot):** Occupied by Enemy Driver. Controls buggy maneuvers.
* **`Buggy_Roof` (1 Slot):** Occupied by Enemy Gunner.

### Dynamic Adjacency Links:
When an enemy vehicle matches velocity and enters a convoy slot, the system dynamically registers temporary traversal links:
* `Enemy_Buggy_01` in `FlankLeft` $\rightarrow$ Connects `Rig_Roof` to `Buggy_Roof` (Boarding distance: 1 Move + Sprint Test).
* `Enemy_Bikers` in `RearChase` $\rightarrow$ Connects to `Rig_Flatbed` (Biker jump boarding).

---

## 4. The Action Economy & Vehicle Maneuvers

Combatants use standard character turns (1 Move + 1 Action + 1 Minor Activation). 

### 4.1 The Driver Role (Vehicle Maneuvers)
A character occupying the *Steering Wheel Slot* in `Rig_Cabin` gains access to **Vehicle Maneuver Actions** (consuming their Major Action):

| Maneuver | Target Slot | Roll / Check | Tactical Effect |
| :--- | :--- | :--- | :--- |
| **Sideswipe / Ram** | `FlankLeft` or `FlankRight` | 3d6 + Driving vs DC 11 | Deals $2\text{d}6 + \text{Armor}$ damage to enemy vehicle. Shoves target back into `RearChase`. |
| **Hard Brake-Check** | `RearChase` | 3d6 + Driving vs DC 12 | Trailing vehicle crashes into rear bumper. Deals heavy frontal damage to enemy; test for boarder dislodgement. |
| **Evasive Drift** | Self | 3d6 + Driving vs DC 10 | Imposes $-2$ to hit on all enemy ranged attacks targeting the Rig until next turn. |
| **Speed Boost (Nitrous)** | Self | Free / Burns Fuel | Forces all vehicles in `RearChase` to test to maintain contact; breaks 1 boarding grapple. |

---

### 4.2 The Car-Walker / Boarder Mechanics
Characters on the `Rig_Roof` or `Rig_Flatbed` can perform the **Boarding Leap**:

```
[Rig: Roof] ──(Click Enemy Vehicle)──► [Check: 3d6 + Athletics vs DC 11]
                                              │
                      ┌───────────────────────┴───────────────────────┐
                      ▼ [PASS]                                        ▼ [FAIL]
        Land cleanly on Enemy Hood;                    Catch onto edge with fingertips;
        ready to act with weapon.                      gain [Dangling] decorator;
                                                       Action lost this turn.
```

* **Attacking the Driver:** A boarder on an enemy car can shoot/stab the driver directly, bypassing vehicle armor.
* **Severing Components:** Boarders can attack tires ($-3$ enemy speed) or fuel lines (instant fire hazard).

---

## 5. Environmental Road Hazard Events (Turn Modifier)

At the start of every round, the road environment shifts. A **Road Event Card** is resolved automatically:

1. **`Narrow Canyons`:** Flanking slots close. Flanking vehicles must drop into `RearChase` or take $3\text{d}6$ wall scraping damage.
2. **`Minefield / Road Spikes`:** Driver must pass a Driving check (DC 12) or take tire damage. Trailing vehicles gain chance of immediate wipeout.
3. **`Dust Cloud / Sandstorm`:** All ranged attacks beyond adjacent zones suffer $-3$ accuracy penalty.
4. **`Ramp / Broken Bridge`:** Speed check required. Jump allows escaping or landing on top of pursuers.
