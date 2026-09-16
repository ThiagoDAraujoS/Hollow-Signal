# Asynchronous Multi-Hero Dialogue Architecture

## 1. System Vision & The Baldur's Gate 3 Paradigm

In Hollow Signal, dialogue is not a global modal that freezes the game world. In both real-time exploration and **Crisis Mode (Turn-Based Tactical Combat)**, heroes act with individual agency:
- **Hero 1** can initiate a dialogue with a jammed door during their turn, pick a turn-consuming brute force action (`<T>`), and end their turn.
- While Hero 1's action is underway, **Hero 2** can move across the room and interrogate a hostile NPC or kick a villain.
- When the turn order cycles back to Hero 1, Hero 1's dialogue coroutine resumes, displays the dice roll and outcome quip, and allows Hero 1 to continue acting.

---

## 2. Window Architecture: Dedicated Window per Hero (Option A)

Instead of a single window that clears and overwrites text when swapping heroes, the system allocates **one dedicated `PRE_DialogUI` instance per party member**.

```
┌──────────────────────────────────────────────────────────┐
│                     DialogueManager                      │
│   • Listens to PlayerBrain.Selection.OnSelectionChanged  │
│   • Enforces: EXACTLY ONE window visible at any time     │
│   • Handles window switching on hero select / cycle      │
└────────────┬─────────────────────────────┬───────────────┘
             │                             │
    Binds Hero 1                  Binds Hero 2
             │                             │
             ▼                             ▼
┌─────────────────────────┐   ┌─────────────────────────┐
│     DialogueRunner      │   │     DialogueRunner      │
│  (Coroutine on Hero 1)  │   │  (Coroutine on Hero 2)  │
│                         │   │                         │
│ • State: Waiting Turn   │   │ • State: Active Choice  │
│ • Owns Window Instance 1│   │ • Owns Window Instance 2│
│ • Separate Transcript   │   │ • Separate Transcript   │
│ • Separate Scroll State │   │ • Separate Scroll State │
└─────────────────────────┘   └─────────────────────────┘
```

### Why Dedicated Windows?
1. **Zero State Rebuilding**: Switching between heroes is an instantaneous visibility toggle (`Show()` / `Hide()`). The UI does not need to serialize, parse, and rebuild rich-text layouts every time the player clicks between party portraits.
2. **Preserved Scroll Positions**: If Hero 1 scrolled up into the transcript to review an ancient inscription, switching to Hero 2 and back leaves Hero 1's scroll bar exactly where they left it.
3. **Clean Separation**: Transcript histories never interleave. Hero 1's lockpicking log remains purely on Hero 1's screen.

---

## 3. Coroutine-Based Dialogue Runners

Dialogue execution operates via asynchronous Unity Coroutines. A coroutine acts as a native state machine that pauses execution at choice gates and turn barriers.

### Turn-Yielding Lifecycle:
```csharp
IEnumerator RunDialogue(Character hero, DialogueNode startNode)
{
    DialogueNode current = startNode;

    while (current != null && current.knotId != DialogueNode.End)
    {
        // 1. Log prompt to this hero's transcript
        window.AppendPrompt(current.speakerId, current.textKey);

        // 2. Yield until the player clicks a choice
        DialogueChoice selectedChoice = null;
        yield return window.WaitForChoice(current.choices, choice => selectedChoice = choice);

        // 3. Turn-Ending Action (<T>): Yield until next hero turn!
        if (selectedChoice.EndsTurn)
        {
            window.Hide();
            TurnManager.ConsumeAction(hero);

            // Coroutine PAUSES HERE. Other heroes execute their turns freely.
            yield return TurnManager.WaitForTurn(hero);

            window.Show();
        }

        // 4. Resolve Problem Archetype (if applicable)
        if (selectedChoice.HasArchetypeCheck)
        {
            yield return window.DisplayResolution(hero, selectedChoice.Archetype);
        }

        // 5. Transition to next knot
        current = currentDialogue.GetNode(selectedChoice.targetKnot);
    }

    window.Hide();
}
```

---

## 4. Centralized Style Tokens & Dynamic UI Formatting

To decouple dialogue writers from UI artists, semantic tokens are written directly into dialogue scripts and resolved visually in [`DialogueOptionUI`](file:///C:/Users/Thiago/Desktop/Personal%20Projects/Horror%20Room/Hollow%20Signal/Hollow%20Signal/Assets/Scripts/UI/Dialog/DialogueOptionUI.cs):

| Token | Meaning | Inspector-Configured Style |
| :--- | :--- | :--- |
| `<T>` | Consumes turn / Ends turn | Colored with `turnCostColor` (e.g. Iron Gall Crimson `#7D1609`) |
| `[Brute Force]` | Physical Approach Tag | Colored with `approachColor` (e.g. Burnished Ochre `#854205`) |
| `(Rusted Mechanism)` | Target Archetype Hint | Subdued graphite/italic formatting |

- Writers write clean plaintext: `<T> [Brute Force] Break the door latch.`
- `DialogueOptionUI` parses the tokens and formats the TextMeshPro text at runtime.
- Designers tweak color palettes directly on the prefab without re-baking dialogue assets.

---

## 5. Window Animation State Transitions

Dialogue windows must not snap abruptly on/off:
- Window root objects contain an `Animator` controller.
- The `DialogueWindow` component communicates via state parameters:
  - `animator.SetBool("IsOpen", true)` triggers the slide-in transition.
  - `animator.SetBool("IsOpen", false)` triggers the slide-out transition.
- Coroutines yield on animation triggers before disabling interaction.
