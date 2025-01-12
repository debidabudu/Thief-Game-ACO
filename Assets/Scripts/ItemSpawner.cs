using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class ItemSpawner : MonoBehaviour
{
    public GameObject[] itemPrefabs;
    public List<Rect> spawnAreas;
    public int numberOfItemsToSpawn;
    public Tilemap itemsToStealTilemap;

    private HashSet<int> spawnedPrefabIndices = new HashSet<int>();

    private void Start()
    {
        SpawnItems();
    }

    private void SpawnItems()
    {
        for (int i = 0; i < numberOfItemsToSpawn; i++)
        {
            int prefabIndex = GetUniquePrefabIndex();

            if (prefabIndex == -1)
            {
                Debug.LogWarning("All prefabs have been spawned.");
                break;
            }

            GameObject itemPrefab = itemPrefabs[prefabIndex];

            int spawnAreaIndex = GetUniqueSpawn();

            if (spawnAreaIndex == -1)
            {
                Debug.LogWarning("All spawn areas are occupied.");
                break;
            }

            Rect spawnArea = spawnAreas[spawnAreaIndex];

            Vector2 spawnPosition = new Vector2(
                Random.Range(spawnArea.xMin, spawnArea.xMax),
                Random.Range(spawnArea.yMin, spawnArea.yMax)
            );

            GameObject spawnedItem = Instantiate(itemPrefab, spawnPosition, Quaternion.identity);
            spawnedItem.transform.parent = itemsToStealTilemap.transform;
            itemsToStealTilemap.SetTile(itemsToStealTilemap.WorldToCell(spawnedItem.transform.position), null);

            spawnedPrefabIndices.Add(prefabIndex);
        }
    }

    private int GetUniquePrefabIndex()
    {
        List<int> unspawnedIndices = new List<int>();
        for (int i = 0; i < itemPrefabs.Length; i++)
        {
            if (!spawnedPrefabIndices.Contains(i))
            {
                unspawnedIndices.Add(i);
            }
        }

        if (unspawnedIndices.Count > 0)
        {
            return unspawnedIndices[Random.Range(0, unspawnedIndices.Count)];
        }

        return -1;
    }

    private int GetUniqueSpawn()
    {
        List<int> unoccupiedSpots = new List<int>();
        for (int i = 0; i < spawnAreas.Count; i++)
        {
            Rect spawnArea = spawnAreas[i];
            bool isOccupied = false;
            foreach (Vector3 childPosition in itemsToStealTilemap.cellBounds.allPositionsWithin)
            {
                if (spawnArea.Contains(childPosition))
                {
                    isOccupied = true;
                    break;
                }
            }

            if (!isOccupied)
            {
                unoccupiedSpots.Add(i);
            }
        }

        if (unoccupiedSpots.Count > 0)
        {
            return unoccupiedSpots[Random.Range(0, unoccupiedSpots.Count)];
        }
        return -1;
    }
}
