using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AudioModeDropdown : MonoBehaviour
{
    public Dropdown drop;
    public GameObject synthV;
    //private SynthController synthScript;
    private SynthVisualizer synthVis;
    // Start is called before the first frame update
    void Start()
    {
        drop = gameObject.GetComponent<Dropdown>();
        synthVis = synthV.GetComponent<SynthVisualizer>();
        drop.value = (int)synthVis.mode;
        drop.onValueChanged.AddListener(delegate { ValueChangeCheck(); });
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void ValueChangeCheck()
    {
        synthVis.mode = (VisualizationMode) drop.value;
        //if (synthVis.mode == VisualizationMode.Clip) { }
    }
}