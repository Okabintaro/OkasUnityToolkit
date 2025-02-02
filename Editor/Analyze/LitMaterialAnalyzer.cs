using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class LitMaterialAnalyzerWindow : EditorWindow
{
    // Scroll position for the materials table
    private Vector2 _scrollPosition;

    // Foldout / accordion toggle for Help
    private bool _showHelpSection = false;

    // SCORE-BASED THRESHOLDS:
    // We'll compute: score = (avgMetallic - avgRoughness).
    // If score >= threshold => ON, else OFF.
    [Header("Score-Based Thresholds (score = Metallic - Roughness)")]
    [SerializeField] private float specularHighlightsThreshold = 0f;
    [SerializeField] private float glossyReflectionsThreshold = 0f;
    [SerializeField] private float lightmappedSpecularThreshold = 0f;

    /// <summary>
    /// Info for each discovered "Lit" material in the scene
    /// </summary>
    private class MaterialData
    {
        public Material material;
        public float avgRoughness;
        public float avgMetallic;
        public bool isTextureReadable;
        public string textureError;
    }

    private List<MaterialData> _materials = new List<MaterialData>();

    // Sorting toggles
    private bool _sortRoughAscending = true;
    private bool _sortMetalAscending = true;

    [MenuItem("Tools/Analysis/Lit Material Analyzer")]
    public static void ShowWindow()
    {
        var window = GetWindow<LitMaterialAnalyzerWindow>("Lit Material Spec/Reflections Analyzer");
        window.RefreshData();
        window.Show();
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Lit Material Spec/Reflections Analyzer", EditorStyles.boldLabel);

        // --- Foldout Help/Info Section ---
        _showHelpSection = EditorGUILayout.Foldout(_showHelpSection, "Help / Info");
        if (_showHelpSection)
        {
            EditorGUILayout.HelpBox(
                "This window scans the scene for Materials using the 'Lit' shader, " +
                "reads their _MaskMap (G=Roughness, B=Metallic) to determine average values, " +
                "and shows a live preview of how each Material's Specular Highlights, " +
                "Glossy Reflections, and Lightmapped Specular would be toggled based on a " +
                "single combined score: (Metallic - Roughness).\n\n" +
                "If score >= threshold, that feature is ON (green). Otherwise, it's OFF (red).\n\n" +
                "Click 'Apply Auto Settings' to write these property values to the materials.\n" +
                "Make sure your _MaskMap is readable (Read/Write Enabled in the Import Settings)!",
                MessageType.Info
            );
        }

        EditorGUILayout.Space();

        // --- Score Threshold Sliders ---
        EditorGUILayout.LabelField("Score = Metallic - Roughness", EditorStyles.boldLabel);
        specularHighlightsThreshold = EditorGUILayout.Slider("Specular Highlights Threshold", specularHighlightsThreshold, -1f, 1f);
        glossyReflectionsThreshold = EditorGUILayout.Slider("Glossy Reflections Threshold", glossyReflectionsThreshold, -1f, 1f);
        lightmappedSpecularThreshold = EditorGUILayout.Slider("Lightmapped Spec Threshold", lightmappedSpecularThreshold, -1f, 1f);

        EditorGUILayout.Space();

        // --- Sorting & refresh/apply ---
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Sort by Roughness " + (_sortRoughAscending ? "↑" : "↓"), GUILayout.Width(160)))
        {
            _sortRoughAscending = !_sortRoughAscending;
            SortByRoughness(_sortRoughAscending);
        }

        if (GUILayout.Button("Sort by Metallic " + (_sortMetalAscending ? "↑" : "↓"), GUILayout.Width(160)))
        {
            _sortMetalAscending = !_sortMetalAscending;
            SortByMetallic(_sortMetalAscending);
        }

        if (GUILayout.Button("Sort by Low Rough / High Metal", GUILayout.Width(200)))
        {
            SortByLowRoughHighMetal();
        }
        EditorGUILayout.EndHorizontal();

        // Buttons for Refresh & Apply
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Refresh Data", GUILayout.Height(30)))
        {
            RefreshData();
        }
        if (GUILayout.Button("Apply Auto Settings", GUILayout.Height(30)))
        {
            ApplyAutoSettings();
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();

        // --- Scrollable table ---
        _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

        // Header row
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Material Name", EditorStyles.boldLabel, GUILayout.MinWidth(130));
        EditorGUILayout.LabelField("Avg Rough", EditorStyles.boldLabel, GUILayout.Width(70));
        EditorGUILayout.LabelField("Avg Metal", EditorStyles.boldLabel, GUILayout.Width(70));
        EditorGUILayout.LabelField("Score", EditorStyles.boldLabel, GUILayout.Width(50));
        EditorGUILayout.LabelField("SpecHigh", EditorStyles.boldLabel, GUILayout.Width(65));
        EditorGUILayout.LabelField("Glossy", EditorStyles.boldLabel, GUILayout.Width(50));
        EditorGUILayout.LabelField("LM Spec", EditorStyles.boldLabel, GUILayout.Width(60));
        EditorGUILayout.LabelField("Error/Notes", EditorStyles.boldLabel, GUILayout.MinWidth(100));
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Space();

        // Rows
        foreach (var md in _materials)
        {
            EditorGUILayout.BeginHorizontal("box");

            // Clickable Material name
            var rect = EditorGUILayout.GetControlRect(false, GUILayout.MinWidth(130));
            if (GUI.Button(rect, md.material.name, GUI.skin.label))
            {
                EditorGUIUtility.PingObject(md.material);
            }

            // Rough & Metal
            if (md.isTextureReadable)
            {
                EditorGUILayout.LabelField(md.avgRoughness.ToString("F3"), GUILayout.Width(70));
                EditorGUILayout.LabelField(md.avgMetallic.ToString("F3"), GUILayout.Width(70));
            }
            else
            {
                EditorGUILayout.LabelField("--", GUILayout.Width(70));
                EditorGUILayout.LabelField("--", GUILayout.Width(70));
            }

            // Calculate the combined score
            float score = md.avgMetallic - md.avgRoughness;
            EditorGUILayout.LabelField(score.ToString("F3"), GUILayout.Width(50));

            // Determine feature ON/OFF based on thresholds
            bool specHighlightsOn = (score >= specularHighlightsThreshold);
            bool glossyReflectionsOn = (score >= glossyReflectionsThreshold);
            bool lightmappedSpecOn = (score >= lightmappedSpecularThreshold);

            // Show colored "ON"/"OFF" labels
            DrawStateLabel(specHighlightsOn, 65f);
            DrawStateLabel(glossyReflectionsOn, 50f);
            DrawStateLabel(lightmappedSpecOn, 60f);

            // Error / Notes
            EditorGUILayout.LabelField(md.textureError, EditorStyles.wordWrappedLabel, GUILayout.MinWidth(100));

            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.EndScrollView();
    }

    /// <summary>
    /// Small helper to draw a label in green if on == true, red if on == false.
    /// </summary>
    private void DrawStateLabel(bool onState, float width = 60f)
    {
        var prevColor = GUI.color;
        GUI.color = onState ? Color.green : Color.red;
        EditorGUILayout.LabelField(onState ? "ON" : "OFF", GUILayout.Width(width));
        GUI.color = prevColor;
    }

    /// <summary>
    /// Refreshes the list of materials in the current scene that use the "Lit" shader.
    /// </summary>
    private void RefreshData()
    {
        _materials.Clear();

        var allRenderers = FindObjectsOfType<Renderer>();
        HashSet<Material> materialSet = new HashSet<Material>();

        foreach (var rend in allRenderers)
        {
            if (rend == null) continue;
            foreach (var mat in rend.sharedMaterials)
            {
                if (mat != null && mat.shader != null && mat.shader.name.ToLower().Contains("lit"))
                {
                    materialSet.Add(mat);
                }
            }
        }

        // Build MaterialData list
        foreach (var mat in materialSet)
        {
            var data = new MaterialData
            {
                material = mat,
                avgRoughness = 0f,
                avgMetallic = 0f,
                isTextureReadable = false,
                textureError = ""
            };

            // Read the _MaskMap
            Texture2D maskMap = mat.GetTexture("_MaskMap") as Texture2D;
            if (maskMap != null)
            {
                try
                {
                    Color[] pixels = maskMap.GetPixels(0);
                    data.isTextureReadable = true;

                    if (pixels.Length > 0)
                    {
                        float totalRough = 0f;
                        float totalMetal = 0f;
                        for (int i = 0; i < pixels.Length; i++)
                        {
                            totalRough += pixels[i].g; // G = Roughness
                            totalMetal += pixels[i].b; // B = Metallic
                        }
                        data.avgRoughness = totalRough / pixels.Length;
                        data.avgMetallic = totalMetal / pixels.Length;
                    }
                }
                catch
                {
                    data.isTextureReadable = false;
                    data.textureError = "Texture not read/write enabled.";
                }
            }
            else
            {
                data.textureError = "No _MaskMap assigned.";
                data.isTextureReadable = true;
            }

            _materials.Add(data);
        }

        // Default sort by Roughness ascending
        SortByRoughness(true);
        Repaint();
    }

    /// <summary>
    /// Applies settings to each material based on the score approach:
    ///     score = metallic - roughness.
    /// If score >= threshold => property ON, else OFF.
    /// </summary>
    private void ApplyAutoSettings()
    {
        foreach (var md in _materials)
        {
            if (md.material == null) continue;

            float rough = md.avgRoughness;
            float metal = md.avgMetallic;
            float score = metal - rough;

            // Specular Highlights => (1 = ON, 0 = OFF)
            bool specularOn = (score >= specularHighlightsThreshold);
            md.material.SetFloat("_SpecularHighlights", specularOn ? 1f : 0f);

            // Glossy Reflections => (1 = ON, 0 = OFF)
            bool glossyOn = (score >= glossyReflectionsThreshold);
            md.material.SetFloat("_GlossyReflections", glossyOn ? 1f : 0f);

            // Lightmapped Specular => (1 = ON, 0 = OFF)
            bool lightmappedOn = (score >= lightmappedSpecularThreshold);
            md.material.SetInt("_LightmappedSpecular", lightmappedOn ? 1 : 0);

            EditorUtility.SetDirty(md.material);
        }
        // Optionally: AssetDatabase.SaveAssets();
    }

    #region Sorting Methods

    private void SortByRoughness(bool ascending)
    {
        if (ascending)
            _materials.Sort((a, b) => a.avgRoughness.CompareTo(b.avgRoughness));
        else
            _materials.Sort((a, b) => b.avgRoughness.CompareTo(a.avgRoughness));
    }

    private void SortByMetallic(bool ascending)
    {
        if (ascending)
            _materials.Sort((a, b) => a.avgMetallic.CompareTo(b.avgMetallic));
        else
            _materials.Sort((a, b) => b.avgMetallic.CompareTo(a.avgMetallic));
    }

    /// <summary>
    /// Sort so that low roughness and high metallic appear first.
    /// That is, sort descending by (metallic - roughness).
    /// </summary>
    private void SortByLowRoughHighMetal()
    {
        _materials.Sort((a, b) =>
        {
            float scoreA = a.avgMetallic - a.avgRoughness;
            float scoreB = b.avgMetallic - b.avgRoughness;
            return scoreB.CompareTo(scoreA);
        });
    }

    #endregion
}
