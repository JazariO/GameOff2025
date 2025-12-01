using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "TreeSO", menuName = "Tree SO")]
public class TreeSO : ScriptableObject
{
    public GameObject tree;
    public Vector3[] perchesPositions;

#if UNITY_EDITOR
    [ContextMenu("Get Perches")]
    private void GetPerches()
    {
        int perchLayer = LayerMask.NameToLayer("Perch");

        GameObject prefab = PrefabUtility.LoadPrefabContents(AssetDatabase.GetAssetPath(tree));

        List<Vector3> perchList = new List<Vector3>();

        FindPerches(prefab.transform, perchLayer, perchList);

        perchesPositions = perchList.ToArray();

        PrefabUtility.UnloadPrefabContents(prefab);
    }

    private void FindPerches(Transform perchTransform, int perchLayer, List<Vector3> perchList)
    {
        if (perchTransform.gameObject.layer == perchLayer)
        {
            perchList.Add(perchTransform.localPosition);
        }

        foreach (Transform child in perchTransform)
        {
            FindPerches(child, perchLayer, perchList);
        }
    }
#endif
}
