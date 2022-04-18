using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InstrumentVolumeSlider : MonoBehaviour
{
    //public GameObject vis;
    public GameObject synth;
    private SynthController synthScript;

    public Slider slider;
    public int instrumentNumber;
    // Start is called before the first frame update
    void Start()
    {

        synthScript = synth.GetComponent<SynthController>();
        slider.value = 0.5f;
        slider.onValueChanged.AddListener(delegate { ValueChangeCheck(); });
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void ValueChangeCheck()
    {
        synthScript.SetGain(instrumentNumber, slider.value);

    }
}
