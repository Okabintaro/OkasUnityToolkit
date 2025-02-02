using UnityEngine;
using UnityEditor;

public class CleanupEmptyGameObjects : EditorWindow
{
    [MenuItem("Tools/Cleanup/Remove Empty GameObjects")]
    public static void RemoveEmptyObjects()
    {
        int deleted = 0;
        GameObject[] allObjects = GameObject.FindObjectsOfType<GameObject>();
        foreach (GameObject obj in allObjects)
        {
            if (obj.transform.childCount == 0 && obj.GetComponents<Component>().Length == 1)
            {
                DestroyImmediate(obj);
                deleted++;
            }
        }
        Debug.Log($"Removed {deleted} empty GameObjects.");
    }
}
