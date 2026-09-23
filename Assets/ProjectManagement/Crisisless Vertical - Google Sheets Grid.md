# Crisisless Vertical Slice — Google Sheets Grid & Design Matrix

> **Location:** `Assets/ProjectManagement/Crisisless Vertical - Google Sheets Grid.md`  
> **Purpose:** Ready-to-copy spreadsheet tables for Google Sheets authoring. Each section provides a visual Markdown table followed by a raw, **Tab-Separated Value (TSV) code block** formatted for direct copy-pasting into Google Sheets.  
> **Design Principle:** Modular, reusable, and fully generic across any scene or vertical in *Hollow Signal*.  
> **Mastery Rule:** Each mastery grants strictly **+1** per associated skill.

---

## 1. Masteries Sheet (Character Identities)

*Matches schema: `Tier | Name | Description | Prerequisites | Skill 1 | Skill 2 | Skill 3 | Skill 4`*

### Visual Table
| Tier | Name | Description | Prerequisites | Skill 1 | Skill 2 | Skill 3 | Skill 4 |
| :---: | :--- | :--- | :--- | :--- | :--- | :--- | :--- |
| **1** | Conduit Sweep | You have spent years crawling through the city's toxic pipe-networks and high-altitude flues, learning how industrial machinery breathes, jams, and vents. | None | Climb | FixMachinery | LiftHeavyObjects | SenseDanger |
| **1** | Disgraced Scry-Clerk | You once stamped official manifests in municipal offices, learning the ins and outs of bureaucratic protocol, forged permits, and guard regulations. | None | NavigateBureaucracy | Deceive | ParleyGuards | ReadIntentions |
| **1** | Under-Sump Mechanist | You keep the black-market tools, makeshift locks, and patched mechanical implants running in the forgotten underbelly of the city. | None | Tinker | PickLocks | FixMachinery | Sneak |
| **1** | Heretical Archivist | You have studied forbidden texts and precursor scripts, granting you an intuitive grasp of arcane circuits and reality-warping phenomena. | None | FixArcaneCircuits | DetectArcaneEnergy | RecallArtifactLore | HackCircuits |

### Google Sheets Copy-Paste Block (Masteries)
```tsv
Tier	Name	Description	Prerequisites	Skill1	Skill2	Skill3	Skill4
1	Conduit Sweep	You have spent years crawling through the city's toxic pipe-networks and high-altitude flues, learning how industrial machinery breathes, jams, and vents.		Climb	FixMachinery	LiftHeavyObjects	SenseDanger
1	Disgraced Scry-Clerk	You once stamped official manifests in municipal offices, learning the ins and outs of bureaucratic protocol, forged permits, and guard regulations.		NavigateBureaucracy	Deceive	ParleyGuards	ReadIntentions
1	Under-Sump Mechanist	You keep the black-market tools, makeshift locks, and patched mechanical implants running in the forgotten underbelly of the city.		Tinker	PickLocks	FixMachinery	Sneak
1	Heretical Archivist	You have studied forbidden texts and precursor scripts, granting you an intuitive grasp of arcane circuits and reality-warping phenomena.		FixArcaneCircuits	DetectArcaneEnergy	RecallArtifactLore	HackCircuits
```

---

## 2. Skills Sheet (Curated Disciplines)

*The exact competence domains used to evaluate the 7 actions across this vertical slice.*

