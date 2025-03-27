using UnityEngine;
using System.Collections;

public class EnemyController : MonoBehaviour
{
    [Header("Enemy Stats")]
    [SerializeField] private int health = 100;
    [SerializeField] private float speed = 5f;
    [SerializeField] private float attackCooldown = 1f;
    [SerializeField] private float jumpForce = 5f;
    [SerializeField] private bool isGrounded;
    [Header("Enemy Level")]
    [Range(1, 10)]
    [SerializeField] private int enemyLevel = 1; // Default to level 1
    [SerializeField] private float sizeMultiplierPerLevel = 1f; // Size increase per level

    [Header("Detection")]
    public float detectionRadius = 5f; // Detection radius for player
    public LayerMask playerLayer; // Layer mask for player detection
    public LayerMask obstacleLayer; // Layer mask for obstacles
    public bool drawDebugRays = true; // Toggle debug visualization
    private bool canSeePlayer = false;

    [Header("Testing")]
    public bool testModeEnabled = false; // Toggle for test mode
    public KeyCode testJumpKey = KeyCode.I;
    public KeyCode testLeftKey = KeyCode.J;
    public KeyCode testRightKey = KeyCode.L;
    public KeyCode testAttackKey = KeyCode.Space;

    [Header("Feedback")]
    [SerializeField] private float stunDuration = 0.5f; // Duration of stun when hit
    private Transform player;
    private float lastAttackTime;
    private SpriteRenderer spriteRenderer;
    private Animator animator;
    private Rigidbody2D rb;

    private bool isStunned = false;
    private float stunnedUntil = 0f;

    // Add a new field to track if the enemy is touching the player
    private bool isTouchingPlayer = false;
    private GameObject playerObject = null;

    [Header("Spawning")]
    [SerializeField] private bool canSpawnMinions = true;
    [SerializeField] private int maxMinionsSpawned = 5;
    [SerializeField] private float spawnCooldown = 5f;
    [SerializeField] private int minLevelToSpawn = 5;
    [SerializeField] private GameObject minionPrefab; // Assign the same enemy prefab
    private float lastSpawnTime;
    private int minionsSpawned;

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player").transform;
        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();

        // Make sure your layer masks are properly set
        if (playerLayer.value == 0)
            Debug.LogWarning("Player layer not set correctly in inspector!");
        if (obstacleLayer.value == 0)
            Debug.LogWarning("Obstacle layer not set correctly in inspector!");

        animator.SetBool("isSpawned", true);

