using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DropScript : MonoBehaviour
{
    public Dropdown drop;
    public GameObject synth;
    private SynthController synthScript;

    // Start is called before the first frame update
    void Start()
    {
      drop = gameObject.GetComponent<Dropdown>();
      synthScript = synth.GetComponent<SynthController>();
      drop.value = synthScript.InstrumentNumber;
      drop.onValueChanged.AddListener (delegate {ValueChangeCheck ();});
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void ValueChangeCheck()
    {
      synthScript.InstrumentNumber = drop.value;
      synthScript.UpdateUI();
    }
}
