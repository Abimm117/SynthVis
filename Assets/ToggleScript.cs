using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ToggleScript : MonoBehaviour
{
    public Toggle toggle;
    public GameObject synthVis;
    private SynthVisualizer synthScript;

    // Start is called before the first frame update
    void Start()
    {
      toggle = gameObject.GetComponent<Toggle>();
      synthScript = synthVis.GetComponent<SynthVisualizer>();
      toggle.isOn = false;
      toggle.onValueChanged.AddListener (delegate {ValueChangeCheck ();});
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void ValueChangeCheck()
    {
      if(toggle.isOn){
            synthScript.soundObjectOn = true;//singleSoundObject.SetActive(true);
      } else {
            synthScript.soundObjectOn = false;//singleSoundObject.SetActive(false);
        }
    }
}
