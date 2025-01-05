using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;
using System.Threading;
using System.Collections;
using UnityEditor.Search;

public enum ChunkState
{
    QueuedForLoad,
    Loaded,
    QueuedForUnload,
    Unloaded
}

public struct TileData
{
    public Vector3Int position;
    public RuleTile tile;

    public TileData(Vector3Int position, RuleTile tile)
    {
        this.position = position;
        this.tile = tile;
    }
}

public struct Chunk
{
    public Vector2Int Position;
    public ChunkState State;
    public TileData[] TileSet;

    public Chunk(Vector2Int position, ChunkState state, TileData[] tileSet = null)
    {
        Position = position;
        State = state;
        TileSet = tileSet;
    }
}


public class ChunkManager : MonoBehaviour
{
    public Tilemap tilemap; // Reference to the Tilemap
    public RuleTile forestTile;
    public RuleTile desertTile;
    public RuleTile mountainTile;

    public int chunkSize = 64; // Size of each chunk
    public int renderDistance = 3; // Number of chunks to load around the player

    private Dictionary<Vector2Int, Chunk> chunks = new Dictionary<Vector2Int, Chunk>();

    private object queueLock = new object();
    private Vector2Int previousPlayerChunk = Vector2Int.zero;
    private bool isRunning = true;

    void Start()
    {
        // Start the chunk processing coroutine
        isRunning = true;
        StartCoroutine(ProcessChunks());
    }

    void OnDestroy()
    {
        // Stop threads when the object is destroyed
        isRunning = false;
    }

    void Update()
    {
        Vector2Int playerChunk = GetPlayerChunk();

        if (playerChunk != previousPlayerChunk)
        {
            LoadChunksAround(playerChunk);
            UnloadChunks(playerChunk);
            previousPlayerChunk = playerChunk; // Update the previous chunk
        }
    }

    IEnumerator ProcessChunks()
    {
        while (isRunning)
        {
            List<Vector2Int> chunksToUnload = new List<Vector2Int>();
            List<Vector2Int> chunksToLoad = new List<Vector2Int>();

            lock (queueLock)
            {
                foreach (var kvp in chunks)
                {
                    Vector2Int chunkCoord = kvp.Key;
                    Chunk chunk = kvp.Value;

                    if (chunk.State == ChunkState.QueuedForUnload)
                    {
                        chunksToUnload.Add(chunkCoord); // Collect chunks to unload
                    }
                    else if (chunk.State == ChunkState.QueuedForLoad && chunk.TileSet != null)
                    {
                        chunksToLoad.Add(chunkCoord); // Collect chunks to load
                    }
                }

                // Process unloading
                foreach (var chunkCoord in chunksToUnload)
                {
                    Chunk chunk = chunks[chunkCoord];
                    ClearChunk(chunk);
                    //chunk.State = ChunkState.Unloaded;
                    chunks.Remove(chunkCoord);
                    //Debug.Log($"Chunk {chunkCoord} unloaded.");
                }

                // Process loading
                foreach (var chunkCoord in chunksToLoad)
                {
                    Chunk chunk = chunks[chunkCoord];
                    ApplyChunk(chunk);
                    chunk.State = ChunkState.Loaded;
                    chunks[chunkCoord] = chunk;
                    //Debug.Log($"Chunk {chunkCoord} loaded.");
                }
            }

            yield return null; // Spread updates across frames
        }
    }

    Vector2Int GetPlayerChunk()
    {
        Vector3 playerPosition = Camera.main.transform.position;
        int chunkX = Mathf.FloorToInt(playerPosition.x / chunkSize);
        int chunkY = Mathf.FloorToInt(playerPosition.y / chunkSize);
        return new Vector2Int(chunkX, chunkY);
    }

    void LoadChunksAround(Vector2Int centerChunk)
    {
        for (int x = -renderDistance; x <= renderDistance; x++)
        {
            for (int y = -renderDistance; y <= renderDistance; y++)
            {
                Vector2Int chunkCoord = new Vector2Int(centerChunk.x + x, centerChunk.y + y);

                lock (queueLock)
                {
                    if (chunks.ContainsKey(chunkCoord))
                    {
                        Chunk chunk = chunks[chunkCoord];

                        // Transition from QueuedForUnload to QueuedForLoad
                        if (chunk.State == ChunkState.QueuedForUnload)
                        {
                            chunk.State = ChunkState.QueuedForLoad;
                            chunks[chunkCoord] = chunk;
                            //Debug.Log($"Chunk {chunkCoord} transitioned from QueuedForUnload to QueuedForLoad.");
                        }
                        // Skip chunks that are already loaded or queued for loading
                        //else if (chunk.State == ChunkState.Loaded || chunk.State == ChunkState.QueuedForLoad)
                        //{
                        //    continue;
                        //}
                    }
                    else
                    {
                        // Add a new chunk if it doesn't exist
                        Chunk newChunk = new Chunk(chunkCoord, ChunkState.QueuedForLoad);
                        chunks[chunkCoord] = newChunk;

                        ThreadPool.QueueUserWorkItem(GenerateChunkData, chunkCoord);
                    }
                }
            }
        }
    }

