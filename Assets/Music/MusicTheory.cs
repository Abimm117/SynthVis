using System.Collections;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

public class MusicTheory
{
    #region define objects
    List<string> notes = new List<string>();
    Dictionary<string, int> root_index = new Dictionary<string, int>();
    double[] total_frequencies = new double[60];
    public Dictionary<KeyCode, string> keyboardPianoMap = new Dictionary<KeyCode, string>();
    public Dictionary<int, string> midiPianoMap = new Dictionary<int, string>();
    public List<int> midiKeyNumbers = new List<int>();
    #endregion

    public MusicTheory()
    {
        List<string> baseNotes = new List<string> { "C", "Db", "D", "Eb", "E", "F", "Gb", "G", "Ab", "A", "Bb", "B" };
        List<string> noteNums = new List<string> { "", "1", "2", "3", "4" };
        foreach(string n in noteNums) { foreach(string b in baseNotes) { notes.Add(b + n); }}

        for (int i = 0; i < 60; i++)
        {
            total_frequencies[i] = Mathf.RoundToInt(440 * (Mathf.Pow(2f, (i - 33) / 12f)));
        }
        for (int i = 0; i < notes.Count; i++)
        {
            root_index.Add(notes[i], i);
        }
        
        keyboardPianoMap.Add(KeyCode.A, "C1");
        keyboardPianoMap.Add(KeyCode.W, "Db1");
        keyboardPianoMap.Add(KeyCode.S, "D1");
        keyboardPianoMap.Add(KeyCode.E, "Eb1");
        keyboardPianoMap.Add(KeyCode.D, "E1");
        keyboardPianoMap.Add(KeyCode.F, "F1");
        keyboardPianoMap.Add(KeyCode.T, "Gb1");
        keyboardPianoMap.Add(KeyCode.G, "G1");
        keyboardPianoMap.Add(KeyCode.Y, "Ab1");
        keyboardPianoMap.Add(KeyCode.H, "A1");
        keyboardPianoMap.Add(KeyCode.U, "Bb1");
        keyboardPianoMap.Add(KeyCode.J, "B1");
        keyboardPianoMap.Add(KeyCode.K, "C2");
        keyboardPianoMap.Add(KeyCode.O, "Db2");
        keyboardPianoMap.Add(KeyCode.L, "D2");
        keyboardPianoMap.Add(KeyCode.P, "Eb2");
        keyboardPianoMap.Add(KeyCode.Semicolon, "E2");

        for (int i = 38; i < 86; i++)
        {
            midiPianoMap.Add(i, notes[i - 26]);
            midiKeyNumbers.Add(i);
        }
    }

    public double[] get_scale(string root, int num_notes, List<int> displacements)
    {
        double[] frequencies = new double[num_notes];
        for (int i = 0; i < num_notes; i++)
        {
            frequencies[i] = total_frequencies[root_index[root] + displacements[i + (displacements.Count - num_notes) / 2]];
        }
        return frequencies;
    }
    public double[] get_pentatonic_scale(string root, int num_notes)
    {
        List<int> p = new List<int> {-12, -10, -8, -5, -3, 0, 2, 4, 7, 9, 12};
        return get_scale(root, num_notes, p);
    }

    public double GetNoteFrequency(string noteName)
    {
        return total_frequencies[root_index[noteName]];
    }
}
