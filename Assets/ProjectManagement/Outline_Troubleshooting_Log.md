# Outline System Troubleshooting Log

This log tracks every attempt, root cause hypothesis, action taken, and outcome so context is never lost across iterations.

---

## Log Entries

- **Try 01 (Initial Setup)**: Implemented `HighlightRegistry`, `InteractableHighlight`, `InteractableOutlineFeature`, and 2 shaders; hover worked but Unity 6 RenderGraph threw `InvalidOperationException` due to global state modification in `RasterCommandBuffer`.
- **Try 02 (Pass Restructure)**: Added `builder.AllowGlobalStateModification(true)` and structured `MaskPassData`/`CompositePassData`; resolved exceptions and feature ran in `Editor.log`, but silhouette mask was completely empty/invisible on screen.
- **Try 03 (Draw Call & Alpha Fix)**: Switched from `cmd.DrawRenderer` to `cmd.DrawMesh` with `MaterialPropertyBlock` and instanced `_SilhouetteColor`; outline still did not show up on screen.
- **Try 04 (Sampler & State Investigation)**: Added explicit `SamplerState` in outline shader; discovered DirectX render target Y-flip discrepancy between offscreen mask RT and active camera color.
- **Try 05 (Back-to-Front Baseline: Static Usable Layer RendererList)**: Success! Minimal native `RendererList` filtered by the `Usable` layer (Layer 10) rendered outlines visibly on screen.
- **Try 06 (Depth Occlusion Support)**: Bound `cameraDepthTexture` to `HighlightSilhouetteMask` with `ZTest LEqual`. Threw `Exception: Mismatch in number of MSAA samples when using resource '_HighlightMaskTexture'. Expected None but got 4 instead.`
- **Try 07 (MSAA Sample Match Fix)**: Copied `msaaSamples` from `cameraDepthTexture` to `_HighlightMaskTexture` descriptor so color attachment MSAA matches depth attachment MSAA (4x). Success! Depth occlusion with wall/door frame clipping works cleanly.
- **Try 08 (Dynamic Layer Swapping Component)**: Designated Layer 14 as `Highlight`. Implemented `InteractableHighlight.cs` to temporarily swap target `Renderer` GameObjects to Layer 14 on hover / Alt-reveal while preserving collider/root on `Usable`.
- **Try 09 (Invisible Interactable Zone Support)**: Added Layer 15 `HighlightInvisible`. Configured `InteractableHighlight` with an `isInvisibleZone` toggle.
- **Try 10 (Material-Based Invisibility & Unlit Support)**: Added `SRPDefaultUnlit` tag to `InteractableOutlineFeature.cs` so cheap URP Unlit materials work.
- **Try 11 (Simplification & Cleanup - KISS)**: Removed redundant `HighlightInvisible` layer and `isInvisibleZone` code. Since invisible zones naturally use transparent/alpha-0 Unlit materials, both visible and invisible interactables swap to the single `Highlight` layer (Layer 14).
