// Save this script in a folder named "Editor" within your Unity project.
// For example: Assets/Editor/ColliderFlattener.cs

using UnityEngine;
using UnityEditor;

public class ColliderFlattener
{
    /// <summary>
    /// Adds a menu item under Tools to flatten MeshColliders and BoxColliders of the selected GameObject.
    /// </summary>
    [MenuItem("Tools/Flatten Colliders")]
    public static void FlattenColliders()
    {
        // Get the currently selected GameObject
        GameObject selected = Selection.activeGameObject;

        if (selected == null)
        {
            EditorUtility.DisplayDialog("Flatten Colliders", "Please select a GameObject.", "OK");
            return;
        }

        // Create a new parent GameObject to hold the flattened colliders
        string flattenedName = selected.name + "_Flattened";
        GameObject flattened = new GameObject(flattenedName);

        // Position the new parent at the same hierarchy level as the original
        flattened.transform.SetParent(selected.transform.parent, false);

        // Find all MeshColliders and BoxColliders in the original GameObject and its children
        Collider[] colliders = selected.GetComponentsInChildren<Collider>(true);

        int colliderCount = 0;

        foreach (Collider col in colliders)
        {
            if (col is BoxCollider || col is MeshCollider)
            {
                colliderCount++;

                // Store original transform properties
                Vector3 originalPosition = col.transform.position;
                Quaternion originalRotation = col.transform.rotation;
                Vector3 originalScale = col.transform.lossyScale;

                // Create a new child GameObject for the collider
                string colliderName = col.gameObject.name + "_FlattenedCollider";
                GameObject colliderChild = new GameObject(colliderName);
                colliderChild.transform.SetParent(flattened.transform, false);

                // Set the child’s global position, rotation, and scale to match the original
                colliderChild.transform.position = originalPosition;
                colliderChild.transform.rotation = originalRotation;
                colliderChild.transform.localScale = originalScale;

                // Copy the collider component
                if (col is BoxCollider box)
                {
                    BoxCollider newBox = colliderChild.AddComponent<BoxCollider>();
                    newBox.center = box.center;
                    newBox.size = box.size;
                    newBox.isTrigger = box.isTrigger;
                    newBox.material = box.material;
                    // Copy other BoxCollider properties if necessary
                }
                else if (col is MeshCollider mesh)
                {
                    MeshCollider newMesh = colliderChild.AddComponent<MeshCollider>();
                    newMesh.sharedMesh = mesh.sharedMesh;
                    newMesh.convex = mesh.convex;
                    newMesh.isTrigger = mesh.isTrigger;
                    // newMesh.inflateMesh = mesh.inflateMesh;
                    // newMesh.skinWidth = mesh.skinWidth;
                    // Copy other MeshCollider properties if necessary
                }

                // Optionally, copy layer and tag
                colliderChild.layer = col.gameObject.layer;
                colliderChild.tag = col.gameObject.tag;
            }
        }

        if (colliderCount == 0)
        {
            EditorUtility.DisplayDialog("Flatten Colliders", "No BoxColliders or MeshColliders found in the selected GameObject.", "OK");
            // Destroy the empty flattened GameObject using UnityEngine.Object.DestroyImmediate
            UnityEngine.Object.DestroyImmediate(flattened);
            return;
        }

        // Optionally, position the flattened parent to the origin or any desired location
        // For this script, it remains at the same position as its original parent

        // Select the flattened GameObject in the Hierarchy
        Selection.activeGameObject = flattened;

        // Inform the user of completion
        EditorUtility.DisplayDialog("Flatten Colliders", $"Successfully flattened {colliderCount} colliders.", "OK");
    }
}
