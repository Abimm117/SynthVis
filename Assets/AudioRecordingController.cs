using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioRecordingController : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
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
        // play four beat countdown

        // record for 8 beats

        //stop recording

        //play back the recorded track
    }

    public void PlayTrack(int tracknum)
    {
        Debug.Log("play track" + tracknum.ToString());
        // play the currently stored track or do nothing
    }

    public void StopTrack(int tracknum)
    {
        Debug.Log("stop track" + tracknum.ToString());
        // stop the currently playing track or do nothing
    }
}
