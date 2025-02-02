using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class MetallicToMaskMapShaderSwitchWindow : EditorWindow
{
    [SerializeField] private List<Material> materials = new List<Material>();

    // You can set this in code below, or change it in the UI if you prefer
    [SerializeField] private Shader defaultShader;
    
    // The user can override the defaultShader by setting this in the UI.
    [SerializeField] private Shader targetShader;
    
    // For scrolling through a large list of Materials
    private Vector2 scrollPos;

    [MenuItem("Tools/Metallic to MaskMap (Switch Shader)")]
    public static void ShowWindow()
    {
        GetWindow<MetallicToMaskMapShaderSwitchWindow>(
            "Metallic to MaskMap (Switch Shader)");
    }

    private void OnEnable()
    {
        // If we haven't assigned a defaultShader yet, try to find one by name.
        // Change this to any Shader name that suits your project.
        if (defaultShader == null)
        {
            defaultShader = Shader.Find("Lit Variants/LibraryLit");
        }
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Drag and Drop Materials:", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        // Create a "drop area" on the GUI
        Rect dropArea = GUILayoutUtility.GetRect(0.0f, 50.0f, GUILayout.ExpandWidth(true));
        GUI.Box(dropArea, "Drop Materials Here", EditorStyles.helpBox);

        // Handle drag-and-drop
        Event evt = Event.current;
        switch (evt.type)
        {
            case EventType.DragUpdated:
            case EventType.DragPerform:
                if (!dropArea.Contains(evt.mousePosition))
                    break;

                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

                if (evt.type == EventType.DragPerform)
                {
                    DragAndDrop.AcceptDrag();
                    foreach (Object draggedObject in DragAndDrop.objectReferences)
                    {
                        if (draggedObject is Material mat && !materials.Contains(mat))
                        {
                            materials.Add(mat);
                        }
                    }
                }
                evt.Use();
                break;
        }

        EditorGUILayout.Space();

        // Show a scrollable list of Materials
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.Height(200));
        for (int i = 0; i < materials.Count; i++)
        {
            materials[i] = (Material)EditorGUILayout.ObjectField(
                materials[i], typeof(Material), false);
        }
        EditorGUILayout.EndScrollView();

        EditorGUILayout.Space();
        // Field to show/set the Default Shader (optional)
        defaultShader = (Shader)EditorGUILayout.ObjectField(
            "Default Shader", defaultShader, typeof(Shader), false);

        // Field to allow the user to override the default Shader
        targetShader = (Shader)EditorGUILayout.ObjectField(
            "Target Shader (optional)", targetShader, typeof(Shader), false);

        EditorGUILayout.Space();
        // Buttons
        if (GUILayout.Button("Clear List"))
        {
            materials.Clear();
        }

        if (GUILayout.Button("Update Materials"))
        {
            UpdateMaterials();
        }
    }

    private void UpdateMaterials()
    {
        // If the user hasn't explicitly set a targetShader, we fallback to defaultShader
        Shader chosenShader = targetShader != null ? targetShader : defaultShader;

        if (chosenShader == null)
        {
            Debug.LogWarning("No Target Shader or Default Shader assigned. Please assign a Shader that has a '_MaskMap' property.");
            return;
        }

        int updatedCount = 0;

        foreach (Material mat in materials)
        {
            if (mat == null) 
                continue;

            // Skip if the material is already using the chosen Shader
            if (mat.shader == chosenShader)
            {
                Debug.Log($"Skipping '{mat.name}' – already using '{chosenShader.name}'.");
                continue;
            }

            // 1) Retrieve the MetallicGlossMap from the old Material (if any)
            Texture metallicGloss = null;
            if (mat.HasProperty("_MetallicGlossMap"))
            {
                metallicGloss = mat.GetTexture("_MetallicGlossMap");
            }
            else
            {
                Debug.LogWarning($"Material '{mat.name}' does not have a _MetallicGlossMap property; skipping copy.");
            }

            // 2) Switch to the new shader
            mat.shader = chosenShader;

            // 3) Assign the texture to _MaskMap (if the property exists)
            if (mat.HasProperty("_MaskMap"))
            {
                mat.SetTexture("_MaskMap", metallicGloss);
                EditorUtility.SetDirty(mat);
                updatedCount++;
                Debug.Log($"'{mat.name}' switched to '{chosenShader.name}' and _MetallicGlossMap copied to _MaskMap.");
            }
            else
            {
                Debug.LogWarning($"Target shader '{chosenShader.name}' on '{mat.name}' does not have a '_MaskMap' property; skipping assignment.");
            }
        }

        // Save all changes
        AssetDatabase.SaveAssets();
        Debug.Log($"Finished updating. {updatedCount} material(s) switched to {chosenShader.name} and updated.");
    }
}
