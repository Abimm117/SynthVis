using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;
using System.IO;
using TMPro;

public enum VisualizationMode { SingleInstrument, AllInstruments}

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
    public GameObject singleSoundObject;
    Vector3 savedObjectscale;
    GameObject singleSound;
    Vector3 savedObjectScaleSingle;
    Material singleSoundMaterial;
    public TextMeshPro instrumentNumberLabel;

    // sound visualization settings
    public bool refreshAux;
    public float refreshSpeed;
    public float noiseMultiplierGINI;
    public float noiseExponentGINI;
    public float noiseMultiplierFLATNESS;
    public float noiseExponentFLATNESS;
    public float noiseEnvelopeExponent;
    public float centroidMultiplier;
    public float wobbleMultiplier;
    public float alphaMultiplier;
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
    public float[] currentSpectrumGiniCoeffs = new float[4];
    public float[] currentSpectrumCentroids = new float[4];
    //public float[] currentSpectrumSpreads = new float[4];
    public float[] currentSpectrumFlatnesses = new float[4];

    public float[] currentNoises = new float[4];
    public float[] currentBrightnesses = new float[4];
    public float[] currentSpeeds = new float[4];
    public float[] currentAlphas = new float[4];
    
    int nSpectrum = 8192;
    List<float> frequencyHz;    

    // auxiliary plots
    public GameObject mainwaveformPlot;
    public GameObject waveform1Plot;
    public GameObject waveform2Plot;
    public GameObject waveform3Plot;
    public GameObject spectrogram;
    public GameObject plotPointPrefab;
    //public float spectrogramHeightShift;
    public float WaveformZoom;
    //public float spectrogramZoom;
    int numPlotPoints = 800;
    List<GameObject> mainWaveformPoints = new List<GameObject>();
    List<GameObject> waveform1Points = new List<GameObject>();
    List<GameObject> waveform2Points = new List<GameObject>();
    List<GameObject> waveform3Points = new List<GameObject>();
    List<GameObject> spectrogramBars = new List<GameObject>();
    public Vector3 plotPointLocalScale;
    public Vector3 plotBarLocalScale;
    //public int spectrogramBarFreqMult;
    //public int spectrogramBarFreqShift;

    float xmin = -5.75f;
    float xmax = 4.25f;
    float ymin = -3f;
    float ymax = 3f;

    #endregion

    private void Awake()
    {
        #region singleton pattern
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;
        #endregion

        #region initialize aux plots
        float xPos = xmin;
        float xStep = (xmax - xmin) / numPlotPoints;
        GameObject[] auxPlots = new GameObject[] { mainwaveformPlot, waveform1Plot, waveform2Plot, waveform3Plot, spectrogram};
        List<GameObject>[] pointsLists = new List<GameObject>[] { mainWaveformPoints, waveform1Points, waveform2Points, waveform3Points, spectrogramBars};
        for (int n = 0; n < auxPlots.Length; n++)
        {
            for (int i = 0; i < numPlotPoints; i++)
            {
                Vector3 pos = new Vector3(xPos + xStep * i, 0, -0.5f);
                GameObject go = Instantiate(plotPointPrefab, Vector3.zero, Quaternion.identity);
                pointsLists[n].Add(go);
                go.transform.parent = auxPlots[n].transform;
                go.transform.localPosition = pos;
                //go.transform.localScale = .05f * Vector3.one;
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

        #endregion

        #region load sound object materials
        instrumentColors = new string[] { Instrument1Color, Instrument2Color, Instrument3Color, Instrument4Color };
        savedObjectscale = singleSoundObject.transform.localScale;
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
        }
    }

    void LoadSingleSoundObject()
    {
        singleSoundObject.SetActive(false);
        singleSound = singleSoundObject;
        savedObjectScaleSingle = savedObjectscale;
        singleSoundMaterial = singleSound.transform.GetComponent<Renderer>().material;
    }

    void UpdateSoundObject(GameObject go, float envVal, int soundNum, bool isSingleSound)
    {       
        go.SetActive(true);
        currentNoises[soundNum] = Mathf.Pow(envVal, noiseEnvelopeExponent) * noiseMultiplierGINI * Mathf.Pow(1f - currentSpectrumGiniCoeffs[soundNum], noiseExponentGINI);
        currentBrightnesses[soundNum] = centroidMultiplier * FreqToMel(currentSpectrumCentroids[soundNum]);
        currentAlphas[soundNum] = Mathf.Max(.2f, 1 - alphaMultiplier*currentBrightnesses[soundNum]);
        Vector3 color = ColorOfInstrument(soundNum);
        Material mat;
        Vector3 scale;
        if (isSingleSound)
        {
            mat = singleSoundMaterial;
            scale = savedObjectScaleSingle;
        }
        else
        {
            mat = allSoundMaterials[soundNum];
            scale = savedObjectScaleAll;
        }
        mat.SetFloat("_noiseScale", currentNoises[soundNum]);
        mat.SetFloat("_brightness", currentBrightnesses[soundNum]);
        mat.SetFloat("_wobbleSpeed", currentSpeeds[soundNum]);
        mat.SetFloat("_redVal", color.x);
        mat.SetFloat("_greenVal", color.y);
        mat.SetFloat("_blueVal", color.z);
        mat.SetFloat("_alpha", currentAlphas[soundNum]);
        go.transform.localScale = Mathf.Pow(envVal, 2) * synthController.currentInstrumentGainValues[soundNum] * scale;
    }

    void UpdateAllInstrumentPlot()
    {
        for (int n = 0; n < 4; n++)
        {
            float envVal = synthController.CurrentEnvelopeValue(n);
            if (float.IsNaN(envVal)) { envVal = .0001f; }
            if (envVal >= .0001f)
            {
                allSoundMarkers[n].SetActive(true);
                UpdateSoundObject(allSoundObjects[n], envVal, n, false);
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
        if (float.IsNaN(envVal)) { envVal = .0001f; }
        if (isPlaying || envVal > .0001f)
        {
            UpdateSoundObject(singleSound, envVal, n, true);
            if (refreshAux) { RefreshAuxPlots(n, envVal); }
        }
        else
        {
            if (singleSound.activeInHierarchy)
            {
                singleSound.SetActive(false);
            }
        }
    }

    Vector3 ColorOfInstrument(int instrumentNum)
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

    float FreqToMel(float freq)
    {
        return 2595f * Mathf.Log(1 + freq / 500);
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

            float[] spec = new float[nSpectrum];
            for (int instrumentNum = 0; instrumentNum < 4; instrumentNum++)
            {
                if (synthController.CurrentEnvelopeValue(instrumentNum) > .0001f)
                {
                    ////////////////////////////////////////////////////////////////////////
                    // aggregate spectra (average) from oscillators assigned to instrumentNum
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
                    //////////////////////////////////////////////////////////////////////////
                    

                    /////////////////////////////////////////////////////////////////////////
                    /// extract features from spectrum data
                    Array.Copy(currentSpectrumData[instrumentNum], spec, nSpectrum);
                    Array.Sort(spec); //sort current spectrum data to calculate gini coefficient       
                    float centroidSum = 0f;
                    float giniSum0 = 0f;
                    float giniSum1 = 0f;
                    float flatnessProduct = 1f;
                    float flatnessSum = 0f;
                    for (int i = 0; i < nSpectrum; i++)
                    {
                        // centroid of energy
                        float energy = currentSpectrumData[instrumentNum][i];
                        float f = frequencyHz[i];
                        float p = energy / maxval;
                        centroidSum += p * f;

                        // gini
                        giniSum0 += (nSpectrum - i) * spec[i];
                        giniSum1 += spec[i];

                        // flatness
                        flatnessProduct *= energy;
                        flatnessSum += energy;
                    }
                    
                    currentSpectrumCentroids[instrumentNum] = centroidSum / nSpectrum;
                    currentSpectrumGiniCoeffs[instrumentNum] = (1f / nSpectrum) * (nSpectrum + 1 - 2 * (giniSum0 / giniSum1));
                    currentSpectrumFlatnesses[instrumentNum] = Mathf.Pow(flatnessProduct, 1 / nSpectrum) / (flatnessSum / nSpectrum);  
                }
            } 
        }              
    }

    void RefreshWaveform(Instrument inst, float envVal, List<GameObject> p)
    {
        float synthVal;
        float height;
        GameObject go;
        for (int n = 0; n < p.Count; n++)
        {
            go = mainWaveformPoints[n];
            synthVal = inst.CustomSynth(WaveformZoom * go.transform.localPosition.x); 
            height = Mathf.Lerp(ymin, ymax, 0.5f * (envVal * synthVal + 1f));
            go.transform.localPosition = new Vector3(go.transform.localPosition.x, height, go.transform.localPosition.z);
            go.transform.localScale = plotPointLocalScale;
        }
    }

    void RefreshWaveform(WaveType type, float freq, float strength, float envVal, List<GameObject> p)
    {
        float synthVal = 0f;
        float x;
        float height;
        GameObject go;
        for (int n = 0; n < p.Count; n++)
        {
            go = p[n];
            x = freq * WaveformZoom * go.transform.localPosition.x;

            switch (type)
            {
                case WaveType.SINE: synthVal = Mathf.Sin(x); break;
                case WaveType.SQUARE: synthVal = Instrument.Square(x); break;
                case WaveType.TRIANGLE: synthVal = Instrument.Triangle(x); break;
                case WaveType.SAWTOOTH: synthVal = Instrument.Sawtooth(x); break;
            }
            height = Mathf.Lerp(ymin, ymax,  0.5f * (strength * envVal * synthVal + 1f));
            go.transform.localPosition = new Vector3(go.transform.localPosition.x, height, go.transform.localPosition.z);
            go.transform.localScale = plotPointLocalScale;
        }
    }

    void RefreshSpectrogram(int instrumentNum, float envVal)
    {
        // spectrogram plot
        float height;
        GameObject go;
        for (int n = 0; n < spectrogramBars.Count; n++)
        {
            go = spectrogramBars[n];
            height = envVal * Mathf.Min(plotBarLocalScale.y * Mathf.Log(1 + currentSpectrumData[instrumentNum][n], 2), 6);
            go.transform.localScale = new Vector3(plotBarLocalScale.x, height, plotBarLocalScale.z);
        }
    }
    
    void RefreshAuxPlots(int instrumentNum, float envVal)
    {
        Instrument inst = synthController.GetInstrument(instrumentNum);
        RefreshWaveform(inst, envVal, mainWaveformPoints);
        RefreshWaveform(inst.wave1type, inst.wave1freq, inst.wave1Strength, envVal, waveform1Points);
        RefreshWaveform(inst.wave2type, inst.wave2freq, inst.wave2Strength, envVal, waveform2Points);
        RefreshWaveform(inst.wave3type, inst.wave3freq, inst.wave3Strength, envVal, waveform3Points);
        RefreshSpectrogram(instrumentNum, envVal);            
    }

    public float GetWaveformZoom()
    {
        return WaveformZoom;
    }

    public void SetWaveformZoom(float f)
    {
        WaveformZoom = f;
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
}