### Visual Table
| Skill Name | Discipline Domain | Core Focus & Application |
| :--- | :--- | :--- |
| **LiftHeavyObjects** | Physical / Athletics | Raw muscular mass, foot bracing, and brute structural leverage. |
| **Smash** | Physical / Kinetic | Impact force, striking welds, kinetic shock damage. |
| **Climb** | Physical / Agility | Traversing sheer pipe-work, suspension catwalks, and vertical shafts. |
| **SenseDanger** | Physical / Awareness | Reflexive awareness of industrial hazards, tripwires, and environmental traps. |
| **FixMachinery** | Craft / Engineering | Diagnosing frozen joints, high-pressure steam pipes, and seized gears. |
| **Tinker** | Craft / Precision | Delicate calibration, fine handcraft, tool leverage, and component isolation. |
| **OperateMachinery** | Craft / Engineering | Understanding industrial flow-valves, gauges, diesel pumps, and consoles. |
| **PickLocks** | Subterfuge / Precision | Tension wrench mechanics, tumbler manipulation, bypassing cylinder locks. |
| **Sneak** | Subterfuge / Stealth | Acoustic discipline, shadow blending, and patrol avoidance. |
| **Deceive** | Social / Guile | Vocal confidence, delivering fabricated alibis, misdirection. |
| **ReadIntentions** | Social / Perception | Spotting micro-expressions, detecting deceit, anticipating reactions. |
| **NavigateBureaucracy** | Social / Protocol | Citing regulations, exploiting red tape, inspecting documentation. |
| **ParleyGuards** | Social / Authority | Enforcer psychology, de-escalating beat cops, military protocol. |
| **ParleyUnderground** | Social / Streetwise | Black-market cant, contraband marks, dealing with hidden exiles. |
| **FixArcaneCircuits** | Arcane / Technical | Re-soldering yellow-metal runic traces and grounding magical runoff. |
| **DetectArcaneEnergy** | Arcane / Perception | Sensing inversion frequencies, reality-leak static, and divine resonance. |
| **HackCircuits** | Arcane / Technical | Overclocking relays, shunting energy pathways, bypassing runic wards. |
| **RecallArtifactLore** | Arcane / Scholarly | Pre-inversion precursor symbols, ancient mechanism languages, relic history. |

### Google Sheets Copy-Paste Block (Skills)
```tsv
SkillName	DisciplineDomain	CoreFocus
LiftHeavyObjects	Physical / Athletics	Raw muscular mass, foot bracing, and brute structural leverage.
Smash	Physical / Kinetic	Impact force, striking welds, kinetic shock damage.
Climb	Physical / Agility	Traversing sheer pipe-work, suspension catwalks, and vertical shafts.
SenseDanger	Physical / Awareness	Reflexive awareness of industrial hazards, tripwires, and environmental traps.
FixMachinery	Craft / Engineering	Diagnosing frozen joints, high-pressure steam pipes, and seized gears.
Tinker	Craft / Precision	Delicate calibration, fine handcraft, tool leverage, and component isolation.
OperateMachinery	Craft / Engineering	Understanding industrial flow-valves, gauges, diesel pumps, and consoles.
PickLocks	Subterfuge / Precision	Tension wrench mechanics, tumbler manipulation, bypassing cylinder locks.
Sneak	Subterfuge / Stealth	Acoustic discipline, shadow blending, and patrol avoidance.
Deceive	Social / Guile	Vocal confidence, delivering fabricated alibis, misdirection.
ReadIntentions	Social / Perception	Spotting micro-expressions, detecting deceit, anticipating reactions.
NavigateBureaucracy	Social / Protocol	Citing regulations, exploiting red tape, inspecting documentation.
ParleyGuards	Social / Authority	Enforcer psychology, de-escalating beat cops, military protocol.
ParleyUnderground	Social / Streetwise	Black-market cant, contraband marks, dealing with hidden exiles.
FixArcaneCircuits	Arcane / Technical	Re-soldering yellow-metal runic traces and grounding magical runoff.
DetectArcaneEnergy	Arcane / Perception	Sensing inversion frequencies, reality-leak static, and divine resonance.
HackCircuits	Arcane / Technical	Overclocking relays, shunting energy pathways, bypassing runic wards.
RecallArtifactLore	Arcane / Scholarly	Pre-inversion precursor symbols, ancient mechanism languages, relic history.
```

---

## 3. Actions Sheet (The 7 Player Verbs)

