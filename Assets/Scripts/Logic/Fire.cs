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

    [Header("Item Settings")]
    [SerializeField] private GameObject[] itemPrefabs;
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
        if (other.CompareTag("Breakable") || other.CompareTag("NoDrops") || other.CompareTag("30Chance"))
        {
            Tilemap tilemap = other.GetComponent<Tilemap>();
            if (tilemap != null)
            {
                // Use multiple points to better detect collision with tilemap
                Vector3 hitPosition = transform.position;
                Vector3Int cellPosition = tilemap.WorldToCell(hitPosition);

                // If nothing found at center position, check a small radius around fire
                if (!tilemap.HasTile(cellPosition))
                {
                    // Check nearby positions in a small box pattern
                    for (int x = -1; x <= 1; x++)
                    {
                        for (int y = -1; y <= 1; y++)
                        {
                            Vector3Int checkPosition = cellPosition + new Vector3Int(x, y, 0);
                            if (tilemap.HasTile(checkPosition))
                            {
                                cellPosition = checkPosition;
                                hitPosition = tilemap.GetCellCenterWorld(cellPosition);
                                break;
                            }
                        }
                    }
                }

                // Only clear the tile if it exists at that position
                if (tilemap.HasTile(cellPosition))
                {
                    tilemap.SetTile(cellPosition, null);

                    // Different spawn logic based on tag
                    if (other.CompareTag("Breakable"))
                    {
                        // 100% chance to drop for "Breakable" tag
                        SpawnDestroyEffect(hitPosition, destroyEffectDuration, true);
                    }
                    else if (other.CompareTag("30Chance"))
                    {
                        // 30% chance to drop for "30Chance" tag
                        bool shouldDrop = Random.Range(0f, 1f) <= 0.3f;
                        SpawnDestroyEffect(hitPosition, destroyEffectDuration, shouldDrop);
                    }
                    else if (other.CompareTag("100Chance"))
                    {
                        SpawnDestroyEffect(hitPosition, destroyEffectDuration, true);
                    }
                    else
                    {
                        // No drops for other tags (e.g., "NoDrops")
                        SpawnDestroyEffect(hitPosition, destroyEffectDuration, false);
                    }
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
                playerComponent.Die();
            }
        }
    }

    public void SpawnDestroyEffect(Vector2 position, float duration, bool drop)
    {
        if (_destroyEffect != null)
        {
            GameObject destroyEffect = Instantiate(_destroyEffect, position, Quaternion.identity);
            Destroy(destroyEffect, duration);
            if (drop)
            {
                SpawnItem(position);
            }
        }
    }

    public void SpawnItem(Vector2 position)
    {
        // Early exit if no item prefabs are defined
        if (itemPrefabs == null || itemPrefabs.Length == 0)
        {
            return;
        }

        // Filter out null prefabs
        var validItems = System.Array.FindAll(itemPrefabs, item => item != null);

        if (validItems.Length == 0)
        {
            return;
        }

        // Select a random item from the valid items
        int randomIndex = Random.Range(0, validItems.Length);
        GameObject selectedItem = validItems[randomIndex];

        // Spawn the selected item
        GameObject spawnedItem = Instantiate(selectedItem, position, Quaternion.identity);
    }
}