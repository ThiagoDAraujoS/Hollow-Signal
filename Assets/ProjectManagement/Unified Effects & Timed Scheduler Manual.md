# Unified Effects & Timed Scheduler Architecture

## 1. Overview & Core Philosophy

In *Hollow Signal*, actions, usable items, tactical abilities, environmental hazards, and timed buffs/debuffs all share a single execution pipeline: **The Unified Effect Cluster**.

Instead of writing fragmented logic across items, spells, and timers, everything is composed of atomic ScriptableObject nodes (**`EffectNode`**) executed through a shared context packet (**`EffectContext`**). Delayed actions are not coroutines or per-component update loops; they are managed globally by a high-performance **Min-Heap Priority Queue** on `CrisisManager` that synchronizes directly with the save system via the `Blackboard`.

---

## 2. Core Architecture

```
                          ┌────────────────────────┐
                          │     Item / Action      │
                          │   (e.g., Stamina Pot)  │
                          └───────────┬────────────┘
                                      │ Invokes Run(context)
                                      ▼
                        ┌────────────────────────────┐
                        │   CompositeEffect (Root)   │
                        └─────┬────────────────┬─────┘
                              │                │
             ┌────────────────┘                └─────────────────┐
             ▼                                                   ▼
┌──────────────────────────┐                        ┌──────────────────────────┐
│       HealEffect         │                        │      ScheduleEffect      │
│  (Restores 5 Flesh)      │                        │     (Delay: 10 mins)     │
└──────────────────────────┘                        └────────────┬─────────────┘
                                                                 │ Enqueues
                                                                 ▼
                                                    ┌──────────────────────────┐
                                                    │     CrisisScheduler      │
                                                    │  (Binary Min-Heap Queue) │
                                                    └────────────┬─────────────┘
                                                                 │ Pops on due time
                                                                 ▼
                                                    ┌──────────────────────────┐
                                                    │  RemoveConditionEffect   │
                                                    │   (Cleanses Adrenaline)  │
                                                    └──────────────────────────┘
```