        // Apply scaling based on enemy level
        ApplyLevelScaling();
    }

    void Update()
    {
        if (player == null) return;

        if (testModeEnabled)
        {
            HandleTestMode();
        }
        else
        {
            HandleAIMode();
        }

        UpdateAnimations();

        // Check if enemy can spawn minions
        if (canSpawnMinions && enemyLevel >= minLevelToSpawn && canSeePlayer)
        {
            TrySpawnMinions();
        }
    }

    void HandleTestMode()
    {
        // Handle horizontal movement with J and L keys
        float horizontalInput = 0f;
        if (Input.GetKey(testLeftKey))
            horizontalInput = -1f;
        else if (Input.GetKey(testRightKey))
            horizontalInput = 1f;

        rb.linearVelocity = new Vector2(horizontalInput * speed, rb.linearVelocity.y);

        // Flip sprite based on movement direction
        if (horizontalInput != 0)
        {
            spriteRenderer.flipX = horizontalInput < 0;
        }

        // Handle jumping with I key
        if (Input.GetKeyDown(testJumpKey) && isGrounded)
        {
            Jump();
        }

        // Allow manual attacking with space, but only if touching the player
        if (Input.GetKeyDown(testAttackKey) && isTouchingPlayer)
        {
            // Only attack if within range and cooldown allows
            if (Time.time - lastAttackTime >= attackCooldown)
            {
                Attack();
            }
        }
        else if (Input.GetKeyDown(testAttackKey))
        {
            // Optional: provide feedback that player is not in contact
            Debug.Log("Attack attempted but not touching player!");
        }
    }

    void HandleAIMode()
    {
        // Check if stunned
        if (isStunned)
        {
            // If stun duration is over, remove stun
            if (Time.time > stunnedUntil)
            {
                isStunned = false;
            }
            else
            {
                // If still stunned, stop movement and return
                rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
                return;
            }
        }

        // Check if player is in sight
        // Check if player is within detection radius and we have line of sight
        canSeePlayer = CheckLineOfSightToPlayer();

        // Check if we need to jump to reach the player
        CheckAndJumpTowardsPlayer();

        // Try to attack if touching player and cooldown allows
        if (isTouchingPlayer && Time.time - lastAttackTime >= attackCooldown)
        {
            Attack();
        }
        // Otherwise move towards player if we can see them
        else if (canSeePlayer)
        {
            MoveTowardsPlayer();
        }
        else
        {
            // Optional: Add idle behavior when player is not detected
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
        }
    }

    void CheckAndJumpTowardsPlayer()
    {
        // Only jump if we can see the player and are on the ground
        if (canSeePlayer && isGrounded)
        {
            // Calculate height difference between player and enemy
            float heightDifference = player.position.y - transform.position.y;

            // Only jump if the player is above us by a minimum threshold
            float minHeightToJump = 1.0f; // Adjust this value based on your game

            if (heightDifference > minHeightToJump)
            {
                // Use a more consistent jump force similar to the player
                // Instead of the dynamic calculation that might be too aggressive

                // Reset vertical velocity for consistent jumps (like player does)
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0);

                // Use a fixed jump force similar to the player's implementation
                rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
            }
        }
    }

    bool CheckLineOfSightToPlayer()
    {
        if (player == null) return false;

        // Check if player is within detection radius
        float distance = Vector3.Distance(transform.position, player.position);
        if (distance > detectionRadius) return false;

        // Use a circle cast instead of a raycast
        RaycastHit2D[] hits = Physics2D.CircleCastAll(
            transform.position,
            0.1f,  // Small radius for the circle cast
            (player.position - transform.position).normalized,
            distance
        );

        bool foundPlayer = false;
        float playerDistance = float.MaxValue;

        // First find the player and its distance
        foreach (RaycastHit2D hit in hits)
        {
            if (hit.collider.CompareTag("Player"))
            {
                foundPlayer = true;
                playerDistance = hit.distance;
                break;
            }
        }

        if (!foundPlayer) return false;

        // Check if any obstacle in our defined obstacle layer is blocking the view
        foreach (RaycastHit2D hit in hits)
        {
            // Skip self
            if (hit.collider.gameObject == gameObject)
                continue;

            // Check if this is an obstacle (using our obstacle layer)
            if (((1 << hit.collider.gameObject.layer) & obstacleLayer) != 0)
            {
                // If we hit an obstacle before the player, we can't see the player
                if (hit.distance < playerDistance)
                {
                    return false;
                }
            }
        }

        // We found the player and no obstacles are in the way
        return true;
    }

    void UpdateAnimations()
    {
        Vector2 velocity = rb.linearVelocity;

        // Check if enemy is moving horizontally
        animator.SetBool("isRunning", Mathf.Abs(velocity.x) > 0.1f);

        // Check if enemy is jumping (positive vertical velocity)
        animator.SetBool("isJumping", velocity.y > 0.1f);

        // Check if enemy is falling (negative vertical velocity)
        animator.SetBool("isFalling", velocity.y < -0.1f);
    }

    void MoveTowardsPlayer()
    {
        Vector2 direction = (player.position - transform.position).normalized;

        rb.linearVelocity = new Vector2(direction.x * speed, rb.linearVelocity.y);

        // Flip sprite based on movement direction
        if (direction.x != 0)
        {
            spriteRenderer.flipX = direction.x < 0;
        }
    }

    void Jump()
    {
        rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
    }

    // Add collision detection for player contact
    void OnCollisionEnter2D(Collision2D collision)
    {
        // Check if the collision is with the ground
        if (collision.gameObject.CompareTag("Unbreakable") || collision.gameObject.CompareTag("Breakable"))
        {
            isGrounded = true;
        }

        // Check if the collision is with the player
        if (collision.gameObject.CompareTag("Player"))
        {
            isTouchingPlayer = true;
            playerObject = collision.gameObject;

            // Try to attack immediately upon contact if cooldown allows
            if (Time.time - lastAttackTime >= attackCooldown)
            {
                Attack();
            }
        }
    }

    void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Unbreakable") || collision.gameObject.CompareTag("Breakable"))
        {
            isGrounded = false;
        }

        // Clear player contact when no longer touching
        if (collision.gameObject.CompareTag("Player"))
        {
            isTouchingPlayer = false;
        }
    }

    // Modify the Attack() method to be simpler
    void Attack()
    {
        // Only attack if we're touching the player
        if (isTouchingPlayer && playerObject != null)
        {
            // Deal damage to player
            playerObject.GetComponent<PlayerController>().Die();
            lastAttackTime = Time.time;
        }
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        // Check for fire tag to handle damage
        if (collision.gameObject.CompareTag("Fire"))
        {
            TakeDamage(10); // Adjust damage amount as needed
        }
    }

    void TakeDamage(int damage)
    {
        health -= damage;

        // Apply stun effect
        isStunned = true;
        stunnedUntil = Time.time + stunDuration;

        // Make sure we always reset the color first to handle interrupted flashes
        if (spriteRenderer != null)
        {
            spriteRenderer.color = Color.white; // Reset to default color
        }

        // Stop previous coroutines if they're running
        StopAllCoroutines(); // Stop ALL coroutines to be safe

        // Start new visual feedback
        StartCoroutine(FlashRed());

        // Set the hit animation and reset it after a delay
        if (animator != null)
        {
            animator.SetBool("isHitted", true);
            StartCoroutine(ResetHitAnimation());
        }

        // Check if enemy should die
        if (health <= 0)
        {
            Die();
        }
    }

    private IEnumerator ResetHitAnimation()
    {
        yield return new WaitForSeconds(0.1f);

        // Reset the hit state if the animator still exists
        if (animator != null)
        {
            animator.SetBool("isHitted", false);
        }
    }

    private IEnumerator FlashRed()
    {
        if (spriteRenderer == null)
            yield break;


        // Store the original color (usually white/clear)
        Color originalColor = Color.white;

        // Change to red to indicate damage
        spriteRenderer.color = Color.red;

        // Wait a short time
        yield return new WaitForSeconds(0.1f);

        // Always return to white/original color, even if coroutine was stopped early
        spriteRenderer.color = originalColor;

    }
    void Die()
    {
        // Disable collider to prevent further collisions but make sure it is fixed in place
        rb.bodyType = RigidbodyType2D.Kinematic;
        GetComponent<Collider2D>().enabled = false;

        // Add death animation or particle effects
        animator.SetBool("isDead", true);

        // Reset minion count
        minionsSpawned = 0;

        // Destroy the enemy game object after a delay
        StartCoroutine(WaitAndDestroy());

        // Could also add score increment, item drops, etc. here
    }

    IEnumerator WaitAndDestroy()
    {
        yield return new WaitForSeconds(0.5f);
        Destroy(gameObject);
    }

    // Draw testing mode status in scene view
    void OnDrawGizmos()
    {
        // Draw test mode indicator
        if (testModeEnabled && Application.isPlaying)
        {
            Gizmos.color = Color.yellow;
            UnityEditor.Handles.Label(transform.position + Vector3.up * 2, "TEST MODE");
        }

        // Draw enemy level indicator with color based on level
        if (Application.isPlaying)
        {
            // Choose color based on enemy level
            Color levelColor;
            if (enemyLevel <= 3)
                levelColor = Color.green; // Levels 1-3: Green
            else if (enemyLevel <= 6)
                levelColor = Color.yellow; // Levels 4-6: Yellow
            else if (enemyLevel <= 9)
                levelColor = Color.red; // Levels 7-9: Red
            else
                levelColor = new Color(1f, 0f, 1f); // Level 10: Purple

            Gizmos.color = levelColor;
            string levelText = "Level " + enemyLevel;
            UnityEditor.Handles.Label(transform.position + Vector3.up * 1.5f, levelText);
        }

        // Draw detection radius
        if (drawDebugRays)
        {
            // Draw detection radius
            Gizmos.color = canSeePlayer ? Color.green : Color.red;
            Gizmos.DrawWireSphere(transform.position, detectionRadius);

            // Draw line of sight ray if player exists
            if (player != null && Application.isPlaying)
            {
                Vector2 directionToPlayer = player.position - transform.position;
                float distanceToPlayer = directionToPlayer.magnitude;

                // Draw ray from enemy to player with appropriate color
                if (canSeePlayer)
                {
                    // Green = Player is visible
                    Debug.DrawLine(transform.position, player.position, Color.green);
                }
                else if (distanceToPlayer <= detectionRadius)
                {
                    // Red = Player is within radius but not visible
                    Debug.DrawLine(transform.position, player.position, Color.red);
                }

                // Add extra visual debug information
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(player.position, 0.2f);
            }
        }
    }

    void ApplyLevelScaling()
    {
        // Validate level is within range
        enemyLevel = Mathf.Clamp(enemyLevel, 1, 10);

        // Calculate scale factor based on level
        // For level 10, we want it to be 10x the size
        float scaleFactor = 1f + (enemyLevel - 1) * sizeMultiplierPerLevel;

        // Apply the scale to the enemy
        transform.localScale = new Vector3(scaleFactor, scaleFactor, 1f);

        // Set health based on level (100 per level)
        health = enemyLevel * 100;
        if(enemyLevel == 1)
        {
            health = 20;
        }
        if(enemyLevel == 10)
        {
            health = 2500;
        }

        // Adjust speed - larger enemies are slower
        // Reduce speed by 5% per level above 1 (up to 45% reduction at level 10)
        float speedReductionFactor = 1f - ((enemyLevel - 1) * 0.05f);
        speed *= speedReductionFactor;

        // Keep jump force consistent regardless of size
        // This ensures all enemies have the same jumping capability
        // No adjustment needed for jumpForce
    }

    public void SetEnemyLevel(int level)
    {
        enemyLevel = Mathf.Clamp(level, 1, 10);
        ApplyLevelScaling();
    }

    public int GetEnemyLevel()
    {
        return enemyLevel;
    }

    void TrySpawnMinions()
    {
        // Check if cooldown has passed and we haven't reached the maximum number of minions
        if (Time.time - lastSpawnTime >= spawnCooldown && minionsSpawned < maxMinionsSpawned)
        {
            SpawnMinion();
            lastSpawnTime = Time.time;
        }
    }

    void SpawnMinion()
    {
        if (minionPrefab == null)
        {
            Debug.LogWarning("Minion prefab not assigned to enemy!");
            return;
        }

        // Check if player reference exists
        if (player == null)
        {
            Debug.LogWarning("Cannot spawn minion: player reference is null");
            return;
        }

        // Determine which direction the enemy is facing (based on sprite direction)
        float facingDirection = spriteRenderer.flipX ? -1f : 1f;

        // Spawn position in front of the enemy based on their facing direction
        Vector3 spawnPos = transform.position + new Vector3(facingDirection * 1.5f, 0f, 0f);

        // Instantiate the minion
        GameObject minion = Instantiate(minionPrefab, spawnPos, Quaternion.identity);

        // Get the colliders from both objects
        Collider2D parentCollider = GetComponent<Collider2D>();
        Collider2D minionCollider = minion.GetComponent<Collider2D>();

        // Ignore collision between parent and minion
        if (parentCollider != null && minionCollider != null)
        {
            Physics2D.IgnoreCollision(parentCollider, minionCollider, true);

            // Start coroutine to enable collision after delay
            StartCoroutine(EnableCollisionAfterDelay(parentCollider, minionCollider, 1f));
        }

        // Set the minion's level to 1
        EnemyController minionController = minion.GetComponent<EnemyController>();
        if (minionController != null)
        {
            minionController.SetEnemyLevel(1);

            // Calculate exact direction from spawn position to player
            Vector2 exactDirectionToPlayer = (player.position - spawnPos).normalized;

            // Add only a small random deviation (±15 degrees)
            float randomAngle = Random.Range(-15f, 15f);
            float angleRad = randomAngle * Mathf.Deg2Rad;

            // Rotate the direction vector by the random angle
            float cosAngle = Mathf.Cos(angleRad);
            float sinAngle = Mathf.Sin(angleRad);
            Vector2 direction = new Vector2(
                exactDirectionToPlayer.x * cosAngle - exactDirectionToPlayer.y * sinAngle,
                exactDirectionToPlayer.x * sinAngle + exactDirectionToPlayer.y * cosAngle
            ).normalized;

            // Apply launch force to the minion
            Rigidbody2D minionRb = minion.GetComponent<Rigidbody2D>();
            float launchForce = 15f;
            minionRb.AddForce(direction * launchForce, ForceMode2D.Impulse);

            // Debug visualization - remove in production
            Debug.DrawRay(spawnPos, direction * 3f, Color.red, 1.0f);
        }

        // Increment the count of spawned minions
        minionsSpawned++;
    }

    // Helper method to enable collision after delay
    private IEnumerator EnableCollisionAfterDelay(Collider2D collider1, Collider2D collider2, float delay)
    {
        yield return new WaitForSeconds(delay);

        // Check if both objects still exist before enabling collision
        if (collider1 != null && collider2 != null)
        {
            Physics2D.IgnoreCollision(collider1, collider2, false);
        }
    }
    // Track destroyed minions
    void OnDestroy()
    {
        // If this is a parent enemy (level >= minLevelToSpawn), nothing to do
        // If this is a minion, notify the parent enemy that it's destroyed
        if (enemyLevel == 1 && transform.parent != null)
        {
            EnemyController parentEnemy = transform.parent.GetComponent<EnemyController>();
            if (parentEnemy != null)
            {
                parentEnemy.MinionDestroyed();
            }
        }
    }

    public void MinionDestroyed()
    {
        // Decrement minion count when one is destroyed
        if (minionsSpawned > 0)
        {
            minionsSpawned--;
        }
    }
}