*Matches schema: `ActionId | DisplayName | ApplicableSkills | RequiredPerk | PerkMode | SuccessQuips | FailureQuips`*

### Visual Table
| ActionId | DisplayName | ApplicableSkills | RequiredPerk | PerkMode | SuccessQuips | FailureQuips |
| :--- | :--- | :--- | :--- | :--- | :--- | :--- |
| **Pry** | Wedge and lever open | LiftHeavyObjects, FixMachinery, Tinker, Smash | Spanner | ShownWhenLocked | Leverage pops the fused seam loose with a sharp metallic report. ; You find the weak seam and heave until the metal yields. | The prybar slips with a shower of rust, failing to gain purchase on the seized seam. ; You lack the leverage to shift the fused metal. |
| **Force** | Force by raw muscle | LiftHeavyObjects, Smash | None | None | Straining your muscles to the limit, you physically heave the mechanism into motion. ; Raw bodily momentum overcomes the dead weight. | Your grip slips under the dead weight, leaving the obstruction completely unmoved. ; Your muscles burn and seize, unable to overcome the static friction. |
| **Tinker** | Calibrate and disassemble | Tinker, FixMachinery, OperateMachinery | InspectionKit | ShownWhenLocked | With a few precise adjustments to the housing, you bypass the seized component. ; You isolate the jammed pressure collar and ease it open. | A delicate internal pin binds under tension, jamming the works tighter than before. ; You fail to balance the fine mechanical tolerances. |
| **Lockpick** | Pick internal tumblers | PickLocks, Tinker | Lockpicks | ShownWhenLocked | The internal tumblers fall into alignment with a crisp, oily click. ; A delicate twist of the tension wire pops the latch free. | The pick binds against the corroded cylinder and refuses to turn. ; The tension tool slips with an audible scratch. |
| **Sneak** | Infiltrate through blind spots | Sneak, Climb, SenseDanger | None | None | You slip through the shadows and cover without making a sound. ; You time your movement perfectly across the observer's blind angle. | A sudden scrape of loose metal gives away your position and draws watchful eyes. ; An unexpected shift in lighting illuminates your silhouette. |
| **Decipher** | Analyze runes or logs | NavigateBureaucracy, RecallArtifactLore, HackCircuits, DetectArcaneEnergy | None | None | The convoluted symbols resolve into clear, readable meaning. ; You deduce the underlying pattern and extract the hidden data. | The text dissolves into a dizzying blur of nonsensical symbols and headache-inducing static. ; The protective cipher locks out the remaining information. |
| **Parley** | Defuse through dialogue | Deceive, NavigateBureaucracy, ParleyGuards, ReadIntentions | None | None | You deliver your words with calm authority, completely disarming their suspicion. ; You feed them a plausible alibi that eases all immediate tension. | They catch the hesitation in your voice and refuse to take your story at face value. ; Your bluff collapses under basic scrutiny. |

