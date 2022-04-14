using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum WaveType { SINE, SQUARE, TRIANGLE, SAWTOOTH};
public class Instrument : MonoBehaviour
{
    //TODO:

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
    public float wave1freq;
    public float wave2freq;
    public float wave3freq;
    public float wave1Strength;
    public float wave2Strength;
    public float wave3Strength;
    public WaveType wave1type;
    public WaveType wave2type;
    public WaveType wave3type;
    public float wave1lfofreq;
    public float wave2lfofreq;
    public float wave3lfofreq;
    public float wave1lfoStrength;
    public float wave2lfoStrength;
    public float wave3lfoStrength;


    float ProcessWave(float x, WaveType wavetype, float strength, float freq, float lfoStrength, float lfofreq)
    {
        float lfoAmp = lfoStrength * Mathf.Sin(x * freq * lfofreq / 44100);
        float f;
        if (wavetype == WaveType.SINE) { f = Mathf.Sin(freq * x); }
        else if (wavetype == WaveType.SQUARE) { f = Square(freq * x); }
        else if (wavetype == WaveType.TRIANGLE) {f = Triangle(freq * x); }
        else if (wavetype == WaveType.SAWTOOTH) { f = Sawtooth(freq * x); }
        else { f = 0f; }
        return strength * ((lfoAmp * f) - (1 - lfoStrength) * f);
    }

    public float CustomSynth(float x)
    {
        float wave1 = ProcessWave(x, wave1type, wave1Strength, wave1freq, wave1lfoStrength, wave1lfofreq);
        float wave2 = ProcessWave(x, wave2type, wave2Strength, wave2freq, wave2lfoStrength, wave2lfofreq);
        float wave3 = ProcessWave(x, wave3type, wave3Strength, wave3freq, wave3lfoStrength, wave3lfofreq);
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

    float Sawtooth(float phase)
    {
        return 2*((phase + Mathf.PI / 2) / (2 * Mathf.PI) - Mathf.Floor((phase + Mathf.PI / 2) / (2 * Mathf.PI) + 0.5f));
    }
}
