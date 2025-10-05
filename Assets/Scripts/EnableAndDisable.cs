using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class EnableAndDisable : MonoBehaviour
{

    public float FirstorSecond;
    Image img;
    private float test1;
    private float btnVal;
    private float maxVal;
    private float curVal;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        btnVal = FirstorSecond / 3f;
        img = gameObject.GetComponent<Image>();
        img.enabled = false;
        
    }

    // Update is called once per frame
    void Update()
    {
        maxVal = gameObject.GetComponentInParent<Slider>().maxValue;
        curVal = gameObject.GetComponentInParent<Slider>().value;
        test1 = curVal / maxVal;
        

        if ( test1 >= btnVal)
        {
            img.enabled = true;
            this.enabled = false;
        }

    }
}
