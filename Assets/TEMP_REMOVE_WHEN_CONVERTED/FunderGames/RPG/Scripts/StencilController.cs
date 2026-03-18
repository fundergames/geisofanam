using UnityEngine;

public class StencilController : MonoBehaviour
{
    private static readonly int StencilID = Shader.PropertyToID("_StencilID");
    [SerializeField] private int startingStencilID = 1;

    public void SetStencilID(int newStencilID)
    {
        startingStencilID = newStencilID;  
        AssignStencilIDToHierarchy(transform);
    }
    
    void Start()
    {
        SetStencilID(startingStencilID);
    }

    // Method to assign the same stencil ID to the entire hierarchy (parent and children)
    void AssignStencilIDToHierarchy(Transform parent)
    {
        // Assign stencil ID to the parent and all its children
        AssignStencilIDRecursively(parent, startingStencilID);
    }

    // Recursively assign the same stencil ID to the parent and all its children
    void AssignStencilIDRecursively(Transform currentObject, int stencilID)
    {
        var renderer = currentObject.GetComponent<Renderer>();

        if (renderer != null)
        {
            // Use renderer.materials to ensure new instances are created for this renderer
            var materials = renderer.materials;  // This instantiates materials if they're shared

            for (int i = 0; i < materials.Length; i++)
            {
                // Set the stencil reference on the material instance
                materials[i].SetFloat(StencilID, stencilID);

                // Log to confirm the change
                Debug.Log($"Assigned Stencil ID {stencilID} to {currentObject.name}, Material {i}");
            }

            // Assign the modified materials back to the renderer (though this may not be necessary)
            renderer.materials = materials;  // Ensures the renderer uses the updated materials
        }

        // Recursively process all child objects
        foreach (Transform child in currentObject)
        {
            AssignStencilIDRecursively(child, stencilID);  // Assign the same stencil ID to the child
        }
    }
}
