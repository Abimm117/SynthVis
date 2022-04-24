using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ClipSoundController : MonoBehaviour
{
    #region define objects
    public AudioSource[] clipSources;
    public GameObject clipSoundObject;
    public GetSpectrumData[] clipSpectra;
    int nClips;

    //public Button button;
    //public Toggle toggle;
    public GameObject specplot;
    List<GameObject> specBars = new List<GameObject>();

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

    public GameObject plotPointPrefab;
    //public float WaveformZoom;
    //public GameObject waveformZoomBar;
    int numPlotPoints = 800;
    public Vector3 plotPointLocalScale;
    public Vector3 plotBarLocalScale;
    float xmin = -5.75f;
    float xmax = 4.25f;
    float ymin = -3f;
    float ymax = 3f;
    #endregion

    void Start()
    {
        #region initialize aux plots
        float xPos = xmin;
        float xStep = (xmax - xmin) / numPlotPoints;
        
        for (int i = 0; i < numPlotPoints; i++)
        {
            Vector3 pos = new Vector3(xPos + xStep * i, 0, -0.5f);
            GameObject go = Instantiate(plotPointPrefab, Vector3.zero, Quaternion.identity);
            specBars.Add(go);
            go.transform.parent = specplot.transform;
            go.transform.localPosition = pos;
        }
        #endregion

        envQueue = new Queue(nEnvelopeQueue);
        #region initialize sound data
        float sampleRate = 44100f;
        int bufferLength = nSpectrum / 2;
        int nSamplePerWave = Mathf.RoundToInt(bufferLength / (446.16f / (sampleRate / bufferLength)));
        frequencyHz = makeArr(0, 32 * nSamplePerWave, nSpectrum);
        #endregion

        savedObjectScaleSingle = clipSoundObject.transform.localScale;
        singleSoundMaterial = clipSoundObject.transform.GetComponent<Renderer>().material;

        nClips = clipSources.Length;
    }

    // Update is called once per frame
    void Update()
    {
        timeT += Time.deltaTime;
        if (timeT > nextTime)
        {
            //Debug.Log(timeT);
            nextTime = timeT + refreshSpeed;
            RefreshSoundData();
            stopped = true;
            for (int i = 0; i < nClips; i++)
            {
                if (clipSources[i].isPlaying)
                {
                    UpdateSoundObject();
                    if (SynthVisualizer.Instance.refreshAux) { RefreshSpectrogram(); }
                    stopped = false;
                }
            }          
            if (stopped)
            {
                clipSoundObject.SetActive(false);
            }

            //if (Input.GetKeyDown(KeyCode.Alpha5)) { PlayClip(); }
            //if (Input.GetKeyDown(KeyCode.Alpha6)) { StopClip(); }

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
                if (hit.transform.name == "Glass") { PlayClip(0); }
                if (hit.transform.name == "Trombone") { PlayClip(1); }
                if (hit.transform.name == "Flute") { PlayClip(2); }
                if (hit.transform.name == "Harp") { PlayClip(3); }
                if (hit.transform.name == "Berlioz") { PlayClip(4); }
            }
        }
    }

    void RefreshSpectrogram()
    {
        float height;
        GameObject go;
        float envVal = Mathf.Max(.01f, Mathf.Min(envQueueMean / .02f, 1f));
        for (int n = 0; n < specBars.Count; n++)
        {
            go = specBars[n];
            height = envVal * Mathf.Min(plotBarLocalScale.y * Mathf.Log(1 + currentSpectrumData[n], 2), 6);
            go.transform.localScale = new Vector3(plotBarLocalScale.x, height, plotBarLocalScale.z);
        }
    }

    public void PlayClip(int i)
    {
        for (int n = 0; n < nClips; n++) { StopClip(n); }
        if (!clipSources[i].isPlaying) { clipSources[i].Play(); }      
    }

    public void StopClip(int i)
    {
        if (clipSources[i].isPlaying) { clipSources[i].Stop(); }
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
        clipSoundObject.SetActive(SynthVisualizer.Instance.soundObjectOn);
        float envVal = Mathf.Max(.01f, Mathf.Min(envQueueMean / .02f, 1f));
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
        for (int n = 0; n < nClips; n++)
        {
            if (clipSources[n].isPlaying)
            {
                currentSpectrumData = clipSpectra[n].GetSpectrum();

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
}