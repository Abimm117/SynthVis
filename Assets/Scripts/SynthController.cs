using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MidiJack;
using UnityEngine.UI;
using System;

public class Dictionary<TKey1, TKey2, TValue> : Dictionary<Tuple<TKey1, TKey2>, TValue>, IDictionary<Tuple<TKey1, TKey2>, TValue>
{

    public TValue this[TKey1 key1, TKey2 key2]
    {
        get { return base[Tuple.Create(key1, key2)]; }
        set { base[Tuple.Create(key1, key2)] = value; }
    }

    public void Add(TKey1 key1, TKey2 key2, TValue value)
    {
        base.Add(Tuple.Create(key1, key2), value);
    }

    public bool ContainsKey(TKey1 key1, TKey2 key2)
    {
        return base.ContainsKey(Tuple.Create(key1, key2));
    }
}

public class SynthController : MonoBehaviour
{
    #region define objects
    // gameobjects to load
    public GameObject[] sounds = new GameObject[8]; // this will contain the Oscillators and the AudioSources
    int numOscillators = 8;
    Oscillator[] osc = new Oscillator[8];
    AudioSource[] audioSource = new AudioSource[8];
    bool[] oscInUse = new bool[8];
    public int[] oscToInstrumentMap = new int[8];
    int numPlaying = 0;
    Dictionary<string, int, Oscillator> noteNameAndInstToOsc = new Dictionary<string, int, Oscillator>();
    Dictionary<string, int, int> noteNameAndInstToOscID = new Dictionary<string, int, int>();
    Dictionary<KeyCode, bool> keyPlaying = new Dictionary<KeyCode, bool>();
    Dictionary<int, bool> midi_keyPlaying = new Dictionary<int, bool>();

    public Instrument instrument0;
    public Instrument instrument1;
    public Instrument instrument2;
    public Instrument instrument3;
    Instrument[] instruments;
    public int InstrumentNumber = 0;

    double[] currentEnvelopeValues = new double[] { 0, 0, 0, 0, 0, 0, 0, 0 };
    public float[] currentInstrumentEnvelopeValues = new float[4];
    bool[] instrumentIsPlaying = new bool[] { false, false, false, false };

    public float volume = .5f;
    public string ROOTNOTE;
    VisualizationMode vizMode;

    MusicTheory mt = new MusicTheory();
    float currentNotePosition = 0f;

    public GameObject forwardDualStrength;
    public GameObject backwardDualStrength;
    public List<GameObject> sliders = new List<GameObject>();

    public Material whiteKey;
    public Material blackKey;
    public Material highlightKey;
    #endregion

    void Awake()
    {
        instruments = new Instrument[] { instrument0, instrument1, instrument2, instrument3};
        // setup audio
        for (int i = 0; i < numOscillators; i++)
        {
            osc[i] = sounds[i].transform.GetComponent<Oscillator>();
            osc[i].SetInstrument(instrument0, this, i);
            audioSource[i] = sounds[i].transform.GetComponent<AudioSource>();
            audioSource[i].Play();
            oscInUse[i] = false;
        }

        // register midi key numbers
        foreach (int i in mt.midiKeyNumbers)
        {
            midi_keyPlaying[i] = false;
        }

        UpdateUI();
    }
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
        oscToInstrumentMap[oscNum] = instrumentNum;
        instrumentIsPlaying[instrumentNum] = true;

        Oscillator o = osc[oscNum];
        noteNameAndInstToOsc[noteName, instrumentNum] = o;
        noteNameAndInstToOscID[noteName, instrumentNum] = oscNum;
        o.SetEnvelope(instruments[instrumentNum]);

        PlayNote(noteName, instrumentNum);
        numPlaying++;

