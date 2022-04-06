using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum WaveType { SINE, SQUARE, TRIANGLE };
public class Instrument : MonoBehaviour
{
    //TODO:

    //LFO and vibrato
    // return Mathf.Sin(p + (float)(0.5 * LFOfreq * Mathf.Sin((float)(LFOamp*2.0 * Math.PI / sampling_frequency))));

    //chorus
    // is this as simple as multiple copies of the sound itself?

    //reverb
    // unknown

    //direct EQ balancing. i think this uses some kind of inverse-FFT to do the following:
    // 1: use FFT to convert sound to frequency hist
    // 2: apply distortions to the frequency hist - some bars get lower, some higher
    // 3: apply inverse-FFT to convert based to sound

    //compression
    // how is this different from direct EQ balancing?

    //envelope settings
    public float attack;
    public float decay;
    public float sustain;
    public float release;

    // custome synth settings
    public float wave1freq = 0.9f;
    public float wave2freq = 0.7f;
    public float wave3freq = 0.4f;
    public float wave1Strength = 1f;
    public float wave2Strength = 1f;
    public float wave3Strength = 1f;
    public WaveType wave1type;
    public WaveType wave2type;
    public WaveType wave3type;

    public float CustomSynth(float x)
    {
        float wave1 = 0f;
        float wave2 = 0f;
        float wave3 = 0f;

        if (wave1type == WaveType.SINE) { wave1 = wave1Strength * Mathf.Sin(wave1freq * x); }
        else if (wave1type == WaveType.SQUARE) { wave1 = wave1Strength * Square(wave1freq * x); }
        else if (wave1type == WaveType.TRIANGLE) { wave1 = wave1Strength * Triangle(wave1freq * x); }

        if (wave2type == WaveType.SINE) { wave2 = wave2Strength * Mathf.Sin(wave2freq * x); }
        else if (wave2type == WaveType.SQUARE) { wave2 = wave2Strength * Square(wave2freq * x); }
        else if (wave2type == WaveType.TRIANGLE) { wave2 = wave2Strength * Triangle(wave2freq * x); }

        if (wave3type == WaveType.SINE) { wave3 = wave3Strength * Mathf.Sin(wave3freq * x); }
        else if (wave3type == WaveType.SQUARE) { wave3 = wave3Strength * Square(wave3freq * x); }
        else if (wave3type == WaveType.TRIANGLE) { wave3 = wave3Strength * Triangle(wave3freq * x); }

        float normalizingConstant = 1f / (3f * (Mathf.Abs(wave1Strength) + Mathf.Abs(wave2Strength) + Mathf.Abs(wave3Strength)));
        return normalizingConstant * (wave1 + wave2 + wave3);
    }

    float Square(float phase)
    {
        return Mathf.Sin(phase) > 0.0 ? 1.0f : -1.0f;
    }

    float Triangle(float phase)
    {
        return Mathf.Abs((((phase - Mathf.PI/2)/4) % Mathf.PI/2) - Mathf.PI/4) * 8f / Mathf.PI - 1;
    }
}
