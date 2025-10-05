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
            
            // Only set grounded if we have valid upward-pointing contacts
            if (hasValidGroundContact && movementController != null)
            {
                movementController.SetGrounded(true);
                Debug.Log($"Set grounded TRUE for {other.name} - valid ground contact detected");
            }
            else
            {
                Debug.Log($"Contact with {other.name} but no valid ground contact - not setting grounded");
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
            
            // Update grounded state based on whether we have valid ground contact
            if (movementController != null)
            {
                if (hasValidGroundContact)
                {
                    movementController.SetGrounded(true);
                }
                else
                {
                    // We're touching the collider but not in a valid "ground" way (e.g., wall contact)
                    movementController.SetGrounded(false);
                    Debug.Log($"Lost valid ground contact with {other.name} - setting grounded FALSE");
                }
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
                // Increased threshold to be more restrictive about what counts as "ground"
                if (contacts[i].normal.y > 0.7f) // More restrictive: normal must point mostly upward
                {
                    avgNormal += contacts[i].normal;
                    validContacts++;
                }
                else
                {
                    Debug.Log($"Rejecting contact with {contacts[i].collider.name} - normal too horizontal: {contacts[i].normal} (y: {contacts[i].normal.y:F2} <= 0.7)");
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
            // No valid upward-pointing contacts found - this is likely a wall
            hasValidGroundContact = false;
            currentGroundNormal = Vector2.up; // Reset to default
            Debug.Log($"No valid ground contacts found for {groundCollider.name} - likely a wall contact");
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