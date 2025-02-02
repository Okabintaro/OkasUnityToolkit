// PrefabLayoutTool.cs
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

public class PrefabLayoutTool : EditorWindow
{
    // Grid settings
    private int columns = 5;
    private float spacing = 3f;

    // Keep track of instantiated objects for cleanup
    private List<GameObject> instantiatedObjects = new List<GameObject>();

    [MenuItem("Tools/Layout/Layout Prefabs")]
    public static void ShowWindow()
    {
        GetWindow<PrefabLayoutTool>("Layout Prefabs");
    }

    private void OnGUI()
    {
        GUILayout.Label("Prefab Layout Settings", EditorStyles.boldLabel);

        columns = EditorGUILayout.IntField("Columns", Mathf.Max(1, columns));
        spacing = EditorGUILayout.FloatField("Spacing", spacing);

        GUILayout.Space(10);

        if (GUILayout.Button("Layout Selected Prefabs"))
        {
            LayoutSelectedPrefabs();
        }

        if (GUILayout.Button("Layout All Prefabs in Selected Folder"))
        {
            LayoutPrefabsInSelectedFolder();
        }

        GUILayout.Space(10);

        if (GUILayout.Button("Clean Up"))
        {
            CleanUp();
        }

        GUILayout.Space(10);
        GUILayout.Label("Note: Prefabs will be instantiated under an empty GameObject named 'Prefab Layout'.", EditorStyles.helpBox);
    }

    private void LayoutSelectedPrefabs()
    {
        // Get selected objects in Project window
        Object[] selectedObjects = Selection.objects;

        List<string> prefabPaths = new List<string>();

        foreach (Object obj in selectedObjects)
        {
            string path = AssetDatabase.GetAssetPath(obj);
            if (IsPrefabAsset(obj))
            {
                prefabPaths.Add(path);
            }
        }

        if (prefabPaths.Count == 0)
        {
            EditorUtility.DisplayDialog("No Prefabs Selected", "Please select one or more Prefabs in the Project window.", "OK");
            return;
        }

        InstantiatePrefabs(prefabPaths.ToArray());
    }

    private void LayoutPrefabsInSelectedFolder()
    {
        // Get the selected folder in Project window
        Object selected = Selection.activeObject;
        string folderPath = "";

        if (selected == null)
        {
            EditorUtility.DisplayDialog("No Folder Selected", "Please select a folder in the Project window.", "OK");
            return;
        }

        string selectedPath = AssetDatabase.GetAssetPath(selected);

        if (AssetDatabase.IsValidFolder(selectedPath))
        {
            folderPath = selectedPath;
        }
        else
        {
            // If a file is selected, get its parent folder
            folderPath = Path.GetDirectoryName(selectedPath);
        }

        // Get all prefab paths in the folder and its subfolders
        string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { folderPath });
        List<string> prefabPaths = new List<string>();

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            prefabPaths.Add(path);
        }

        if (prefabPaths.Count == 0)
        {
            EditorUtility.DisplayDialog("No Prefabs Found", "No Prefabs were found in the selected folder.", "OK");
            return;
        }

        InstantiatePrefabs(prefabPaths.ToArray());
    }

    private void InstantiatePrefabs(string[] prefabPaths)
    {
        // Optionally clean up previous instances
        CleanUp();

        // Create a parent GameObject to hold all instantiated Prefabs
        GameObject parent = new GameObject("Prefab Layout");
        Undo.RegisterCreatedObjectUndo(parent, "Create Prefab Layout Parent");

        int row = 0;
        int column = 0;

        foreach (string path in prefabPaths)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab != null)
            {
                GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                Undo.RegisterCreatedObjectUndo(instance, "Instantiate Prefab");
                instance.transform.parent = parent.transform;

                // Position in grid
                instance.transform.position = new Vector3(column * spacing, 0, row * spacing);

                instantiatedObjects.Add(instance);

                column++;
                if (column >= columns)
                {
                    column = 0;
                    row++;
                }
            }
        }

        // Select the parent in the Hierarchy
        Selection.activeGameObject = parent;
    }

    private void CleanUp()
    {
        // Find the "Prefab Layout" parent
        GameObject parent = GameObject.Find("Prefab Layout");
        if (parent != null)
        {
            if (EditorUtility.DisplayDialog("Delete Prefab Layout",
                "Are you sure you want to delete the existing 'Prefab Layout' GameObject and all its children?",
                "Yes", "No"))
            {
                Undo.DestroyObjectImmediate(parent);
            }
        }
    }

    /// <summary>
    /// Checks if the given object is a Prefab asset.
    /// </summary>
    /// <param name="obj">The object to check.</param>
    /// <returns>True if the object is a Prefab asset; otherwise, false.</returns>
    private bool IsPrefabAsset(Object obj)
    {
        // Use GetPrefabAssetType to determine if the object is a Prefab asset
        PrefabAssetType assetType = PrefabUtility.GetPrefabAssetType(obj);
        return assetType != PrefabAssetType.NotAPrefab;
    }
}
