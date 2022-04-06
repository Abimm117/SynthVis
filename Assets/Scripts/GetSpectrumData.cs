using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GetSpectrumData : MonoBehaviour
{
    #region define objects
    public AudioSource audioSrc;
    float[] spectrum = new float[4096];
    #endregion

    private void Start()
    {
        StartCoroutine(UpdateSpectrumData());
    }

    public float[] GetSpectrum()
    {
        float[] publicSpectrum = new float[1000];
        Array.Copy(spectrum, publicSpectrum, 1000);
        return publicSpectrum;
    }

    public float[] GetFullSpectrum()
    {
        return spectrum;
    }

    IEnumerator UpdateSpectrumData()
    {
        yield return new WaitForSeconds(.1f);
        audioSrc.GetSpectrumData(spectrum, 0, FFTWindow.BlackmanHarris);
        StartCoroutine(UpdateSpectrumData());
    }

}