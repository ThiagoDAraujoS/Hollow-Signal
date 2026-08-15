// VATMaterialState.cs
// Author: Luke Stilson

using UnityEngine;

public struct VATMaterialState : System.IEquatable<VATMaterialState>
{
    // CPU-time (debug / optional path for brute-force timing)
    public int frameIndexA, nextFrameA, frameIndexB, nextFrameB;
    public float interpA, interpB;

    // Shared (Needed for GPU / CPU handling)
    public int frameStartA, frameEndA, frameStartB, frameEndB;

    // GPU-time 
    public bool useGpuTimeline; // toggles shader to use time-based path
    public float fpsA, fpsB;     // from anim data (framerate)
    public float loopA, loopB;   // 1 = looping, 0 = clamp

    // Blend timing (GPU)
    public float blend;              // CPU blend factor (currently unused in GPU, kept for future implementation)
    public float blendStartTime;     // absolute time when transition started
    public float blendDuration;      // seconds
    public float blendDirection;     // 1 = A->B, 0 = B->A

    // Per-track start time offsets (used as timers in shader)
    public float timeOffsetA, timeOffsetB;

    public float seqStartA, seqStartB;
    public float useSeqA, useSeqB;

    public bool Equals(VATMaterialState other)
    {
        const float EPS = 0.0001f;

        return
            // CPU-time
            frameIndexA == other.frameIndexA &&
            nextFrameA == other.nextFrameA &&
            Mathf.Abs(interpA - other.interpA) < EPS &&
            frameIndexB == other.frameIndexB &&
            nextFrameB == other.nextFrameB &&
            Mathf.Abs(interpB - other.interpB) < EPS &&

            // Shared (both paths)
            frameStartA == other.frameStartA &&
            frameEndA == other.frameEndA &&
            frameStartB == other.frameStartB &&
            frameEndB == other.frameEndB &&

            // GPU-time
            useGpuTimeline == other.useGpuTimeline &&
            Mathf.Abs(fpsA - other.fpsA) < EPS &&
            Mathf.Abs(fpsB - other.fpsB) < EPS &&
            Mathf.Abs(loopA - other.loopA) < EPS &&
            Mathf.Abs(loopB - other.loopB) < EPS &&

            // Blend timing
            Mathf.Abs(blend - other.blend) < EPS &&
            Mathf.Abs(blendStartTime - other.blendStartTime) < EPS &&
            Mathf.Abs(blendDuration - other.blendDuration) < EPS &&
            Mathf.Abs(blendDirection - other.blendDirection) < EPS &&

            // Sequence toggles/starts
            Mathf.Abs(useSeqA - other.useSeqA) < EPS &&
            Mathf.Abs(useSeqB - other.useSeqB) < EPS &&
            Mathf.Abs(seqStartA - other.seqStartA) < EPS &&
            Mathf.Abs(seqStartB - other.seqStartB) < EPS;
    }
}
