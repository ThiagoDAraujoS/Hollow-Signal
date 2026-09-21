# Hollow Signal — Core RPG System & Dice Resolution

## 1. Core Resolution Mechanic: The 3d6 Bell Curve

Hollow Signal replaces the standard swingy d20 roll with a **3d6 bell curve resolution system**.

```
             [ 3d6 PROBABILITY DISTRIBUTION ]
             Mean: 10.5 | Range: 3 to 18

    12.5% |               _  _
    10.0% |             /      \
     7.5% |           /          \
     5.0% |         /              \
     2.5% |       /                  \
     0.0% +---+---+---+---+---+---+---+---+---+
          3   5   7   9  10.5 12  14  16  18
```

### Why 3d6?
- **Character Backgrounds Trump Die Variance:** In a flat d20 system, each $+1$ bonus is only a $5\%$ shift, meaning a random roll easily drowns out character identity. On 3d6, the probability forms a steep bell curve centered at $10.5$.
- **High-Impact Bonuses:** In the critical decision range (rolls 10–13), **every single $+1$ is worth $\approx 11.5\% \text{ to } 13.9\%$ in success rate**. 
- **Untrained Attempts Hurt:** Without relevant training or tools ($+0$), pushing against standard obstacles feels steep and punishing, discouraging thoughtless brute-forcing outside of a hero's background.
- **Mastery Consistency:** A master with specialized gear ($+3$ to $+4$) performs with dependable professional confidence on standard tasks, yet still faces real dramatic tension on heroic feats.

---

## 2. The Bonus Economy (+0 to +4)

Bonuses are intentionally bounded to a strict **$+0$ to $+4$ range** to ensure every modifier maintains monumental weight:

| Tier | Bonus | Description | Example Profile |
| :--- | :---: | :--- | :--- |
| **Untrained** | **$+0$** | No background Mastery and no specialized tools. | A street urchin attempting to calibrate a diesel reactor. |
| **Novice / Improvised** | **$+1$** | Minor background familiarity OR using a basic improvised tool. | A soldier using an improvised pry-bar on a locked hatch. |
| **Proficient** | **$+2$** | Direct background Mastery OR a standard dedicated tool. | A *Dockyard Boiler-Hauler* forcing a rusted valve by hand. |
| **Expert** | **$+3$** | High-tier Mastery synergy OR Mastery paired with basic tools. | A *Trench Engineer* dismantling a landmine with a pocket kit. |
| **Master + Dedicated Tool** | **$+4$** | Specialized Mastery fully equipped with high-grade dedicated gear. | A *Master Machinist* repairing a pneumatic spindle with a hydraulic toolkit. |

---

## 3. Probability & Difficulty Calibration Table

The following table outlines the exact probability of meeting or exceeding a target Difficulty Class (DC) with a **3d6 + Bonus** roll:

| Difficulty Tier | Target DC | Untrained (+0) | Novice (+1) | Proficient (+2) | Expert (+3) | Master + Tool (+4) | Design Intent |
| :--- | :---: | :---: | :---: | :---: | :---: | :---: | :--- |
| **Routine** | **DC 10** | $62.5\%$ | $74.1\%$ | $83.8\%$ | $90.7\%$ | $95.4\%$ | Everyday tasks; untrained can manage, masters virtually never fail. |
| **Moderate** | **DC 11** | $50.0\%$ | $62.5\%$ | $74.1\%$ | $83.8\%$ | $90.7\%$ | Noticeable friction; coin-flip for untrained, masters highly reliable. |
| **Challenging** | **DC 12** | $37.5\%$ | $50.0\%$ | $62.5\%$ | $74.1\%$ | $83.8\%$ | Standard adventuring obstacle. Being untrained is a serious risk. |
| **Hard** | **DC 13** | $25.9\%$ | $37.5\%$ | $50.0\%$ | $62.5\%$ | $74.1\%$ | Serious test of skill; untrained fails $\approx 75\%$ of the time. |
| **Very Hard** | **DC 14** | $16.2\%$ | $25.9\%$ | $37.5\%$ | $50.0\%$ | $62.5\%$ | Severe impediment; masters sweat, untrained requires miraculous luck. |
| **Formidable** | **DC 15** | $9.3\%$ | $16.2\%$ | $25.9\%$ | $37.5\%$ | $50.0\%$ | Nearing human limit; masters have a fighting chance. |
| **Heroic** | **DC 16** | $4.6\%$ | $9.3\%$ | $16.2\%$ | $25.9\%$ | $37.5\%$ | Extreme milestone; demands grit, tools, and expertise. |

