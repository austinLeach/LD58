using UnityEngine;

public class GroundDetector : MonoBehaviour
{
    private MovementController movementController;
    private LayerMask groundLayerMask;
    private int groundContactCount = 0;
    
    // Store information about the ground we're touching
    private Vector2 currentGroundNormal = Vector2.up; // Default to flat ground
    private Collider2D currentGroundCollider = null;
    private bool hasValidGroundContact = false;
    
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
            
            // Get contact information from the collision
            UpdateGroundContactInfo(other);
            
            if (movementController != null)
            {
                movementController.SetGrounded(true);
            }
        }
    }
    
    private void OnTriggerStay2D(Collider2D other)
    {
        // Check if the object is on the ground layer
        int layerMaskValue = 1 << other.gameObject.layer;
        if ((groundLayerMask.value & layerMaskValue) != 0)
        {
            // Continuously update ground contact info while in contact
            UpdateGroundContactInfo(other);
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
                hasValidGroundContact = false;
                currentGroundCollider = null;
                currentGroundNormal = Vector2.up; // Reset to default
                
                if (movementController != null)
                {
                    movementController.SetGrounded(false);
                }
            }
        }
    }
    
    private void UpdateGroundContactInfo(Collider2D groundCollider)
    {
        currentGroundCollider = groundCollider;
        
        // Get the player's rigidbody for contact point detection
        Rigidbody2D playerRb = GetComponentInParent<Rigidbody2D>();
        if (playerRb == null) return;
        
        // Get all contact points between the player and all colliders
        ContactPoint2D[] contacts = new ContactPoint2D[10];
        int contactCount = playerRb.GetContacts(contacts);
        
        Vector2 avgNormal = Vector2.zero;
        int validContacts = 0;
        
        // Find all contacts with ground layer objects and average their normals
        for (int i = 0; i < contactCount; i++)
        {
            int contactLayerMask = 1 << contacts[i].collider.gameObject.layer;
            if ((groundLayerMask.value & contactLayerMask) != 0)
            {
                // Only consider contacts pointing generally upward (prevents wall normals)
                if (contacts[i].normal.y > 0.1f)
                {
                    avgNormal += contacts[i].normal;
                    validContacts++;
                }
            }
        }
        
        if (validContacts > 0)
        {
            // Use averaged normal for more stable detection
            currentGroundNormal = (avgNormal / validContacts).normalized;
            hasValidGroundContact = true;
            Debug.Log($"Ground Contact (Averaged) - Surface: {groundCollider.name}, Normal: {currentGroundNormal}, Angle: {Vector2.Angle(currentGroundNormal, Vector2.up):F1}°, Contacts: {validContacts}");
        }
        else
        {
            // Fallback: if no valid contacts found, estimate normal from collider bounds
            EstimateGroundNormal(groundCollider);
        }
    }
    
    private void EstimateGroundNormal(Collider2D groundCollider)
    {
        // Fallback method: cast a short ray down to the collider surface
        Vector2 rayStart = transform.position;
        RaycastHit2D hit = Physics2D.Raycast(rayStart, Vector2.down, 1f, 1 << groundCollider.gameObject.layer);
        
        if (hit.collider == groundCollider)
        {
            currentGroundNormal = hit.normal;
            hasValidGroundContact = true;
            Debug.Log($"Ground Contact (Estimated) - Surface: {groundCollider.name}, Normal: {currentGroundNormal}, Angle: {Vector2.Angle(currentGroundNormal, Vector2.up):F1}°");
        }
        else
        {
            // Ultimate fallback: assume flat ground
            currentGroundNormal = Vector2.up;
            hasValidGroundContact = false;
            Debug.Log($"Ground Contact (Default) - Surface: {groundCollider.name}, Normal: Vector2.up (default)");
        }
    }
    
    // Public methods for MovementController to access ground info
    public Vector2 GetGroundNormal()
    {
        return currentGroundNormal;
    }
    
    public Collider2D GetGroundCollider()
    {
        return currentGroundCollider;
    }
    
    public bool HasValidGroundContact()
    {
        return hasValidGroundContact && groundContactCount > 0;
    }
    
    // Force refresh the ground contact info
    public void RefreshGroundContact()
    {
        if (currentGroundCollider != null && groundContactCount > 0)
        {
            UpdateGroundContactInfo(currentGroundCollider);
        }
    }
}