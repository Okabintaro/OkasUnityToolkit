// Script that uses AutoLOD asset: http://leochaumartin.com/wiki/index.php/AutoLOD
// Disabled for now

#if false

using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using AutoLOD.MeshDecimator; // Ensure this namespace is correct based on your AutoLOD package

public class MeshDecimatorWindow : EditorWindow
{
    // List to hold prefabs
    private List<Object> prefabs = new List<Object>();

    // Decimation backend options
    private enum DecimationBackend
    {
        Fast,
        Quality
    }

    private DecimationBackend selectedBackend = DecimationBackend.Fast;

    // Decimation percentage
    private float decimationPercentage = 50f;

    // Replacement option
    private bool replaceOriginal = true;

    // Scroll position for the prefab list
    private Vector2 scrollPos;

    // Add menu item to open the window
    [MenuItem("Tools/Optimize/Mesh Decimator")]
    public static void ShowWindow()
    {
        GetWindow<MeshDecimatorWindow>("Mesh Decimator");
    }

    private void OnGUI()
    {
        GUILayout.Label("Mesh Decimator Settings", EditorStyles.boldLabel);

        EditorGUILayout.Space();

        // Drag-and-Drop area
        GUILayout.Label("Drag Prefabs Here:", EditorStyles.label);
        Rect dropArea = GUILayoutUtility.GetRect(0.0f, 50.0f, GUILayout.ExpandWidth(true));
        GUI.Box(dropArea, "Drag & Drop Prefabs Here", EditorStyles.helpBox);

        HandleDragAndDrop(dropArea);

        EditorGUILayout.Space();

        // Prefab list
        GUILayout.Label("Selected Prefabs:", EditorStyles.label);
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.Height(150));
        for (int i = 0; i < prefabs.Count; i++)
        {
            EditorGUILayout.BeginHorizontal();
            prefabs[i] = EditorGUILayout.ObjectField(prefabs[i], typeof(GameObject), false);
            if (GUILayout.Button("Remove", GUILayout.Width(60)))
            {
                prefabs.RemoveAt(i);
                i--;
            }
            EditorGUILayout.EndHorizontal();
        }
        EditorGUILayout.EndScrollView();

        // Add Prefab button
        if (GUILayout.Button("Add Prefab via Dialog"))
        {
            AddPrefab();
        }

        EditorGUILayout.Space();

        // Decimation backend selection
        GUILayout.Label("Decimation Backend:", EditorStyles.label);
        selectedBackend = (DecimationBackend)EditorGUILayout.EnumPopup(selectedBackend);

        // Decimation percentage
        GUILayout.Label("Decimation Percentage (%):", EditorStyles.label);
        decimationPercentage = EditorGUILayout.Slider(decimationPercentage, 0.01f, 100f);

        // Replacement option
        GUILayout.Label("Prefab Modification:", EditorStyles.label);
        replaceOriginal = EditorGUILayout.Toggle("Replace Original Prefabs", replaceOriginal);
        if (!replaceOriginal)
        {
            EditorGUILayout.HelpBox("New decimated prefabs will have a '_Decimated' suffix.", MessageType.Info);
        }

        EditorGUILayout.Space();

