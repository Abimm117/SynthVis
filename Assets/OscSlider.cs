using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class OscSlider : MonoBehaviour
{
    public GameObject vis;
    private SynthVisualizer visScript;
    public Slider slider;
    // Start is called before the first frame update
    void Start()
    {

      visScript = vis.GetComponent<SynthVisualizer>();
      slider.value = visScript.waveformPlotZoom;
      slider.onValueChanged.AddListener (delegate {ValueChangeCheck ();});
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void ValueChangeCheck(){
      visScript.waveformPlotZoom = slider.value;
    }
}
