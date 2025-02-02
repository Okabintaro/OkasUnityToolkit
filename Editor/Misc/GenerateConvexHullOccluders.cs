using UnityEngine;
using UnityEditor;

public class GenerateConvexOccluders : Editor
{
    // We use a single static material reference to avoid creating duplicates
    private static Material _opaqueMaterial;
    
    private static Material OpaqueMaterial
    {
        get
        {
            if (_opaqueMaterial == null)
            {
                // Create or find a built-in opaque material
                // For the built-in pipeline, "Standard" is typically fine.
                _opaqueMaterial = new Material(Shader.Find("Standard"))
                {
                    name = "Occluder_Opaque_Mat"
                };

                // If you need to ensure it's fully opaque, adjust the Render Queue
                // or settings as needed. For the Standard shader:
                // _opaqueMaterial.SetFloat("_Mode", 0); // Opaque
            }
            return _opaqueMaterial;
        }
    }

    [MenuItem("Tools/Generate Recursive Occluders")]
    public static void GenerateOccluders()
    {
        // Ensure there's a valid selection
        GameObject selectedGO = Selection.activeGameObject;
        if (selectedGO == null)
        {
            Debug.LogError("No GameObject selected. Please select a parent object in the Hierarchy.");
            return;
        }

        // Create a new "OccluderGroup" at the root (no parent)
        GameObject occluderGroup = new GameObject("OccluderGroup");

        // Mark the main OccluderGroup as Occluder Static
        GameObjectUtility.SetStaticEditorFlags(occluderGroup, StaticEditorFlags.OccluderStatic);

        // Recursively search for all MeshColliders
        AddOccludersRecursively(selectedGO.transform, occluderGroup.transform);

        Debug.Log($"Occluders generated under '{occluderGroup.name}' at the root of the scene.");
    }

    private static void AddOccludersRecursively(Transform current, Transform occluderGroup)
    {
        // If this Transform has a MeshCollider, create an occluder
        MeshCollider meshCollider = current.GetComponent<MeshCollider>();
        if (meshCollider != null && meshCollider.sharedMesh != null)
        {
            CreateOccluderObject(current, meshCollider, occluderGroup);
        }

        // Recursively check all children
        foreach (Transform child in current)
        {
            AddOccludersRecursively(child, occluderGroup);
        }
    }

    private static void CreateOccluderObject(Transform sourceTransform, MeshCollider meshCollider, Transform occluderGroup)
    {
        // Create a new occluder GameObject
        string occluderName = sourceTransform.name + "_Occluder";
        GameObject occluderObj = new GameObject(occluderName);

        // Set it under the "OccluderGroup" in the scene
        occluderObj.transform.SetParent(occluderGroup, false);

        // Copy world position/rotation/scale from the source to the new occluder
        occluderObj.transform.position = sourceTransform.position;
        occluderObj.transform.rotation = sourceTransform.rotation;
        occluderObj.transform.localScale = sourceTransform.lossyScale;

        // Add the MeshFilter & MeshRenderer
        MeshFilter mf = occluderObj.AddComponent<MeshFilter>();
        mf.sharedMesh = meshCollider.sharedMesh;

        MeshRenderer mr = occluderObj.AddComponent<MeshRenderer>();
        mr.sharedMaterial = OpaqueMaterial; // Assign the shared opaque material

        // Tag as EditorOnly (excludes from builds)
        occluderObj.tag = "EditorOnly";

        // Mark as Occluder Static
        GameObjectUtility.SetStaticEditorFlags(occluderObj, StaticEditorFlags.OccluderStatic);
    }
}
