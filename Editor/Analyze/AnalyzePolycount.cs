using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class ScenePolycountWindow : EditorWindow
{
    private Vector2 _scrollPosition;
    
    private class MeshPolyInfo
    {
        public Mesh mesh;
        public string meshName;
        public string assetPath;
        public int usageCount;
        public int trianglesPerMesh;
        public int totalTriangles;
    }

    private List<MeshPolyInfo> _meshPolyInfoList = new List<MeshPolyInfo>();
    private int totalMeshRenderers = 0;
    private int totalMeshes = 0;

    [MenuItem("Tools/Analysis/Scene Polycount")]
    public static void ShowWindow()
    {
        var window = GetWindow<ScenePolycountWindow>("Scene Polycount");
        window.RefreshData();
        window.Show();
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Scene Polycount Analyzer", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Sort by Usage Count", GUILayout.Width(150)))
            _meshPolyInfoList.Sort((a, b) => b.usageCount.CompareTo(a.usageCount));
        if (GUILayout.Button("Sort by Triangles/Mesh", GUILayout.Width(150)))
            _meshPolyInfoList.Sort((a, b) => b.trianglesPerMesh.CompareTo(a.trianglesPerMesh));
        if (GUILayout.Button("Sort by Total Triangles", GUILayout.Width(150)))
            _meshPolyInfoList.Sort((a, b) => b.totalTriangles.CompareTo(a.totalTriangles));
        EditorGUILayout.EndHorizontal();

        if (GUILayout.Button("Refresh Data", GUILayout.Height(30)))
            RefreshData();

        EditorGUILayout.Space();
        
        _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Mesh Name", EditorStyles.boldLabel, GUILayout.MinWidth(100));
        EditorGUILayout.LabelField("Asset Path", EditorStyles.boldLabel, GUILayout.MinWidth(200));
        EditorGUILayout.LabelField("Usage Count", EditorStyles.boldLabel, GUILayout.Width(80));
        EditorGUILayout.LabelField("Triangles/Mesh", EditorStyles.boldLabel, GUILayout.Width(100));
        EditorGUILayout.LabelField("Total Triangles", EditorStyles.boldLabel, GUILayout.Width(100));
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Space();

        int grandTotal = 0;

        foreach (var info in _meshPolyInfoList)
        {
            grandTotal += info.totalTriangles;
            EditorGUILayout.BeginHorizontal("box");

            var rect = EditorGUILayout.GetControlRect(false, GUILayout.MinWidth(100));
            if (GUI.Button(rect, info.meshName, GUI.skin.label))
            {
                if (!string.IsNullOrEmpty(info.assetPath) && info.assetPath != "[Runtime Mesh]")
                {
                    var obj = AssetDatabase.LoadAssetAtPath<Object>(info.assetPath);
                    if (obj != null) EditorGUIUtility.PingObject(obj);
                }
            }

            EditorGUILayout.LabelField(info.assetPath, GUILayout.MinWidth(200));
            EditorGUILayout.LabelField(FormatNumber(info.usageCount), GUILayout.Width(80));
            EditorGUILayout.LabelField(FormatNumber(info.trianglesPerMesh), GUILayout.Width(100));
            EditorGUILayout.LabelField(FormatNumber(info.totalTriangles), GUILayout.Width(100));
            
            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.EndScrollView();
        EditorGUILayout.Space();
        EditorGUILayout.LabelField($"Grand Total Triangles: {FormatNumber(grandTotal)}", EditorStyles.boldLabel);
        EditorGUILayout.LabelField($"Total Mesh Renderers: {totalMeshRenderers}", EditorStyles.boldLabel);
        EditorGUILayout.LabelField($"Total Meshes: {totalMeshes}", EditorStyles.boldLabel);
    }

    private void RefreshData()
    {
        _meshPolyInfoList.Clear();
        totalMeshRenderers = 0;
        totalMeshes = 0;

        var meshRenderers = FindObjectsOfType<MeshRenderer>();
        var skinnedMeshRenderers = FindObjectsOfType<SkinnedMeshRenderer>();
        var meshUsageDict = new Dictionary<string, MeshPolyInfo>();

        foreach (var mr in meshRenderers)
        {
            var mf = mr.GetComponent<MeshFilter>();
            if (mf && mf.sharedMesh) {
                ProcessMesh(mf.sharedMesh, meshUsageDict);
                totalMeshes++;
            }
            totalMeshRenderers++;
        }
        foreach (var smr in skinnedMeshRenderers)
        {
            if (smr.sharedMesh) {
                ProcessMesh(smr.sharedMesh, meshUsageDict);
                totalMeshes++;
            }
            totalMeshRenderers++;
        }

        _meshPolyInfoList.AddRange(meshUsageDict.Values);
        Repaint();
    }

    private void ProcessMesh(Mesh mesh, Dictionary<string, MeshPolyInfo> dict)
    {
        string path = AssetDatabase.GetAssetPath(mesh);
        if (string.IsNullOrEmpty(path))
            path = "[Runtime Mesh]";

        if (!dict.ContainsKey(path))
        {
            dict[path] = new MeshPolyInfo
            {
                mesh = mesh,
                meshName = mesh.name,
                assetPath = path,
                usageCount = 0,
                trianglesPerMesh = mesh.triangles.Length / 3
            };
        }

        dict[path].usageCount++;
        dict[path].totalTriangles = dict[path].trianglesPerMesh * dict[path].usageCount;
    }

    private string FormatNumber(int number)
    {
        if (number >= 1_000_000)
            return (number / 1_000_000f).ToString("0.00") + "M";
        if (number >= 1_000)
            return (number / 1_000f).ToString("0.00") + "K";
        return number.ToString("N0");
    }
}
