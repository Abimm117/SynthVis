using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Note
{
    public double frequency;
    public Instrument instrument;
    public float volume;

    public Note(double frequency, float volume, Instrument instrument)
    {
        this.frequency = frequency;
        this.instrument = instrument;
        this.volume = volume;
    }
}
