using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DualSlider : MonoBehaviour
{
    public float limit = 1f;
    public Slider mainSlider;
    public GameObject altSlider;
    private DualSlider altScript;
    public GameObject synth;
    private SynthController synthScript;
    private Instrument synthInst;
    // Start is called before the first frame update
    void Start()
    {
      mainSlider.onValueChanged.AddListener (delegate {ValueChangeCheck ();});
      altScript = altSlider.GetComponent<DualSlider>();
      synthScript = synth.GetComponent<SynthController>();
      limit = 1 - altSlider.GetComponent<Slider>().value;
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void ValueChangeCheck()
    {
      if(mainSlider.value > limit){
        mainSlider.value = limit;
      }
      altScript.limit = 1 -  mainSlider.value;

      synthInst = synthScript.CurrentInstrument();

      if(mainSlider.direction == Slider.Direction.LeftToRight){
        synthInst.wave1Strength = mainSlider.value;
        synthInst.wave3Strength = 1 - limit;
      } else {
        synthInst.wave1Strength = 1 - limit;
        synthInst.wave3Strength = mainSlider.value;
      }

      synthInst.wave2Strength = limit - mainSlider.value;


        synthScript.UpdateWaveMarkers();

    }
}
