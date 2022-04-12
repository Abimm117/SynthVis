using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;
using System.IO;
using TMPro;

public enum VisualizationMode { SingleInstrument, AllInstruments}

public enum VertexShaderMode { Object, World, Tangent};


public class SynthVisualizer : MonoBehaviour
{
    #region define objects
    public VisualizationMode mode;

    // time
    float timeT = 0f;
    float nextTime = 0.5f;

    // main plots
    public GameObject allInstrumentPlot;
    public GameObject singleInstrumentPlot;

    // all instrument plot  
    public GameObject[] allSoundObjects;
    Vector3 savedObjectScaleAll;
    public GameObject[] allSoundMarkers;
    Material[] allSoundMaterials;

    // single instrument plot
    public VertexShaderMode vertexMode;
    VertexShaderMode savedVertexMode;
    public GameObject singleSoundObject;
    Vector3 savedObjectscale;
    Vector3 savedWorldscale;
    Vector3 savedTangentscale;
    public GameObject singleSoundObjectWorld;
    public GameObject singleSoundObjectTangent;
    GameObject singleSound;
    Vector3 savedObjectScaleSingle;
    Material singleSoundMaterial;
    public TextMeshPro instrumentNumberLabel;

    // sound visualization settings
    public bool refreshAux = false;
    public float refreshSpeed;
    public float noiseMultiplier;
    public float noiseExponent;
    public float frequencyPositionMultiplier;
    public float frequencyPositionShift;
    public float objectLerpSpeed;
    float noiseLevel;

    // sound data
    public AudioListener mix;
    public SynthController synthController;
    bool isPlaying = false;
    float[] currentSpectrumData;
    public GetSpectrumData[] spectra;
    public float currentSpectrumGiniCoeff = 1f;
    public float currentSpectrumCentroid;
    public float currentSpectrumSpread;
    public float currentSpectrumFlatness;
    int nSpectrum = 4096;
    List<float> frequencyHz;
    

    // auxiliary plots
    public GameObject waveformPlot;
    public GameObject spectrogram;
    public GameObject plotPointPrefab;
    public float spectrogramHeightShift;
    public float waveformPlotZoom;
    public float spectrogramZoom;
    int numPlotPoints = 1000;
    List<GameObject> points = new List<GameObject>();
    List<GameObject> points2 = new List<GameObject>();
    float xmin = -5.75f;
    float xmax = 4.25f;
    float ymin = -3f;
    float ymax = 3f;

    #endregion

    void LoadSingleSoundObject()
    {
        singleSoundObject.SetActive(false);
        singleSoundObjectWorld.SetActive(false);
        singleSoundObjectTangent.SetActive(false);
        if (vertexMode == VertexShaderMode.Object)
        {
            singleSound = singleSoundObject;
            savedObjectScaleSingle = savedObjectscale;
        }
        else if (vertexMode == VertexShaderMode.World)
        {
            singleSound = singleSoundObjectWorld;
            savedObjectScaleSingle = savedWorldscale;
        }
        else if (vertexMode == VertexShaderMode.Tangent)
        {
            singleSound = singleSoundObjectTangent;
            savedObjectScaleSingle = savedTangentscale;
        }
        singleSoundMaterial = singleSound.transform.GetComponent<Renderer>().sharedMaterial;
        //savedObjectScaleSingle = singleSound.transform.localScale;
    }

