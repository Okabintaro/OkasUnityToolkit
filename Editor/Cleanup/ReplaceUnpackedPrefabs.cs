// ReplaceUnpackedPrefabsEditor.cs
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class ReplaceUnpackedPrefabsEditor : EditorWindow
{
    // Fields for user input
    private string meshName = "";
    private GameObject prefabToReplaceWith = null;

    // Toggle to choose between Mesh Name and Prefab selection
    private bool useMeshName = true;

    // For search results
    private int foundCount = 0;

    // Add a menu item to open the window
    [MenuItem("Tools/Replace Unpacked Prefabs")]
    public static void ShowWindow()
    {
        GetWindow<ReplaceUnpackedPrefabsEditor>("Replace Unpacked Prefabs");
    }

    private void OnGUI()
    {
        GUILayout.Label("Replace Unpacked Prefabs", EditorStyles.boldLabel);
        GUILayout.Space(10);

        // Toggle between Mesh Name and Prefab selection
        useMeshName = GUILayout.Toggle(useMeshName, "Identify Targets by Mesh Name");

        GUILayout.Space(5);

        if (useMeshName)
        {
            // Input field for Mesh Name
            GUILayout.Label("Mesh Name:", EditorStyles.label);
            meshName = EditorGUILayout.TextField(meshName);

            // Optional: Validate mesh name
            if (!string.IsNullOrEmpty(meshName))
            {
                Mesh mesh = FindMeshByName(meshName);
                if (mesh == null)
                {
                    EditorGUILayout.HelpBox($"No mesh found with the name '{meshName}'.", MessageType.Warning);
                }
            }
        }
        else
        {
            // Object field for Prefab selection
            GUILayout.Label("Prefab to Replace With:", EditorStyles.label);
            prefabToReplaceWith = (GameObject)EditorGUILayout.ObjectField(prefabToReplaceWith, typeof(GameObject), false);

            // Optional: Validate prefab selection
            if (prefabToReplaceWith != null)
            {
                if (!PrefabUtility.IsPartOfPrefabAsset(prefabToReplaceWith))
                {
                    EditorGUILayout.HelpBox("The selected GameObject is not a prefab asset.", MessageType.Warning);
                }
            }
        }

        GUILayout.Space(20);

        // Replace button
        if (GUILayout.Button("Replace Unpacked Prefabs"))
        {
            ReplaceUnpackedPrefabs();
        }

        GUILayout.Space(10);

        // Display found count after replacement
        if (foundCount > 0)
        {
            EditorGUILayout.HelpBox($"Found and replaced {foundCount} instance(s).", MessageType.Info);
        }
    }

    /// <summary>
    /// Finds a mesh in the project by its name.
    /// </summary>
    /// <param name="name">Name of the mesh to find.</param>
    /// <returns>The Mesh if found; otherwise, null.</returns>
    private Mesh FindMeshByName(string name)
    {
        string[] meshGuids = AssetDatabase.FindAssets($"{name} t:Mesh");
        if (meshGuids.Length == 0)
            return null;

        string meshPath = AssetDatabase.GUIDToAssetPath(meshGuids[0]);
        Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
        return mesh;
    }

    /// <summary>
    /// Main method to replace unpacked prefab instances based on user input.
    /// </summary>
    private void ReplaceUnpackedPrefabs()
    {
        // Reset found count
        foundCount = 0;

        // Validate input
        if (useMeshName)
        {
            if (string.IsNullOrEmpty(meshName))
            {
                EditorUtility.DisplayDialog("Replace Unpacked Prefabs", "Please enter a mesh name.", "OK");
                return;
            }

            Mesh targetMesh = FindMeshByName(meshName);
            if (targetMesh == null)
            {
                EditorUtility.DisplayDialog("Replace Unpacked Prefabs", $"No mesh found with the name '{meshName}'.", "OK");
                return;
            }
        }
        else
        {
            if (prefabToReplaceWith == null)
            {
                EditorUtility.DisplayDialog("Replace Unpacked Prefabs", "Please select a prefab to replace with.", "OK");
                return;
            }

            if (!PrefabUtility.IsPartOfPrefabAsset(prefabToReplaceWith))
            {
                EditorUtility.DisplayDialog("Replace Unpacked Prefabs", "The selected GameObject is not a prefab asset.", "OK");
                return;
            }
        }

        // Get the active scene
        Scene activeScene = SceneManager.GetActiveScene();
        if (!activeScene.isLoaded)
        {
            EditorUtility.DisplayDialog("Replace Unpacked Prefabs", "No active scene is loaded.", "OK");
            return;
        }

        // Collect all target GameObjects
        List<GameObject> objectsToReplace = new List<GameObject>();
        GameObject[] allRootObjects = activeScene.GetRootGameObjects();

        foreach (GameObject rootObj in allRootObjects)
        {
            CollectObjectsToReplace(rootObj, objectsToReplace);
        }

        if (objectsToReplace.Count == 0)
        {
            EditorUtility.DisplayDialog("Replace Unpacked Prefabs", "No matching GameObjects found in the active scene.", "OK");
            return;
        }

        // Begin Undo Group for better undo management
        Undo.IncrementCurrentGroup();
        int undoGroup = Undo.GetCurrentGroup();

        // Perform replacement
        foreach (GameObject obj in objectsToReplace)
        {
            if (obj == null)
                continue;

            // Record original transform and parent
            Vector3 originalPosition = obj.transform.position;
            Quaternion originalRotation = obj.transform.rotation;
            Vector3 originalScale = obj.transform.localScale;
            Transform originalParent = obj.transform.parent;

            GameObject newPrefabInstance = null;

            if (useMeshName)
            {
                // Find the original prefab based on mesh name
                // Assuming that the prefab has the same mesh name
                string[] prefabGuids = AssetDatabase.FindAssets($"{meshName} t:Prefab");
                if (prefabGuids.Length == 0)
                {
                    Debug.LogWarning($"No prefab found with a mesh named '{meshName}'.");
                    continue;
                }

                string prefabPath = AssetDatabase.GUIDToAssetPath(prefabGuids[0]);
                GameObject originalPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

                if (originalPrefab == null)
                {
                    Debug.LogWarning($"Failed to load prefab at path '{prefabPath}'.");
                    continue;
                }

                // Instantiate the prefab in the scene
                newPrefabInstance = (GameObject)PrefabUtility.InstantiatePrefab(originalPrefab, activeScene);
            }
            else
            {
                // Use the user-selected prefab
                newPrefabInstance = (GameObject)PrefabUtility.InstantiatePrefab(prefabToReplaceWith, activeScene);
            }

            if (newPrefabInstance != null)
            {
                // Set the transform to match the original
                newPrefabInstance.transform.position = originalPosition;
                newPrefabInstance.transform.rotation = originalRotation;
                newPrefabInstance.transform.localScale = originalScale;

                // Set the parent to match the original
                if (originalParent != null)
                {
                    newPrefabInstance.transform.SetParent(originalParent, worldPositionStays: true);
                }

                // Register the creation and destruction with Undo
                Undo.RegisterCreatedObjectUndo(newPrefabInstance, "Replace Unpacked Prefab");
                Undo.DestroyObjectImmediate(obj);

                foundCount++;
            }
            else
            {
                Debug.LogWarning("Failed to instantiate the replacement prefab.");
            }
        }

        // End Undo Group
        Undo.CollapseUndoOperations(undoGroup);

        // Mark the scene as dirty to ensure changes are saved
        EditorSceneManager.MarkSceneDirty(activeScene);

        // Provide feedback to the user
        EditorUtility.DisplayDialog("Replace Unpacked Prefabs", $"Successfully replaced {foundCount} instance(s).", "OK");
    }

    /// <summary>
    /// Recursively collects GameObjects to replace based on user input.
    /// </summary>
    /// <param name="obj">Current GameObject to check.</param>
    /// <param name="collection">List to store the matching GameObjects.</param>
    private void CollectObjectsToReplace(GameObject obj, List<GameObject> collection)
    {
        if (useMeshName)
        {
            MeshRenderer meshRenderer = obj.GetComponent<MeshRenderer>();
            MeshFilter meshFilter = obj.GetComponent<MeshFilter>();

            if (meshRenderer != null && meshFilter != null && meshFilter.sharedMesh != null)
            {
                if (meshFilter.sharedMesh.name == meshName)
                {
                    // Optionally, check if the GameObject is not already a prefab instance
                    if (!PrefabUtility.IsPartOfPrefabInstance(obj))
                    {
                        collection.Add(obj);
                    }
                }
            }
        }
        else
        {
            // Identify unpacked prefab instances based on the selected prefab
            // Check if the GameObject is a prefab instance and not connected to any prefab
            // Alternatively, define criteria as needed
            // For simplicity, let's assume that we want to replace GameObjects that are not prefab instances
            // and match the mesh name of the selected prefab

            // Get the prefab asset
            GameObject prefabAsset = PrefabUtility.GetCorrespondingObjectFromSource(prefabToReplaceWith);
            if (prefabAsset == null)
            {
                // The selected prefab is itself not a prefab asset
                // Proceed to find unpacked instances based on some criteria
                // Here, let's assume replacing all instances that have the same mesh as the prefab's mesh

                MeshFilter prefabMeshFilter = prefabToReplaceWith.GetComponent<MeshFilter>();
                if (prefabMeshFilter != null && prefabMeshFilter.sharedMesh != null)
                {
                    Mesh prefabMesh = prefabMeshFilter.sharedMesh;
                    MeshFilter objMeshFilter = obj.GetComponent<MeshFilter>();

                    if (objMeshFilter != null && objMeshFilter.sharedMesh != null)
                    {
                        if (objMeshFilter.sharedMesh.name == prefabMesh.name)
                        {
                            // Check if it's not a prefab instance
                            if (!PrefabUtility.IsPartOfPrefabInstance(obj))
                            {
                                collection.Add(obj);
                            }
                        }
                    }
                }
            }
            else
            {
                // The selected prefab is a valid prefab asset
                // Find unpacked instances of this prefab

                // Check if the GameObject is an unpacked prefab instance of the selected prefab
                string prefabAssetPath = AssetDatabase.GetAssetPath(prefabAsset);
                string objPrefabPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(obj);

                if (objPrefabPath != prefabAssetPath)
                {
                    // It's either not a prefab instance or a different prefab
                    // Define what constitutes an unpacked instance
                    // For simplicity, let's consider any GameObject that is not a prefab instance
                    // and matches the prefab's mesh

                    MeshFilter prefabMeshFilter = prefabAsset.GetComponent<MeshFilter>();
                    if (prefabMeshFilter != null && prefabMeshFilter.sharedMesh != null)
                    {
                        Mesh prefabMesh = prefabMeshFilter.sharedMesh;
                        MeshFilter objMeshFilter = obj.GetComponent<MeshFilter>();

                        if (objMeshFilter != null && objMeshFilter.sharedMesh != null)
                        {
                            if (objMeshFilter.sharedMesh.name == prefabMesh.name)
                            {
                                collection.Add(obj);
                            }
                        }
                    }
                }
            }
        }

        // Recursively process children
        foreach (Transform child in obj.transform)
        {
            if (child != null)
            {
                CollectObjectsToReplace(child.gameObject, collection);
            }
        }
    }
}
