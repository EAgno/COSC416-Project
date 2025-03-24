using UnityEngine;
using UnityEngine.Tilemaps;

public class Fire : MonoBehaviour
{
    [Header("Destroy Effect")]
    [Tooltip("The effect that will be spawned when the block is destroyed")]
    [SerializeField] private GameObject _destroyEffect;
    [SerializeField] private float destroyEffectDuration = 1;

    [Header("Fire Settings")]
    [SerializeField] private float fireDuration = 0.7f; // How long the fire exists

    private void Start()
    {
        // Schedule the fire to destroy itself
        Invoke(nameof(DisableCollider), fireDuration * 0.5f); // Disable collider slightly before destroying
        Invoke(nameof(DestroyFire), fireDuration);
    }

    private void DisableCollider()
    {
        // Disable the collider before the visual effect disappears
        Collider2D collider = GetComponent<Collider2D>();
        if (collider != null)
        {
            collider.enabled = false;
        }
    }

    private void DestroyFire()
    {
        Destroy(gameObject);
    }

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
                    SpawnDestroyEffect(hitPosition, destroyEffectDuration);
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
    public void SpawnDestroyEffect(Vector2 position, float duration)
    {
        if (_destroyEffect != null)
        {
            GameObject destroyEffect = Instantiate(_destroyEffect, position, Quaternion.identity);
            Destroy(destroyEffect, duration);
        }
    }
}