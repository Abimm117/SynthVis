using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using TMPro;

public enum VisualizationMode { SingleInstrument, AllInstruments}

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
    public GameObject singleSoundObject;
    Vector3 savedObjectScaleSingle;
    Material singleSoundMaterial;
    public TextMeshPro instrumentNumberLabel;

    // sound visualization settings
    public bool refresh = true;
    public float plotRefreshSpeed = 0.1f;
    public float soundObjectUpdateTime;
    public float noiseMultiplier;
    public float frequencyPositionMultiplier;
    public float frequencyPositionShift;
    public float objectLerpSpeed;

    // sound data
    public SynthController synthController;
    bool isPlaying = false;
    public GetSpectrumData[] spectra;
    List<float> currentSpectrumData;
    List<float> currentFullSpectrumData;
    public float currentSpectrumGiniCoeff;
    public float currentSpectrumCentroid;
    public float currentSpectrumSpread;
    public float currentSpectrumFlatness;
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

    private void Awake()
    {
        #region initialize aux plots
        float xPos = xmin;
        float xStep = (xmax - xmin) / numPlotPoints;
        GameObject[] auxPlots = new GameObject[] { waveformPlot, spectrogram};//{ oscPlotZoomIn, oscPlotZoomOut, spectrogram };
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
        currentSpectrumData = new List<float>();
        currentFullSpectrumData = new List<float>();
        for (int n = 0; n < nSpectrum; n++)
        {
            currentSpectrumData.Add(0f);
        }
        for (int n = 0; n < 4096; n++)
        {
            currentFullSpectrumData.Add(0f);
        }
        #endregion

        #region load materials
        singleSoundMaterial = singleSoundObject.transform.GetComponent<Renderer>().sharedMaterial;
        allSoundMaterials = new Material[allSoundObjects.Length];
        for (int i = 0; i < 4; i++)
        {
            allSoundMaterials[i] = allSoundObjects[i].transform.GetComponent<Renderer>().sharedMaterial;
        }

        savedObjectScaleAll = allSoundObjects[0].transform.localScale;
        savedObjectScaleSingle = singleSoundObject.transform.localScale;
        #endregion
    }

    // Start is called before the first frame update
    void Start()
    {
        float sampleRate = 44100f;
        int bufferLength = nSpectrum/2;
        int nSamplePerWave = Mathf.RoundToInt(bufferLength / (446.16f / (sampleRate / bufferLength)));
        frequencyHz = makeArr(0, 32*nSamplePerWave, nSpectrum);
        StartCoroutine(Refresh());
    }

    private void Update()
    {
        timeT += Time.deltaTime;
        if (timeT > nextTime)
        {
            nextTime = timeT + soundObjectUpdateTime;            
            if (mode == VisualizationMode.AllInstruments)
            {
                UpdateAllInstrumentPlot();
            }
            else if (mode == VisualizationMode.SingleInstrument)
            {
                UpdateSingleInstrumentPlot();
            }           
        }
    }

    void UpdateAllInstrumentPlot()
    {
        int n = synthController.CurrentInstrumentNumber();
        float envVal = (float)synthController.CurrentEnvelopeValue() + .0001f;

        allSoundMarkers[n].SetActive(true);
        if (isPlaying)
        {
            GameObject go = allSoundObjects[n];
            go.SetActive(true);

            //update sound position based on pitch (currently determined as the location of the maximum in the spectrogram)
            // represent this on log scale because human perception of audio is logarithmic
            // this method is currently glitchy
            float target_frequency_pos = Mathf.Lerp(xmin, xmax, Mathf.Log(1 + (currentSpectrumCentroid * frequencyPositionMultiplier + frequencyPositionShift) / 4096f, 2));
            go.transform.localPosition = Vector3.MoveTowards(go.transform.localPosition, new Vector3(target_frequency_pos, go.transform.localPosition.y, go.transform.localPosition.z), objectLerpSpeed * Time.deltaTime);

            // update sound noise
            float noiseLevel = Mathf.Pow(noiseMultiplier * (1f - currentSpectrumGiniCoeff), 2);
            allSoundMaterials[n].SetFloat("_scale", noiseLevel);

            //TODO:

            //update sound size based on volume & envelope
            go.transform.localScale = envVal * savedObjectScaleAll;

            //update sound stretch based on the currentSpectrumSpread of the spectrogram
        }
        else if (envVal > .0001f)
        {
            GameObject go = allSoundObjects[n];
            go.transform.localScale = envVal * savedObjectScaleAll;
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
        float envVal = (float)synthController.CurrentEnvelopeValue() + .0001f;

        if (isPlaying)
        {
            singleSoundObject.SetActive(true);

            // update sound noise
            float noiseLevel = Mathf.Pow(noiseMultiplier * (1f - currentSpectrumGiniCoeff), 2);
            singleSoundMaterial.SetFloat("_scale", noiseLevel);

            //TODO:

            //update sound size based on volume & envelope
            
            singleSoundObject.transform.localScale = envVal * savedObjectScaleSingle;

            //update sound stretch based on the left/right spread of the spectrogram
        }
        else if (envVal > .00001f)
        {
            singleSoundObject.transform.localScale = envVal * savedObjectScaleSingle;
        }
        else
        {
            singleSoundObject.SetActive(false);
        }
    }

    IEnumerator Refresh()
    {
        yield return new WaitForSeconds(plotRefreshSpeed);

        if (refresh)
        {
            RefreshSoundData();

            if (mode == VisualizationMode.SingleInstrument)
            {
                singleInstrumentPlot.SetActive(true);
                allInstrumentPlot.SetActive(false);
                RefreshAuxPlots();
            }
            else if (mode == VisualizationMode.AllInstruments)
            {
                singleInstrumentPlot.SetActive(false);
                allInstrumentPlot.SetActive(true);
            }           
        }
        StartCoroutine(Refresh());
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
            List<float> allSpectrumVals = new List<float>();
            List<float[]> allSpectra = new List<float[]>();

            //List<float> currentFullSpectrumData = new List<float>();
            //List<float[]> allFullSpectra = new List<float[]>();
            for (int j = 0; j < 8; j++)
            {
                allSpectra.Add(spectra[j].GetSpectrum());
                //allFullSpectra.Add(spectra[j].GetFullSpectrum());
            }
            for (int i = 0; i < nSpectrum; i++)
            {
                float val = 0f;
                for (int j = 0; j < 8; j++)
                {
                    val += allSpectra[j][i];
                }
                allSpectrumVals.Add(val);
            }

            /*
            currentSpectrumMaxFreqIndex = 0;
            float currentMaxVal = 0f;
            for (int i = 0; i < 4096; i++)
            {
                float val = 0f;
                for (int j = 0; j < 8; j++)
                {
                    val += allFullSpectra[j][i];
                }
                if (val > currentMaxVal)
                {
                    currentMaxVal = val;
                    currentSpectrumMaxFreqIndex = i;
                }
                currentFullSpectrumData.Add(val);
            }*/

            //then, adjust currentSpectrumData so that it is bounded between 0 and 1 using the max val from allSpectrumVals
            float maxval = Mathf.Max(allSpectrumVals.ToArray());
            currentSpectrumData = new List<float>();
            for (int i = 0; i < nSpectrum; i++)
            {
                float specVal = allSpectrumVals[i] / maxval;
                currentSpectrumData.Add(specVal);
            }

            //sort current spectrum data to calculate gini coefficient
            List<float> sortedSpectrumList = new List<float>();
            for (int i = 0; i < nSpectrum; i++)
            {
                sortedSpectrumList.Add(currentSpectrumData[i]);
            }
            sortedSpectrumList.Sort();

            //calculate spectrum centroid
            float s = 0f;
            for (int i = 0; i < nSpectrum; i++)
            {
                float p = currentSpectrumData[i];
                float f = frequencyHz[i];
                s += p * f;
            }
            currentSpectrumCentroid = (1f/numPlaying) * s / nSpectrum;

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


            //calculate spectrum gini coefficient
            s0 = 0f;
            s1 = 0f;
            for (int i = 0; i < nSpectrum; i++)
            {
                s0 += (nSpectrum - i) * sortedSpectrumList[i];
                s1 += sortedSpectrumList[i];
            }
            currentSpectrumGiniCoeff = (1f / nSpectrum) * (nSpectrum + 1 - 2 * (s0 / s1));
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