    void GenerateChunkData(object state)
    {
        Vector2Int chunkCoord = (Vector2Int)state;

        int chunkStartX = chunkCoord.x * chunkSize;
        int chunkStartY = chunkCoord.y * chunkSize;

        TileData[] tileData = new TileData[chunkSize * chunkSize];

        for (int x = 0; x < chunkSize; x++)
        {
            for (int y = 0; y < chunkSize; y++)
            {
                int worldX = chunkStartX + x;
                int worldY = chunkStartY + y;

                float noiseValue = Mathf.PerlinNoise(worldX * 0.01f, worldY * 0.01f);
                RuleTile selectedTile = noiseValue > 0.6f ? forestTile :
                                        noiseValue > 0.3f ? desertTile : mountainTile;

                tileData[y * chunkSize + x] = new TileData(new Vector3Int(worldX, worldY, 0), selectedTile);
            }
        }

        lock (queueLock)
        {
            if (chunks.ContainsKey(chunkCoord))
            {
                Chunk updatedChunk = chunks[chunkCoord];
                updatedChunk.TileSet = tileData;
                chunks[chunkCoord] = updatedChunk;
            }
        }
    }

    void UnloadChunks(Vector2Int centerChunk)
    {
        List<Vector2Int> chunksToUnload = new List<Vector2Int>();

        // Collect chunks to modify
        lock (queueLock)
        {
            foreach (var kvp in chunks)
            {
                Vector2Int chunkCoord = kvp.Key;
                Chunk chunk = kvp.Value;

                int distanceX = Mathf.Abs(chunkCoord.x - centerChunk.x);
                int distanceY = Mathf.Abs(chunkCoord.y - centerChunk.y);

                // Mark chunks for unloading only if they are loaded
                if ((distanceX > renderDistance || distanceY > renderDistance) && chunk.State == ChunkState.Loaded)
                {
                    chunksToUnload.Add(chunkCoord);
                }
            }

            // Modify the collected chunks
            foreach (var chunkCoord in chunksToUnload)
            {
                Chunk chunk = chunks[chunkCoord];
                chunk.State = ChunkState.QueuedForUnload;
                chunks[chunkCoord] = chunk;
                //Debug.Log($"Chunk {chunkCoord} marked for unloading.");
            }
        }
    }


    void ApplyChunk(Chunk chunk)
    {
        foreach (var data in chunk.TileSet)
        {
            tilemap.SetTile(data.position, data.tile);
        }
        //Debug.Log($"Chunk {chunk.Position} applied.");
    }

    void ClearChunk(Chunk chunk)
    {
        int chunkStartX = chunk.Position.x * chunkSize;
        int chunkStartY = chunk.Position.y * chunkSize;

        for (int x = 0; x < chunkSize; x++)
        {
            for (int y = 0; y < chunkSize; y++)
            {
                tilemap.SetTile(new Vector3Int(chunkStartX + x, chunkStartY + y, 0), null);
            }
        }
        //Debug.Log($"Chunk {chunk.Position} cleared.");
    }

    void OnDrawGizmos()
    {        
        foreach (var chunk in chunks.Values)
        {
            Vector3 chunkCenter = new Vector3(chunk.Position.x * chunkSize + chunkSize / 2, chunk.Position.y * chunkSize + chunkSize / 2, 0);
            
            switch(chunk.State)
            {
                case ChunkState.QueuedForLoad:
                    Gizmos.color = Color.green;
                    break;
                case ChunkState.Loaded:
                    Gizmos.color = Color.blue;
                    break;
                case ChunkState.QueuedForUnload:
                    Gizmos.color = Color.yellow;
                    break;
                case ChunkState.Unloaded:
                    Gizmos.color = Color.red;
                    break;
                default:
                    Gizmos.color = Color.black;
                    break;
            }
            Gizmos.DrawWireCube(chunkCenter, new Vector3(chunkSize, chunkSize, 0));
        }
    }
}
