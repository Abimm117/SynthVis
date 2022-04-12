using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundEnvelope : MonoBehaviour
{
    #region define objects
    public double attackTime = 0.1;
    public double decayTime = 0.01;
    public double releaseTime = 0.2;
    public double sustainAmplitude= 0.8;
    public double startAmplitude = 1.0;
    public double triggerOnTime = 0.0;
    public double triggerOffTime = 0.0;
    public bool bNoteOn = false;
    double maxAmpSoFar;
    #endregion

    public double getAmplitude(double time)
    {
        double amp = 0.0;
        double lifeTime = time - triggerOnTime; //lifetime is how long the note has been going on for

        if (bNoteOn)
        {
            //ATTACK
            if (lifeTime <= attackTime)
            {
                amp = (lifeTime / attackTime) * startAmplitude;
                maxAmpSoFar = amp;
            }

            //DECAY
            if (lifeTime > attackTime && lifeTime <= (attackTime + decayTime))
            {
                amp = ((lifeTime - attackTime) / decayTime) * (sustainAmplitude - startAmplitude) + startAmplitude;
            }

            //SUSTAIN
            if (lifeTime > (attackTime + decayTime))
            {
                amp = sustainAmplitude;
            }
        }
        else
        {
            //RELEASE
            double a = Mathf.Min((float)sustainAmplitude, (float)maxAmpSoFar);           
            amp = ((time - triggerOffTime) / releaseTime) * (0.0 - a) + a;
        }

        if (amp <= .0001)
        {
            amp = 0.0;
        }

        return amp;
    }

    public void noteOn(double time)
    {
        triggerOnTime = time;
        bNoteOn = true;
    }

    public void noteOff(double time)
    {
        triggerOffTime = time;
        bNoteOn = false;
    }
}
