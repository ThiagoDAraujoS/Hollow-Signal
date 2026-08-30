// VATAnimStateMachine.cs
// Author: Luke Stilson

using System.Collections.Generic;
using UnityEngine;

public class VATAnimStateMachine
{
    // Animation data
    private List<VATAnimationData.VATAnimation> anims;
    private int animAIndex = -1, animBIndex = -1;
    private bool usingA = true;

    // Per-track frame state (CPU-visible but not necessarily used by GPU path)
    private int frameIndexA, frameIndexB;
    private float interpA, interpB;
    private int nextFrameA, nextFrameB;

    // Shader/GPU timing & blend bookkeeping
    private float blendTimer, blendDuration;
    private bool blendForward = true;
    private float blendStartTimeAbs = 0f;
    private bool isInitialized = false;
    private float timeOffsetA = 0f;
    private float timeOffsetB = 0f;

    // Sequence outputs for the shader
    private float seqStartA = -1f, seqStartB = -1f; // first frame of FIRST clip in sequence
    private float useSeqA = 0f, useSeqB = 0f;       // 0/1 toggles per track

    private VATMaterialState lastMaterialState;

    // ------------ INIT/CORE -----------
    public void Initialize(List<VATAnimationData.VATAnimation> anims, int startIndex)
    {
        if (isInitialized) return; // guard multi-init
        this.anims = anims;
        if (anims == null || anims.Count == 0) return;

        animAIndex = Mathf.Clamp(startIndex, 0, anims.Count - 1);
        SetTrackState(toA: true, index: animAIndex, startFrame: anims[animAIndex].frameStart);

        animBIndex = animAIndex;
        SetTrackState(toA: false, index: animBIndex, startFrame: anims[animBIndex].frameStart);

        // Clear any sequence state
        useSeqA = 0f; useSeqB = 0f;
        seqStartA = -1f; seqStartB = -1f;

        blendDuration = 0f;
        blendTimer = 0f;
        blendStartTimeAbs = Time.time;
        usingA = true;
        blendForward = true;
        isInitialized = true;
    }

    // ----------- PLAY METHODS ------------
    public void PlayIndex(int index, float transitionTime = 0.25f)
    {
        if (anims == null || index < 0 || index >= anims.Count) return;
        if (!isInitialized) { Initialize(anims, index); return; }

        // Clear finished blend (safety)
        if (blendDuration > 0f && blendTimer >= blendDuration)
        {
            blendDuration = 0f;
            blendTimer = 0f;
        }

        // Flip destination track
        usingA = !usingA;

        if (transitionTime <= 0f)
        {
            if (usingA) SetTrackState(true, index, anims[index].frameStart);
            else SetTrackState(false, index, anims[index].frameStart);

            blendDuration = 0f;
            blendTimer = 0f;
            blendStartTimeAbs = Time.time;
            return;
        }

        if (usingA)
        {
            animBIndex = index;
            SetTrackState(false, animBIndex, anims[animBIndex].frameStart);
            blendForward = true;  // A -> B
        }
        else
        {
            animAIndex = index;
            SetTrackState(true, animAIndex, anims[animAIndex].frameStart);
            blendForward = false; // B -> A
        }

        blendDuration = transitionTime;
        blendTimer = 0f;
        blendStartTimeAbs = Time.time;
    }

    public void Play(string name, float transitionTime = 0.25f)
    {
        int index = anims?.FindIndex(a => a.name == name) ?? -1;
        if (index < 0) return;
        PlayIndex(index, transitionTime);
    }

    public void PlayIndexRandomStart(int index, float transitionTime = 0.25f)
    {
        if (anims == null || index < 0 || index >= anims.Count) return;
        var anim = anims[index];
        int randomFrame = Random.Range(anim.frameStart, anim.frameEnd + 1);

        if (!isInitialized)
        {
            Initialize(anims, index);
            SetTrackState(true, index, randomFrame);
            SetTrackState(false, index, randomFrame);
            return;
        }

        usingA = !usingA;

        if (transitionTime <= 0f)
        {
            if (usingA) SetTrackState(true, index, randomFrame);
            else SetTrackState(false, index, randomFrame);

            blendDuration = 0f;
            blendTimer = 0f;
            blendStartTimeAbs = Time.time;
            return;
        }

        if (usingA)
        {
            animBIndex = index;
            SetTrackState(false, animBIndex, randomFrame);
            blendForward = true;
        }
        else
        {
            animAIndex = index;
            SetTrackState(true, animAIndex, randomFrame);
            blendForward = false;
        }

        blendDuration = transitionTime;
        blendTimer = 0f;
        blendStartTimeAbs = Time.time;
    }

