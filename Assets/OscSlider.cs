using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class OscSlider : MonoBehaviour
{
    public GameObject vis;
    private SynthVisualizer visScript;
    public Slider slider;
    //public int waveformPlotNum;
    // Start is called before the first frame update
    void Start()
    {

      visScript = vis.GetComponent<SynthVisualizer>();
      slider.value = visScript.WaveformZoom;
      slider.onValueChanged.AddListener (delegate {ValueChangeCheck ();});
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void ValueChangeCheck(){
      visScript.WaveformZoom = slider.value;

    }
}
