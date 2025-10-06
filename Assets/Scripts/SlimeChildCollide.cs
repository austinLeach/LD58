using UnityEngine;

public class SlimeChildCollide : MonoBehaviour
{

    private Collider2D collision;
    public bool hit = false;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        collision = GetComponent<Collider2D>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        if (collision.gameObject.layer == 6)
        {
            hit = true;
        }
        else
        {
            hit = false;
        }
    }
}
