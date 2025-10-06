using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class Bag : MonoBehaviour
{

    public float FirstorSecond;
    private float pleasehelp;
    SpriteRenderer sprite;
    private float test1;
    private float btnVal;
    private float maxVal;
    private float curVal;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        pleasehelp = FirstorSecond;
        sprite = gameObject.GetComponent<SpriteRenderer>();
        sprite.enabled = false;

        if (FirstorSecond == 0)
        {
            pleasehelp = FirstorSecond;
            FirstorSecond = 1f;
            sprite.enabled = true;
        }
        btnVal = FirstorSecond / 3f;

    }

    // Update is called once per frame
    void Update()
    {
        maxVal = GlobalVariables.levelCoins;
        curVal = GlobalVariables.currentCoins;
        test1 = curVal / maxVal;

        //Delete thine self lowly sprite
        if (pleasehelp == 0)
        {
            if (test1 >= 1f / 3f)
            {
                Debug.Log("A");
                //End the life of first bag
                sprite.enabled = false;
                Destroy(this);
                this.enabled = false;
            }
        }
        if (pleasehelp == 1)
        {
            if (test1 >= 2f / 3f || test1 == 3f / 3f)
            {
                Debug.Log("B");
                //End the life of second bag
                sprite.enabled = false;
                Destroy(this);
                this.enabled = false;
            }
        }


        if ( test1 >= btnVal)
        {           
            if(pleasehelp == 1f)
            {
                Debug.Log("C");
                //Turn on 2nd bag
                sprite.enabled = true;

            }
            if (pleasehelp == 2f)
            {
                sprite.enabled = true;
            }

        }

    }
}
