using UnityEngine;
using UnityEngine.Tilemaps;

public class ProceduralTilemapGenerator : MonoBehaviour
{
    public Tilemap tilemap; // Assign your WorldTilemap here
    public RuleTile groundTile; // Assign your Rule Tile for ground
    public RuleTile rockTile;   // Assign your Rule Tile for rocks or other features

    public int mapWidth = 50;  // Width of the map
    public int mapHeight = 50; // Height of the map
    public float noiseScale = 10f; // Adjust for terrain variety

    void Start()
    {
        GenerateTilemap();
    }

    void GenerateTilemap()
    {
        for (int x = 0; x < mapWidth; x++)
        {
            for (int y = 0; y < mapHeight; y++)
            {
                // Use Perlin noise to determine the type of tile
                float noiseValue = Mathf.PerlinNoise(x / noiseScale, y / noiseScale);

                if (noiseValue > 0.7f) // Higher threshold = rarer tile (e.g., rocks)
                {
                    tilemap.SetTile(new Vector3Int(x, y, 0), rockTile);
                }
                else
                {
                    tilemap.SetTile(new Vector3Int(x, y, 0), groundTile);
                }
            }
        }
    }
}
