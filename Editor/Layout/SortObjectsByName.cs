using UnityEngine;
using UnityEditor;
using System.Linq;
using UnityEngine.SceneManagement;

public class SortGameObjectsByName
{
    [MenuItem("Tools/Layout/Sort Root GameObjects By Name")]
    private static void SortRootObjects()
    {
        // Get all root objects in the active scene
        var rootObjects = SceneManager.GetActiveScene().GetRootGameObjects();
        
        // Sort them by name
        var sorted = rootObjects.OrderBy(go => go.name).ToArray();
        
        // Set sibling indices according to the sorted order
        for (int i = 0; i < sorted.Length; i++)
        {
            sorted[i].transform.SetSiblingIndex(i);
        }
        
        Debug.Log("Root GameObjects have been sorted by name.");
    }
}