### Google Sheets Copy-Paste Block (Actions)
```tsv
ActionId	DisplayName	ApplicableSkills	RequiredPerk	PerkMode	SuccessQuips	FailureQuips
Pry	Wedge and lever open	LiftHeavyObjects, FixMachinery, Tinker, Smash	Spanner	ShownWhenLocked	Leverage pops the fused seam loose with a sharp metallic report. ; You find the weak seam and heave until the metal yields.	The prybar slips with a shower of rust, failing to gain purchase on the seized seam. ; You lack the leverage to shift the fused metal.
Force	Force by raw muscle	LiftHeavyObjects, Smash	None	None	Straining your muscles to the limit, you physically heave the mechanism into motion. ; Raw bodily momentum overcomes the dead weight.	Your grip slips under the dead weight, leaving the obstruction completely unmoved. ; Your muscles burn and seize, unable to overcome the static friction.
Tinker	Calibrate and disassemble	Tinker, FixMachinery, OperateMachinery	InspectionKit	ShownWhenLocked	With a few precise adjustments to the housing, you bypass the seized component. ; You isolate the jammed pressure collar and ease it open.	A delicate internal pin binds under tension, jamming the works tighter than before. ; You fail to balance the fine mechanical tolerances.
Lockpick	Pick internal tumblers	PickLocks, Tinker	Lockpicks	ShownWhenLocked	The internal tumblers fall into alignment with a crisp, oily click. ; A delicate twist of the tension wire pops the latch free.	The pick binds against the corroded cylinder and refuses to turn. ; The tension tool slips with an audible scratch.
Sneak	Infiltrate through blind spots	Sneak, Climb, SenseDanger	None	None	You slip through the shadows and cover without making a sound. ; You time your movement perfectly across the observer's blind angle.	A sudden scrape of loose metal gives away your position and draws watchful eyes. ; An unexpected shift in lighting illuminates your silhouette.
Decipher	Analyze runes or logs	NavigateBureaucracy, RecallArtifactLore, HackCircuits, DetectArcaneEnergy	None	None	The convoluted symbols resolve into clear, readable meaning. ; You deduce the underlying pattern and extract the hidden data.	The text dissolves into a dizzying blur of nonsensical symbols and headache-inducing static. ; The protective cipher locks out the remaining information.
Parley	Defuse through dialogue	Deceive, NavigateBureaucracy, ParleyGuards, ReadIntentions	None	None	You deliver your words with calm authority, completely disarming their suspicion. ; You feed them a plausible alibi that eases all immediate tension.	They catch the hesitation in your voice and refuse to take your story at face value. ; Your bluff collapses under basic scrutiny.
```

---

## 4. Problem Archetypes Sheet (Modular Obstacles)

*Matches schema: `ArchetypeId | Generic Obstacle Description | Allowed Actions & Level Offsets`*

### Visual Table
| ArchetypeId | Generic Obstacle Description | Allowed Actions & Level Offsets |
| :--- | :--- | :--- |
| **SeizedValve** | An industrial shutoff wheel, pressure regulator, or fluid valve fused solid by corrosion, oxidation, or mineral deposits. | `Pry:0, Tinker:-1, Force:+1` |
| **SecurityGate** | A reinforced portcullis, barred pneumatic gate, or security checkpoint restricting physical access. | `Sneak:0, Lockpick:0, Pry:+1` |
| **ClassifiedRecord** | An encrypted document, sealed manifest, coded ledger, or locked data cylinder. | `Decipher:0, Tinker:-1, Parley:+1` |
| **RunicFlicker** | A damaged arcane circuit, etched golden trace, or conduit junction leaking unstable reality-warping energy. | `Tinker:0, Decipher:-1, Force:+2` |
| **SuspiciousSentry** | An armed guard, inspector, or watchful sentry challenging intruders and inspecting authorization. | `Parley:0, Sneak:-1, Force:+1` |

### Google Sheets Copy-Paste Block (Problem Archetypes)
```tsv
ArchetypeId	Generic Obstacle Description	Allowed Actions & Level Offsets
SeizedValve	An industrial shutoff wheel, pressure regulator, or fluid valve fused solid by corrosion, oxidation, or mineral deposits.	Pry:0, Tinker:-1, Force:+1
SecurityGate	A reinforced portcullis, barred pneumatic gate, or security checkpoint restricting physical access.	Sneak:0, Lockpick:0, Pry:+1
ClassifiedRecord	An encrypted document, sealed manifest, coded ledger, or locked data cylinder.	Decipher:0, Tinker:-1, Parley:+1
RunicFlicker	A damaged arcane circuit, etched golden trace, or conduit junction leaking unstable reality-warping energy.	Tinker:0, Decipher:-1, Force:+2
SuspiciousSentry	An armed guard, inspector, or watchful sentry challenging intruders and inspecting authorization.	Parley:0, Sneak:-1, Force:+1
```
