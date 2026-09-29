# Wasteland Roadtrip: Node 1 Specification — The Lone Shack

> **Project:** Hollow Signal (Wasteland Roadtrip CRPG)  
> **Source Material:** User One-Shot Design Notes  
> **Location:** "Shack in the Middle of Nowhere"  
> **Engine Integration:** `DialogSystem` Knots, Problem Archetypes, Loot Tables

---

## 1. Scene Setting & Overview

* **Visual Atmosphere:** A weathered, dilapidated wooden shack stands solitary against the desert winds, miraculously intact. Beside it, a rusted windmill creaks in the gusting dust, feeding power cables into the shack.
* **Ticking Clock / Danger:** The shack is completely safe to rest in until nightfall. If the party stays into the night without securing the basement, the subterranean degenerate mutant awakens to drag a crew member into the depths.

```
                    ┌─────────────────────────┐
                    │     SURFACE LEVEL       │
                    │ • Ruined Living Room    │
                    │ • Windmill Wiring       │
                    │ • Broken Sofa (Trapdoor)│
                    └───────────┬─────────────┘
                                │
                        [SECRET HATCH]
                      (Locked Mag-Latch)
                                │
                                ▼
                    ┌─────────────────────────┐
                    │    UNDERGROUND LEVEL    │
                    │ • Dormant Degenerate    │
                    │ • Sparkling Server Rack │
                    │ • Active Terminal       │
                    │ • Engine 2.0 & RPG Tube │
                    └─────────────────────────┘
```

---

## 2. Interactive Problems & Archetype Mappings

### Problem 1: The Hidden Trapdoor below the Sofa
* **Dialogue Hook:** *"Wires snake across the warped linoleum, disappearing beneath the sagging frame of a stained sofa."*
* **Approaches:**
  * **`[SEARCH]` Move the sofa and examine the floor.**  
    *Archetype:* `Perception` $\rightarrow$ Evaluates `ScavengerEye` / `NoticeClues`.  
    *Success:* *"Beneath a frayed rug lies a heavy steel hatch sealed flush with the concrete foundation."*

---

### Problem 2: The Sealed Trapdoor (Mag-Latch)
* **Dialogue Hook:** *"The hatch has no padlock or keyhole. A reinforced magnetic clamp hums faintly with windmill current."*
* **Approaches:**
  * **Option A: `[HACK]` Splice the exterior power line to trip the relay.**  
    *Archetype:* `HackCircuit` $\rightarrow$ Evaluates `Electronics` / `Tinker` (DC 11).  
    *Success (Piston-Wrench quip):* *"A quick spark from your pliers blows the capacitor. The magnetic clamp releases with a solid clunk."*
  * **Option B: `[BRUTE FORCE]` Pry open the hatch with a heavy crowbar.**  
    *Archetype:* `Smash` $\rightarrow$ Evaluates `LiftHeavy` / `BruteForce` (DC 13).  
    *Success (Roof-Runner quip):* *"You wedge the iron bar into the seam and throw your full weight down. The lock pin shears with a violent snap."*
  * **Option C: `[INVESTIGATE]` Search the kitchenette for the breaker switch.**  
    *Archetype:* `Investigate` $\rightarrow$ Evaluates `ScavengeSalvage` (DC 10).  
    *Success:* *"Hidden behind a rusted breadbox is the emergency cutoff switch. Flipping it cuts the latch power cleanly."*

---

### Problem 3: The Basement Degenerate & The Malware Web
* **Dialogue Hook:** *"A humanoid horror—skin grayed and sinews twitching—hangs suspended in a web of pulsing Ethernet and high-voltage cables. In the corner, a single green phosphor monitor blinks with lines of corrupted code."*
* **Approaches:**
  * **Option A: `[TERMINAL HACK]` Jack into the server to overload the line.**  
    *Archetype:* `HackCircuit` $\rightarrow$ Evaluates `Electronics` / `TerminalProficiency` (DC 11).  
    *Outcome:* Sends a 5,000-volt surge through the cable web. The mutant is electrocuted instantly without alerting others or burning ammo.
  * **Option B: `[STEALTH TAKEDOWN]` Rook sneaks forward to sever the life-support drip.**  
    *Archetype:* `Acrobatics` / `Stealth` (DC 12).  
    *Outcome:* The degenerate expires silently before waking.
  * **Option C: `[OPEN FIRE]` Shoot it before it detaches.**  
    *Cost:* Consumes 4 rounds of ammunition and alerts any scouts within half a mile.

---

## 3. Loot & Upgrades Table

### Surface Level Cache:
* **Fuel:** $+15$ Gallons (enough for 2 highway legs).
* **Rations:** 2 Canned MREs (restores party stamina).
* **Scrap:** $2\text{d}6 + 3$ raw mechanical parts.

### Underground Server Cache:
* **Engine 2.0 Module:**  
  * *Effect:* Installs into the War Rig. Increases Rig Cruise Speed by $+1$ and lowers fuel consumption by $15\%$. Unlocks *Emergency Nitrous Burn* maneuver during road battles.
* **Disposable Rocket Launcher (1-Shot):**  
  * *Effect:* Heavy ordinance weapon. Deals $4\text{d}6$ explosive damage in an area, instantly disabling buggies or blasting through highway blockades.
* **Ammunition & Meds:**  
  * $12\times$ 9mm rounds.  
  * $1\times$ *Ditropan Injector* (Boosts Speed by $+2$ for 1 encounter).