        // Decimate button
        if (GUILayout.Button("Decimate Meshes"))
        {
            if (prefabs.Count == 0)
            {
                EditorUtility.DisplayDialog("No Prefabs Selected", "Please add at least one prefab to decimate.", "OK");
            }
            else
            {
                DecimateSelectedPrefabs();
            }
        }
    }

    /// <summary>
    /// Handles drag-and-drop events within the specified drop area.
    /// </summary>
    /// <param name="dropArea">The rectangular area designated for drag-and-drop.</param>
    private void HandleDragAndDrop(Rect dropArea)
    {
        Event evt = Event.current;
        switch (evt.type)
        {
            case EventType.DragUpdated:
            case EventType.DragPerform:
                if (!dropArea.Contains(evt.mousePosition))
                    break;

                // Check if the dragged objects are valid
                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

                if (evt.type == EventType.DragPerform)
                {
                    DragAndDrop.AcceptDrag();

                    foreach (Object draggedObject in DragAndDrop.objectReferences)
                    {
                        if (draggedObject is GameObject draggedPrefab)
                        {
                            // Ensure it's a prefab by checking if it's a prefab asset
                            string assetPath = AssetDatabase.GetAssetPath(draggedPrefab);
                            if (PrefabUtility.IsPartOfPrefabAsset(draggedPrefab))
                            {
                                if (!prefabs.Contains(draggedPrefab))
                                {
                                    prefabs.Add(draggedPrefab);
                                    Debug.Log($"Added prefab: {draggedPrefab.name}");
                                }
                                else
                                {
                                    Debug.LogWarning($"Prefab '{draggedPrefab.name}' is already in the list.");
                                }
                            }
                            else
                            {
                                Debug.LogWarning($"Object '{draggedPrefab.name}' is not a valid prefab.");
                            }
                        }
                        else
                        {
                            Debug.LogWarning($"Unsupported object type: {draggedObject.GetType().Name}. Only prefabs are allowed.");
                        }
                    }
                }
                Event.current.Use();
                break;
        }
    }

    private void AddPrefab()
    {
        // Open a file panel to select prefabs
        string path = EditorUtility.OpenFilePanel("Select Prefab", "Assets", "prefab");
        if (!string.IsNullOrEmpty(path))
        {
            // Convert absolute path to relative project path
            if (path.StartsWith(Application.dataPath))
            {
                string relativePath = "Assets" + path.Substring(Application.dataPath.Length);
                Object prefab = AssetDatabase.LoadAssetAtPath<GameObject>(relativePath);
                if (prefab != null)
                {
                    if (!prefabs.Contains(prefab))
                    {
                        prefabs.Add(prefab);
                    }
                    else
                    {
                        EditorUtility.DisplayDialog("Prefab Already Added", "This prefab is already in the list.", "OK");
                    }
                }
                else
                {
                    EditorUtility.DisplayDialog("Invalid Prefab", "The selected file is not a valid prefab.", "OK");
                }
            }
            else
            {
                EditorUtility.DisplayDialog("Invalid Path", "Please select a prefab within the project's Assets folder.", "OK");
            }
        }
    }

    private void DecimateSelectedPrefabs()
    {
        // Confirm action
        if (!EditorUtility.DisplayDialog("Confirm Decimation",
            $"Are you sure you want to decimate {prefabs.Count} prefab(s)?",
            "Yes", "No"))
        {
            return;
        }

        int successCount = 0;
        int failureCount = 0;

        foreach (Object obj in prefabs)
        {
            if (obj is GameObject prefab)
            {
                string prefabPath = AssetDatabase.GetAssetPath(prefab);
                if (string.IsNullOrEmpty(prefabPath))
                {
                    Debug.LogWarning($"Could not find path for prefab '{prefab.name}'. Skipping.");
                    failureCount++;
                    continue;
                }

                // Load the prefab contents
                GameObject prefabInstance = PrefabUtility.LoadPrefabContents(prefabPath);
                if (prefabInstance == null)
                {
                    Debug.LogWarning($"Could not load prefab '{prefab.name}'. Skipping.");
                    failureCount++;
                    continue;
                }

                bool prefabModified = false;

                MeshFilter[] meshFilters = prefabInstance.GetComponentsInChildren<MeshFilter>();

                foreach (MeshFilter mf in meshFilters)
                {
                    Mesh originalMesh = mf.sharedMesh;
                    if (originalMesh == null)
                    {
                        Debug.LogWarning($"Prefab '{prefab.name}' has a MeshFilter with no mesh assigned.");
                        continue;
                    }

                    int originalFaceCount = originalMesh.triangles.Length / 3;
                    int targetFaceCount = Mathf.Max(1, Mathf.RoundToInt(originalFaceCount * (decimationPercentage / 100f)));

                    Mesh decimatedMesh = null;

                    try
                    {
                        switch (selectedBackend)
                        {
                            case DecimationBackend.Fast:
                                decimatedMesh = CFastMeshDecimator.DecimateMeshStatic(originalMesh, targetFaceCount, false);
                                break;
                            case DecimationBackend.Quality:
                                decimatedMesh = CQualityMeshDecimator.DecimateMeshStatic(originalMesh, targetFaceCount, false);
                                break;
                        }

                        if (decimatedMesh != null && decimatedMesh.triangles.Length / 3 >= 1)
                        {
                            // Save the decimated mesh as a new asset
                            string meshAssetPath = GetDecimatedMeshPath(prefabPath, originalMesh.name);
                            meshAssetPath = AssetDatabase.GenerateUniqueAssetPath(meshAssetPath);

                            AssetDatabase.CreateAsset(decimatedMesh, meshAssetPath);
                            AssetDatabase.SaveAssets();

                            // Assign the decimated mesh to the MeshFilter
                            mf.sharedMesh = AssetDatabase.LoadAssetAtPath<Mesh>(meshAssetPath);
                            prefabModified = true;

                            Debug.Log($"Decimated mesh '{originalMesh.name}' in prefab '{prefab.name}' and saved to '{meshAssetPath}'.");
                        }
                        else
                        {
                            Debug.LogWarning($"Decimation failed for mesh '{originalMesh.name}' in prefab '{prefab.name}'. Decimated mesh has insufficient faces.");
                        }
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogError($"Error decimating mesh '{originalMesh.name}' in prefab '{prefab.name}': {ex.Message}");
                        failureCount++;
                    }
                }

                if (prefabModified)
                {
                    if (replaceOriginal)
                    {
                        PrefabUtility.SaveAsPrefabAsset(prefabInstance, prefabPath);
                        successCount++;
                    }
                    else
                    {
                        string directory = Path.GetDirectoryName(prefabPath);
                        string filename = Path.GetFileNameWithoutExtension(prefabPath);
                        string newPrefabPath = Path.Combine(directory, $"{filename}_Decimated.prefab");

                        // Ensure the path uses forward slashes
                        newPrefabPath = newPrefabPath.Replace("\\", "/");

                        // Save the new prefab with decimated meshes
                        PrefabUtility.SaveAsPrefabAsset(prefabInstance, newPrefabPath);
                        successCount++;

                        Debug.Log($"Created decimated prefab '{newPrefabPath}'.");
                    }
                }
                else
                {
                    Debug.LogWarning($"No meshes were decimated for prefab '{prefab.name}'.");
                }

                // Unload the prefab instance
                PrefabUtility.UnloadPrefabContents(prefabInstance);
            }
            else
            {
                Debug.LogWarning("One of the selected objects is not a prefab. Skipping.");
                failureCount++;
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("Decimation Complete",
            $"Successfully decimated {successCount} prefab(s).\nFailed to decimate {failureCount} prefab(s). Check the Console for details.",
            "OK");
    }

    private string GetDecimatedMeshPath(string prefabPath, string originalMeshName)
    {
        string prefabDirectory = Path.GetDirectoryName(prefabPath);
        string meshFilename = $"{originalMeshName}_Decimated.asset";
        return Path.Combine(prefabDirectory, meshFilename).Replace("\\", "/");
    }
}

#endif