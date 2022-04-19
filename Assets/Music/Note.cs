using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Note
{
    public double frequency;
    public Instrument instrument;

    public Note(double frequency, Instrument instrument)
    {
        this.frequency = frequency;
        this.instrument = instrument;
    }
}
