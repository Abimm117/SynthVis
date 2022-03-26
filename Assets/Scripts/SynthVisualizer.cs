using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class SynthVisualizer : MonoBehaviour
{
    #region define objects
    public SynthController synthController;

    // custom synth object
    public GameObject SynthSoundObjects0Parent;
    public GameObject SynthSoundObjects1Parent;
    public GameObject SynthSoundObjects2Parent;
    public GameObject SynthSoundObjects3Parent;
    public int[] heights = new int[] { 1, 3, 5 };
    public int[] widths = new int[] { -1, 0, 1 };
    public float[] noises = new float[] { 0f, 0.25f, 0.5f, 0.75f, 1f };
    int synthSoundObjectHeight;
    int synthSoundObjectWidth;
    float synthSoundObjectNoise;
    int currentSynthSoundObjectHeight;
    int currentSynthSoundObjectWidth;
    float currentSynthSoundObjectNoise;
    int synthSoundObjectTypeCounter = 0;
    string currentSynthSoundObjectName = "";
    float[] currentSynthValArray;
    bool isPlaying = false;

    // plot
    public GameObject oscPlotZoomIn;
    public GameObject oscPlotZoomOut;
    public GameObject spectrogram;
    public GameObject plotPointPrefab;
    int numPlotPoints = 550;
    List<GameObject> points = new List<GameObject>();
    List<GameObject> points1 = new List<GameObject>();
    List<GameObject> points2 = new List<GameObject>();
    float xmin = -5.75f;
    float xmax = 4.25f;
    float ymin = -3f;
    float ymax = 3f;
    public bool refreshVisuals = true;
    public float plotRefreshSpeed = 0.1f;

    //vowels
    List<float> ahy = new List<float>();
    List<float> eey = new List<float>();
    List<float> ooy = new List<float>();
    public string nearestVowel;

    //spectra
    public GetSpectrumData spec1;
    public GetSpectrumData spec2;
    public GetSpectrumData spec3;
    public GetSpectrumData spec4;
    public GetSpectrumData spec5;
    public GetSpectrumData spec6;
    public GetSpectrumData spec7;
    public GetSpectrumData spec8;
    GetSpectrumData[] spectra;
    public List<float> currentSpectrumData; 
    int nSpectrum = 550;
    #endregion

    private void Awake()
    {
        // render plot points
        float xPos = xmin;
        float xStep = (xmax - xmin) / numPlotPoints;
        for (int i = 0; i < numPlotPoints; i++)
        {
            Vector3 pos = new Vector3(xPos + xStep * i, 0, -0.5f);
            GameObject go = Instantiate(plotPointPrefab, Vector3.zero, Quaternion.identity);
            points.Add(go);
            go.transform.parent = oscPlotZoomIn.transform;
            go.transform.localPosition = pos;
            go.transform.localScale = .05f * Vector3.one;
        }

        for (int i = 0; i < numPlotPoints; i++)
        {
            Vector3 pos = new Vector3(xPos + xStep * i, 0, -0.5f);
            GameObject go = Instantiate(plotPointPrefab, Vector3.zero, Quaternion.identity);
            points1.Add(go);
            go.transform.parent = oscPlotZoomOut.transform;
            go.transform.localPosition = pos;
            go.transform.localScale = .05f * Vector3.one;
        }

        for (int i = 0; i < numPlotPoints; i++)
        {
            Vector3 pos = new Vector3(xPos + xStep * i, 0, -0.5f);
            GameObject go = Instantiate(plotPointPrefab, Vector3.zero, Quaternion.identity);
            points2.Add(go);
            go.transform.parent = spectrogram.transform;
            go.transform.localPosition = pos;
            go.transform.localScale = .05f * Vector3.one;
        }
        spectra = new GetSpectrumData[] { spec1, spec2, spec3, spec4, spec5, spec6, spec7, spec8 };

        LoadVowels();

        currentSpectrumData = new List<float>();
        for (int n = 0; n < 550; n++)
        {
            currentSpectrumData.Add(0f);
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        StartCoroutine(DelayStartRefreshVisuals());
        StartCoroutine(SwitchTempObjectType());   
    }

    IEnumerator DelayStartRefreshVisuals()
    {
        yield return new WaitForSeconds(0.5f);
        StartCoroutine(RefreshVisuals());
    }

    IEnumerator RefreshVisuals()
    {
        yield return new WaitForSeconds(plotRefreshSpeed);

        // add up all spectrum data coming from instrument voices
        List<float> allSpectrumVals = new List<float>();
        List<float[]> allSpectra = new List<float[]>();
        for (int j = 0; j < 8; j++)
        {
            allSpectra.Add(spectra[j].GetSpectrum());
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

        //then, adjust currentSpectrumData so that it is bounded between 0 and 1 using the max val from allSpectrumVals
        float maxval = Mathf.Max(allSpectrumVals.ToArray());
        currentSpectrumData = new List<float>();
        for (int i = 0; i < nSpectrum; i++)
        {
            currentSpectrumData.Add(allSpectrumVals[i] / maxval);
        }

        //determine the nearest vowel based on currentSpectrumData
        nearestVowel = GetNearestVowel();

        if (refreshVisuals)
        {
            RefreshPlot();
            RefreshSynthSoundObject();
        }
        StartCoroutine(RefreshVisuals());
    }

    void RefreshPlot()
    {
        currentSynthValArray = new float[points.Count];
        isPlaying = false;

        // first plot
        for (int n = 0; n < points.Count; n++)
        {
            GameObject go = points[n];
            float synthVal = 0f;
            if (synthController.NumPlaying() > 0)
            {
                for (int i = 0; i < 5; i++)
                {
                    if (synthController.OscillatorInUse(i))
                    {
                        synthVal = synthController.CurrentInstrument().CustomSynth(go.transform.localPosition.x);
                    }
                }
                isPlaying = true;
            }
            currentSynthValArray[n] = synthVal;

            // map the synth val, which is between -1 and 1, to a height on the backplate 
            float height = Mathf.Lerp(ymin, ymax, 0.5f * (synthVal + 1f));
            go.transform.localPosition = new Vector3(go.transform.localPosition.x, height, go.transform.localPosition.z);
        }

        // second plot: for now, 20 times the frequency of the first one, to see LFO
        for (int n = 0; n < points1.Count; n++)
        {
            GameObject go = points1[n];
            float synthVal = 0f;
            if (synthController.NumPlaying() > 0)
            {
                for (int i = 0; i < 5; i++)
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

        // third plot: showing the current sound's spectrogram data
        for (int n = 0; n < points2.Count; n++)
        {
            GameObject go = points2[n];
            // map the freq val, which is between 0 and 1, to a height on the backplate 
            float height = Mathf.Lerp(ymin, ymax, currentSpectrumData[n]);
            go.transform.localPosition = new Vector3(go.transform.localPosition.x, height, go.transform.localPosition.z);
        }
    }

    void RefreshSynthSoundObject()
    {
        if (isPlaying)
        {
            string _name = GetSynthSoundObjectName();
            if (currentSynthSoundObjectName == "")
            {
                SynthSoundObjects0Parent.transform.Find(_name).gameObject.SetActive(true);
                currentSynthSoundObjectName = _name;
            }
            else if (_name != currentSynthSoundObjectName)
            {
                Debug.Log("DING NEW OBJECT");
                Debug.Log(nearestVowel);
                SynthSoundObjects0Parent.transform.Find(currentSynthSoundObjectName).gameObject.SetActive(false);
                SynthSoundObjects0Parent.transform.Find(_name).gameObject.SetActive(true);
                currentSynthSoundObjectName = _name;
            }
        }
        else
        {
            if (currentSynthSoundObjectName != "")
            {
                SynthSoundObjects0Parent.transform.Find(currentSynthSoundObjectName).gameObject.SetActive(false);
                currentSynthSoundObjectName = "";
            }
        }
    }

    string GetSynthSoundObjectName()
    {
        // display the corresponding sound object based on the floats in the custom synth array
        synthSoundObjectHeight = GetSynthSoundObjectHeight();
        synthSoundObjectWidth = GetSynthSoundObjectWidth();
        synthSoundObjectNoise = GetSynthSoundObjectNoise();
        string result = "sound";
        result += "_h" + synthSoundObjectHeight.ToString();
        result += "_w" + synthSoundObjectWidth.ToString();
        result += "_n" + synthSoundObjectNoise.ToString();
        return result;
    }

    int GetSynthSoundObjectHeight()
    {
        if (nearestVowel == "ah")
        {
            return 5;
        }
        else if (nearestVowel == "ee")
        {
            return 3;
        }
        else if (nearestVowel == "oo")
        {
            return 1;
        }
        else
        {
            return currentSynthSoundObjectHeight;
        }
    }

    int GetSynthSoundObjectWidth()
    {
        if (nearestVowel == "ah")
        {
            return 0;
        }
        else if (nearestVowel == "ee")
        {
            return 1;
        }
        else if (nearestVowel == "oo")
        {
            return -1;
        }
        else
        {
            return currentSynthSoundObjectWidth;
        }
    }

    float GetSynthSoundObjectNoise()
    {
        if (nearestVowel == "ah")
        {
            return 0.5f;
        }
        else if (nearestVowel == "ee")
        {
            return 0.25f;
        }
        else if (nearestVowel == "oo")
        {
            return 1f;
        }
        else
        {
            return currentSynthSoundObjectNoise;
        }
    }

    IEnumerator SwitchTempObjectType()
    {
        yield return new WaitForSeconds(plotRefreshSpeed);
        int w_ix = synthSoundObjectTypeCounter % widths.Length;
        int h_ix = (synthSoundObjectTypeCounter % (widths.Length * heights.Length)) / heights.Length;      
        int n_ix = synthSoundObjectTypeCounter / (widths.Length * heights.Length);

        currentSynthSoundObjectHeight = heights[h_ix];
        currentSynthSoundObjectWidth = widths[w_ix];
        currentSynthSoundObjectNoise = noises[n_ix];

        synthSoundObjectTypeCounter = (synthSoundObjectTypeCounter + 1) % (heights.Length * widths.Length * noises.Length);

        StartCoroutine(SwitchTempObjectType());
    }

    string GetNearestVowel()
    {
        float ah_mse = MSE(ahy, currentSpectrumData);
        float ee_mse = MSE(eey, currentSpectrumData);
        float oo_mse = MSE(ooy, currentSpectrumData);

        if (ah_mse < ee_mse && ah_mse < oo_mse)
        {
            return "ah";
        }
        else if (ee_mse < ah_mse && ee_mse < oo_mse)
        {
            return "ee";
        }
        else if (oo_mse < ah_mse && oo_mse < ee_mse)
        {
            return "oo";
        }
        else
        {
            return "none";
        }
    }

    void LoadHistogram(string filename, ref List<float> ylist)
    {
        using (StreamReader sr = new StreamReader(filename))
        {
            string line;
            int linecounter = 0;
            while ((line = sr.ReadLine()) != null)
            {
                if (linecounter > 0)
                {
                    string[] linestrings = line.Split(',');
                    float f = float.Parse(linestrings[1]);
                    ylist.Add(f);
                }
                linecounter++;
            }
        }
    }

    void LoadVowels()
    {
        LoadHistogram("Assets/vowels/ah_interp.csv", ref ahy);
        LoadHistogram("Assets/vowels/ee_interp.csv", ref eey);
        LoadHistogram("Assets/vowels/oo_interp.csv", ref ooy);
    }

    float MSE(List<float> l1, List<float> l2)
    {
        float total = 0;

        for (int i = 0; i < nSpectrum; i++)
        {
            total += (l1[i] - l2[i]) * (l1[i] - l2[i]);
        }
        return total;
    }
}
