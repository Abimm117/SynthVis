using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Window_Spectrogram : MonoBehaviour
{
    [SerializeField] private Sprite circleSprite;
    private RectTransform graphContainer;

    public float xPositionScale;
    // Start is called before the first frame update

    private void Awake()
    {
        graphContainer = transform.Find("graphContainer").GetComponent<RectTransform>();

        List<float> testList = new List<float>() { .2f, .1f, .3f, .2f, .6f, .1f, .9f, .8f, .7f, .6f, .4f, .2f, .1f };
        ShowGraph(testList);
        //CreateCircle(new Vector2(200, 200));
    }



    private void CreateCircle(Vector2 anchorPosition)
    {
        GameObject gameObject = new GameObject("circle", typeof(Image));
        gameObject.transform.SetParent(graphContainer, false);
        gameObject.GetComponent<Image>().sprite = circleSprite;
        RectTransform rectTransform = gameObject.GetComponent<RectTransform>();
        rectTransform.anchoredPosition = anchorPosition;
        rectTransform.sizeDelta = new Vector2(11, 11);
        rectTransform.anchorMin = new Vector2(0, 0);
        rectTransform.anchorMax = new Vector2(0, 0);
    }

    private void ShowGraph(List<float> valueList)
    {
        float graphHeight = graphContainer.sizeDelta.y;
        float xSize = xPositionScale / valueList.Count;
        for (int i = 0; i < valueList.Count; i++)
        {
            float xPosition = i * xSize;
            float yPosition = valueList[i] * graphHeight;
            CreateCircle(new Vector2(xPosition, yPosition));
        }
    }
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
