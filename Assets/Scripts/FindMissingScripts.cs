using UnityEngine;
using UnityEditor;

public class FindMissingScripts
{
    [MenuItem("Tools/Find Missing Scripts In Scene")]
    static void FindInScene()
    {
        GameObject[] gos = Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);

        foreach (GameObject go in gos)
        {
            int count = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(go);
            if (count > 0)
            {
                Debug.Log($"Missing scripts on: {go.name}", go);
            }
        }
    }
}