using UnityEngine;

public class Bomb : MonoBehaviour
{

    private PlayerController playerController;

    [Header("Prefabs")]
    [SerializeField] private GameObject firePrefab;

    [Header("Explosion Settings")]
    private int explosionPower = 1;

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


    public void Explode()
    {
        // Destroy the bomb itself
        Destroy(gameObject);

        // Spawn fire and smoke at the bomb's position
        // if player power is 1, spawn one tile in each direction.
        if (firePrefab != null)
        {
            SpawnFire(transform.position);

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
    void SpawnFire(Vector2 position)
    {
        GameObject fire = Instantiate(firePrefab, position, Quaternion.identity);
    }

    // spread the fire based on position of the bomb based on power.
    void SpreadFire(Vector2 startPosition, Vector2 direction)
    {
        for (int i = 1; i <= explosionPower; i++)
        {
            Vector2 newPosition = startPosition + direction * i;

            // Use a more reliable detection method with overlap
            Collider2D[] colliders = Physics2D.OverlapCircleAll(newPosition, 0.1f);
            bool blocked = false;

            // Check all colliders at this position
            foreach (var collider in colliders)
            {
                if (collider.CompareTag("Unbreakable"))
                {
                    blocked = true;
                    break;
                }
            }

            if (blocked)
            {
                // Stop spreading fire in this direction
                break;
            }

            // Spawn fire if position is not blocked
            SpawnFire(newPosition);
        }
    }
}
