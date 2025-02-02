using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

public class MeshRendererAnalyzerWindow : EditorWindow
{
    // A scroll position for the entire window
    private Vector2 _scrollPosition;

    // The optional GameObject for partial analysis
    private GameObject targetGameObject;

    // Data structure to hold info about MeshRenderers that have multiple materials
    private class MeshRendererInfo
    {
        public MeshRenderer Renderer;
        public int MaterialCount;
        public string HierarchyPath;
        public List<Material> Materials;
    }

    // Data structure to hold groups of materials
    private class MaterialGroup
    {
        public int GroupID;
        public List<Material> Materials;
        public string Description; // e.g., "Identical Shader and Textures"
    }

    // Data structure to hold usage info
    private class MaterialUsage
    {
        public Material Material;
        public int UsageCount;
    }

    // Internal data lists
    private List<MeshRendererInfo> meshRenderersWithMultipleMaterials = new List<MeshRendererInfo>();
    private List<MaterialGroup> materialGroups = new List<MaterialGroup>();
    private List<MaterialUsage> mostUsedMaterials = new List<MaterialUsage>();

    // Tracking for unique group IDs
    private int materialGroupCounter = 1;

    [MenuItem("Tools/Analysis/Mesh Renderer Analyzer")]
    public static void ShowWindow()
    {
        var window = GetWindow<MeshRendererAnalyzerWindow>("Mesh Renderer Analyzer");
        // Refresh data immediately when the window is shown
        window.RefreshData();
        window.Show();
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Mesh Renderer Analyzer", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        // Optional target GameObject
        targetGameObject = (GameObject)EditorGUILayout.ObjectField(
            "Target GameObject (optional)",
            targetGameObject,
            typeof(GameObject),
            true
        );

        EditorGUILayout.Space();

        // Top row of buttons for sorting / refreshing
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Sort by Material Count", GUILayout.Width(180)))
        {
            meshRenderersWithMultipleMaterials = meshRenderersWithMultipleMaterials
                .OrderByDescending(m => m.MaterialCount)
                .ToList();
        }
        if (GUILayout.Button("Sort Most Used Materials", GUILayout.Width(180)))
        {
            mostUsedMaterials = mostUsedMaterials
                .OrderByDescending(mu => mu.UsageCount)
                .ToList();
        }
        EditorGUILayout.EndHorizontal();

        // Refresh data button
        if (GUILayout.Button("Refresh Data", GUILayout.Height(30)))
        {
            RefreshData();
        }

        EditorGUILayout.Space();

        // Begin main scroll
        _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

        // ----------------------------------
        // Section 1: MeshRenderers with multiple materials
        // ----------------------------------
        EditorGUILayout.LabelField("MeshRenderers with Multiple Materials", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        if (meshRenderersWithMultipleMaterials.Count == 0)
        {
            EditorGUILayout.HelpBox("No MeshRenderers with multiple materials found.", MessageType.Info);
        }
        else
        {
            // Table header
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Hierarchy Path", EditorStyles.boldLabel, GUILayout.MinWidth(200));
            EditorGUILayout.LabelField("Material Count", EditorStyles.boldLabel, GUILayout.Width(100));
            EditorGUILayout.LabelField("Materials", EditorStyles.boldLabel, GUILayout.MinWidth(200));
            EditorGUILayout.EndHorizontal();

            // Entries
            foreach (var info in meshRenderersWithMultipleMaterials)
            {
                EditorGUILayout.BeginHorizontal("box");

                // Clickable path to ping the object
                Rect rect = EditorGUILayout.GetControlRect(false, GUILayout.MinWidth(200));
                if (GUI.Button(rect, info.HierarchyPath, GUI.skin.label))
                {
                    if (info.Renderer != null)
                    {
                        EditorGUIUtility.PingObject(info.Renderer.gameObject);
                    }
                }

                EditorGUILayout.LabelField(info.MaterialCount.ToString(), GUILayout.Width(100));

                // Display the material names
                if (info.Materials != null && info.Materials.Count > 0)
                {
                    EditorGUILayout.BeginVertical();
                    foreach (var mat in info.Materials)
                    {
                        if (mat != null)
                        {
                            if (GUILayout.Button(mat.name, EditorStyles.miniButton))
                            {
                                EditorGUIUtility.PingObject(mat);
                            }
                        }
                        else
                        {
                            EditorGUILayout.LabelField("<none>");
                        }
                    }
                    EditorGUILayout.EndVertical();
                }

                EditorGUILayout.EndHorizontal();
            }
        }

        EditorGUILayout.Space();
        EditorGUILayout.Space();

        // ----------------------------------
        // Section 2: Suggested Material Groups for merging
        // ----------------------------------
        EditorGUILayout.LabelField("Suggested Material Groups for Merging", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        if (materialGroups.Count == 0)
        {
            EditorGUILayout.HelpBox("No Material Groups identified for merging.", MessageType.Info);
        }
        else
        {
            // Table header
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Group ID", EditorStyles.boldLabel, GUILayout.Width(80));
            EditorGUILayout.LabelField("Description", EditorStyles.boldLabel, GUILayout.MinWidth(100));
            EditorGUILayout.LabelField("Materials", EditorStyles.boldLabel, GUILayout.MinWidth(200));
            EditorGUILayout.EndHorizontal();

            // Entries
            foreach (var group in materialGroups)
            {
                EditorGUILayout.BeginHorizontal("box");

                EditorGUILayout.LabelField($"Group {group.GroupID}", GUILayout.Width(80));
                EditorGUILayout.LabelField(group.Description, GUILayout.MinWidth(100));

                // List the materials
                EditorGUILayout.BeginVertical();
                foreach (var mat in group.Materials)
                {
                    if (mat != null)
                    {
                        if (GUILayout.Button(mat.name, EditorStyles.miniButton))
                        {
                            EditorGUIUtility.PingObject(mat);
                        }
                    }
                    else
                    {
                        EditorGUILayout.LabelField("<none>");
                    }
                }
                EditorGUILayout.EndVertical();

                EditorGUILayout.EndHorizontal();
            }
        }

        EditorGUILayout.Space();
        EditorGUILayout.Space();

        // ----------------------------------
        // Section 3: Most Used Materials
        // ----------------------------------
        EditorGUILayout.LabelField("Most Used Materials", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        if (mostUsedMaterials.Count == 0)
        {
            EditorGUILayout.HelpBox("No materials found in the scene.", MessageType.Info);
        }
        else
        {
            // Table header
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Material Name", EditorStyles.boldLabel, GUILayout.MinWidth(200));
            EditorGUILayout.LabelField("Usage Count", EditorStyles.boldLabel, GUILayout.Width(100));
            EditorGUILayout.EndHorizontal();

            // Entries
            foreach (var usage in mostUsedMaterials)
            {
                EditorGUILayout.BeginHorizontal("box");
                if (usage.Material != null)
                {
                    if (GUILayout.Button(usage.Material.name, GUI.skin.label, GUILayout.MinWidth(200)))
                    {
                        EditorGUIUtility.PingObject(usage.Material);
                    }
                }
                else
                {
                    EditorGUILayout.LabelField("<none>", GUILayout.MinWidth(200));
                }
                EditorGUILayout.LabelField(usage.UsageCount.ToString(), GUILayout.Width(100));
                EditorGUILayout.EndHorizontal();
            }
        }

        EditorGUILayout.EndScrollView(); // End main scroll
    }

    /// <summary>
    /// RefreshData is the main entry point for analyzing MeshRenderers.
    /// If targetGameObject is null, it will analyze the entire scene.
    /// Otherwise, it analyzes only targetGameObject and its children.
    /// </summary>
    private void RefreshData()
    {
        meshRenderersWithMultipleMaterials.Clear();
        materialGroups.Clear();
        mostUsedMaterials.Clear();
        materialGroupCounter = 1;

        // Get all MeshRenderers from the scene or from the targetGameObject
        MeshRenderer[] allMeshRenderers;
        if (targetGameObject == null)
        {
            // Analyze all MeshRenderers in the scene
            allMeshRenderers = FindObjectsOfType<MeshRenderer>();
        }
        else
        {
            // Analyze only from the targetGameObject down
            allMeshRenderers = targetGameObject.GetComponentsInChildren<MeshRenderer>();
        }

        // Track usage for each Material
        Dictionary<Material, int> materialUsageDict = new Dictionary<Material, int>();

        // Collect data about which MeshRenderers have multiple materials
        foreach (var renderer in allMeshRenderers)
        {
            Material[] materials = renderer.sharedMaterials;

            // Update usage
            foreach (var mat in materials)
            {
                if (mat == null) continue;
                if (materialUsageDict.ContainsKey(mat))
                    materialUsageDict[mat]++;
                else
                    materialUsageDict[mat] = 1;
            }

            // Check how many materials
            if (materials.Length > 1)
            {
                var info = new MeshRendererInfo
                {
                    Renderer = renderer,
                    MaterialCount = materials.Length,
                    HierarchyPath = GetHierarchyPath(renderer.gameObject),
                    Materials = materials.Where(m => m != null).ToList()
                };
                meshRenderersWithMultipleMaterials.Add(info);
            }
        }

        // Sort that list by descending number of materials
        meshRenderersWithMultipleMaterials = meshRenderersWithMultipleMaterials
            .OrderByDescending(i => i.MaterialCount)
            .ToList();

        // Analyze materials for merging
        AnalyzeMaterialsForMerging(meshRenderersWithMultipleMaterials);

        // Analyze most used materials
        AnalyzeMostUsedMaterials(materialUsageDict);

        // Force the editor window to repaint
        Repaint();
    }

    private void AnalyzeMaterialsForMerging(List<MeshRendererInfo> meshInfos)
    {
        // Gather all unique materials
        List<Material> allMaterials = meshInfos
            .SelectMany(info => info.Materials)
            .Where(mat => mat != null)
            .Distinct()
            .ToList();

        // Group by some basic properties
        var grouped = allMaterials
            .GroupBy(mat => new
            {
                ShaderName = mat.shader.name,
                MainTexture = mat.mainTexture != null ? mat.mainTexture.name : "None",
            })
            .Where(g => g.Count() > 1) // Only interested in duplicates
            .ToList();

        foreach (var group in grouped)
        {
            MaterialGroup matGroup = new MaterialGroup
            {
                GroupID = materialGroupCounter++,
                Materials = group.ToList(),
                Description = $"Shader: {group.Key.ShaderName}, Main Texture: {group.Key.MainTexture}"
            };
            materialGroups.Add(matGroup);
        }
    }

    private void AnalyzeMostUsedMaterials(Dictionary<Material, int> usageDict)
    {
        mostUsedMaterials = usageDict
            .Select(kvp => new MaterialUsage { Material = kvp.Key, UsageCount = kvp.Value })
            .OrderByDescending(mu => mu.UsageCount)
            .ToList();
    }

    // Simple hierarchy path builder
    private string GetHierarchyPath(GameObject obj)
    {
        string path = obj.name;
        Transform current = obj.transform.parent;
        while (current != null)
        {
            path = current.name + "/" + path;
            current = current.parent;
        }
        return path;
    }
}
