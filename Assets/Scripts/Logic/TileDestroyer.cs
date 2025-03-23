using UnityEngine;
using UnityEngine.Tilemaps;

public class TileDestroyer : MonoBehaviour
{
    public Tilemap tilemap;  // Reference to the Tilemap

    void DestroyTile(Vector3 worldPosition)
    {
        Vector3Int cellPosition = tilemap.WorldToCell(worldPosition); // Convert world position to tilemap cell
        tilemap.SetTile(cellPosition, null); // Remove the tile at that position
    }
}
