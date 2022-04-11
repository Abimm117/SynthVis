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
    // Start is called before the first frame update
    void Start()
    {
      mainSlider.onValueChanged.AddListener (delegate {ValueChangeCheck ();});
      altScript = altSlider.GetComponent<DualSlider>();
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
    }
}
