using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class GroupPropsByMaterial : Editor
{
    private const string propsMaterialFolder = "Assets/Library/Materials/props_materials";

    [MenuItem("Tools/Group Props in Scene")]
    private static void GroupPropsInScene()
    {
        // Find or create our "Group_Props" parent in the scene
        GameObject groupPropsParent = GameObject.Find("Group_Props");
        if (groupPropsParent == null)
        {
            groupPropsParent = new GameObject("Group_Props");
        }

        // Get the active scene
        Scene currentScene = SceneManager.GetActiveScene();
        if (!currentScene.IsValid() || !currentScene.isLoaded)
        {
            Debug.LogWarning("No valid or loaded scene found!");
            return;
        }

        // Get all root objects in the scene
        GameObject[] rootObjects = currentScene.GetRootGameObjects();

        int objectsGrouped = 0;
        foreach (GameObject rootObj in rootObjects)
        {
            // Skip if it's the Group_Props object itself
            if (rootObj == groupPropsParent) continue;

            // Check if this root object or any of its children
            // use a material from our propsMaterialFolder
            if (RootHasPropMaterial(rootObj))
            {
                // Re-parent the entire root object under "Group_Props"
                if (rootObj.transform.parent != groupPropsParent.transform)
                {
                    Undo.SetTransformParent(rootObj.transform, groupPropsParent.transform, "Group Props");
                    objectsGrouped++;
                }
            }
        }

        Debug.Log($"Group Props: Moved {objectsGrouped} object(s) under '{groupPropsParent.name}'.");
    }

    /// <summary>
    /// Recursively checks if any child (or the root itself) has a MeshRenderer
    /// referencing a material in the propsMaterialFolder.
    /// </summary>
    private static bool RootHasPropMaterial(GameObject rootObj)
    {
        // BFS or DFS approach: traverse all children
        Queue<Transform> queue = new Queue<Transform>();
        queue.Enqueue(rootObj.transform);

        while (queue.Count > 0)
        {
            Transform current = queue.Dequeue();
            
            // Check MeshRenderer on this Transform
            MeshRenderer mr = current.GetComponent<MeshRenderer>();
            if (mr != null && HasPropMaterial(mr.sharedMaterials))
            {
                // If we find at least one, we know it's a prop
                return true;
            }

            // Enqueue children
            for (int i = 0; i < current.childCount; i++)
            {
                queue.Enqueue(current.GetChild(i));
            }
        }

        return false;
    }

    /// <summary>
    /// Returns true if any of the given materials is from the propsMaterialFolder.
    /// </summary>
    private static bool HasPropMaterial(Material[] materials)
    {
        foreach (Material mat in materials)
        {
            if (mat == null) 
                continue;

            string assetPath = AssetDatabase.GetAssetPath(mat);
            if (!string.IsNullOrEmpty(assetPath))
            {
                // If the asset path starts with the props material folder,
                // it’s considered a prop material.
                if (assetPath.StartsWith(propsMaterialFolder))
                {
                    return true;
                }
            }
        }

        return false;
    }
}
