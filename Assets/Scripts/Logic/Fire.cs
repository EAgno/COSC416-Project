using UnityEngine;
using UnityEngine.Tilemaps;

public class Fire : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        // Handle Tilemap collisions
        if (other.CompareTag("Breakable"))
        {
            Tilemap tilemap = other.GetComponent<Tilemap>();
            if (tilemap != null)
            {
                // Get the contact point from the collision
                Vector3 hitPosition = other.ClosestPoint(transform.position);
                Vector3Int cellPosition = tilemap.WorldToCell(hitPosition);

                // Only clear the tile if it exists at that position
                if (tilemap.HasTile(cellPosition))
                {
                    tilemap.SetTile(cellPosition, null);
                }

                return; // Exit to avoid processing the entire Tilemap further
            }

            // Handle non-tilemap breakable objects
            Breakable breakable = other.GetComponent<Breakable>();
            if (breakable != null)
            {
                breakable.DestroyBlock();
            }
        }

        Debug.Log($"Fire collided with: {other.name}");
        // Handle player collision logic
        if (other.CompareTag("Player"))
        {
            var playerComponent = other.GetComponent<PlayerController>();
            if (playerComponent != null)
            {
                Debug.Log("Player hit by fire!");
                playerComponent.Die();
            }
        }
    }
}