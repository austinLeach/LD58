using UnityEngine;

public class Parallax : MonoBehaviour
{

    [System.Serializable]
    public class ParallaxLayer
    {
        public Transform layer;
        [Range(0, 1)] public float parallaxStrength;
    }

    public ParallaxLayer[] layers;

    public Transform camTransform;
    private Vector3 lastCameraPosition;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        camTransform.position = lastCameraPosition;
    }

    private void LateUpdate()
    {
        Vector3 cameraDelta = camTransform.position - lastCameraPosition;
        foreach (ParallaxLayer layer in layers)
        {
            float moveX = cameraDelta.x * layer.parallaxStrength;
            float movey = cameraDelta.y * layer.parallaxStrength;

            layer.layer.position += new Vector3(moveX, movey, 0);
        }
        lastCameraPosition = camTransform.position;
    }

}
