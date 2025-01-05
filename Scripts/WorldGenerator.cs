using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class WorldGenerator : MonoBehaviour
{
    public Tilemap tilemap;
    public Tile grassTile;
    public Tile rockTile;

    public int mapWidth = 100;
    public int mapHeight = 100;

    void Start()
    {
        GenerateWorld();
    }

    void GenerateWorld()
    {
        for (int x = 0; x < mapWidth; x++)
        {
            for (int y = 0; y < mapHeight; y++)
            {
                if (Random.Range(0f, 1f) > 0.7f) // Adjust threshold for density
                {
                    tilemap.SetTile(new Vector3Int(x, y, 0), rockTile);
                }
                else
                {
                    tilemap.SetTile(new Vector3Int(x, y, 0), grassTile);
                }
            }
        }
    }
}
