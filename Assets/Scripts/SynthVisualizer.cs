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
    public static SynthVisualizer Instance { get; private set; }
    #region define objects
    public VisualizationMode mode;
    VisualizationMode savedMode;

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
    public float refreshSpeed = .05f;
    public float noiseMultiplier = 1500;
    public float noiseExponent = 1.5f;
    public float noiseEnvelopeExponent;
    public float centroidMultiplier;
    public float wobbleMultiplier;
    public string Instrument1Color;
    public string Instrument2Color;
    public string Instrument3Color;
    public string Instrument4Color;
    string[] instrumentColors;

    // sound data
    public SynthController synthController;
    bool isPlaying = false;
    List<float[]> currentSpectrumData;
    public GetSpectrumData[] spectra;
    public float giniSlowdownMultiplier;
    public float[] currentSpectrumGiniCoeffs = new float[4];
    public float[] previousSpectrumGiniCoeffs = new float[4];
    public float[] currentSpectrumCentroids = new float[4];
    public float[] currentNoises = new float[4];
    public float[] currentBrightnesses = new float[4];
    public float[] currentSpeeds = new float[4];
    public float[] currentSpectrumSpreads = new float[4];
    public float[] currentSpectrumFlatnesses = new float[4];
    int nSpectrum = 8192;
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
        singleSoundMaterial = singleSound.transform.GetComponent<Renderer>().material;
    }

    Vector3 ColorStringToRGB(int instrumentNum)
    {
        switch (instrumentColors[instrumentNum])
        {
            case "red": return new Vector3(1, 0, 0);
            case "green": return new Vector3(0, 1, 0);
            case "blue": return new Vector3(0, 0, 1);
            case "yellow": return new Vector3(1, 1, 0);
            case "orange": return new Vector3(1, .5f, 0);
            case "magenta": return new Vector3(1, 0, 1);
            case "cyan": return new Vector3(0, 1, 1);
            case "grey": return Vector3.one;
            default: return Vector3.zero;
        }
    }

    private void Awake()
    {
        instrumentColors = new string[] { Instrument1Color, Instrument2Color, Instrument3Color, Instrument4Color };
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;
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
        currentSpectrumData = new List<float[]>();
        for (int i = 0; i < 4; i++)
        {
            currentSpectrumData.Add(new float[nSpectrum]);
            for (int n = 0; n < nSpectrum; n++)
            {
                currentSpectrumData[i][n] = 0f;
            }
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
        #endregion

        #region load sound object materials
        savedObjectscale = singleSoundObject.transform.localScale;
        savedWorldscale = singleSoundObjectWorld.transform.localScale;
        savedTangentscale = singleSoundObjectTangent.transform.localScale;
        LoadSingleSoundObject();
        allSoundMaterials = new Material[allSoundObjects.Length];
        for (int i = 0; i < 4; i++)
        {
            allSoundMaterials[i] = allSoundObjects[i].transform.GetComponent<Renderer>().material;
        }
        savedObjectScaleAll = allSoundObjects[0].transform.localScale;
        #endregion

        savedMode = (mode == VisualizationMode.AllInstruments) ? VisualizationMode.SingleInstrument : VisualizationMode.AllInstruments;
    }

    private void Update()
    {
        instrumentColors = new string[] { Instrument1Color, Instrument2Color, Instrument3Color, Instrument4Color };
        timeT += Time.deltaTime;
        if (timeT > nextTime)
        {
            nextTime = timeT + refreshSpeed;
            RefreshSoundData();
            if (mode == VisualizationMode.AllInstruments)
            {
                UpdateAllInstrumentPlot();
                if (savedMode != mode)
                {
                    singleInstrumentPlot.SetActive(false);
                    allInstrumentPlot.SetActive(true);
                    synthController.ResetKeyboard(mode);
                }                                
            }
            else if (mode == VisualizationMode.SingleInstrument)
            {
                UpdateSingleInstrumentPlot();
                if (savedMode != mode)
                {
                    singleInstrumentPlot.SetActive(true);
                    allInstrumentPlot.SetActive(false);
                    synthController.ResetKeyboard(mode);
                }
            }
            savedMode = mode;

            if (refreshAux)
            {
                //RefreshAuxPlots();
            }
            if (vertexMode != savedVertexMode)
            {
                LoadSingleSoundObject();
                savedVertexMode = vertexMode;
            }
        }
    }

    float FreqToMel(float freq)
    {
        return 2595f * Mathf.Log(1 + freq / 500);
    }

    void UpdateSingleSoundObject(GameObject go, float envVal, int soundNum)
    {
        if (float.IsNaN(envVal)) { envVal = .01f; }
        go.SetActive(true);
        float gini = giniSlowdownMultiplier * currentSpectrumGiniCoeffs[soundNum] + (1 - giniSlowdownMultiplier) * previousSpectrumGiniCoeffs[soundNum];
        currentNoises[soundNum] = Mathf.Pow(envVal, noiseEnvelopeExponent) * noiseMultiplier * Mathf.Pow(1f - gini, noiseExponent);
        currentBrightnesses[soundNum] = centroidMultiplier * FreqToMel(currentSpectrumCentroids[soundNum]);
        currentSpeeds[soundNum] = synthController.GetInstrumentTotalLFO(soundNum);
        Vector3 color = ColorStringToRGB(soundNum);
        singleSoundMaterial.SetFloat("_noiseScale", currentNoises[soundNum]);
        singleSoundMaterial.SetFloat("_brightness", currentBrightnesses[soundNum]);
        singleSoundMaterial.SetFloat("_wobbleSpeed", currentSpeeds[soundNum]);
        singleSoundMaterial.SetFloat("_redVal", color.x);
        singleSoundMaterial.SetFloat("_greenVal", color.y);
        singleSoundMaterial.SetFloat("_blueVal", color.z);
        go.transform.localScale = Mathf.Pow(envVal, 2) * savedObjectScaleSingle;
    }

    void UpdateSoundObject(GameObject go, float envVal, int soundNum)
    {
        if (float.IsNaN(envVal)) { envVal = .01f; }
        go.SetActive(true);
        float gini = giniSlowdownMultiplier * Time.deltaTime * currentSpectrumGiniCoeffs[soundNum] + (1 - giniSlowdownMultiplier * Time.deltaTime) * previousSpectrumGiniCoeffs[soundNum];
        currentNoises[soundNum] = Mathf.Pow(envVal, noiseEnvelopeExponent) * noiseMultiplier * Mathf.Pow(1f - gini, noiseExponent);
        currentBrightnesses[soundNum] = centroidMultiplier * FreqToMel(currentSpectrumCentroids[soundNum]);
        currentSpeeds[soundNum] = synthController.GetInstrumentTotalLFO(soundNum);
        Vector3 color = ColorStringToRGB(soundNum);
        allSoundMaterials[soundNum].SetFloat("_noiseScale", currentNoises[soundNum]);
        allSoundMaterials[soundNum].SetFloat("_brightness", currentBrightnesses[soundNum]);
        allSoundMaterials[soundNum].SetFloat("_wobbleSpeed", currentSpeeds[soundNum]);
        allSoundMaterials[soundNum].SetFloat("_redVal", color.x);
        allSoundMaterials[soundNum].SetFloat("_greenVal", color.y);
        allSoundMaterials[soundNum].SetFloat("_blueVal", color.z);
        go.transform.localScale = Mathf.Pow(envVal, 2) * savedObjectScaleAll;       
    }

    void UpdateAllInstrumentPlot()
    {
        for (int n = 0; n < 4; n++)
        {
            float envVal = synthController.CurrentEnvelopeValue(n);
            if (envVal > .0001f)
            {
                allSoundMarkers[n].SetActive(true);
                UpdateSoundObject(allSoundObjects[n], envVal, n);
            }
            else
            {
                allSoundObjects[n].SetActive(false);
                allSoundMarkers[n].SetActive(false);
            }
        }
    }

    void UpdateSingleInstrumentPlot()
    {
        int n = synthController.CurrentInstrumentNumber();
        instrumentNumberLabel.SetText("Inst #" + (n+1).ToString());
        float envVal = synthController.CurrentEnvelopeValue(n);
        if (isPlaying || envVal > .0001f)
        {
            UpdateSingleSoundObject(singleSound, envVal, n);
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
            
            //float[] vals = new float[4];

            for (int instrumentNum = 0; instrumentNum < 4; instrumentNum++)
            {
                if (synthController.CurrentEnvelopeValue(instrumentNum) > .0001f)
                {
                    float maxval = 0f;
                    for (int i = 0; i < nSpectrum; i++)
                    {
                        float val = 0f;
                        for (int j = 0; j < numPlaying; j++)
                        {
                            if (instrumentNum == synthController.GetOscillatorInstrumentNumber(j))
                            {
                                val += allSpectra[j][i];
                            }
                        }
                        currentSpectrumData[instrumentNum][i] = val;
                        if (val > maxval) { maxval = val; }
                    }

                    float centroidSum = 0f;
                    for (int i = 0; i < nSpectrum; i++)
                    {
                        float p = currentSpectrumData[instrumentNum][i] / maxval;
                        float f = frequencyHz[i];
                        centroidSum += p * f;
                    }
                    currentSpectrumCentroids[instrumentNum] = (1f / numPlaying) * centroidSum / nSpectrum;

                    //calculate spectrum gini coefficient
                    //sort current spectrum data to calculate gini coefficient
                    Array.Copy(currentSpectrumGiniCoeffs, previousSpectrumGiniCoeffs, 4);
                    Array.Sort(currentSpectrumData[instrumentNum]);
                    float giniSum0 = 0f;
                    float giniSum1 = 0f;
                    for (int i = 0; i < nSpectrum; i++)
                    {
                        giniSum0 += (nSpectrum - i) * currentSpectrumData[instrumentNum][i];
                        giniSum1 += currentSpectrumData[instrumentNum][i];
                    }
                    currentSpectrumGiniCoeffs[instrumentNum] = (1f / nSpectrum) * (nSpectrum + 1 - 2 * (giniSum0 / giniSum1));
                               
                    //calculate spectrum spread
                    float spreadSum = 0f;
                    for (int i = 0; i < nSpectrum; i++)
                    {
                        float p = currentSpectrumData[instrumentNum][i];
                        float f = frequencyHz[i];
                        spreadSum += p * Mathf.Pow((f - currentSpectrumCentroids[instrumentNum]), 2);
                    }
                    currentSpectrumSpreads[instrumentNum] = Mathf.Sqrt(spreadSum);

                    //calculate spectrum flatness
                    float flatnessProduct = 0f;
                    float flatnessSum = 0f;
                    for (int i = 0; i < nSpectrum; i++)
                    {
                        float a = currentSpectrumData[instrumentNum][i];
                        flatnessProduct *= a;
                        flatnessSum += a;
                    }
                    currentSpectrumFlatnesses[instrumentNum] = Mathf.Pow(flatnessProduct, 1 / nSpectrum) / (flatnessSum / nSpectrum);
            
                }
            }
            
        }              
    }

    /*
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
    */
}
