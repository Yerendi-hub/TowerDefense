using System.Collections.Generic;
using System.Linq;
using TowerDefense.Components;
using UnityEngine;

[CreateAssetMenu(fileName = "TileDefinitionDatabase", menuName = "TowerDefense/Tile Definition Database")]
public class TileDefinitionDatabase : ScriptableObject
{
    public List<TileDefinition> tilePrefabs = new();

    public TileDefinition GetTilePrefabById(string definitionId)
    {
        return tilePrefabs.FirstOrDefault(tile => tile.DefinitionId == definitionId);
    }

#if UNITY_EDITOR
    [ContextMenu("Find All Tile Prefabs")]
    void FindAllTilePrefabs()
    {
        string[] guids = UnityEditor.AssetDatabase.FindAssets("t:Prefab");
        tilePrefabs.Clear();
        foreach (string guid in guids)
        {
            string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
            GameObject go = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (go != null)
            {
                TileDefinition td = go.GetComponent<TileDefinition>();
                if (td != null && !tilePrefabs.Contains(td))
                {
                    tilePrefabs.Add(td);
                }
            }
        }
        UnityEditor.EditorUtility.SetDirty(this);
    }
#endif
}