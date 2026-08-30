// VATController.cs
// Author: Luke Stilson

using System.Collections.Generic;
using UnityEngine;

[AddComponentMenu("OpenVAT/VAT Controller")]
[RequireComponent(typeof(Renderer))]
public class VATController : MonoBehaviour
{
    [Header("Playback Mode")]
    private bool useGpuTimeline = true;
    public enum PlayInitMode { Single, Sequence }

    [Header("Animation Data")]
    public VATAnimationData animationData;

    [Header("Init Playback")]
    public PlayInitMode playInitMode = PlayInitMode.Single;

    public int singleAnimIndex = 0;
    public int seqStartIndex = 0;
    public int seqEndIndex = 0;
    public float seqTransition = 0.25f;
    public bool seqLoop = false;

    private VATAnimStateMachine animState;
    private VATMaterialBinder binder;
    private VATMaterialState _lastState;

    // Expose the animation list in editor
    public List<VATAnimationData.VATAnimation> Anims => animationData ? animationData.animations : null;

    void OnEnable()
    {
        // Recreate state machine every enable so we get fresh timers/offsets. (Check garbage collection with this?)
        animState = new VATAnimStateMachine();

        if (binder == null)
            binder = new VATMaterialBinder(GetComponent<Renderer>());

        InitPlayback();
    }

    private void InitPlayback()
    {
        _lastState = default;

        var anims = Anims;
        if (animationData == null || anims == null || anims.Count == 0)
            return;

        // Initialize the machine to a known start clip
        int start = Mathf.Clamp(singleAnimIndex, 0, anims.Count - 1);
        animState.Initialize(anims, start);

        // Apply init play mode immediately (no initial blend)
        switch (playInitMode)
        {
            case PlayInitMode.Single:
                // already initialized on 'start' — nothing else to do
                break;

            case PlayInitMode.Sequence:
                seqStartIndex = Mathf.Clamp(seqStartIndex, 0, anims.Count - 1);
                seqEndIndex = Mathf.Clamp(seqEndIndex, 0, anims.Count - 1);
                // zero initial transition to guarantee a deterministic first frame
                animState.PlaySequence(seqStartIndex, seqEndIndex, seqTransition, seqLoop, 0f);
                break;
        }

        // Force-push initial material state without waiting a frame
        ApplyState(force: true);
    }

    private void ApplyState(bool force = false)
    {
        // Use 0 delta on the init tick so no blend progress occurs.
        var state = animState.UpdateAndGetState(0f);
        state.useGpuTimeline = useGpuTimeline;

        if (force || !_lastState.Equals(state))
        {
            binder.ApplyState(state);
            _lastState = state;
        }
    }

    // Public API — triggers still work the same, and we push immediately.
    public void PlayIndex(int index, float transitionTime = 0.25f)
    {
        animState.PlayIndex(index, transitionTime);
        ApplyState(force: false);
    }

    public void Play(string name, float transitionTime = 0.25f)
    {
        animState.Play(name, transitionTime);
        ApplyState(force: false);
    }

    public void PlayInstant(int index)
    {
        animState.PlayIndex(index, 0.25f);
        ApplyState(force: false);
    }

    public void PlaySequence(int minIndex, int maxIndex, float transitionTime = 0.25f)
    {
        animState.PlaySequence(minIndex, maxIndex, transitionTime);
        ApplyState(force: false);
    }
}