    private void Awake()
    {
        #region initialize aux plots
        float xPos = xmin;
        float xStep = (xmax - xmin) / numPlotPoints;
        GameObject[] auxPlots = new GameObject[] { waveformPlot, spectrogram};
        List<GameObject>[] pointsLists = new List<GameObject>[] { points, points2 };
        for (int n = 0; n < auxPlots.Length; n++)
        {
            for (int i = 0; i < numPlotPoints; i++)
            {
                Vector3 pos = new Vector3(xPos + xStep * i, 0, -0.5f);
                GameObject go = Instantiate(plotPointPrefab, Vector3.zero, Quaternion.identity);
                pointsLists[n].Add(go);
                go.transform.parent = auxPlots[n].transform;
                go.transform.localPosition = pos;
                go.transform.localScale = .05f * Vector3.one;
            }
        }
        #endregion

        #region initialize sound data
        currentSpectrumData = new float[nSpectrum];
        for (int n = 0; n < nSpectrum; n++)
        {
            currentSpectrumData[n] = 0f;
        }

        float sampleRate = 44100f;
        int bufferLength = nSpectrum / 2;
        int nSamplePerWave = Mathf.RoundToInt(bufferLength / (446.16f / (sampleRate / bufferLength)));
        frequencyHz = makeArr(0, 32 * nSamplePerWave, nSpectrum);
        for (int i = 0; i < nSpectrum; i++)
        {
            Debug.Log(i.ToString() + ": " + frequencyHz[i].ToString());
        }
        savedVertexMode = vertexMode;
        noiseLevel = 0f;
        #endregion

        #region load sound object materials
        savedObjectscale = singleSoundObject.transform.localScale;
        savedWorldscale = singleSoundObjectWorld.transform.localScale;
        savedTangentscale = singleSoundObjectTangent.transform.localScale;
        LoadSingleSoundObject();
        allSoundMaterials = new Material[allSoundObjects.Length];
        for (int i = 0; i < 4; i++)
        {
            allSoundMaterials[i] = allSoundObjects[i].transform.GetComponent<Renderer>().sharedMaterial;
        }
        savedObjectScaleAll = allSoundObjects[0].transform.localScale;
        #endregion
    }

    private void Update()
    {
        timeT += Time.deltaTime;
        if (timeT > nextTime)
        {
            nextTime = timeT + refreshSpeed;
            RefreshSoundData();
            if (mode == VisualizationMode.AllInstruments)
            {
                singleInstrumentPlot.SetActive(false);
                allInstrumentPlot.SetActive(true);
                UpdateAllInstrumentPlot();
            }
            else if (mode == VisualizationMode.SingleInstrument)
            {
                singleInstrumentPlot.SetActive(true);
                allInstrumentPlot.SetActive(false);
                UpdateSingleInstrumentPlot();
            }     

            if (refreshAux)
            {
                RefreshAuxPlots();
            }
            if (vertexMode != savedVertexMode)
            {
                LoadSingleSoundObject();
                savedVertexMode = vertexMode;
            }
        }
    }

    void UpdateSoundObject(GameObject go, float envVal, int soundNum)
    {
        go.SetActive(true);
        noiseLevel = Mathf.Pow(envVal, 2) * noiseMultiplier * Mathf.Pow(1f - currentSpectrumGiniCoeff, noiseExponent);
        if (soundNum == -1)
        {
            singleSoundMaterial.SetFloat("_scale", noiseLevel);
            go.transform.localScale = envVal * savedObjectScaleSingle;
        }
        else
        {
            allSoundMaterials[soundNum].SetFloat("_scale", noiseLevel);
            go.transform.localScale = envVal * savedObjectScaleAll;
        }

        //TODO:
        //update sound stretch based on the currentSpectrumSpread of the spectrogram
    }

    void UpdateAllInstrumentPlot()
    {
        int n = synthController.CurrentInstrumentNumber();
        float envVal = (float)synthController.CurrentEnvelopeValue();

        allSoundMarkers[n].SetActive(true);
        if (isPlaying || envVal > .0001f)
        {
            GameObject go = allSoundObjects[n];
            UpdateSoundObject(go, envVal, n);

            // update sound position based on pitch
            // current calculated as centroid of the spectrum (weighted average of frequencies multiplied by their heights in the spectrogram)
            // represent this on log scale because human perception of audio is logarithmic
            // this method is currently glitchy as fuck
            float target_frequency_pos = Mathf.Lerp(xmin, xmax, frequencyPositionMultiplier * Mathf.Log(currentSpectrumCentroid) + frequencyPositionShift);
            go.transform.localPosition = Vector3.MoveTowards(go.transform.localPosition, new Vector3(target_frequency_pos, go.transform.localPosition.y, go.transform.localPosition.z), objectLerpSpeed * Time.deltaTime);
        }
        else
        {
            for (int i = 0; i < 4; i++)
            {
                allSoundObjects[i].SetActive(false);
                if (i != n)
                {
                    allSoundMarkers[i].SetActive(false);
                }
            }
        }
    }

