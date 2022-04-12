using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GetSpectrumData : MonoBehaviour
{
    #region define objects
    public AudioSource audioSrc;
    float[] spectrum = new float[8192];
    #endregion

    public float[] GetSpectrum()
    {        
        audioSrc.GetSpectrumData(spectrum, 0, FFTWindow.Blackman);
        return spectrum;
    }
}