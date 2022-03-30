using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MidiJack;

public enum WaveType { SINE, SQUARE};

public class SynthController : MonoBehaviour
{
    #region define objects
    // gameobjects to load
    public GameObject[] sounds = new GameObject[8]; // this will contain the Oscillators and the AudioSources
    int numOscillators = 8;
    Oscillator[] osc = new Oscillator[8];
    AudioSource[] audioSource = new AudioSource[8];
    bool[] oscInUse = new bool[8];
    int numPlaying = 0;
    Dictionary<string, Oscillator> noteNameToOsc = new Dictionary<string, Oscillator>();
    Dictionary<string, int> noteNameToOscID = new Dictionary<string, int>();
    Dictionary<KeyCode, bool> keyPlaying = new Dictionary<KeyCode, bool>();
    Dictionary<int, bool> midi_keyPlaying = new Dictionary<int, bool>();

    public Instrument instrument0;
    public Instrument instrument1;
    public Instrument instrument2;
    public Instrument instrument3;
    Instrument[] instruments;
    public int InstrumentNumber = 0;

    public float volume = .07f;

    MusicTheory mt = new MusicTheory();

    public Material whiteKey;
    public Material blackKey;
    public Material highlightKey;
    #endregion

    // Start is called before the first frame update
    void Awake()
    {
        instruments = new Instrument[] { instrument0, instrument1, instrument2, instrument3};
        // setup audio
        for (int i = 0; i < numOscillators; i++)
        {
            osc[i] = sounds[i].transform.GetComponent<Oscillator>();
            osc[i].SetInstrument(instrument0);
            audioSource[i] = sounds[i].transform.GetComponent<AudioSource>();
            audioSource[i].Play();
            oscInUse[i] = false;
        }

        // register keyboard keys
        foreach (KeyCode keyName in mt.keyboardPianoMap.Keys)
        {
            keyPlaying[keyName] = false;
        }

        // register midi key numbers
        foreach (int i in mt.midiKeyNumbers)
        {
            midi_keyPlaying[i] = false;
        }
    }

    int GetFirstAvailableOscillatorNum()
    {
        for(int i = 0; i < numOscillators; i++)
        {
            if (!oscInUse[i])
            {
                return i;
            }
        }
        return -1;
    }

    void AssignNoteToOscillator(string noteName, int oscNum, int instrumentNum)
    {
        oscInUse[oscNum] = true;
        
        Oscillator o = osc[oscNum];
        noteNameToOsc.Add(noteName, o);
        noteNameToOscID.Add(noteName, oscNum);
        o.SetEnvelope(instruments[instrumentNum]);

        PlayNote(noteName);
        numPlaying++;

        //press key in the prefab
        Vector3 _p = transform.Find("Keys/Note" + noteName).localPosition;
        Transform key = transform.Find("Keys/Note" + noteName);
        key.localPosition = new Vector3(_p[0] - 0.04f, _p[1], _p[2]);
        key.GetComponent<MeshRenderer>().material = highlightKey;

    }

    void ReleaseOscillatorFromNote(string noteName)
    {
        noteNameToOsc[noteName].env.noteOff(noteNameToOsc[noteName].timeT);
        noteNameToOsc.Remove(noteName);
        oscInUse[noteNameToOscID[noteName]] = false;
        
        noteNameToOscID.Remove(noteName);
        numPlaying--;

        //unpress key in the prefab
        Vector3 _p = transform.Find("Keys/Note" + noteName).localPosition;
        Transform key = transform.Find("Keys/Note" + noteName);
        key.localPosition = new Vector3(_p[0] + 0.04f, _p[1], _p[2]);
        key.GetComponent<MeshRenderer>().material = key.name.Contains("b") ? blackKey : whiteKey;
    }

    IEnumerator TurnOffNoteAfterDelay(string noteName, float delay)
    {
        yield return new WaitForSeconds(delay);
        ReleaseOscillatorFromNote(noteName);
    }

    // Update is called once per frame
    void Update()
    {
        CheckForComputerKeyboardAction();
        CheckForMidiKeyboardAction();
    }

    private void OnDestroy()
    {
        for (int i = 0; i < numOscillators; i++)
        {
            audioSource[i].Stop();
        }
    }

    void CheckForComputerKeyboardAction()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1)) { InstrumentNumber = 0; }
        if (Input.GetKeyDown(KeyCode.Alpha2)) { InstrumentNumber = 1; }
        if (Input.GetKeyDown(KeyCode.Alpha3)) { InstrumentNumber = 2; }
        if (Input.GetKeyDown(KeyCode.Alpha4)) { InstrumentNumber = 3; }

        foreach (KeyCode keyName in mt.keyboardPianoMap.Keys)
        {
            string keyboard_noteName = mt.keyboardPianoMap[keyName];
            if (Input.GetKeyDown(keyName) && !keyPlaying[keyName])
            {
                int oscNum = GetFirstAvailableOscillatorNum();
                if (oscNum != -1)
                {
                    AssignNoteToOscillator(keyboard_noteName, oscNum, InstrumentNumber);
                    keyPlaying[keyName] = true;
                }
            }

            if (Input.GetKeyUp(keyName) && keyPlaying[keyName])
            {
                ReleaseOscillatorFromNote(keyboard_noteName);
                keyPlaying[keyName] = false;
            }
        }
    }

    void CheckForMidiKeyboardAction()
    {
        foreach (int i in mt.midiKeyNumbers)
        {
            string midi_noteName = mt.midiPianoMap[i];
            if (MidiMaster.GetKeyDown(i) && !midi_keyPlaying[i])
            {
                int oscNum = GetFirstAvailableOscillatorNum();
                if (oscNum != -1)
                {
                    AssignNoteToOscillator(midi_noteName, oscNum, InstrumentNumber);
                    midi_keyPlaying[i] = true;
                }
            }

            if (MidiMaster.GetKeyUp(i) && midi_keyPlaying[i])
            {
                ReleaseOscillatorFromNote(midi_noteName);
                midi_keyPlaying[i] = false;
            }
        }
    }

    public void PlayNote(string noteName)
    {
        Note n = new Note(mt.GetNoteFrequency(noteName), volume, instruments[InstrumentNumber]);
        noteNameToOsc[noteName].PlayNote(n);
    }

    public void PlayNote(string noteName, float noteOffDelay)
    {
        PlayNote(noteName);
        IEnumerator delay = TurnOffNoteAfterDelay(noteName, noteOffDelay);
        StartCoroutine(delay);
    }

    public bool OscillatorInUse(int i)
    {
        return oscInUse[i];
    }

    public int NumPlaying()
    {
        return numPlaying;
    }

    public Instrument CurrentInstrument()
    {
        return instruments[InstrumentNumber];
    }  

    public int CurrentInstrumentNumber()
    {
        return InstrumentNumber;
    }
}
