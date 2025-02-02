using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

public class FlattenHierarchy : Editor
{
    [MenuItem("Tools/Cleanup/Flatten Mesh Hierarchy")]
    public static void FlattenSelectedHierarchy()
    {
        // Make sure a Transform is actually selected
        if (Selection.activeTransform == null)
        {
            Debug.LogError("No Transform selected. Please select the root GameObject you want to flatten.");
            return;
        }

        // Get the selected Transform as the root
        Transform root = Selection.activeTransform;
        
        // Flatten the hierarchy
        FlattenMeshHierarchy(root);
    }

    private static void FlattenMeshHierarchy(Transform root)
    {
        // Get ALL children (includes inactive) under root
        List<Transform> allChildren = root.GetComponentsInChildren<Transform>(true).ToList();
        
        // Remove the root itself from this list so we don't accidentally process or delete it
        allChildren.Remove(root);

        // STEP 1: Re-parent any object with a MeshRenderer so it's a direct child of root
        foreach (Transform child in allChildren)
        {
            if (child == null) 
                continue;

            MeshRenderer meshRenderer = child.GetComponent<MeshRenderer>();
            if (meshRenderer != null)
            {
                // Keep the same world position, rotation, and scale
                child.SetParent(root, true); 
            }
        }

        // STEP 2: Remove empty GameObjects
        // We iterate in reverse so we handle deeper children first
        allChildren.Reverse();
        foreach (Transform child in allChildren)
        {
            if (child == null || child == root)
                continue;

            // "Empty" means it has no children AND no components 
            // (other than the mandatory Transform)
            if (child.childCount == 0 
                && child.GetComponents<Component>().Length == 1)
            {
                // Safe to remove
                GameObject.DestroyImmediate(child.gameObject);
            }
        }

        Debug.Log($"Finished flattening under root: {root.name}");
    }
}