### 2.1 The Data Packet: `EffectContext`
- **Location**: `Assets/Scripts/Data/Effects/EffectContext.cs`
- **Role**: Pure context carrier. It stores the live reference to the target [`Sheet`](file:///C:/Users/Thiago/Desktop/Personal%20Projects/Horror%20Room/Hollow%20Signal/Hollow%20Signal/Assets/Scripts/World/Actors/Sheet.cs) and automatically caches the persistent `targetId` from `UniqueId`.
- **Targeting**: Anchored to `Sheet` (the master base class for `CharacterSheet` and upcoming enemies/NPCs).
- **Zero Query Overhead**: Leaf effects access `context.target` or `context.Character` without running runtime `GetComponent` queries.
- **Dynamic Payload**: Exposes indexer `context["key"] = value` for arbitrary parameter passing.

### 2.2 The Executable Building Block: `EffectNode`
- **Location**: `Assets/Scripts/Data/Effects/EffectNode.cs`
- **Role**: Abstract ScriptableObject with an `Id` string and `Run(EffectContext context)`.
- **Preloaded Registry (`EffectDatabase`)**: Maps string `Id` $\rightarrow$ `EffectNode` asset for fast $O(1)$ lookups during deserialization.

### 2.3 The Composite Branch: `CompositeEffect`
- **Location**: `Assets/Scripts/Data/Effects/CompositeEffect.cs`
- **Role**: Holds a list of `EffectNode` children. When executed, it passes the same `EffectContext` through each child sequentially.

### 2.4 The Timer Node: `ScheduleEffect`
- **Location**: `Assets/Scripts/Data/Effects/ScheduleEffect.cs`
- **Role**: Calculates `executionTime = ElapsedWorldTime + (durationMinutes * 60f)` and inserts a `ScheduledEntry` into the `CrisisScheduler`.
- **Time Invariance**: In exploration, time advances by `Time.deltaTime`. In Crisis combat, each round advances the clock by 60 seconds (1 minute per round).

### 2.5 The Priority Queue: `CrisisScheduler`
- **Location**: `Assets/Scripts/Core/Crisis/CrisisScheduler.cs`
- **Structure**: Binary Min-Heap backed by a `List<ScheduledEntry>`:
  - $O(1)$ peek: Checks if the earliest event is due.
  - $O(\log n)$ enqueue and dequeue.
  - $O(n)$ heap rebuild upon game load.
- **Blackboard Persistence**: Implements `ITracked`. Serializes all scheduled entries into a list of dictionaries in the save partition.

---

## 3. The "Saved Game" Lifecycle (`ResolveTargetReference`)

Because save files cannot persist live C# object pointers in JSON, rehydrating scheduled events requires special handling:

1. **During Live Gameplay**:
   `entry.context.target` directly references the live `Sheet` in memory.
2. **On Game Save**:
   Only primitive data is saved (`time`, `effect_id`, `target_id`).
3. **On Game Load**:
   `CrisisScheduler.Load()` reconstructs `ScheduledEntry` instances with `target = null`.
4. **On Execution (`ResolveTargetReference`)**:
   ```csharp
   private void ResolveTargetReference(ScheduledEntry entry){
       if (entry.context.target != null || string.IsNullOrEmpty(entry.targetId)) return;
       foreach (BlackboardClient client in BlackboardClient.ActiveClients)
           if (client.EntityId == entry.targetId)
               entry.context.target = client.GetComponent<Sheet>();
   }
   ```
   If `target` is already assigned (live session), it exits instantly ($O(1)$).
   If `target` is null (reloaded save), it scans `BlackboardClient.ActiveClients` to re-link the live scene `Sheet` before `Run()` executes.

---

## 4. Setup Guide: Authoring a 10-Minute Buff Item

Here is the exact step-by-step procedure to create a consumable item that restores 5 Flesh and grants a temporary 10-minute Mastery buff:

### Step 1: Create the Leaf Effects
1. **Heal Effect**:
   - Right-click Project window $\rightarrow$ **Create $\rightarrow$ CRPG $\rightarrow$ Effects $\rightarrow$ Leaves $\rightarrow$ Heal Effect**.
   - Name it `Effect_Heal_5_Flesh`.
   - Set **Pool**: `Flesh`, **Amount**: `5`.
2. **Apply Condition Effect**:
   - Right-click $\rightarrow$ **Create $\rightarrow$ CRPG $\rightarrow$ Effects $\rightarrow$ Leaves $\rightarrow$ Apply Condition Effect**.
   - Name it `Effect_Apply_Adrenaline`.
   - Assign the `Adrenaline` Mastery asset to the **Condition** slot.
3. **Remove Condition Effect**:
   - Right-click $\rightarrow$ **Create $\rightarrow$ CRPG $\rightarrow$ Effects $\rightarrow$ Leaves $\rightarrow$ Remove Condition Effect**.
   - Name it `Effect_Remove_Adrenaline`.
   - Assign **Id**: `"remove_adrenaline"` *(critical for save reload lookup)*.
   - Assign the `Adrenaline` Mastery asset to the **Condition** slot.

### Step 2: Create the Schedule Node
1. Right-click $\rightarrow$ **Create $\rightarrow$ CRPG $\rightarrow$ Effects $\rightarrow$ Schedule Effect**.
2. Name it `Effect_Schedule_Remove_Adrenaline_10m`.
3. Set **Duration Minutes**: `10`.
4. Set **Effect To Schedule**: Drag `Effect_Remove_Adrenaline` here.

### Step 3: Assemble the Composite Cluster
1. Right-click $\rightarrow$ **Create $\rightarrow$ CRPG $\rightarrow$ Effects $\rightarrow$ Composite Effect**.
2. Name it `Effect_Item_AdrenalineShot_Cluster`.
3. In the **Children** list, add 3 entries in this exact order:
   - Index 0: `Effect_Heal_5_Flesh`
   - Index 1: `Effect_Apply_Adrenaline`
   - Index 2: `Effect_Schedule_Remove_Adrenaline_10m`

### Step 4: Register with EffectDatabase
1. Select the project's `EffectDatabase` asset.
2. Add `Effect_Remove_Adrenaline` to the **Effects** list.
   *(Any effect that can be triggered from the scheduler after loading a save must have its ID in this database).*

### Step 5: Assign to Item
1. Open or create an `ItemDefinition` asset (e.g. `Item_AdrenalineShot`).
2. Ensure **Is Consumable** is checked.
3. Drag `Effect_Item_AdrenalineShot_Cluster` into the **On Use Effect** slot.

---

## 5. Safe Rooms, Antidotes & Event Cancellation

When a party rests at a safe room or takes a cleansing antidote:
- Call `CharacterSheet.RestAndRecover()`.
- Under the hood, this executes:
  ```csharp
  temporaryConditions.Clear();
  composure.HealAll();
  CrisisManager.Instance.Scheduler.CancelForTarget(GetComponent<UniqueId>().Id);
  RebuildAllSkills();
  ```
- All pending scheduled entries targeting that character in the Min-Heap are stripped, and the heap is restored immediately.
