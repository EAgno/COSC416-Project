using UnityEngine;

public class Bomb : MonoBehaviour
{

    private PlayerController playerController;

    [Header("Prefabs")]
    [SerializeField] private GameObject firePrefab;

    [Header("Explosion Settings")]
    [SerializeField] private float explosionDuration = 0.7f;
    private int explosionPower = 1; // Default power

    public void SetPlayerReference(PlayerController player)
    {
        playerController = player;
        explosionPower = playerController.getExplosionPower();
    }

    void Start()
    {
        // Destroy the bomb after 2 seconds
        Invoke(nameof(Explode), 2f);
    }

    void Explode()
    {
        // Destroy the bomb itself
        Destroy(gameObject);

        // Spawn fire and smoke at the bomb's position
        // if player power is 1, spawn one tile in each direction.
        if (firePrefab != null)
        {
            SpawnFire(transform.position, explosionDuration);

            Vector2[] directions = { Vector2.up, Vector2.down, Vector2.left, Vector2.right };

            foreach (var direction in directions)
            {
                SpreadFire(transform.position, direction);
            }
        }

        // give the bomb attacks back to the player (regenerate)
        if (playerController != null)
        {
            int currentBombs = playerController.getBombAttacks();
            playerController.setBombAttacks(currentBombs + 1);
        }

    }

    // spawn the fire based on position of the bomb
    void SpawnFire(Vector2 position, float explosionDuration)
    {
        GameObject fire = Instantiate(firePrefab, position, Quaternion.identity);
        Destroy(fire, explosionDuration);
    }

    // spread the fire based on position of the bomb based on power.
    void SpreadFire(Vector2 startPosition, Vector2 direction)
    {
        for (int i = 1; i <= explosionPower; i++)
        {
            Vector2 newPosition = startPosition + direction * i;

            // Check if there's an unbreakable wall at this position
            RaycastHit2D hit = Physics2D.Raycast(newPosition, Vector2.zero);
            if (hit.collider != null && hit.collider.CompareTag("Unbreakable"))
            {
                // Stop spreading fire if we hit an unbreakable wall
                break;
            }

            SpawnFire(newPosition, explosionDuration + (i * 0.2f));
        }
    }
}
