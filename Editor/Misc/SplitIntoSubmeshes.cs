using UnityEngine;
using UnityEditor;
using System.IO;

public class MeshSplitter
{
    private const string MenuName = "GameObject/Split Mesh by Submeshes";

    // Adds a context menu item "Split Mesh by Submeshes" when right-clicking on a GameObject
    [MenuItem(MenuName, false, 0)]
    private static void SplitMeshBySubmeshes(MenuCommand menuCommand)
    {
        // Get the selected GameObject
        GameObject original = menuCommand.context as GameObject;
        if (original == null)
        {
            EditorUtility.DisplayDialog("Split Mesh", "Please select a single GameObject.", "OK");
            return;
        }

        // Get MeshFilter and MeshRenderer components
        MeshFilter meshFilter = original.GetComponent<MeshFilter>();
        MeshRenderer meshRenderer = original.GetComponent<MeshRenderer>();

        if (meshFilter == null || meshRenderer == null)
        {
            EditorUtility.DisplayDialog("Split Mesh", "Selected GameObject must have both MeshFilter and MeshRenderer components.", "OK");
            return;
        }

        Mesh originalMesh = meshFilter.sharedMesh;
        if (originalMesh == null)
        {
            EditorUtility.DisplayDialog("Split Mesh", "MeshFilter does not have a mesh assigned.", "OK");
            return;
        }

        int subMeshCount = originalMesh.subMeshCount;
        if (subMeshCount <= 1)
        {
            EditorUtility.DisplayDialog("Split Mesh", "Mesh does not have multiple submeshes.", "OK");
            return;
        }

        // Get the materials
        Material[] materials = meshRenderer.sharedMaterials;
        if (materials.Length < subMeshCount)
        {
            EditorUtility.DisplayDialog("Split Mesh", "Not enough materials assigned to match the submeshes.", "OK");
            return;
        }

        // Determine the path to save the new meshes
        string originalMeshPath = AssetDatabase.GetAssetPath(originalMesh);
        if (string.IsNullOrEmpty(originalMeshPath))
        {
            EditorUtility.DisplayDialog("Split Mesh", "Original mesh must be an asset in the project.", "OK");
            return;
        }

        string directory = Path.GetDirectoryName(originalMeshPath);
        string meshName = Path.GetFileNameWithoutExtension(originalMeshPath);
        string meshesFolder = Path.Combine(directory, meshName + "_Submeshes");

        // Create the folder if it doesn't exist
        if (!AssetDatabase.IsValidFolder(meshesFolder))
        {
            string guid = AssetDatabase.CreateFolder(directory, meshName + "_Submeshes");
            if (string.IsNullOrEmpty(guid))
            {
                EditorUtility.DisplayDialog("Split Mesh", "Failed to create folder for submesh assets.", "OK");
                return;
            }
        }

        // Parent transform
        Transform parentTransform = original.transform;

        // Disable the original GameObject's renderer
        meshRenderer.enabled = false;

        for (int i = 0; i < subMeshCount; i++)
        {
            // Create a new GameObject for each submesh
            GameObject child = new GameObject(original.name + "_Submesh_" + i);
            child.transform.parent = parentTransform;
            child.transform.localPosition = Vector3.zero;
            child.transform.localRotation = Quaternion.identity;
            child.transform.localScale = Vector3.one;

            // Add MeshFilter and MeshRenderer
            MeshFilter childMeshFilter = child.AddComponent<MeshFilter>();
            MeshRenderer childMeshRenderer = child.AddComponent<MeshRenderer>();

            // Create a new mesh for the submesh
            Mesh subMesh = new Mesh();
            subMesh.name = originalMesh.name + "_Submesh_" + i;

            // Get the triangles for this submesh
            int[] triangles = originalMesh.GetTriangles(i);
            subMesh.vertices = originalMesh.vertices;
            subMesh.normals = originalMesh.normals;
            subMesh.uv = originalMesh.uv;
            subMesh.uv2 = originalMesh.uv2;
            subMesh.uv3 = originalMesh.uv3;
            subMesh.uv4 = originalMesh.uv4;
            subMesh.tangents = originalMesh.tangents;
            subMesh.colors = originalMesh.colors;

            subMesh.SetTriangles(triangles, 0);

            // Optionally, recalculate bounds and normals
            subMesh.RecalculateBounds();
            subMesh.RecalculateNormals();

            // Assign the submesh to the MeshFilter
            childMeshFilter.mesh = subMesh;

            // Assign the corresponding material
            childMeshRenderer.sharedMaterial = materials[i];

            // Save the submesh as an asset
            string subMeshAssetName = subMesh.name + ".asset";
            string subMeshPath = Path.Combine(meshesFolder, subMeshAssetName);
            subMeshPath = AssetDatabase.GenerateUniqueAssetPath(subMeshPath); // Ensure unique path

            AssetDatabase.CreateAsset(subMesh, subMeshPath);
            AssetDatabase.ImportAsset(subMeshPath);
        }

        // Refresh the AssetDatabase to recognize new assets
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("Split Mesh", $"Mesh has been split into {subMeshCount} submeshes and saved as assets.", "OK");
    }

    // Validate that the menu item is only active when a suitable GameObject is selected
    [MenuItem(MenuName, true)]
    private static bool SplitMeshBySubmeshes_Validate()
    {
        // Ensure exactly one GameObject is selected
        if (Selection.gameObjects.Length != 1)
            return false;

        GameObject selected = Selection.activeGameObject;
        if (selected == null)
            return false;

        MeshFilter meshFilter = selected.GetComponent<MeshFilter>();
        MeshRenderer meshRenderer = selected.GetComponent<MeshRenderer>();

        if (meshFilter == null || meshRenderer == null)
            return false;

        Mesh mesh = meshFilter.sharedMesh;
        if (mesh == null || mesh.subMeshCount <= 1)
            return false;

        // Check if there are enough materials
        Material[] materials = meshRenderer.sharedMaterials;
        return materials.Length >= mesh.subMeshCount;
    }
}

