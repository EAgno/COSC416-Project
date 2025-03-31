using UnityEngine;
using System.Collections;

public class FlameThrower : MonoBehaviour
{
    [Header("Flame Thrower Settings")]
    [SerializeField] private GameObject _fireEffect;
    [SerializeField] private float _fireDuration = 2f;
    [SerializeField] private float _fireSpeed = 5f;
    [SerializeField] private float _fireSpawnOffset = 1f;
    [SerializeField] private int _fireDirections = 4; // Number of directions to spawn fire
    [SerializeField] private float _spreadWidth = 3f; // Width of the fire spread in blocks
    [SerializeField] private int _spreadLength = 3; // Length of the fire spread in blocks
    [SerializeField] private float _staggerDelay = 0.1f; // Delay between fire spawns

    private PlayerController _playerController;
    private bool _isFacingLeft = false;

    private void Start()
    {
        _playerController = GetComponentInParent<PlayerController>();
    }

    public void Shoot()
    {
        AudioManager.instance.PlaySFX("FireSpray");
        // Get player's facing direction
        _isFacingLeft = transform.localScale.x < 0;

        // Spawn fire in multiple directions with staggered timing
        StartCoroutine(SpawnFireInDirectionsStaggered());
    }

    private IEnumerator SpawnFireInDirectionsStaggered()
    {
        // Forward direction (based on player facing)
        Vector2 forwardDirection = _isFacingLeft ? Vector2.left : Vector2.right;

        // Create a spread with configurable width
        float spreadStep = _spreadWidth / 2f;
        Vector2 perpendicular = Vector2.Perpendicular(forwardDirection).normalized;

        // For each direction
        yield return StartCoroutine(SpawnFireLineStaggered(forwardDirection, perpendicular, spreadStep));

        // If we want 4 directions, add up, down, and backward
        if (_fireDirections >= 4)
        {
            // Small delay between different direction spreads
            yield return new WaitForSeconds(_staggerDelay * 2);

            // Up direction with spread
            StartCoroutine(SpawnFireLineStaggered(Vector2.up, Vector2.right, spreadStep));

            // Small delay between different direction spreads
            yield return new WaitForSeconds(_staggerDelay * 2);

            // Down direction with spread
            StartCoroutine(SpawnFireLineStaggered(Vector2.down, Vector2.right, spreadStep));

            // Small delay between different direction spreads
            yield return new WaitForSeconds(_staggerDelay * 2);

            // Backward direction with spread
            StartCoroutine(SpawnFireLineStaggered(-forwardDirection, perpendicular, spreadStep));
        }
    }

    private IEnumerator SpawnFireLineStaggered(Vector2 mainDirection, Vector2 perpendicularDirection, float spreadStep)
    {
        // Spawn the main line with length
        for (int i = 1; i <= _spreadLength; i++)
        {
            float distanceMultiplier = i * _fireSpawnOffset;

            // Center fire
            SpawnFire(mainDirection, distanceMultiplier);

            // Short delay between center, left, and right fires
            yield return new WaitForSeconds(_staggerDelay / 3);

            // Left side fire
            SpawnFire(mainDirection + perpendicularDirection * spreadStep, distanceMultiplier);

            // Short delay between left and right fires
            yield return new WaitForSeconds(_staggerDelay / 3);

            // Right side fire
            SpawnFire(mainDirection - perpendicularDirection * spreadStep, distanceMultiplier);

            // Small delay before the next row of fires
            yield return new WaitForSeconds(_staggerDelay / 3);
        }
    }

    private void SpawnFire(Vector2 direction, float distanceMultiplier = 1f)
    {
        // Normalize the direction to ensure consistent speed
        direction = direction.normalized;

        // Calculate spawn position
        Vector3 spawnPosition = transform.position + (Vector3)(direction * distanceMultiplier);

        // Check if there's an unbreakable object at the spawn position using a raycast
        // Use the Ground layer mask instead of Default
        int groundLayerMask = LayerMask.GetMask("Ground");
        RaycastHit2D hit = Physics2D.Raycast(transform.position, direction, distanceMultiplier, groundLayerMask);

        if (hit.collider != null)
        {
            // If we hit anything on the Ground layer, don't spawn fire
            return;
        }

        // Secondary check using OverlapCircle at the spawn position
        Collider2D[] colliders = Physics2D.OverlapCircleAll(spawnPosition, 0.4f, groundLayerMask);
        if (colliders.Length > 0)
        {
            // Don't spawn fire if it would hit a ground object
            return;
        }

        // Spawn the fire effect
        if (_fireEffect != null)
        {
            GameObject fireInstance = Instantiate(_fireEffect, spawnPosition, Quaternion.identity);

            // Tag the fire effect
            fireInstance.tag = "Fire";

            // Add velocity to the fire (if it has a Rigidbody2D)
            Rigidbody2D rb = fireInstance.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.linearVelocity = direction * _fireSpeed;
            }

            // Set rotation based on direction
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            fireInstance.transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);

            // Destroy after duration
            Destroy(fireInstance, _fireDuration);
        }
    }
}