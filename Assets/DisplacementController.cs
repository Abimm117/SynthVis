using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DisplacementController : MonoBehaviour
{

    public float displacementAmount;
    MeshRenderer meshRenderer;
    // Start is called before the first frame update
    void Start()
    {
        meshRenderer = transform.GetComponent<MeshRenderer>();
    }

    // Update is called once per frame
    void Update()
    {
        displacementAmount = Mathf.Lerp(displacementAmount, 0, Time.deltaTime);
        meshRenderer.material.SetFloat("_scale", displacementAmount);
    }
}
