using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InstSlideScript : MonoBehaviour
{
    public Dropdown drop;
    public GameObject synth;
    public int wave;
    private SynthController synthScript;

    // Start is called before the first frame update
    void Start()
    {
      drop = gameObject.GetComponent<Dropdown>();
      synthScript = synth.GetComponent<SynthController>();
      synthScript.waveDrops.Add(gameObject);
      Instrument synthInst = synthScript.CurrentInstrument();
      switch(wave)
      {
        case 1:
          drop.value = (int) synthInst.wave1type;
        break;

        case 2:
          drop.value = (int) synthInst.wave2type;
        break;

        case 3:
          drop.value = (int) synthInst.wave3type;
        break;

        default:
        break;
      }
      drop.onValueChanged.AddListener (delegate {ValueChangeCheck ();});
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void ValueChangeCheck()
    {
      Instrument synthInst = synthScript.CurrentInstrument();
      switch(wave)
      {
        case 1:
          synthInst.wave1type = (WaveType) drop.value;
        break;

        case 2:
          synthInst.wave2type = (WaveType) drop.value;
        break;

        case 3:
          synthInst.wave3type = (WaveType) drop.value;
        break;

        default:
        break;
      }
    }
}
