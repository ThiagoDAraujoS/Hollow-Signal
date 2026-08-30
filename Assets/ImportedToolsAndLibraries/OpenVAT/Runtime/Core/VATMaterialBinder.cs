// VATMaterialBinder.cs
// Author: Luke Stilson

using UnityEngine;

public sealed class VATMaterialBinder
{
    private readonly Renderer rend;
    private readonly MaterialPropertyBlock mpb;

    // Common toggles / timers
    private static readonly int TIME_OFFSET_A = Shader.PropertyToID("_TimeOffsetA");
    private static readonly int TIME_OFFSET_B = Shader.PropertyToID("_TimeOffsetB");

    // Per-track ranges
    private static readonly int FRAME_START_A = Shader.PropertyToID("_FrameStartA");
    private static readonly int FRAME_END_A = Shader.PropertyToID("_FrameEndA");
    private static readonly int FRAME_START_B = Shader.PropertyToID("_FrameStartB");
    private static readonly int FRAME_END_B = Shader.PropertyToID("_FrameEndB");

    // Per-track timing
    private static readonly int FPS_A = Shader.PropertyToID("_FPSA");
    private static readonly int FPS_B = Shader.PropertyToID("_FPSB");
    private static readonly int LOOP_A = Shader.PropertyToID("_LoopA");
    private static readonly int LOOP_B = Shader.PropertyToID("_LoopB");

    // CPU path (optional)
    private static readonly int FRAME_INDEX_A = Shader.PropertyToID("_FrameIndexA");
    private static readonly int FRAME_NEXT_A = Shader.PropertyToID("_FrameNextA");
    private static readonly int FRAME_INTERP_A = Shader.PropertyToID("_FrameInterpA");
    private static readonly int FRAME_INDEX_B = Shader.PropertyToID("_FrameIndexB");
    private static readonly int FRAME_NEXT_B = Shader.PropertyToID("_FrameNextB");
    private static readonly int FRAME_INTERP_B = Shader.PropertyToID("_FrameInterpB");

    // Blend control
    private static readonly int BLEND = Shader.PropertyToID("_Blend");
    private static readonly int BLEND_START = Shader.PropertyToID("_BlendStartTime");
    private static readonly int BLEND_DUR = Shader.PropertyToID("_BlendDuration");
    private static readonly int BLEND_DIR = Shader.PropertyToID("_BlendDirection");

    // NEW: Sequence controls
    private static readonly int SEQ_START_A = Shader.PropertyToID("_SeqStartA");
    private static readonly int SEQ_START_B = Shader.PropertyToID("_SeqStartB");
    private static readonly int USE_SEQ_A = Shader.PropertyToID("_UseSeqA");
    private static readonly int USE_SEQ_B = Shader.PropertyToID("_UseSeqB");

    public VATMaterialBinder(Renderer r)
    {
        rend = r;
        mpb = new MaterialPropertyBlock();
    }

    public void ApplyState(VATMaterialState s)
    {
        mpb.Clear();

        // Per-track ranges (driven by the active clip OR last clip in a sequence)
        mpb.SetInt(FRAME_START_A, s.frameStartA);
        mpb.SetInt(FRAME_END_A, s.frameEndA);
        mpb.SetInt(FRAME_START_B, s.frameStartB);
        mpb.SetInt(FRAME_END_B, s.frameEndB);

        // Per-track timing
        mpb.SetFloat(FPS_A, s.fpsA);
        mpb.SetFloat(LOOP_A, s.loopA);
        mpb.SetFloat(FPS_B, s.fpsB);
        mpb.SetFloat(LOOP_B, s.loopB);

        // Per-track time offsets (GPU uses these as timers)
        mpb.SetFloat(TIME_OFFSET_A, s.timeOffsetA);
        mpb.SetFloat(TIME_OFFSET_B, s.timeOffsetB);

        // Blend timing (GPU tracks A-B)
        mpb.SetFloat(BLEND_START, s.blendStartTime);
        mpb.SetFloat(BLEND_DUR, s.blendDuration);
        mpb.SetFloat(BLEND_DIR, s.blendDirection);

        // Optional CPU path/debug (noop for GPU timing, safe to keep)
        mpb.SetInt(FRAME_INDEX_A, s.frameIndexA);
        mpb.SetInt(FRAME_NEXT_A, s.nextFrameA);
        mpb.SetFloat(FRAME_INTERP_A, s.interpA);
        mpb.SetInt(FRAME_INDEX_B, s.frameIndexB);
        mpb.SetInt(FRAME_NEXT_B, s.nextFrameB);
        mpb.SetFloat(FRAME_INTERP_B, s.interpB);

        // CPU blend factor (if your shader still reads it anywhere)
        mpb.SetFloat(BLEND, s.blend);

        // Sequence controls
        // UseSeq are 0/1 floats that gate the sequence lerp in shader
        mpb.SetFloat(USE_SEQ_A, s.useSeqA);
        mpb.SetFloat(USE_SEQ_B, s.useSeqB);
        // SeqStart are absolute frame indices (first frame of FIRST clip in sequence)
        mpb.SetFloat(SEQ_START_A, s.seqStartA);
        mpb.SetFloat(SEQ_START_B, s.seqStartB);

        rend.SetPropertyBlock(mpb);
    }
}
