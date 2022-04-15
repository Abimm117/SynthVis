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
    public bool refreshAux = false;
    public float refreshSpeed;
    public float noiseMultiplier;
    public float noiseExponent;
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
    //public float giniSlowdownMultiplier;
    public float[] currentSpectrumGiniCoeffs = new float[4];
    //public float[] previousSpectrumGiniCoeffs = new float[4];
    public float[] currentSpectrumCentroids = new float[4];
    public float[] currentSpectrumSpreads = new float[4];
    public float[] currentSpectrumFlatnesses = new float[4];

    public float[] currentNoises = new float[4];
    public float[] currentBrightnesses = new float[4];
    public float[] currentSpeeds = new float[4];
    
    int nSpectrum = 8192;
    List<float> frequencyHz;    

    // auxiliary plots
    public GameObject waveformPlot;
    public GameObject waveform1Plot;
    public GameObject waveform2Plot;
    public GameObject waveform3Plot;
    public GameObject spectrogram;
    public GameObject plotPointPrefab;
    public float spectrogramHeightShift;
    public float waveformPlotZoom;
    public float spectrogramZoom;
    int numPlotPoints = 1000;
    List<GameObject> points = new List<GameObject>();
    List<GameObject> points1 = new List<GameObject>();
    List<GameObject> points2 = new List<GameObject>();
    List<GameObject> points3 = new List<GameObject>();
    
    float xmin = -5.75f;
    float xmax = 4.25f;
    float ymin = -3f;
    float ymax = 3f;

    #endregion

    void LoadSingleSoundObject()
    {
        singleSoundObject.SetActive(false);
        singleSound = singleSoundObject;
        savedObjectScaleSingle = savedObjectscale;
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
        GameObject[] auxPlots = new GameObject[] { waveformPlot, waveform1Plot, waveform2Plot, waveform3Plot};
        List<GameObject>[] pointsLists = new List<GameObject>[] { points, points1, points2, points3 };
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
            //Debug.Log(i.ToString() + ": " + frequencyHz[i].ToString());
        }

        #endregion

        #region load sound object materials
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

    float FreqToMel(float freq)
    {
        return 2595f * Mathf.Log(1 + freq / 500);
    }

    void UpdateSoundObject(GameObject go, float envVal, int soundNum, bool isSingleSound)
    {
        
        go.SetActive(true);
        //float gini = giniSlowdownMultiplier * currentSpectrumGiniCoeffs[soundNum] + (1 - giniSlowdownMultiplier) * previousSpectrumGiniCoeffs[soundNum];
        currentNoises[soundNum] = Mathf.Pow(envVal, noiseEnvelopeExponent) * noiseMultiplier * Mathf.Pow(1f - currentSpectrumGiniCoeffs[soundNum], noiseExponent);
        currentBrightnesses[soundNum] = centroidMultiplier * FreqToMel(currentSpectrumCentroids[soundNum]);
        currentSpeeds[soundNum] = .1f;//synthController.GetInstrumentTotalLFO(soundNum);
        Vector3 color = ColorStringToRGB(soundNum);
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
        go.transform.localScale = Mathf.Pow(envVal, 2) * scale;
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
                    currentSpectrumCentroids[instrumentNum] = centroidSum / nSpectrum;

                    //calculate spectrum gini coefficient

                    //Array.Copy(currentSpectrumGiniCoeffs, previousSpectrumGiniCoeffs, 4);

                    //sort current spectrum data to calculate gini coefficient
                    Array.Sort(currentSpectrumData[instrumentNum]);
                    float giniSum0 = 0f;
                    float giniSum1 = 0f;
                    for (int i = 0; i < nSpectrum; i++)
                    {
                        giniSum0 += (nSpectrum - i) * currentSpectrumData[instrumentNum][i];
                        giniSum1 += currentSpectrumData[instrumentNum][i];
                    }
                    currentSpectrumGiniCoeffs[instrumentNum] = (1f / nSpectrum) * (nSpectrum + 1 - 2 * (giniSum0 / giniSum1)); // (nSpectrum + 1 - 2 * (giniSum0 / giniSum1));
                               
                    /*
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
                    */
                }
            } 
        }              
    }

    
    void RefreshAuxPlots(int instrumentNum, float envVal)
    {
        // waveform plot
        for (int n = 0; n < points.Count; n++)
        {
            GameObject go = points[n];
            float synthVal = synthController.GetInstrument(instrumentNum).CustomSynth(waveformPlotZoom * go.transform.localPosition.x);               
                        

            // map the synth val, which is between -1 and 1, to a height on the backplate
            // represent the envelope by scaling the waveform amplitude over time
            float height = Mathf.Lerp(ymin, ymax, 0.5f * (envVal * synthVal + 1f));
            go.transform.localPosition = new Vector3(go.transform.localPosition.x, height, go.transform.localPosition.z);
        }

        // waveform1 plot
        WaveType type = synthController.GetInstrument(instrumentNum).wave1type;
        float freq = synthController.GetInstrument(instrumentNum).wave1freq;
        float strength = synthController.GetInstrument(instrumentNum).wave1Strength;
        for (int n = 0; n < points1.Count; n++)
        {
            GameObject go = points1[n];
            float synthVal1 = 0f;// = func1(waveformPlotZoom * go.transform.localPosition.x);
            
            switch (type)
            {
                case WaveType.SINE: synthVal1 = Mathf.Sin(freq * waveformPlotZoom * go.transform.localPosition.x); break;
                case WaveType.SQUARE: synthVal1 = Instrument.Square(freq * waveformPlotZoom * go.transform.localPosition.x); break;
                case WaveType.TRIANGLE: synthVal1 = Instrument.Triangle(freq * waveformPlotZoom * go.transform.localPosition.x); break;
                case WaveType.SAWTOOTH: synthVal1 = Instrument.Sawtooth(freq * waveformPlotZoom * go.transform.localPosition.x); break;
            }

            // map the synth val, which is between -1 and 1, to a height on the backplate
            // represent the envelope by scaling the waveform amplitude over time
            float height1 = Mathf.Lerp(ymin, ymax, strength * 0.5f * (envVal * synthVal1 + 1f));
            go.transform.localPosition = new Vector3(go.transform.localPosition.x, height1, go.transform.localPosition.z);
        }

        // waveform2 plot
        type = synthController.GetInstrument(instrumentNum).wave2type;
        freq = synthController.GetInstrument(instrumentNum).wave2freq;
        strength = synthController.GetInstrument(instrumentNum).wave2Strength;
        for (int n = 0; n < points2.Count; n++)
        {
            GameObject go = points2[n];
            float synthVal2 = 0f;// = func1(waveformPlotZoom * go.transform.localPosition.x);
            
            switch (type)
            {
                case WaveType.SINE: synthVal2 = Mathf.Sin(freq * waveformPlotZoom * go.transform.localPosition.x); break;
                case WaveType.SQUARE: synthVal2 = Instrument.Square(freq * waveformPlotZoom * go.transform.localPosition.x); break;
                case WaveType.TRIANGLE: synthVal2 = Instrument.Triangle(freq * waveformPlotZoom * go.transform.localPosition.x); break;
                case WaveType.SAWTOOTH: synthVal2 = Instrument.Sawtooth(freq * waveformPlotZoom * go.transform.localPosition.x); break;
            }

            // map the synth val, which is between -1 and 1, to a height on the backplate
            // represent the envelope by scaling the waveform amplitude over time
            float height2 = Mathf.Lerp(ymin, ymax, strength *  0.5f * (envVal * synthVal2 + 1f));
            go.transform.localPosition = new Vector3(go.transform.localPosition.x, height2, go.transform.localPosition.z);
        }

        // waveform3 plot
        type = synthController.GetInstrument(instrumentNum).wave3type;
        freq = synthController.GetInstrument(instrumentNum).wave3freq;
        strength = synthController.GetInstrument(instrumentNum).wave3Strength;
        for (int n = 0; n < points3.Count; n++)
        {
            GameObject go = points3[n];
            float synthVal3 = 0f;// = func1(waveformPlotZoom * go.transform.localPosition.x);
            switch (type)
            {
                case WaveType.SINE: synthVal3 = Mathf.Sin(freq * waveformPlotZoom * go.transform.localPosition.x); break;
                case WaveType.SQUARE: synthVal3 = Instrument.Square(freq * waveformPlotZoom * go.transform.localPosition.x); break;
                case WaveType.TRIANGLE: synthVal3 = Instrument.Triangle(freq * waveformPlotZoom * go.transform.localPosition.x); break;
                case WaveType.SAWTOOTH: synthVal3 = Instrument.Sawtooth(freq * waveformPlotZoom * go.transform.localPosition.x); break;
            }

            // map the synth val, which is between -1 and 1, to a height on the backplate
            // represent the envelope by scaling the waveform amplitude over time
            float height3 = Mathf.Lerp(ymin, ymax, strength * 0.5f * (envVal * synthVal3 + 1f));
            go.transform.localPosition = new Vector3(go.transform.localPosition.x, height3, go.transform.localPosition.z);
        }

        /*
        // spectrogram plot
        for (int n = 0; n < points2.Count; n++)
        {
            GameObject go = points2[n];
            // map the freq val, which is between 0 and 1, to a height on the backplate using a log scale
            float height = Mathf.Lerp(ymin, ymax, Mathf.Log(1 + currentSpectrumData[instrumentNum][n], 2));
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

        }*/
    }
    
}