    // ------- GPU-driven sequence handling ----------
    public void PlaySequence(int minIndex, int maxIndex, float stepTransition = 0.25f, bool _loopIgnored = false, float initialTransition = 0f)
    {
        if (anims == null || minIndex < 0 || maxIndex >= anims.Count || minIndex > maxIndex) return;

        int firstIndex = minIndex;
        int lastIndex = maxIndex;

        int firstStartFrame = anims[firstIndex].frameStart; // SeqStartX
        int lastStartFrame = anims[lastIndex].frameStart;  // frameStartX (range and timing come from last clip)

        // Flip destination track like PlayIndex
        usingA = !usingA;

        if (initialTransition <= 0f)
        {
            if (usingA)
            {
                // Immediate on A
                SetSequenceOnTrackA(lastIndex, firstStartFrame, lastStartFrame);
            }
            else
            {
                // Immediate on B
                SetSequenceOnTrackB(lastIndex, firstStartFrame, lastStartFrame);
            }

            blendDuration = 0f;
            blendTimer = 0f;
            blendStartTimeAbs = Time.time;
            return;
        }

        // Crossfade path: put the sequence on the opposite track and blend toward it
        if (usingA)
        {
            animBIndex = lastIndex;
            SetSequenceOnTrackB(lastIndex, firstStartFrame, lastStartFrame);
            blendForward = true;   // A -> B
        }
        else
        {
            animAIndex = lastIndex;
            SetSequenceOnTrackA(lastIndex, firstStartFrame, lastStartFrame);
            blendForward = false;  // B -> A
        }

        blendDuration = stepTransition;
        blendTimer = 0f;
        blendStartTimeAbs = Time.time;
    }

    public void PlayNext(float transitionTime = 0.25f)
    {
        if (anims == null || anims.Count == 0) return;
        int current = usingA ? animAIndex : animBIndex;
        int next = (current + 1) % anims.Count;
        PlayIndex(next, transitionTime);
    }


    // Make sure unused/irrelevant data is not being handled here
    public VATMaterialState UpdateAndGetState(float deltaTime)
    {
        var animA = anims[animAIndex];
        var animB = (animBIndex >= 0) ? anims[animBIndex] : animA;

        lastMaterialState = new VATMaterialState
        {
            // CPU frame (debug/optional)
            frameIndexA = frameIndexA,
            nextFrameA = nextFrameA,
            interpA = interpA,
            frameIndexB = frameIndexB,
            nextFrameB = nextFrameB,
            interpB = interpB,

            // Shared frame bounds (per-track)
            frameStartA = animA.frameStart,
            frameEndA = animA.frameEnd,
            frameStartB = animB.frameStart,
            frameEndB = animB.frameEnd,

            // GPU timeline params (per-track)
            fpsA = animA.framerate,
            fpsB = animB.framerate,
            loopA = animA.looping ? 1f : 0f,
            loopB = animB.looping ? 1f : 0f,

            // GPU blend timing
            blendStartTime = blendStartTimeAbs,
            blendDuration = blendDuration,
            blendDirection = blendForward ? 1f : 0f,

            // Per-track start time offsets (used as timers in shader)
            timeOffsetA = timeOffsetA,
            timeOffsetB = timeOffsetB,

            // Sequence controls for the shader
            seqStartA = seqStartA,
            seqStartB = seqStartB,
            useSeqA = useSeqA,
            useSeqB = useSeqB,
        };

        return lastMaterialState;
    }

    // ----------- Helpers -----------
    private void SetTrackState(bool toA, int index, int startFrame)
    {
        if (toA)
        {
            animAIndex = index;
            frameIndexA = startFrame;
            interpA = 0f;
            nextFrameA = frameIndexA + 1;
            timeOffsetA = Time.time;

            // Clear sequence on the track we're explicitly driving by frames
            useSeqA = 0f;
            seqStartA = -1f;
        }
        else
        {
            animBIndex = index;
            frameIndexB = startFrame;
            interpB = 0f;
            nextFrameB = frameIndexB + 1;
            timeOffsetB = Time.time;

            useSeqB = 0f;
            seqStartB = -1f;
        }
    }

    private void SetSequenceOnTrackA(int lastIndex, int seqStartFrame, int lastStartFrame)
    {
        animAIndex = lastIndex;

        // CPU-visible fields (not used by GPU for sequencing but kept consistent)
        frameIndexA = lastStartFrame;
        interpA = 0f;
        nextFrameA = frameIndexA + 1;
        timeOffsetA = Time.time;

        // Sequence outputs
        seqStartA = seqStartFrame; // first frame of FIRST clip
        useSeqA = 1f;

        // The other track is not sequencing
        useSeqB = 0f;
        // (leave seqStartB as-is. Shader should ignore when useSeqB == 0)
    }

    private void SetSequenceOnTrackB(int lastIndex, int seqStartFrame, int lastStartFrame)
    {
        animBIndex = lastIndex;

        frameIndexB = lastStartFrame;
        interpB = 0f;
        nextFrameB = frameIndexB + 1;
        timeOffsetB = Time.time;

        seqStartB = seqStartFrame;
        useSeqB = 1f;

        useSeqA = 0f;
    }
}
