using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public float CustomSynth(float x)
    {
        float wave1 = wave1Strength * (wave1type == WaveType.SQUARE ? Square(wave1freq * x) : Mathf.Sin(wave1freq * x));
        float wave2 = wave2Strength * (wave2type == WaveType.SQUARE ? Square(wave2freq * x) : Mathf.Sin(wave2freq * x));
        float wave3 = wave3Strength * (wave3type == WaveType.SQUARE ? Square(wave3freq * x) : Mathf.Sin(wave3freq * x));
        float normalizingConstant = 1f / (3f * (Mathf.Abs(wave1Strength) + Mathf.Abs(wave2Strength) + Mathf.Abs(wave3Strength)));
        return normalizingConstant * (wave1 + wave2 + wave3);
    }

    float Square(float phase)
    {
        float x = Mathf.Sin(phase);
        if (x > 0.0)
        {
            return 1.0f;
        }
        else
        {
            return -1.0f;
        }
    }
}
