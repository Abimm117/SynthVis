using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using UnityEngine;
using UnityEngine.Assertions.Must;

public class Oscillator : MonoBehaviour
{
    #region define objects
    private double increment;
    private double phase;
    private double sampling_frequency = 44100.0;
    Instrument instrument;
    public float gain;
    public double frequency;
    public SoundEnvelope env;
    public float timeT = 0.0f;

    public List<float> floatBuffer = new List<float>();
    public bool storeDataInBuffer = false;
    SynthController synthController;
    int idNum;
    #endregion

    private void Update()
    {
        timeT += Time.deltaTime;
    }

    public void SetInstrument(Instrument inst, SynthController synth, int i)
    {
        instrument = inst;
        synthController = synth;
        idNum = i;
    }

    public void SetEnvelope(Instrument i)
    {
        env.attackTime = i.attack;
        env.decayTime = i.decay;
        env.sustainAmplitude = i.sustain;
        env.releaseTime = i.release;
    }

    public void PlayNote(Note n)
    {
        gain = n.volume;
        frequency = n.frequency;
        instrument = n.instrument;
        env.noteOn(timeT);
    }

    private void OnAudioFilterRead(float[] data, int channels)
    {
        increment = frequency * 2.0 * Math.PI / sampling_frequency;

        for(int i = 0; i < data.Length; i += channels)
        {
            phase += increment;
            double e = env.getAmplitude(timeT);
            synthController.SetCurrentEnvelopeValue(idNum, e);
            float soundData = (float)(gain * e * instrument.CustomSynth((float)phase));
            data[i] = soundData;

            if (storeDataInBuffer)
            {
                floatBuffer.Add(soundData);
            }

            if (channels == 2)
            {
                data[i + 1] = data[i];
            }

            if (phase > (Mathf.PI *2))
            {
                phase = 0.0;
            }
        }
    }

    public void ClearBuffer()
    {
        floatBuffer = new List<float>();
    }
}
