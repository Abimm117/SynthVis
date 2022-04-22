using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ClipSoundController : MonoBehaviour
{
    #region define objects
    public AudioSource clipSource;
    public GameObject clipSoundObject;
    public GetSpectrumData clipSpectrum;

    //public Button button;
    //public Toggle toggle;

    // time
    float timeT = 0f;
    float nextTime = 0.5f;

    Vector3 savedObjectScaleSingle;
    Material singleSoundMaterial;

    public float refreshSpeed;
    public float noiseMultiplierGINI;
    public float noiseExponentGINI;
    public float noiseEnvelopeExponent;
    public float centroidMultiplier;
    public float wobbleMultiplier;
    public float alphaMultiplier;

    // sound data
    float[] spec = new float[8192];
    float[] currentSpectrumData = new float[8192];
    public float currentSpectrumGiniCoeff;
    public float currentSpectrumCentroid;
    public float currentNoise;
    public float currentBrightness;
    public float currentSpeed;
    public float currentAlpha;
    int nSpectrum = 8192;
    List<float> frequencyHz;
    bool stopped;

    public int nEnvelopeQueue;
    int savedNEnvelopeQueue;
    float envQueueMean;

    Queue envQueue;
    #endregion

    void Start()
    {
        envQueue = new Queue(nEnvelopeQueue);
        //toggle.isOn = clipSource.isPlaying;
        //toggle.onValueChanged.AddListener(delegate { ValueChangeCheck(); });
        //button.onClick.AddListener(PlayClip);

        #region initialize sound data
        float sampleRate = 44100f;
        int bufferLength = nSpectrum / 2;
        int nSamplePerWave = Mathf.RoundToInt(bufferLength / (446.16f / (sampleRate / bufferLength)));
        frequencyHz = makeArr(0, 32 * nSamplePerWave, nSpectrum);
        #endregion

        savedObjectScaleSingle = clipSoundObject.transform.localScale;
        singleSoundMaterial = clipSoundObject.transform.GetComponent<Renderer>().material;
    }
    /*
    public void ValueChangeCheck()
    {
        if (toggle.isOn)
        {
            PlayClip();
        }
        else
        {
            clipSource.Stop();
        }
    }*/

    // Update is called once per frame
    void Update()
    {
        timeT += Time.deltaTime;
        if (timeT > nextTime)
        {
            //Debug.Log(timeT);
            nextTime = timeT + refreshSpeed;
            RefreshSoundData();
            if (clipSource.isPlaying)
            {
                UpdateSoundObject();
                stopped = false;
            }
            else if (!stopped)
            {
                stopped = true;
                clipSoundObject.SetActive(false);
            }

            if (Input.GetKeyDown(KeyCode.Alpha5)) { PlayClip(); }
            if (Input.GetKeyDown(KeyCode.Alpha6)) { StopClip(); }

            if (savedNEnvelopeQueue != nEnvelopeQueue)
            {
                savedNEnvelopeQueue = nEnvelopeQueue;
                envQueue = new Queue(savedNEnvelopeQueue);
            }
        }

        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, Mathf.Infinity))
            {
                if (hit.transform.name == "PlayClipButton") { PlayClip(); }
            }
        }
    }

    public void PlayClip()
    {
        if (!clipSource.isPlaying) { clipSource.Play(); }
    }

    public void StopClip()
    {
        if (clipSource.isPlaying) { clipSource.Stop(); }
    }

    float FreqToMel(float freq)
    {
        return 2595f * Mathf.Log(1 + freq / 500);
    }

    List<float> makeArr(float startValue, float stopValue, int cardinality)
    {
        List<float> arr = new List<float>();
        float step = (stopValue - startValue) / (cardinality - 1);
        for (int i = 0; i < cardinality; i++)
        {
            float val = startValue + (step * i);
            arr.Add(val);
        }
        return arr;
    }

    void UpdateSoundObject()
    {
        float envVal = Mathf.Max(.01f, Mathf.Min(envQueueMean / .02f, 1f));
        clipSoundObject.SetActive(true);
        currentNoise = Mathf.Pow(envVal, noiseEnvelopeExponent) * noiseMultiplierGINI * Mathf.Pow(1f - currentSpectrumGiniCoeff, noiseExponentGINI);
        currentBrightness = centroidMultiplier * FreqToMel(currentSpectrumCentroid);
        currentAlpha = Mathf.Max(.2f, 1 - alphaMultiplier * currentBrightness);
        Vector3 color = Vector3.one;
        Material mat = singleSoundMaterial;
        Vector3 scale = savedObjectScaleSingle;

        mat.SetFloat("_noiseScale", currentNoise);
        mat.SetFloat("_brightness", currentBrightness);
        mat.SetFloat("_wobbleSpeed", currentSpeed);
        mat.SetFloat("_redVal", color.x);
        mat.SetFloat("_greenVal", color.y);
        mat.SetFloat("_blueVal", color.z);
        mat.SetFloat("_alpha", currentAlpha);
        clipSoundObject.transform.localScale = Mathf.Pow(envVal, 2) * scale;
    }

    void RefreshSoundData()
    {
        if (clipSource.isPlaying)
        {
            currentSpectrumData = clipSpectrum.GetSpectrum();

            if (envQueue.Count >= nEnvelopeQueue) { envQueue.Dequeue(); }
            float maxval = 0f;
            float val;
            for (int i = 0; i < nSpectrum; i++)
            {
                val = currentSpectrumData[i];
                if (maxval < val) { maxval = val; }
            }
            envQueue.Enqueue(maxval);
            envQueueMean = 0f;
            object[] envArray = envQueue.ToArray();
            foreach (object o in envArray)
            {
                envQueueMean += ((float)o) / nEnvelopeQueue;
            }

            /// extract features from spectrum data
            Array.Copy(currentSpectrumData, spec, nSpectrum);
            Array.Sort(spec); //sort current spectrum data to calculate gini coefficient       
            float centroidSum = 0f;
            float giniSum0 = 0f;
            float giniSum1 = 0f;

            for (int i = 0; i < nSpectrum; i++)
            {
                // centroid of energy
                float energy = currentSpectrumData[i];
                float f = frequencyHz[i];
                float p = energy / maxval;
                centroidSum += p * f;

                // gini
                giniSum0 += (nSpectrum - i) * spec[i];
                giniSum1 += spec[i];
            }
            currentSpectrumCentroid = centroidSum / nSpectrum;
            currentSpectrumGiniCoeff = (1f / nSpectrum) * (nSpectrum + 1 - 2 * (giniSum0 / giniSum1));
        }
    }
}