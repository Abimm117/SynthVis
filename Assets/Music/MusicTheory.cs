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
    public Dictionary<KeyCode, int> keyboardRowMap = new Dictionary<KeyCode, int>();
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
            total_frequencies[i] = 220 * Mathf.Pow(2f, (i - 34) / 12f);
        }
        for (int i = 0; i < notes.Count; i++)
        {
            root_index.Add(notes[i], i);
        }

        for (int i = 24; i < 84; i++)
        {
            midiPianoMap.Add(i, notes[i - 24]);
            midiKeyNumbers.Add(i);
        }
    }

    public void SetSingleRowKeyboardPianoMap(string root)
    {
        keyboardPianoMap = new Dictionary<KeyCode, string>();

        // major scale
        List<int> p = new List<int> { 0, 2, 4, 5, 7, 9, 11, 12, 14, 16 };

        // middle row of letters
        keyboardPianoMap.Add(KeyCode.A, notes[root_index[root] + p[0]]);
        keyboardPianoMap.Add(KeyCode.S, notes[root_index[root] + p[1]]);
        keyboardPianoMap.Add(KeyCode.D, notes[root_index[root] + p[2]]);
        keyboardPianoMap.Add(KeyCode.F, notes[root_index[root] + p[3]]);
        keyboardPianoMap.Add(KeyCode.G, notes[root_index[root] + p[4]]);
        keyboardPianoMap.Add(KeyCode.H, notes[root_index[root] + p[5]]);
        keyboardPianoMap.Add(KeyCode.J, notes[root_index[root] + p[6]]);
        keyboardPianoMap.Add(KeyCode.K, notes[root_index[root] + p[7]]);
        keyboardPianoMap.Add(KeyCode.L, notes[root_index[root] + p[8]]);
        keyboardPianoMap.Add(KeyCode.Semicolon, notes[root_index[root] + p[9]]);
    }

    public void SetFullKeyboardPianoMap(string root)
    {
        keyboardPianoMap = new Dictionary<KeyCode, string>();
        keyboardRowMap = new Dictionary<KeyCode, int>();

        // major scale
        List<int> p = new List<int> { 0, 2, 4, 5, 7, 9, 11, 12, 14, 16 };

        // bottom row of letters
        
        keyboardPianoMap.Add(KeyCode.Z, notes[root_index[root] + p[0]]);
        keyboardPianoMap.Add(KeyCode.X, notes[root_index[root] + p[1]]);
        keyboardPianoMap.Add(KeyCode.C, notes[root_index[root] + p[2]]);
        keyboardPianoMap.Add(KeyCode.V, notes[root_index[root] + p[3]]);
        keyboardPianoMap.Add(KeyCode.B, notes[root_index[root] + p[4]]);
        keyboardPianoMap.Add(KeyCode.N, notes[root_index[root] + p[5]]);
        keyboardPianoMap.Add(KeyCode.M, notes[root_index[root] + p[6]]);
        keyboardRowMap.Add(KeyCode.Z, 0);
        keyboardRowMap.Add(KeyCode.X, 0);
        keyboardRowMap.Add(KeyCode.C, 0);
        keyboardRowMap.Add(KeyCode.V, 0);
        keyboardRowMap.Add(KeyCode.B, 0);
        keyboardRowMap.Add(KeyCode.N, 0);
        keyboardRowMap.Add(KeyCode.M, 0);

        // middle row of letters
        keyboardPianoMap.Add(KeyCode.A, notes[root_index[root] + p[0]]);
        keyboardPianoMap.Add(KeyCode.S, notes[root_index[root] + p[1]]);
        keyboardPianoMap.Add(KeyCode.D, notes[root_index[root] + p[2]]);
        keyboardPianoMap.Add(KeyCode.F, notes[root_index[root] + p[3]]);
        keyboardPianoMap.Add(KeyCode.G, notes[root_index[root] + p[4]]);
        keyboardPianoMap.Add(KeyCode.H, notes[root_index[root] + p[5]]);
        keyboardPianoMap.Add(KeyCode.J, notes[root_index[root] + p[6]]);
        keyboardPianoMap.Add(KeyCode.K, notes[root_index[root] + p[7]]);
        keyboardPianoMap.Add(KeyCode.L, notes[root_index[root] + p[8]]);
        keyboardPianoMap.Add(KeyCode.Semicolon, notes[root_index[root] + p[9]]);
        keyboardRowMap.Add(KeyCode.A, 1);
        keyboardRowMap.Add(KeyCode.S, 1);
        keyboardRowMap.Add(KeyCode.D, 1);
        keyboardRowMap.Add(KeyCode.F, 1);
        keyboardRowMap.Add(KeyCode.G, 1);
        keyboardRowMap.Add(KeyCode.H, 1);
        keyboardRowMap.Add(KeyCode.J, 1);
        keyboardRowMap.Add(KeyCode.K, 1);
        keyboardRowMap.Add(KeyCode.L, 1);
        keyboardRowMap.Add(KeyCode.Semicolon, 1);

        // top row of letters
        keyboardPianoMap.Add(KeyCode.Q, notes[root_index[root] + p[0]]);
        keyboardPianoMap.Add(KeyCode.W, notes[root_index[root] + p[1]]);
        keyboardPianoMap.Add(KeyCode.E, notes[root_index[root] + p[2]]);
        keyboardPianoMap.Add(KeyCode.R, notes[root_index[root] + p[3]]);
        keyboardPianoMap.Add(KeyCode.T, notes[root_index[root] + p[4]]);
        keyboardPianoMap.Add(KeyCode.Y, notes[root_index[root] + p[5]]);
        keyboardPianoMap.Add(KeyCode.U, notes[root_index[root] + p[6]]);
        keyboardPianoMap.Add(KeyCode.I, notes[root_index[root] + p[7]]);
        keyboardPianoMap.Add(KeyCode.O, notes[root_index[root] + p[8]]);
        keyboardPianoMap.Add(KeyCode.P, notes[root_index[root] + p[9]]);
        keyboardRowMap.Add(KeyCode.Q, 2);
        keyboardRowMap.Add(KeyCode.W, 2);
        keyboardRowMap.Add(KeyCode.E, 2);
        keyboardRowMap.Add(KeyCode.R, 2);
        keyboardRowMap.Add(KeyCode.T, 2);
        keyboardRowMap.Add(KeyCode.Y, 2);
        keyboardRowMap.Add(KeyCode.U, 2);
        keyboardRowMap.Add(KeyCode.I, 2);
        keyboardRowMap.Add(KeyCode.O, 2);
        keyboardRowMap.Add(KeyCode.P, 2);

        // row of numbers
        keyboardPianoMap.Add(KeyCode.Alpha1, notes[root_index[root] + p[0]]);
        keyboardPianoMap.Add(KeyCode.Alpha2, notes[root_index[root] + p[1]]);
        keyboardPianoMap.Add(KeyCode.Alpha3, notes[root_index[root] + p[2]]);
        keyboardPianoMap.Add(KeyCode.Alpha4, notes[root_index[root] + p[3]]);
        keyboardPianoMap.Add(KeyCode.Alpha5, notes[root_index[root] + p[4]]);
        keyboardPianoMap.Add(KeyCode.Alpha6, notes[root_index[root] + p[5]]);
        keyboardPianoMap.Add(KeyCode.Alpha7, notes[root_index[root] + p[6]]);
        keyboardPianoMap.Add(KeyCode.Alpha8, notes[root_index[root] + p[7]]);
        keyboardPianoMap.Add(KeyCode.Alpha9, notes[root_index[root] + p[8]]);
        keyboardPianoMap.Add(KeyCode.Alpha0, notes[root_index[root] + p[9]]);
        keyboardRowMap.Add(KeyCode.Alpha1, 3);
        keyboardRowMap.Add(KeyCode.Alpha2, 3);
        keyboardRowMap.Add(KeyCode.Alpha3, 3);
        keyboardRowMap.Add(KeyCode.Alpha4, 3);
        keyboardRowMap.Add(KeyCode.Alpha5, 3);
        keyboardRowMap.Add(KeyCode.Alpha6, 3);
        keyboardRowMap.Add(KeyCode.Alpha7, 3);
        keyboardRowMap.Add(KeyCode.Alpha8, 3);
        keyboardRowMap.Add(KeyCode.Alpha9, 3);
        keyboardRowMap.Add(KeyCode.Alpha0, 3);
    }

    public double[] GetScale(string root, int num_notes, List<int> displacements)
    {
        double[] frequencies = new double[num_notes];
        for (int i = 0; i < num_notes; i++)
        {
            frequencies[i] = total_frequencies[root_index[root] + displacements[i + (displacements.Count - num_notes) / 2]];
        }
        return frequencies;
    }

    public double[] GetMajorScale(string root, int num_notes)
    {
        List<int> p = new List<int> { 0, 2, 4, 5, 7, 9, 11, 12, 14, 16 };
        return GetScale(root, num_notes, p);
    }
    public double[] GetPentatonicScale(string root, int num_notes)
    {
        List<int> p = new List<int> {-12, -10, -8, -5, -3, 0, 2, 4, 7, 9, 12};
        return GetScale(root, num_notes, p);
    }

    public double GetNoteFrequency(string noteName)
    {
        return total_frequencies[root_index[noteName]];
    }
}
