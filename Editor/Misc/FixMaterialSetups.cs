// Project dependent script to fix materials
// TODO: Make more general, removing hardcoded paths and shader names

#if false
using UnityEngine;
using UnityEditor;
using System.IO;

public class FixMaterialSetups : Editor
{
    [MenuItem("Tools/Fix Material Setups")]
    public static void FixMaterials()
    {
        // 1. Find all materials in "Assets/Library/Materials"
        //    If you want to search the entire project, remove the second array parameter below.
        string[] materialGuids = AssetDatabase.FindAssets("t:Material", new string[] { "Assets/Library/Materials" });
        
        int fixedCount = 0;
        
        foreach (var matGuid in materialGuids)
        {
            string matPath = AssetDatabase.GUIDToAssetPath(matGuid);
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (mat == null)
                continue;

            // 2. Only proceed if this material uses the Standard Shader
            if (mat.shader == null || mat.shader.name != "Standard")
                continue;

            // 3. Check if there's a Normal map assigned
            if (mat.HasProperty("_BumpMap"))
            {
                Texture normalMap = mat.GetTexture("_BumpMap");
                if (normalMap == null)
                    continue; // no normal assigned, skip

                // 4. Check if the BaseColor (_MainTex) is missing
                if (mat.HasProperty("_MainTex") && mat.GetTexture("_MainTex") == null)
                {
                    // We’ll attempt to locate a texture named "<normalNameReplaced>_BaseColor"
                    // in the *same folder* as the Normal map
                    string normalMapPath = AssetDatabase.GetAssetPath(normalMap);
                    string normalMapName = Path.GetFileNameWithoutExtension(normalMapPath);

                    // Make sure the normal texture name contains "_Normal"
                    // so we can attempt to replace it
                    if (normalMapName.Contains("_Normal"))
                    {
                        // Replace "_Normal" with "_BaseColor" to guess the matching name
                        string baseName = normalMapName.Replace("_Normal", "_BaseColor");

                        // Construct the path that we expect for the BaseColor texture
                        string directory = Path.GetDirectoryName(normalMapPath);
                        string extension = Path.GetExtension(normalMapPath);
                        string baseColorPath = Path.Combine(directory, baseName + extension);

                        // 5. Attempt to load that texture
                        Texture2D baseColorTex = AssetDatabase.LoadAssetAtPath<Texture2D>(baseColorPath);
                        
                        if (baseColorTex != null)
                        {
                            // 6. Assign and mark material as dirty to save changes
                            mat.SetTexture("_MainTex", baseColorTex);
                            EditorUtility.SetDirty(mat);
                            fixedCount++;
                        }
                        else
                        {
                            // If the file doesn't exist or can't be loaded, log a warning
                            Debug.LogWarning($"[FixMaterialSetups] No BaseColor texture found for " +
                                             $"material '{mat.name}' at expected path '{baseColorPath}'");
                        }
                    }
                }
            }
        }
        
        // 7. Save all asset changes
        AssetDatabase.SaveAssets();
        Debug.Log($"[FixMaterialSetups] Fixed {fixedCount} materials.");
    }
}

#endif