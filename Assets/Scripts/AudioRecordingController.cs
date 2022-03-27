using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioRecordingController : MonoBehaviour
{
    public int tempo;
    double nextTime = 0;
    public GameObject[] sounds = new GameObject[8]; // this will contain the Oscillators and the AudioSources
    int numOscillators = 8;
    Oscillator[] osc = new Oscillator[8];
    AudioSource[] audioSource = new AudioSource[8];

    public AudioSource metronomeAudio;
    public bool metronomeOn;

    float timeT;
    int beatNum = 0;
    int startRecordingBeatNum;

    int recordingTrackNum;
    bool prerecording = false;
    bool recording = false;
    float[] recordedData;
    List<float> recordedDataList = new List<float>();

    private void Start()
    {
        // setup audio
        for (int i = 0; i < numOscillators; i++)
        {
            osc[i] = sounds[i].transform.GetComponent<Oscillator>();
            audioSource[i] = sounds[i].transform.GetComponent<AudioSource>();
        }
    }

    // Update is called once per frame
    void Update()
    {
        timeT += Time.deltaTime; 

        if (timeT >= nextTime)
        {
            beatNum++;

            if (metronomeOn)
            {
                metronomeAudio.Stop();
                metronomeAudio.Play();
            }
            
            if ((beatNum < startRecordingBeatNum + 5) && prerecording)
            {
                // initial metronome
                metronomeOn = true;
                Debug.Log("almost recording...");
                
            }
            else if ((beatNum > startRecordingBeatNum + 5) && (beatNum < startRecordingBeatNum + 13) && (prerecording || recording))
            {
                for (int i = 0; i < numOscillators; i++)
                {
                    osc[i].storeDataInBuffer = true;
                }

                prerecording = false;
                recording = true;
                int maxlength = 100000000;
                // recording ongoing
                Debug.Log("recording");

                for (int k = 0; k < maxlength; k++)
                {
                    for (int j = 0; j < numOscillators; j++)
                    {
                        if (k == osc[j].floatBuffer.Count)
                        {
                            break;
                        }
                    }

                    for (int i = 0; i < numOscillators; i++)
                    {                       
                        recordedDataList.Add(osc[i].floatBuffer[k]);
                    }                   
                }
                    
            }
            else if (recording)
            {
                recording = false;

                // end recording
                Debug.Log("done recording");
                AudioClip clip = AudioClip.Create("Track" + recordingTrackNum.ToString() + " clip", recordedDataList.Count, numOscillators, 44100, false);
                recordedData = recordedDataList.ToArray();
                clip.SetData(recordedData, 0);

                AudioSource src = transform.Find("Track" + recordingTrackNum.ToString()).GetComponent<AudioSource>();
                src.clip = clip;

                // stop metronome
                metronomeOn = false;

                for (int i = 0; i < numOscillators; i++)
                {
                    osc[i].ClearBuffer();
                    osc[i].storeDataInBuffer = false;
                }
            }
            nextTime = timeT + 60f / tempo;
        }

        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, Mathf.Infinity))
            {
                if (hit.transform.name == "Rec") { RecordTrack(int.Parse(hit.transform.parent.name.Substring(5))); }
                if (hit.transform.name == "Play") { PlayTrack(int.Parse(hit.transform.parent.name.Substring(5))); }
                if (hit.transform.name == "Stop") { StopTrack(int.Parse(hit.transform.parent.name.Substring(5))); }
            }
        }       
    }

    public void RecordTrack(int tracknum)
    {
        Debug.Log("recording started" + tracknum.ToString());

        startRecordingBeatNum = beatNum;

        recordingTrackNum = tracknum;

        prerecording = true;
    }

    public void PlayTrack(int tracknum)
    {
        StopTrack(tracknum);
        Debug.Log("play track" + tracknum.ToString());
        // play the currently stored track or do nothing
        transform.Find("Track" + tracknum.ToString()).GetComponent<AudioSource>().Play();
    }

    public void StopTrack(int tracknum)
    {
        Debug.Log("stop track" + tracknum.ToString());
        // stop the currently playing track or do nothing

        transform.Find("Track" + tracknum.ToString()).GetComponent<AudioSource>().Stop();
    }
}
