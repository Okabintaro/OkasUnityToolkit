using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

public class ReplaceMeshesWithOptimized : EditorWindow
{
    // Configuration variables
    private string meshesFolder = "Assets/Meshes/";
    private string optimizedFolderName = "Optimized";

    // Scroll position for log display
    private Vector2 scrollPos;
    private List<string> logMessages = new List<string>();

    [MenuItem("Tools/Replace Meshes with Optimized Versions")]
    public static void ShowWindow()
    {
        GetWindow<ReplaceMeshesWithOptimized>("Replace Meshes");
    }

    private void OnGUI()
    {
        GUILayout.Label("Mesh Replacement Settings", EditorStyles.boldLabel);

        // Input fields for folder paths
        meshesFolder = EditorGUILayout.TextField("Meshes Folder", meshesFolder);
        optimizedFolderName = EditorGUILayout.TextField("Optimized Subfolder Name", optimizedFolderName);

        GUILayout.Space(10);

        // Button to execute the replacement
        if (GUILayout.Button("Replace Meshes in Selected Prefabs"))
        {
            ReplaceMeshes();
        }

        GUILayout.Space(20);

        // Log display
        GUILayout.Label("Log Output", EditorStyles.boldLabel);
        scrollPos = GUILayout.BeginScrollView(scrollPos, GUILayout.Height(200));
        foreach (string log in logMessages)
        {
            GUILayout.Label(log);
        }
        GUILayout.EndScrollView();
    }

    private void ReplaceMeshes()
    {
        logMessages.Clear();
        Log("Starting Mesh Replacement Process...");

        // Validate meshes folder
        if (!AssetDatabase.IsValidFolder(meshesFolder))
        {
            LogError($"Meshes folder not found: {meshesFolder}");
            return;
        }

        string optimizedPath = Path.Combine(meshesFolder, optimizedFolderName);
        if (!AssetDatabase.IsValidFolder(optimizedPath))
        {
            LogError($"Optimized folder not found: {optimizedPath}");
            return;
        }

        // Get optimized meshes
        string[] optimizedMeshGUIDs = AssetDatabase.FindAssets("t:Mesh", new[] { optimizedPath });
        Dictionary<string, string> optimizedMeshes = new Dictionary<string, string>();
        foreach (string guid in optimizedMeshGUIDs)
        {
            string optimizedMeshPath = AssetDatabase.GUIDToAssetPath(guid);
            string meshName = Path.GetFileNameWithoutExtension(optimizedMeshPath);
            if (!optimizedMeshes.ContainsKey(meshName))
            {
                optimizedMeshes.Add(meshName, optimizedMeshPath);
            }
        }

        Log($"Found {optimizedMeshes.Count} optimized meshes.");

        // Get selected GameObjects in the scene
        GameObject[] selectedObjects = Selection.gameObjects;
        if (selectedObjects.Length == 0)
        {
            LogError("No GameObjects selected. Please select prefab instances in the scene.");
            return;
        }

        // Collect unique prefab asset paths
        HashSet<string> prefabAssetPaths = new HashSet<string>();
        foreach (GameObject obj in selectedObjects)
        {
            // Get the corresponding prefab asset
            string prefabAssetPath = GetPrefabAssetPath(obj);
            if (!string.IsNullOrEmpty(prefabAssetPath))
            {
                prefabAssetPaths.Add(prefabAssetPath);
            }
            else
            {
                LogWarning($"GameObject '{obj.name}' is not a prefab instance or has no associated prefab asset.");
            }
        }

        if (prefabAssetPaths.Count == 0)
        {
            LogError("No valid prefab assets found from the selected GameObjects.");
            return;
        }

        Log($"Processing {prefabAssetPaths.Count} unique prefab assets...");

        int totalMeshesReplaced = 0;
        int totalMeshesSkipped = 0;

        foreach (string prefabPath in prefabAssetPaths)
        {
            Log($"Processing Prefab: {prefabPath}");

            // Load the prefab asset
            GameObject prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefabAsset == null)
            {
                LogWarning($"Failed to load prefab asset at path: {prefabPath}");
                continue;
            }

            // Get all MeshFilter components in the prefab
            MeshFilter[] meshFilters = prefabAsset.GetComponentsInChildren<MeshFilter>(includeInactive: true);
            bool prefabModified = false;

            foreach (MeshFilter meshFilter in meshFilters)
            {
                if (meshFilter.sharedMesh == null)
                {
                    LogWarning($"MeshFilter on '{meshFilter.gameObject.name}' has no mesh assigned.");
                    totalMeshesSkipped++;
                    continue;
                }

                string originalMeshName = meshFilter.sharedMesh.name;

                if (optimizedMeshes.TryGetValue(originalMeshName, out string optimizedMeshPath))
                {
                    Mesh optimizedMesh = AssetDatabase.LoadAssetAtPath<Mesh>(optimizedMeshPath);
                    if (optimizedMesh != null)
                    {
                        // Replace the mesh
                        meshFilter.sharedMesh = optimizedMesh;
                        prefabModified = true;
                        totalMeshesReplaced++;
                        Log($"Replaced mesh '{originalMeshName}' with optimized version.");
                    }
                    else
                    {
                        LogWarning($"Optimized mesh not found or failed to load at path: {optimizedMeshPath}");
                        totalMeshesSkipped++;
                    }
                }
                else
                {
                    LogWarning($"No optimized mesh found for '{originalMeshName}' in '{optimizedPath}'.");
                    totalMeshesSkipped++;
                }
            }

            if (prefabModified)
            {
                // Save the changes to the prefab
                PrefabUtility.SavePrefabAsset(prefabAsset);
                AssetDatabase.SaveAssets();
                Log($"Prefab '{prefabPath}' updated successfully.");
            }
            else
            {
                Log($"No meshes were replaced in prefab '{prefabPath}'.");
            }
        }

        Log($"Mesh Replacement Completed: {totalMeshesReplaced} meshes replaced, {totalMeshesSkipped} meshes skipped.");
    }

    /// <summary>
    /// Retrieves the prefab asset path for a given GameObject instance in the scene.
    /// </summary>
    /// <param name="go">The GameObject instance.</param>
    /// <returns>The prefab asset path, or null if not applicable.</returns>
    private string GetPrefabAssetPath(GameObject go)
    {
        PrefabInstanceStatus status = PrefabUtility.GetPrefabInstanceStatus(go);
        if (status == PrefabInstanceStatus.Connected || status == PrefabInstanceStatus.Disconnected)
        {
            // Correctly get the prefab source as a UnityEngine.Object
            Object prefabSource = PrefabUtility.GetCorrespondingObjectFromSource(go);
            if (prefabSource != null)
            {
                return AssetDatabase.GetAssetPath(prefabSource);
            }
        }
        return null;
    }

    /// <summary>
    /// Logs a standard message.
    /// </summary>
    /// <param name="message">The message to log.</param>
    private void Log(string message)
    {
        logMessages.Add($"<color=white>{message}</color>");
        Debug.Log(message);
    }

    /// <summary>
    /// Logs a warning message.
    /// </summary>
    /// <param name="message">The warning message.</param>
    private void LogWarning(string message)
    {
        logMessages.Add($"<color=yellow>Warning: {message}</color>");
        Debug.LogWarning(message);
    }

    /// <summary>
    /// Logs an error message.
    /// </summary>
    /// <param name="message">The error message.</param>
    private void LogError(string message)
    {
        logMessages.Add($"<color=red>Error: {message}</color>");
        Debug.LogError(message);
    }
}
