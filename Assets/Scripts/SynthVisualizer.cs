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

    // sound objects
    public float soundObjectUpdateTime;
    public GameObject singleSoundObject;
    Material singleSoundMaterial;
    public GameObject[] allSoundObjects;
    public GameObject[] allSoundMarkers;
    Material[] allSoundMaterials;

    // sound visualization settings
    public float noiseMultiplier;
    public float leftRightMultiplier;
    public float objectLerpSpeed;

    // sound data
    public SynthController synthController;
    float[] currentSynthValArray;
    bool isPlaying = false;
    public GetSpectrumData[] spectra;
    List<float> currentSpectrumData;
    List<float> currentFullSpectrumData;
    float currentSpectrumGiniCoeff;
    float currentSpectrumMaxFreqIndex;
    float currentSpectrumWidth;
    float currentVolume;
    int nSpectrum = 1000;

    // single instrument plot
    public TextMeshPro instrumentNumberLabel;

    // auxiliary plots
    public GameObject oscPlotZoomIn;
    public GameObject oscPlotZoomOut;
    public GameObject spectrogram;
    public GameObject plotPointPrefab;
    int numPlotPoints = 1000;
    List<GameObject> points = new List<GameObject>();
    List<GameObject> points1 = new List<GameObject>();
    List<GameObject> points2 = new List<GameObject>();
    float xmin = -5.75f;
    float xmax = 4.25f;
    float ymin = -3f;
    float ymax = 3f;
    public bool refresh = true;
    public float plotRefreshSpeed = 0.1f;
    #endregion

    private void Awake()
    {
        #region initialize aux plots
        float xPos = xmin;
        float xStep = (xmax - xmin) / numPlotPoints;
        GameObject[] auxPlots = new GameObject[] { oscPlotZoomIn, oscPlotZoomOut, spectrogram };
        List<GameObject>[] pointsLists = new List<GameObject>[] { points, points1, points2 };
        for (int n = 0; n < 3; n++)
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
        #endregion
    }

    // Start is called before the first frame update
    void Start()
    {
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
        allSoundMarkers[n].SetActive(true);
        if (isPlaying)
        {
            GameObject go = allSoundObjects[n];
            go.SetActive(true);

            //update sound left/right position based on pitch (currently determined as the location of the maximum in the spectrogram)
            // this method is currently glitchy
            float target_left_right_pos = Mathf.Lerp(xmin, xmax, currentSpectrumMaxFreqIndex / leftRightMultiplier);
            go.transform.localPosition = Vector3.MoveTowards(go.transform.localPosition, new Vector3(target_left_right_pos, go.transform.localPosition.y, go.transform.localPosition.z), objectLerpSpeed * Time.deltaTime);

            // update sound noise
            float noiseLevel = Mathf.Pow(noiseMultiplier * (1f - currentSpectrumGiniCoeff), 2);
            allSoundMaterials[n].SetFloat("_scale", noiseLevel);

            //TODO:

            //update sound size based on volume & envelope

            //update sound stretch based on the left/right spread of the spectrogram
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

        if (isPlaying)
        {
            singleSoundObject.SetActive(true);

            // update sound noise
            float noiseLevel = Mathf.Pow(noiseMultiplier * (1f - currentSpectrumGiniCoeff), 2);
            singleSoundMaterial.SetFloat("_scale", noiseLevel);

            //TODO:

            //update sound size based on volume & envelope

            //update sound stretch based on the left/right spread of the spectrogram
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

    void RefreshSoundData()
    {
        isPlaying = synthController.NumPlaying() > 0;
        // add up all spectrum data coming from instrument voices
        List<float> allSpectrumVals = new List<float>();
        List<float[]> allSpectra = new List<float[]>();

        List<float> currentFullSpectrumData = new List<float>();
        List<float[]> allFullSpectra = new List<float[]>();
        for (int j = 0; j < 8; j++)
        {
            allSpectra.Add(spectra[j].GetSpectrum());
            allFullSpectra.Add(spectra[j].GetFullSpectrum());
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
        }

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


        //calculate gini coefficient
        float sum0 = 0f;
        float sum1 = 0f;
        for (int i = 0; i < nSpectrum; i++)
        {
            sum0 += (nSpectrum - i) * sortedSpectrumList[i];
            sum1 += sortedSpectrumList[i];
        }
        currentSpectrumGiniCoeff = (1f / nSpectrum) * (nSpectrum + 1 - 2 * (sum0 / sum1));
    }

    void RefreshAuxPlots()
    {
        currentSynthValArray = new float[points.Count];

        // first aux plot
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
                        synthVal = synthController.CurrentInstrument().CustomSynth(go.transform.localPosition.x);
                    }
                }
            }
            currentSynthValArray[n] = synthVal;

            // map the synth val, which is between -1 and 1, to a height on the backplate 
            float height = Mathf.Lerp(ymin, ymax, 0.5f * (synthVal + 1f));
            go.transform.localPosition = new Vector3(go.transform.localPosition.x, height, go.transform.localPosition.z);
        }

        // second aux plot: for now, 20 times the frequency of the first one, to see LFO
        for (int n = 0; n < points1.Count; n++)
        {
            GameObject go = points1[n];
            float synthVal = 0f;
            if (synthController.NumPlaying() > 0)
            {
                for (int i = 0; i < synthController.sounds.Length; i++)
                {
                    if (synthController.OscillatorInUse(i))
                    {
                        synthVal = synthController.CurrentInstrument().CustomSynth(20 * go.transform.localPosition.x);
                    }
                }
            }

            // map the synth val, which is between -1 and 1, to a height on the backplate 
            float height = Mathf.Lerp(ymin, ymax, 0.5f * (synthVal + 1f));
            go.transform.localPosition = new Vector3(go.transform.localPosition.x, height, go.transform.localPosition.z);
        }

        // third aux plot: showing the current sound's spectrogram data
        for (int n = 0; n < points2.Count; n++)
        {
            GameObject go = points2[n];
            // map the freq val, which is between 0 and 1, to a height on the backplate using a log scale
            float height = Mathf.Lerp(ymin, ymax, Mathf.Log(1 + currentSpectrumData[n], 2));
            go.transform.localPosition = new Vector3(go.transform.localPosition.x, height, go.transform.localPosition.z);
        }
    }
}