        //press key in the prefab
        Vector3 _p = transform.Find("Keys/Note" + noteName).localPosition;
        Transform key = transform.Find("Keys/Note" + noteName);
        key.localPosition = new Vector3(_p[0] - 0.04f, _p[1], _p[2]);
        key.GetComponent<MeshRenderer>().material = highlightKey;
    }

    void ReleaseOscillatorFromNote(string noteName, int instrumentNum)
    {
        noteNameAndInstToOsc[noteName, instrumentNum].env.noteOff(noteNameAndInstToOsc[noteName, instrumentNum].timeT);
        noteNameAndInstToOsc.Remove(new Tuple<string, int>(noteName, instrumentNum));
        int oscNum = noteNameAndInstToOscID[noteName, instrumentNum];
        oscInUse[oscNum] = false;

        noteNameAndInstToOscID.Remove(new Tuple<string, int>(noteName, instrumentNum));
        numPlaying--;

        //unpress key in the prefab
        Vector3 _p = transform.Find("Keys/Note" + noteName).localPosition;
        Transform key = transform.Find("Keys/Note" + noteName);
        key.localPosition = new Vector3(_p[0] + 0.04f, _p[1], _p[2]);
        key.GetComponent<MeshRenderer>().material = key.name.Contains("b") ? blackKey : whiteKey;
    }

    IEnumerator TurnOffNoteAfterDelay(string noteName, int instrumentNum, float delay)
    {
        yield return new WaitForSeconds(delay);
        ReleaseOscillatorFromNote(noteName, instrumentNum);
    }

    void UpdateUI(){
      Instrument inst = CurrentInstrument();
      float a = inst.wave1Strength;
      float b = inst.wave2Strength;
      float c = inst.wave3Strength;
      float t = a + b + c;

      forwardDualStrength.GetComponent<Slider>().value = a / t;
      backwardDualStrength.GetComponent<Slider>().value = c / t;

      forwardDualStrength.GetComponent<DualSlider>().limit = 1 - c;
      backwardDualStrength.GetComponent<DualSlider>().limit = 1 - a;

      foreach(GameObject g in sliders)
      {
          EnvSlider gScript = g.GetComponent<EnvSlider>();
          Slider gSlider = g.GetComponent<Slider>();

          switch(gScript.role){
            case "attack":
              gSlider.value = inst.attack;
              break;

            case "decay":
              gSlider.value = inst.decay;
              break;

            case "sustain":
              gSlider.value = inst.sustain;
              break;

            case "release":
               gSlider.value = inst.release;
              break;

            default:
              break;
          }
      }
    }

    void CheckForComputerKeyboardAction()
    {
        if (vizMode == VisualizationMode.SingleInstrument)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1)) { InstrumentNumber = 0; UpdateUI(); }
            if (Input.GetKeyDown(KeyCode.Alpha2)) { InstrumentNumber = 1; UpdateUI(); }
            if (Input.GetKeyDown(KeyCode.Alpha3)) { InstrumentNumber = 2; UpdateUI(); }
            if (Input.GetKeyDown(KeyCode.Alpha4)) { InstrumentNumber = 3; UpdateUI(); }
        }
        

        foreach (KeyCode keyName in mt.keyboardPianoMap.Keys)
        {
            string keyboard_noteName = mt.keyboardPianoMap[keyName];
            if (Input.GetKeyDown(keyName) && !keyPlaying[keyName])
            {
                int oscNum = GetFirstAvailableOscillatorNum();
                if (oscNum != -1)
                {
                    if (vizMode == VisualizationMode.SingleInstrument)
                    {
                        AssignNoteToOscillator(keyboard_noteName, oscNum, InstrumentNumber);
                    }
                    else if (vizMode == VisualizationMode.AllInstruments)
                    {
                        int rowNum = mt.keyboardRowMap[keyName];
                        AssignNoteToOscillator(keyboard_noteName, oscNum, rowNum);
                    }
                    keyPlaying[keyName] = true;
                }
            }

            if (Input.GetKeyUp(keyName) && keyPlaying[keyName])
            {
                if (vizMode == VisualizationMode.SingleInstrument)
                {
                    ReleaseOscillatorFromNote(keyboard_noteName, InstrumentNumber);
                }
                else if (vizMode == VisualizationMode.AllInstruments)
                {
                    int rowNum = mt.keyboardRowMap[keyName];
                    ReleaseOscillatorFromNote(keyboard_noteName, rowNum);
                }               
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
                ReleaseOscillatorFromNote(midi_noteName, InstrumentNumber);
                midi_keyPlaying[i] = false;
            }
        }
    }

    public void PlayNote(string noteName, int instrumentNumber)
    {
        Note n = new Note(mt.GetNoteFrequency(noteName), volume, instruments[instrumentNumber]);
        noteNameAndInstToOsc[noteName, instrumentNumber].PlayNote(n);
    }

    public void PlayNote(string noteName, int instrumentNumber, float noteOffDelay)
    {
        PlayNote(noteName, instrumentNumber);
        IEnumerator delay = TurnOffNoteAfterDelay(noteName, instrumentNumber, noteOffDelay);
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

    public Instrument GetInstrument(int instrumentNum)
    {
        return instruments[instrumentNum];
    }

    public float GetInstrumentTotalLFO(int instrumentNum)
    {
        Instrument inst = instruments[instrumentNum];
        return inst.wave1lfofreq * inst.wave1lfoStrength + inst.wave2lfofreq * inst.wave2lfoStrength + inst.wave3lfofreq * inst.wave3lfoStrength;
    }

    public Instrument CurrentInstrument()
    {
        return instruments[InstrumentNumber];
    }

    public int CurrentInstrumentNumber()
    {
        return InstrumentNumber;
    }

    public void SetCurrentEnvelopeValue(int oscNum, double e)
    {
        currentEnvelopeValues[oscNum] = e;
    }

    public float CurrentEnvelopeValue(int instrumentNumber)
    {
        int numInUse = 0;
        float total = 0;
        for (int i = 0; i < 8; i++)
        {
            if (oscToInstrumentMap[i] == instrumentNumber)
            {
                float e = (float)currentEnvelopeValues[i];
                total += e;
                if (e > .0001f) { numInUse++; }
            }            
        }
        currentInstrumentEnvelopeValues[instrumentNumber] = total / numInUse;
        return total / numInUse;
    }

    public void ResetKeyboard(VisualizationMode mode)
    {
        vizMode = mode;
        if (mode == VisualizationMode.AllInstruments)
        {
            mt.SetFullKeyboardPianoMap(ROOTNOTE);
            
        }
        else if (mode == VisualizationMode.SingleInstrument)
        {
            mt.SetSingleRowKeyboardPianoMap(ROOTNOTE);
        }
        foreach (KeyCode keyName in mt.keyboardPianoMap.Keys)
        {
            keyPlaying[keyName] = false;
        }
    }

    public int GetOscillatorInstrumentNumber(int oscNum)
    {
        return oscToInstrumentMap[oscNum];
    }

    public bool InstrumentIsPlaying(int instNum)
    {
        return instrumentIsPlaying[instNum];
    }
}
