# Wasteland Roadtrip: Crew Archetypes & Character Roster

> **Project:** Hollow Signal (Wasteland Roadtrip CRPG)  
> **Module:** Character System & Crew Roster Specification  
> **Turnaround Model Sheets Location:** `Assets/Art/concept/gameconcept/characters/*turnaround*.jpg`

---

## 1. Overview & Party Dynamics

The crew of the War Rig is an eccentric, exaggerated cast of wasteland survivors. Each member possesses **3 Core Attributes** (Might, Speed, Intellect), a unique **Combat Role**, and specific **Masteries** that unlock hidden dialogue approaches and narrative quips during road exploration and roadside vignettes.

```
┌────────────────────────────────────────────────────────────────────────────────────────┐
│                               THE WAR RIG CREW ROSTER                                  │
├────────────────────┬───────────────────┬─────────────────────────┬─────────────────────┤
│ Character Name     │ Archetype / Lens  │ Primary Combat Role     │ Key Mechanic        │
├────────────────────┼───────────────────┼─────────────────────────┼─────────────────────┤
│ Granny Naphtha     │ The Blast-Smith   │ Demolitions & Ordnance  │ Can-Mortar / Mines  │
├────────────────────┼───────────────────┼─────────────────────────┼─────────────────────┤
│ "Big Rig" Morris   │ The Wheel-Demon   │ Combat Driver & Rammer  │ Sleepless Trucker   │
├────────────────────┼───────────────────┼─────────────────────────┼─────────────────────┤
│ "Sprocket" Pip     │ The Trash-Gremlin │ Feral Mechanic & Sabot  │ Crawlspaces / Rivet │
├────────────────────┼───────────────────┼─────────────────────────┼─────────────────────┤
│ Unit 07-Bonesaw    │ The Autodoc       │ Glitched Combat Surgeon │ Biped Legs & Saw    │
└────────────────────┴───────────────────┴─────────────────────────┴─────────────────────┘
```

---

## 2. Character Turnarounds & Profiles

