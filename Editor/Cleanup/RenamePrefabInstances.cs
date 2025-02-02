// Filename: RenamePrefabInstances.cs
// Place this script inside a folder named "Editor" (e.g., Assets/Editor/RenamePrefabInstances.cs)

using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class RenamePrefabInstances : EditorWindow
{
    // Add a menu item named "Rename Prefab Instances" to the Tools menu
    [MenuItem("Tools/Cleanup/Rename Prefab Instances")]
    public static void ShowWindow()
    {
        // Show a dialog prompting the user to select prefabs
        if (Selection.objects.Length == 0)
        {
            EditorUtility.DisplayDialog("No Prefab Selected", "Please select one or more prefab assets in the Project window.", "OK");
            return;
        }

        // Check if the selected objects are prefab assets
        List<GameObject> selectedPrefabs = new List<GameObject>();
        foreach (Object obj in Selection.objects)
        {
            // Ensure the selected object is a GameObject and a prefab
            if (obj is GameObject)
            {
                string path = AssetDatabase.GetAssetPath(obj);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab != null)
                {
                    selectedPrefabs.Add(prefab);
                }
            }
        }

        if (selectedPrefabs.Count == 0)
        {
            EditorUtility.DisplayDialog("Invalid Selection", "Please select valid prefab assets in the Project window.", "OK");
            return;
        }

        // Ask for confirmation
        if (EditorUtility.DisplayDialog(
            "Rename Prefab Instances",
            $"Are you sure you want to rename all instances of the selected prefab(s) in the active scene?",
            "Yes",
            "No"))
        {
            // Proceed with renaming
            foreach (GameObject prefab in selectedPrefabs)
            {
                RenameInstancesOfPrefab(prefab);
            }

            // Mark the scene as dirty to ensure changes are saved
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());

            EditorUtility.DisplayDialog("Renaming Complete", "All selected prefab instances have been renamed successfully.", "OK");
        }
    }

    /// <summary>
    /// Renames all instances of a given prefab in the active scene by appending a unique numerical suffix.
    /// </summary>
    /// <param name="prefab">The prefab whose instances will be renamed.</param>
    private static void RenameInstancesOfPrefab(GameObject prefab)
    {
        if (prefab == null)
        {
            Debug.LogWarning("Prefab is null. Skipping.");
            return;
        }

        string prefabPath = AssetDatabase.GetAssetPath(prefab);
        if (string.IsNullOrEmpty(prefabPath))
        {
            Debug.LogWarning($"Could not find asset path for prefab: {prefab.name}. Skipping.");
            return;
        }

        // Get all root GameObjects in the active scene
        Scene activeScene = SceneManager.GetActiveScene();
        if (!activeScene.isLoaded)
        {
            Debug.LogWarning($"Active scene '{activeScene.name}' is not loaded. Skipping prefab '{prefab.name}'.");
            return;
        }

        GameObject[] rootObjects = activeScene.GetRootGameObjects();

        // List to hold all prefab instances
        List<GameObject> prefabInstances = new List<GameObject>();

        // Iterate through all root objects and their children to find prefab instances
        foreach (GameObject rootObj in rootObjects)
        {
            FindPrefabInstancesRecursive(rootObj, prefabPath, prefabInstances);
        }

        if (prefabInstances.Count == 0)
        {
            Debug.Log($"No instances of prefab '{prefab.name}' found in the active scene.");
            return;
        }

        // Sort the instances to ensure consistent naming
        prefabInstances.Sort((a, b) => a.GetInstanceID().CompareTo(b.GetInstanceID()));

        // Rename each instance with the prefab name and a unique index
        int index = 1;
        foreach (GameObject instance in prefabInstances)
        {
            string newName = $"{prefab.name}_{index}";
            Undo.RecordObject(instance, "Rename Prefab Instance");
            instance.name = newName;
            index++;
        }

        Debug.Log($"Renamed {prefabInstances.Count} instances of prefab '{prefab.name}'.");
    }

    /// <summary>
    /// Recursively searches for prefab instances matching the given prefab path.
    /// </summary>
    /// <param name="obj">The current GameObject to check.</param>
    /// <param name="prefabPath">The asset path of the prefab.</param>
    /// <param name="instances">The list to store found instances.</param>
    private static void FindPrefabInstancesRecursive(GameObject obj, string prefabPath, List<GameObject> instances)
    {
        // Check if the GameObject is a prefab instance
        GameObject sourcePrefab = PrefabUtility.GetCorrespondingObjectFromSource(obj);
        if (sourcePrefab != null)
        {
            string sourcePath = AssetDatabase.GetAssetPath(sourcePrefab);
            if (sourcePath == prefabPath)
            {
                instances.Add(obj);
            }
        }

        // Recursively check children
        foreach (Transform child in obj.transform)
        {
            FindPrefabInstancesRecursive(child.gameObject, prefabPath, instances);
        }
    }
}
