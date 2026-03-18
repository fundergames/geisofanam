using UnityEngine;

public class CharacterSpinController : MonoBehaviour
{
    public Transform character;  // Reference to the character's transform
    public float rotationSpeed = 5f;  // Speed of rotation
    
    private bool isDragging = false;
    private float lastMouseX;

    void Update()
    {
        // Check for mouse input to rotate the character
        if (Input.GetMouseButtonDown(0))  // Mouse left-click down
        {
            isDragging = true;
            lastMouseX = Input.mousePosition.x;
        }

        if (Input.GetMouseButtonUp(0))  // Mouse left-click up
        {
            isDragging = false;
        }

        if (!isDragging) return;
        var currentMouseX = Input.mousePosition.x;
        var deltaX = currentMouseX - lastMouseX;
            
        // Rotate the character around the Y-axis
        character.Rotate(Vector3.up, -deltaX * rotationSpeed * Time.deltaTime);
            
        lastMouseX = currentMouseX;
    }
}