### 1. Granny Naphtha — The Blast-Smith
* **Model Sheet Turnaround:** [`granny_naphtha_turnaround_1790611616901.jpg`](file:///C:/Users/Thiago/Desktop/Personal%20Projects/Horror%20Room/Hollow%20Signal/Hollow%20Signal/Assets/Art/concept/gameconcept/characters/granny_naphtha_turnaround_1790611616901.jpg)
* **Portrait:** [`granny_naphtha_blastsmith_1790609811433.jpg`](file:///C:/Users/Thiago/Desktop/Personal%20Projects/Horror%20Room/Hollow%20Signal/Hollow%20Signal/Assets/Art/concept/gameconcept/characters/granny_naphtha_blastsmith_1790609811433.jpg)
* **Visual Identity:** Diminutive 72-year-old grandmother. Wild white hair tied with yellow zip-ties, thick spectacles, welding goggles on her forehead, and a quilted floral apron over a heavy Kevlar flak jacket lined with measuring spoons and silver duct tape. Chewing on dried citrus peel with all 10 fingers intact.
* **Signature Weapon:** **"The Granny Launcher"** — A custom pneumatic shoulder-fired can-mortar wrapped in floral insulation tape.
* **Mastery: `[Blast-Smith]`**
  * *Hidden Bonuses:* `HandleExplosives: +3`, `HeavyWeapons: +2`, `DisarmTraps: +2`.
  * *Combat Role:* Occupies the `Rig_Roof` or `Rig_Flatbed`. Launches shrapnel cans at pursuing buggies and drops rolling mine barrels backward.
  * *Dialogue Utility:* Disarms booby traps, brews fertilizer explosives, blasts rockslide blockages.

---

### 2. "Big Rig" Morris — The Wheel-Demon (Driver)
* **Model Sheet Turnaround:** [`morris_character_turnaround_1790611525740.jpg`](file:///C:/Users/Thiago/Desktop/Personal%20Projects/Horror%20Room/Hollow%20Signal/Hollow%20Signal/Assets/Art/concept/gameconcept/characters/morris_character_turnaround_1790611525740.jpg)
* **Visual Updates:** Beard is cleaned of food crumbs while retaining its rugged, weathered trucker heft.
* **Visual Identity:** Massive, burly long-haul trucker who hasn't slept in five years. Deep purple fatigue bags under bloodshot eyes, bushy gray-streaked beard, mesh trucker cap with fishing hooks in the brim, denim overalls, and a coiled CB radio microphone cord wrapped around his wrist like prayer beads. Constantly clutches a thermos of radiator-boiled black coffee sludge.
* **Signature Habit:** Talks non-stop to phantom truckers over static on channel 19. Treats the War Rig like sacred family property.
* **Mastery: `[Wheel-Demon]`**
  * *Hidden Bonuses:* `DriveVehicle: +3`, `EvasiveReflexes: +2`, `NavigateWasteland: +2`.
  * *Combat Role:* Occupies `Rig_Cabin` steering wheel. Uses **Sideswipe / Ram**, **Brake-Check**, and **Emergency Drift**.
  * *Dialogue Utility:* Anticipates sandstorms, navigates washed-out highways, bypasses roadblock ambushes.

---

### 3. "Sprocket" Pip — The Trash Mechanic (11-Year-Old Feral Gremlin)
* **Model Sheet Turnaround:** [`sprocket_pip_turnaround_1790611736960.jpg`](file:///C:/Users/Thiago/Desktop/Personal%20Projects/Horror%20Room/Hollow%20Signal/Hollow%20Signal/Assets/Art/concept/gameconcept/characters/sprocket_pip_turnaround_1790611736960.jpg)
* **Portrait:** [`sprocket_pip_gremlin_1790610939523.jpg`](file:///C:/Users/Thiago/Desktop/Personal%20Projects/Horror%20Room/Hollow%20Signal/Hollow%20Signal/Assets/Art/concept/gameconcept/characters/sprocket_pip_gremlin_1790610939523.jpg)
* **Visual Identity:** 65-pound, 11-year-old orphan covered in motor grease. Oversized adult welding helmet pushed up on her forehead like a duck bill, covered in stickers. Baggy overalls held with hose clamps and copper wire, metal roller-skate wheels bolted to her work boot soles. Wields a pneumatic pop-rivet gun connected to a yellow scuba air tank on her back.
* **Unique Constraints & Perks:**
  * **Hard Gun Limit:** Cannot wield shotguns, rifles, or heavy weapons (recoil knocks her prone).
  * **Unique Weapon:** High-velocity rivet gun (blinds gunners, shreds tires) and pocketfuls of ball bearings/caltrops.
  * **Crawlspace Master:** Fits into ventilation ducts, drainage pipes, and narrow engine bays where adults cannot go.
  * **Combat Role:** Mid-fight repairs on the `Rig_Hood` and sliding under enemy chassis on roller-skates to cut brake lines.
* **Mastery: `[Grease-Gremlin]`**
  * *Hidden Bonuses:* `FixMachinery: +3`, `Acrobatics: +2`, `ScavengeSalvage: +3`, `Tinker: +2`.

---

### 4. Unit 07-Bonesaw ("Doc-O-Matic") — The Wasteland Sawbones
* **Model Sheet Turnaround:** [`unit_07_autodoc_turnaround_1790611803439.jpg`](file:///C:/Users/Thiago/Desktop/Personal%20Projects/Horror%20Room/Hollow%20Signal/Hollow%20Signal/Assets/Art/concept/gameconcept/characters/unit_07_autodoc_turnaround_1790611803439.jpg)
* **Visual Updates:** Upgraded to lightweight articulated bipedal hydraulic legs with stabilizer claw feet (replacing the heavy tank tread) for nimble traversal across the War Rig's cabin and exterior.
* **Visual Identity:** Rusted, olive-drab pre-war military automated triage robot. Head is a cracked green-phosphor CRT screen displaying an 8-bit smiling face. Articulated mechanical arms feature a circular motorized bonesaw, a butane cauterizing blowtorch, an industrial staple gun, syringe vials of glowing amber serum, and a gripper holding an intact red cherry lollipop.
* **Personality:** Cheerful, recorded corporate customer service voice while delivering excruciating battlefield surgery: *"Please hold completely still during amputation! Rating this procedure 5 stars earns you a lollipop!"*
* **Mastery: `[Combat-Autodoc]`**
  * *Hidden Bonuses:* `FieldMedicine: +3`, `AnatomyAnalysis: +2`, `TriageSurgery: +3`, `Electronics: +1`.
  * *Combat Role:* Stabilizes wounded crew, injects combat stimulants (Speed boosts), and can cauterize boarders with its blowtorch.
  * *Dialogue Utility:* Treats radiation sickness, analyzes strange chemical toxins, hacks military hospital terminals.
