using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class EnvSlider : MonoBehaviour
{
    public Slider mainSlider;
    public GameObject synth;
    public string role;
    private SynthController synthScript;
    private Instrument synthInst;
    // Start is called before the first frame update
    void Start()
    {
      mainSlider.onValueChanged.AddListener (delegate {ValueChangeCheck ();});
      synthScript = synth.GetComponent<SynthController>();
      synthScript.sliders.Add(gameObject);
      synthInst = synthScript.CurrentInstrument();

      switch(role){
        case "attack":
          mainSlider.value = synthInst.attack;
          break;

        case "decay":
          mainSlider.value = synthInst.decay;
          break;

        case "sustain":
          mainSlider.value = synthInst.sustain;
          break;

        case "release":
          mainSlider.value = synthInst.release;
          break;

        case "freq1":
          mainSlider.value = synthInst.wave1freq;
          break;

        case "freq2":
          mainSlider.value = synthInst.wave2freq;
          break;

        case "freq3":
          mainSlider.value = synthInst.wave3freq;
          break;

        default:
          break;
      }

    }

    // Update is called once per frame
    void Update()
    {

    }

    public void ValueChangeCheck()
    {
        if (SynthVisualizer.Instance.mode == VisualizationMode.SingleInstrument)
        {
            synthInst = synthScript.CurrentInstrument();

            switch (role)
            {
                case "attack":
                    synthInst.attack = mainSlider.value;
                    break;

                case "decay":
                    synthInst.decay = mainSlider.value;
                    break;

                case "sustain":
                    synthInst.sustain = mainSlider.value;
                    break;

                case "release":
                    synthInst.release = mainSlider.value;
                    break;

                case "freq1":
                    synthInst.wave1freq = mainSlider.value;
                    break;

                case "freq2":
                    synthInst.wave2freq = mainSlider.value;
                    break;

                case "freq3":
                    synthInst.wave3freq = mainSlider.value;
                    break;

                default:
                    break;
            }

            synthScript.UpdateEnvelopeMarkers();
        }

        
    }
}
