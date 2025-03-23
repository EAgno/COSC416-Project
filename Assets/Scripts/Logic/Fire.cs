using UnityEngine;
using UnityEngine.Tilemaps;

public class Fire : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Breakable") && other.TryGetComponent<TilemapCollider2D>(out var tilemapCollider))
        {
            Tilemap tilemap = tilemapCollider.GetComponent<Tilemap>();

            if (tilemap != null)
            {
                // Use the fire's position directly
                Vector3Int cellPosition = tilemap.WorldToCell(transform.position);

                if (tilemap.HasTile(cellPosition))
                {
                    tilemap.SetTile(cellPosition, null);
                }
            }
        }

        // Keep your existing player collision logic
        if (other.CompareTag("Player"))
        {
            var playerComponent = other.GetComponent<PlayerController>();
            if (playerComponent != null)
            {
                playerComponent.Die();
            }
        }
    }
}