---

## 4. Integration with the Triad Simulation Engine

Every roll in Hollow Signal flows through the Triad:
1. **The Physical Obstacle (Sensory Prompt):** A world event or dialogue knot presents a concrete scenario without spoiling numerical formulas.
2. **Player Deduction:** The player chooses an action approach that intuitively matches a party member's background identity.
3. **Black Box Query:** The engine queries the acting hero's sheet:
   $$\text{Final Bonus} = \\text{Clamp}\\Big(\\text{Base Skill Bonus} + \\text{Mastery Modifiers} + \\text{Equipment Modifiers} + \\text{Spot Buffs}, \\, 0, \\, 4\\Big)$$
4. **Resolution:**
   $$\text{Result} = (\\text{Die } 1 + \\text{Die } 2 + \\text{Die } 3) + \\text{Final Bonus}$$
5. **Outcome & Quip Reveal:**
   - On **Success**: Reveal the contributing Mastery and a flavor quip showing how their training conquered the task.
   - On **Untrained Failure ($+0$)**: Emphasize lack of familiarity (*\"The tangle of corroded brass tubing and obscure pressure gauges is completely foreign to your background\"*).

---

## 5. The Cypher-Inspired Effort & Health Model

Hollow Signal adapts the Cypher System philosophy—where **exerting effort directly expends your survivability**—adapted cleanly to our Mastery & dieselpunk setting without requiring bloated attribute stat scores.

### 5.1 The Dual Health Tracks

Characters maintain two separate vitality pools:

```
┌────────────────────────────────────────────────────────┐
│  [FLESH]     ■ ■ ■ ■ ■ ■ ■ ■ □ □  (Physical Health)    │
│  • Depleted by shrapnel, gunshots, fire, and falls.    │
│  • 0 Flesh = Incapacitated / Bleeding Out.             │
├────────────────────────────────────────────────────────┤
│  [COMPOSURE] ■ ■ ■ ■ ■ ■ □ □ □ □  (Mental / Nerve Pool)│
│  • Depleted by psychological trauma, fear, & EFFORT.   │
│  • 0 Composure = Panic / Shellshock (Crisis collapse). │
└────────────────────────────────────────────────────────┘
```

1. **Flesh (Physical Health):** Structural, bodily integrity.
2. **Composure (Nerve / Focus Pool):** Psychological resilience, mental clarity, and tactical discipline. **This pool serves as the fuel for Effort.**

---

### 5.2 Baseline Pools & Progression Modifiers

To preserve clarity while supporting deep RPG builds, characters begin with standard generic baselines that are shaped by their background, gear, and narrative journey:

$$\text{Max Composure} = \text{Base}(10) + \text{Mastery Deltas} + \text{Equipment Deltas} + \text{Narrative Scars/Boons}$$

- **The Standard Baseline:**
  - **Base Flesh:** $10$
  - **Base Composure:** $10$ *(Grants exactly $5$ full Effort pushes at full health)*

#### A. Masteries (Psychological Trade-offs)
Masteries grant active and passive shifts to a character's mental threshold:
- **Hardened / Disciplined:**
  - *Trench Veteran:* $+3$ Max Composure *(iron nerve under fire, 6 pushes)*.
  - *Dockyard Boiler-Hauler:* $+2$ Max Flesh, $+1$ Max Composure.
- **High-Strung / Obsessive (The Specialists):**
  - *Arcane Circuit Hermit:* High arcane skill bonuses, but **$-2$ Max Composure** *(brilliant mind, but socially frayed and prone to nervous panic)*.
  - *Obsessive Archivist:* $+2$ investigation/records skills, but $-1$ Composure.

#### B. Equipment & Dieselpunk Curios
Carried gear provides tangible physical or psychological anchors:
- **Protective Gear:**
  - *Filter Rebreather (Head):* Protects against gas and provides $+1$ Composure against ambient horror.
- **Curios & Keepsakes (Accessory Slots):**
  - *Engraved Brass Lighter:* $+2$ Max Composure *(a grounding physical memory from before the atmospheric bubble)*.
  - *Lead-Lined Flask:* Composure damage taken from horror attacks reduced by $1$.
  - *Eerie Static Receiver:* $+2$ to perception/signals, but **$-2$ Max Composure** *(whispers unending noise into your ear)*.

#### C. Narrative Scars & Triumphs (Blackboard Persistence)
Because state is stored in the `Blackboard` via `TrackedBehaviour`, world events permanently alter a hero's psychological baseline:
- **Trauma Scars (Critical Failures / Atmospheric Horrors):**
  - Watching an ally fall or failing a terrifying encounter inflicts conditions like *Trauma: Night Terrors* ($-2$ Max Composure until treated in a safe hub).
- **Hardened Convictions (Narrative Triumphs):**
  - Restoring district power, saving an orphanage, or discovering truth about the city adds permanent $+1$ Max Composure.

---

### 5.3 Mastery-Gated Effort (Protecting Expertise)

In Cypher System, spending pool points lowers difficulty. In Hollow Signal, **Composure is spent to bend the dice**, but with one fundamental constraint:

> **The Golden Rule:** You can **ONLY** spend Composure to push or reroll an action if the check is backed by a relevant **Mastery** (Bonus $\ge +1$).

- **Why this constraint exists:**
  - **Narrative Logic:** An experienced boiler technician who fails to turn a valve can grimace, push through the mental strain, and leverage their technique a second time. An untrained academic staring at the same valve has no idea what they did wrong; sweating and straining harder won't magically grant mechanical insight.
  - **Zero Stat Bloat:** We avoid adding artificial Might/Speed/Intellect numbers. The character's Masteries directly define where they have the confidence and technique to spend Effort.

---

### 5.4 Mechanics of Spending Effort

When facing a check backed by an active Mastery, the player can choose to burn **Composure**:

1. **Focused Approach (Pre-Roll Effort):**
   - **Cost:** $2$ Composure points.
   - **Effect:** Grants **Advantage**. Roll **4d6 and drop the lowest die**.
   - Used when failure is not an option and the character commits total focus before committing the act.
2. **Desperate Recovery (Post-Roll Reroll):**
   - **Cost:** $2$ Composure points.
   - **Effect:** Triggered only on a failed roll. The player **rerolls the lowest single die** of the three rolled d6s, keeping the new result.
   - Represents catching yourself just as the tool slips or finding last-second leverage.

---

### 5.5 The Cost of Pushing: Composure Depletion & Shellshock

Because Composure is also mental health, burning it recklessly leaves heroes vulnerable to panic and horror:

| Composure State | Threshold | Gameplay Consequences |
| :--- | :---: | :--- |
| **Steady** | $50\% \text{ to } 100\%$ | Full mental clarity. Normal operation and full access to Effort. |
| **Rattled** | $1\% \text{ to } 49\%$ | The character is sweating, breathing heavily, and fraying under stress. Suffering mental attacks in combat inflicts $+1$ bonus damage. |
| **Shellshocked** | **$0$ Composure** | **Breakdown.** The character cannot spend Effort on any action. In Crisis combat, they roll defenses with Disadvantage (drop highest die) or suffer involuntary panic behaviors (retreating to nearest cover). |

---

### 5.6 Replenishing Composure
- **Field Rations & Dieselpunk Stimulants:** Smoking a rationed cigarette, injecting a nerve stabilizer, or sharing a flask.
- **Safe Rooms & Shelters:** Resting at designated secure anchor spots away from ambient atmospheric bubble hazards.
- **Narrative Triumphs:** Solving a major crisis or uncovering critical truth restores party-wide Composure.

---

## 6. Critical Triumphs, Fumbles & The Decorator Stack

### 6.1 Critical Trigger Rules (Under Consideration)

In a 3d6 system, a single natural 18 or 3 occurs only $0.46\%$ of the time (1 in 216 rolls), which is too rare for active tactical gameplay. Two trigger models have been evaluated:

1. **The Threshold Model:**
   - **Critical Triumph:** Natural $17$ or $18$ ($4$ in $216 = 1.85\%$, $\approx 1$ in $54$ rolls).
   - **Critical Fumble:** Natural $3$ or $4$ ($4$ in $216 = 1.85\%$, $\approx 1$ in $54$ rolls).
   - *Feel:* Rare, catastrophic, and climactic.

2. **The Doubles Model (Current Preferred Direction):**
   - **Double 6s (or Triple 6s):** **Critical Triumph** ($16$ in $216 = \mathbf{7.41\%}$, $\approx 1$ in $13.5$ rolls).
   - **Double 1s (or Triple 1s):** **Critical Fumble / Complication** ($16$ in $216 = \mathbf{7.41\%}$, $\approx 1$ in $13.5$ rolls).
   - *Feel:* Highly dynamic. Occurs slightly more often than a d20 crit ($5\%$), meaning roughly 1 triumph and 1 complication emerge per combat encounter, constantly shifting tactical momentum.

> **Status:** Double 6s / Double 1s is currently favored for combat and interaction testing. Further playtesting will determine if non-combat narrative rolls use the stricter Threshold Model ($17\text{–}18 / 3\text{–}4$) to prevent excessive narrative complications.

---

### 6.2 Narrative & Tactical Philosophy: Shifting Momentum

Rather than flat "double damage" or instant death, criticals dynamically inject **temporary Decorators** into the character, enemy, or environment:

#### On a Critical Triumph (Positive Decorators):
- **Tactical Combat:**
  - `[Momentum: Staggering Strike]`: Target is knocked off balance, losing 1 Zone Move on their next turn.
  - `[Adrenaline Surge]`: Hero instantly recovers $2$ Composure and gains free intra-zone repositioning.
  - `[Exposed Weakpoint]`: Target gains a decorator granting $+1$ to-hit for all subsequent allied attacks this round.
- **World & Machine Interactions:**
  - `[Overclocked Spindle]`: The seized valve or console yields cleanly, unlocking auxiliary power or restoring lights to the room.

#### On a Critical Fumble (Negative Decorators / Complications):
- **Tactical Combat:**
  - `[Jammed Mechanism]`: The weapon’s pneumatic cycle jams; requires $1$ Action to un-jam before firing again.
  - `[Displaced / Off-Balance]`: The hero stumbles out of their `TacticalSlot` into open ground, immediately losing their cover decorator.
  - `[Splintered Cover]`: The cover spot absorbing the shot degrades (decorator drops from $+2$ Defense to $+1$).
- **World & Machine Interactions:**
  - `[Strained Ligament]`: Minor sprain; $-1$ Zone movement until the end of the next encounter.
  - `[Frayed Nerves]`: Sudden backfire or loud alarm costs $2$ Composure immediately.

---

### 6.3 Future Architectural Foundation: The Character Decorator Stack

To cleanly support temporary bonuses, permanent masteries, equipment, and critical consequences without mutation bugs or hardcoded lists, the character sheet is architected as a **Dynamic Decorator Stack**:

$$\text{Effective Stat / Bonus} = \text{Base} + \sum (\text{Active Decorators})$$

```
┌─────────────────────────────────────────────────────────────┐
│                    CHARACTER SHEET                          │
│                                                             │
│  [BASE STATS]                                               │
│  • Base Flesh: 10   • Base Composure: 10   • Speed: 1 Zone  │
│                                                             │
│  ▼ DECORATOR STACK (Evaluated in Real-Time)                 │
│  ┌───────────────────────────────────────────────────────┐  │
│  │ [Permanent Mastery] Dockyard Boiler-Hauler            │  │
│  │   +1 Max Flesh, +2 Smash, +2 LiftHeavy                │  │
│  ├───────────────────────────────────────────────────────┤  │
│  │ [Equipment Slot] Trench Coat                          │  │
│  │   +1 Armor, +1 Ballistic Defense                      │  │
│  ├───────────────────────────────────────────────────────┤  │
│  │ [Tactical Spot] Sandbag Emplacement (Temporary)       │  │
│  │   +2 Ranged Defense, Grants "Braced"                  │  │
│  ├───────────────────────────────────────────────────────┤  │
│  │ [Combat Crit Event] Adrenaline Surge (Expires: 1 Turn)│  │
│  │   +1 Move Speed, Free Intra-Zone Reposition           │  │
│  ├───────────────────────────────────────────────────────┤  │
│  │ [Fumble Consequence] Jammed Mechanism (Expires: Action│  │
│  │   Weapon disabled until cleared                       │  │
│  └───────────────────────────────────────────────────────┘  │
│                                                             │
│  ▼ RESOLVED TOTALS                                          │
│  Flesh: 11 | Composure: 10 | Smash: +2 | Defense: +3        │
└─────────────────────────────────────────────────────────────┘
```

#### Explicit Decorator Lifecycles:
1. **Permanent:** Character background Masteries and narrative convictions.
2. **Equipped:** Bound to equipped items; removed when unequipped.
3. **Spatial (Spot):** Bound to `TacticalSlot`; added upon entry, removed upon exit.
4. **Turn-Duration:** Counted down each round in Crisis Mode.
5. **Encounter / Rest:** Cleared when combat concludes or during Safe Room resting.

---

## 7. Minimalist Inventory: Gear & Single-Use "Sparks"

Hollow Signal rejects bloated inventory screens, grid-tetris management, and vendor trash. Every carried item is a high-stakes tactical decision.

### 7.1 Semi-Permanent Gear Slots (The Character Changers)

Characters do not wear dozens of jewelry pieces. Instead, each hero has **three dedicated Gear Slots** that act as core Decorators:

```
┌─────────────────────────────────────────────────────────────────────────┐
│                           HERO GEAR SLOTS                               │
│                                                                         │
│  [1. IMPLEMENT / WEAPON]     [2. PROTECTIVE RIG]   [3. SPECIALIST TOOL] │
│  Trench Shotgun              Reinforced Hazmat     Machinist's Spanner  │
│  • +2 Ballistic Attack       • +2 Max Flesh        • +2 FixMachinery    │
│  • +1 BreachHatch            • +2 Gas Resistance   • +1 Tinker          │
│  • -1 Stealth                • -1 Move Speed                            │
└─────────────────────────────────────────────────────────────────────────┘
```

1. **Implement / Weapon:** Dictates physical attacks, leverage, and ballistic options in Crisis combat and world puzzles.
2. **Protective Rig:** Dictates bodily survivability, Flesh maximums, environmental hazard resistances, and movement encumbrance tradeoffs.
3. **Specialist Tool:** Injects $+1$ or $+2$ proficiencies into specific problem archetypes, allowing heroes to reach the $+4$ Master cap when paired with Masteries.

---

### 7.2 Single-Use "Sparks" (The Rule-Breakers)

Instead of traditional class skill trees, magic spellbooks, or cooldown hotbars, **extraordinary tactical abilities come exclusively from single-use anomalous devices called "Sparks"** (inspired by Cypher System cyphers).

#### A. The Capacity Limit & Volatility
- **Strict Capacity:** Each hero can carry a maximum of **$2$ (or $3$) Sparks** at any time.
- **Spark Volatility:** Sparks are unstable diesel-arcane batteries, pressurized chemical canisters, or strange relics. Carrying beyond capacity is hazardous: surplus Sparks leak static or toxic vapor, directly draining Composure or Flesh.
- **The Gameplay Loop:** Because capacity is low and new Sparks are found regularly in the environment, **players are heavily encouraged to burn them boldly** rather than hoard them.

#### B. Examples of Single-Use Sparks

| Spark Item | Flavor / Type | Tactical / Narrative Effect |
| :--- | :--- | :--- |
| **Overdrive Adrenaline Injector** | Alchemical Stim | Gain an immediate extra Action this turn, but suffer $2$ Flesh damage at the end of the round. |
| **Pneumatic Breaching Sledge-Charge** | Demolition Device | Instantly demolish a reinforced door, barricade, or vault hatch without rolling an archetype check. |
| **Phosphor-Arc Flare** | Chemical Munition | Detonate in target Tactical Zone. All occupants gain `[Blind]` decorator for 1 round; strips stealth/invisibility. |
| **Sub-Acoustic Disruptor** | Arcane Frequency | Disrupts arcane automatons in target zone, forcing them to skip their action phase. |
| **Phrenic Dampener Capsule** | Medicinal Stabilizer | Instantly restore $6$ Composure to acting hero; cures *Rattled* condition. |

---

### 7.3 Reserve Pocket Capacity

Outside of the 3 equipped Gear slots and 2 Spark slots, characters have **$3$ to $4$ Reserve Pockets** in their rucksack:
- Storing alternate weapons/tools for swap-outs.
- Storing resting supplies (e.g. rationed cigarettes, bandages).
- Storing key narrative quest items (e.g. encoded cylinder records, power cells).
- The entire 3-to-4 hero party maintains a total footprint of under $\approx 15$ items across the whole company, keeping UI minimal and decisions sharp.

---

## 8. Turn-Based Combat (Crisis Mode) on 3d6

### 8.1 Turn Budget & Action Economy

During each team turn, each character receives a simple, clear action budget:
- **Movement:** 1 Area / Zone Move (free).
- **Minor Activation:** 1 Object Interaction (open door, flip switch, pick up a Spark).
- **Action:** 1 Major Action (Attack, Try to Hide, Complex Terminal Dialog, Use a Spark, or **Trade Action for a 2nd Area Move**).

### 8.2 The Sprint Test (Moving Twice AND Acting)

If a hero attempts to move across two areas **and** still execute their Major Action, the game prompts a **Sprint Test**:

- **The Roll:** **3d6 + Athletics/Mobility Bonus vs DC 11 (Moderate)** (or DC 12 in rough/muddy terrain).
- **Mastery-Gated Effort:** If the hero has a mobility-related Mastery (e.g., *Trench Courier*, *Dockyard Runner*), they may spend **2 Composure** to reroll the lowest die on a failure.
- **Outcomes:**
  - **Success:** Hero sprints across both areas smoothly and retains their full Action to attack or interact.
  - **Failure:** The hero reaches the second area panting; their Action is lost for the turn, and they gain the temporary decorator **`[Winded]`** ($-1$ Defense until next turn).
  - **Critical Fumble (Double 1s):** **`[Stumbled & Sprawled]`** — Knocked prone in open ground, losing all cover benefits.

### 8.3 Interaction Handshake Flows

- **Case 1 (Move Once → Move Twice → Try Acting):**
  1. Hero moves once (free).
  2. Hero moves a second time (expending their default Action).
  3. Player clicks an interaction or enemy target in the new area.
  4. System prompts: *"Attempt Sprint to act this turn?"*
  5. If **Yes**: Rolls 3d6.
     - **Pass:** Opens target interaction dialogue / ready to attack. The player is free to close, re-inspect, or switch actions in the same area before committing the Action.
     - **Fail:** Action is lost, `[Winded]` is applied, turn terminates.
  6. If **No**: Move ends; hero stands in Area 2 without acting.

- **Case 2 (Move Once → Act → Try Moving Again):**
  1. Hero moves once and completes their Action in Area 1.
  2. Player clicks to move into an adjacent Area 2.
  3. System prompts: *"Attempt Sprint to move again?"*
  4. If **Yes**: Rolls 3d6.
     - **Pass:** Hero successfully moves into Area 2.
     - **Fail:** Hero fails to move, gains `[Winded]`, and the sprint attempt is wasted.
  5. If **No**: Move is canceled; hero stays in Area 1.
