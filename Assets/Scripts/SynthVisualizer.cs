using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class SynthVisualizer : MonoBehaviour
{
    #region define objects

    // time
    float timeT = 0f;
    float nextTime = 0.5f;

    // sound objects
    public float soundObjectUpdateTime;
    public GameObject Sound0Object, Sound1Object, Sound2Object, Sound3Object;
    public GameObject Sound0Marker, Sound1Marker, Sound2Marker, Sound3Marker;
    GameObject[] soundObjects;
    GameObject[] soundMarkers;
    Material sound0material;
    Material sound1material;
    Material sound2material;
    Material sound3material;
    Material[] soundMaterials;

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
    public bool refreshVisuals = true;
    public float plotRefreshSpeed = 0.1f;
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

        sound0material = Sound0Object.transform.GetComponent<Renderer>().sharedMaterial;
        sound1material = Sound1Object.transform.GetComponent<Renderer>().sharedMaterial;
        sound2material = Sound2Object.transform.GetComponent<Renderer>().sharedMaterial;
        sound3material = Sound3Object.transform.GetComponent<Renderer>().sharedMaterial;
        soundObjects = new GameObject[] { Sound0Object, Sound1Object, Sound2Object, Sound3Object };
        soundMarkers = new GameObject[] { Sound0Marker, Sound1Marker, Sound2Marker, Sound3Marker };
        soundMaterials = new Material[] { sound0material, sound1material, sound2material, sound3material };
    }

    // Start is called before the first frame update
    void Start()
    {
        StartCoroutine(DelayStartRefreshVisuals());
        //StartCoroutine(SwitchTempObjectType());
    }

    private void Update()
    {
        timeT += Time.deltaTime;
        if (timeT > nextTime)
        {
            nextTime = timeT + soundObjectUpdateTime;
            int n = synthController.CurrentInstrumentNumber();
            soundMarkers[n].SetActive(true);
            if (isPlaying)
            {
                
                GameObject go = soundObjects[n];
                go.SetActive(true);
                

                //update sound left/right position based on pitch (hopefully the location of the maximum in the spectrogram)
                // this method is currently glitchy
                float target_left_right_pos = Mathf.Lerp(xmin, xmax, currentSpectrumMaxFreqIndex / leftRightMultiplier);
                go.transform.localPosition = Vector3.MoveTowards(go.transform.localPosition, new Vector3(target_left_right_pos, go.transform.localPosition.y, go.transform.localPosition.z), objectLerpSpeed * Time.deltaTime);

                // update sound noise
                float noiseLevel = Mathf.Pow(50f * (1f - currentSpectrumGiniCoeff), 2);
                soundMaterials[n].SetFloat("_scale", noiseLevel);

                //TODO:

                

                

                //update sound height based on volume

                //update sound width based on the left/right spread of the spectrogram
            }
            else
            {
                for (int i = 0; i < 4; i++)
                {
                    soundObjects[i].SetActive(false);
                    if (i != n)
                    {
                        soundMarkers[i].SetActive(false);
                    }
                    
                }                
            }
        }
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

        //sort current full spectrum data to calculate gini coefficient
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

        if (refreshVisuals)
        {
            RefreshAuxPlots();
        }
        StartCoroutine(RefreshVisuals());
    }

    void RefreshAuxPlots()
    {
        currentSynthValArray = new float[points.Count];
        isPlaying = false;

        // first aux plot
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

        // second aux plot: for now, 20 times the frequency of the first one, to see LFO
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

        // third aux plot: showing the current sound's spectrogram data
        for (int n = 0; n < points2.Count; n++)
        {
            GameObject go = points2[n];
            // map the freq val, which is between 0 and 1, to a height on the backplate using a log scale
            float height = Mathf.Lerp(ymin, ymax, Mathf.Log(1 + currentSpectrumData[n], 2));
            go.transform.localPosition = new Vector3(go.transform.localPosition.x, height, go.transform.localPosition.z);
        }
    }

    #region old code using pre-generated blender objects
    /*
    void RefreshSynthSoundObject()
    {
        #region using pre-generated blender objects
        
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
        
        #endregion
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
        return currentSynthSoundObjectHeight;        
    }

    int GetSynthSoundObjectWidth()
    {
        return currentSynthSoundObjectWidth;
    }

    float GetSynthSoundObjectNoise()
    {
        return currentSynthSoundObjectNoise;
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

    float MSE(List<float> l1, List<float> l2)
    {
        float total = 0;

        for (int i = 0; i < nSpectrum; i++)
        {
            total += (l1[i] - l2[i]) * (l1[i] - l2[i]);
        }
        return total;
    }
    */
    #endregion
}