    void UpdateSingleInstrumentPlot()
    {
        int n = synthController.CurrentInstrumentNumber();
        instrumentNumberLabel.SetText("Inst #" + (n+1).ToString());
        float envVal = synthController.CurrentEnvelopeValue();
        if (isPlaying || envVal > .0001f)
        {
            UpdateSoundObject(singleSound, envVal, -1);
        }
        else
        {
            if (singleSound.activeInHierarchy)
            {
                singleSound.SetActive(false);
            }
        }
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

    void RefreshSoundData()
    {
        int numPlaying = synthController.NumPlaying();
        isPlaying = numPlaying > 0;

        if (isPlaying)
        {
            // add up all spectrum data coming from instrument voices
            List<float[]> allSpectra = new List<float[]>();
            
            for (int j = 0; j < numPlaying; j++)
            {
                allSpectra.Add(spectra[j].GetSpectrum());
            }
            float maxval = 0f;
            float val;
            
            for (int i = 0; i < nSpectrum; i++)
            {
                val = 0f;
                for (int j = 0; j < numPlaying; j++)
                {
                    val += allSpectra[j][i];
                }
                currentSpectrumData[i] = val;
                if (val > maxval) { maxval = val; }
            }

            float centroidSum = 0f;
            for (int i = 0; i < nSpectrum; i++)
            {
                float p = currentSpectrumData[i] / maxval;
                float f = frequencyHz[i];
                centroidSum += p * f;
            }
            currentSpectrumCentroid = (1f / numPlaying) * centroidSum / nSpectrum;
            /*
           
            //calculate spectrum spread
            s = 0f;
            for (int i = 0; i < nSpectrum; i++)
            {
                float p = currentSpectrumData[i];
                float f = frequencyHz[i];
                s += p * Mathf.Pow((f - currentSpectrumCentroid), 2);
            }
            currentSpectrumSpread = Mathf.Sqrt(s);

            //calculate spectrum flatness
            float s0 = 0f;
            float s1 = 0f;
            for (int i = 0; i < nSpectrum; i++)
            {
                float a = currentSpectrumData[i];
                s0 *= a;
                s1 += a;
            }
            currentSpectrumFlatness = Mathf.Pow(s0, 1 / nSpectrum) / (s1 / nSpectrum);
            */

            //calculate spectrum gini coefficient
            //sort current spectrum data to calculate gini coefficient
            Array.Sort(currentSpectrumData);
            float giniSum0 = 0f;
            float giniSum1 = 0f;
            for (int i = 0; i < nSpectrum; i++)
            {
                giniSum0 += (nSpectrum - i) * currentSpectrumData[i];
                giniSum1 += currentSpectrumData[i];
            }
            currentSpectrumGiniCoeff = (1f / nSpectrum) * (nSpectrum + 1 - 2 * (giniSum0 / giniSum1));
        }              
    }

    void RefreshAuxPlots()
    {
        // waveform plot
        for (int n = 0; n < points.Count; n++)
        {
            GameObject go = points[n];
            float synthVal = 0f;
            if (synthController.NumPlaying() > 0)
            {
                for (int i = 0; i < synthController.sounds.Length; i++)
                {
                    if (synthController.OscillatorInUse(i))
                    {
                        synthVal = synthController.CurrentInstrument().CustomSynth(waveformPlotZoom * go.transform.localPosition.x);
                    }
                }
            }

            // map the synth val, which is between -1 and 1, to a height on the backplate 
            float height = Mathf.Lerp(ymin, ymax, 0.5f * (synthVal + 1f));
            go.transform.localPosition = new Vector3(go.transform.localPosition.x, height, go.transform.localPosition.z);
        }

        // spectrogram plot
        for (int n = 0; n < points2.Count; n++)
        {
            GameObject go = points2[n];
            // map the freq val, which is between 0 and 1, to a height on the backplate using a log scale
            float height = Mathf.Lerp(ymin, ymax, Mathf.Log(1 + currentSpectrumData[n], 2));
            float xPos = go.transform.localPosition.x;// * spectrogramZoom;
            if (xPos <= xmax)
            {
                go.SetActive(true);
                go.transform.localPosition = new Vector3(xPos, height + spectrogramHeightShift, go.transform.localPosition.z);
            }
            else
            {
                go.SetActive(false);
            }
            
        }
    }
}
