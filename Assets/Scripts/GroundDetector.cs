using UnityEngine;

public class GroundDetector : MonoBehaviour
{
    private MovementController movementController;
    private LayerMask groundLayerMask;
    private int groundContactCount = 0;
    
    public void Initialize(MovementController controller, LayerMask layerMask)
    {
        movementController = controller;
        groundLayerMask = layerMask;
    }
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        // Check if the object is on the ground layer
        int layerMaskValue = 1 << other.gameObject.layer;
        if ((groundLayerMask.value & layerMaskValue) != 0)
        {
            groundContactCount++;
            if (movementController != null)
            {
                movementController.SetGrounded(true);
            }
        }
    }
    
    private void OnTriggerExit2D(Collider2D other)
    {
        // Check if the object is on the ground layer
        int layerMaskValue = 1 << other.gameObject.layer;
        if ((groundLayerMask.value & layerMaskValue) != 0)
        {
            groundContactCount--;
            if (groundContactCount <= 0)
            {
                groundContactCount = 0; // Ensure it doesn't go negative
                if (movementController != null)
                {
                    movementController.SetGrounded(false);
                }
            }
        }
    }
}