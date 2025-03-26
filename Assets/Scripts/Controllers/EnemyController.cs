using UnityEngine;
using System.Collections;

public class EnemyController : MonoBehaviour
{
    [Header("Enemy Stats")]
    [SerializeField] private int health = 100;
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private float speed = 5f;
    [SerializeField] private float attackRange = 1f;
    [SerializeField] private float attackCooldown = 1f;
    [SerializeField] private float jumpForce = 5f;
    [SerializeField] private bool isGrounded;
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

    [SerializeField] private Transform player;
    [SerializeField] private float lastAttackTime;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Animator animator;
    [SerializeField] private Rigidbody2D rb;

    private bool isFlashing = false;

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

        // Allow manual attacking with space
        if (Input.GetKeyDown(testAttackKey))
        {
            if (Time.time - lastAttackTime >= attackCooldown)
            {
                Attack();
            }
        }
    }

    void HandleAIMode()
    {
        // Check if player is in sight
        float distance = Vector3.Distance(transform.position, player.position);

        // Check if player is within detection radius and we have line of sight
        canSeePlayer = CheckLineOfSightToPlayer();

        // Check if we need to jump to reach the player (instead of using time interval)
        CheckAndJumpTowardsPlayer();

        if (canSeePlayer)
        {
            if (distance <= attackRange)
            {
                if (Time.time - lastAttackTime >= attackCooldown)
                {
                    Attack();
                }
            }
            else
            {
                MoveTowardsPlayer();
            }
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

    void OnCollisionEnter2D(Collision2D collision)
    {
        // Check if the collision is with the ground (you might want to use tags or layers)
        if (collision.gameObject.CompareTag("Unbreakable") || collision.gameObject.CompareTag("Breakable"))
        {
            isGrounded = true;
        }
    }

    void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Unbreakable") || collision.gameObject.CompareTag("Breakable"))
        {
            isGrounded = false;
        }
    }

    void Attack()
    {
        // Deal damage to player
        player.GetComponent<PlayerController>().Die();

        lastAttackTime = Time.time;
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

        // Visual feedback - only start a new flash if we're not already flashing
        if (!isFlashing)
        {
            StartCoroutine(FlashRed());

            // Set the hit animation and reset it after a delay
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
        // Wait for the hit animation to play (adjust time to match your animation length)
        yield return new WaitForSeconds(0.5f); // Example: 0.5 seconds

        // Reset the hit state
        animator.SetBool("isHitted", false);
    }

    private IEnumerator FlashRed()
    {
        if (spriteRenderer == null)
            yield break;

        isFlashing = true;

        // Store the original color
        Color originalColor = spriteRenderer.color;

        // Change to red to indicate damage
        spriteRenderer.color = Color.red;

        // Wait a short time
        yield return new WaitForSeconds(0.1f);

        // Return to original color
        spriteRenderer.color = originalColor;

        isFlashing = false;
    }

    void Die()
    {
        // Disable collider to prevent further collisions but make sure it is fixed in place
        rb.bodyType = RigidbodyType2D.Kinematic;
        GetComponent<Collider2D>().enabled = false;

        // Add death animation or particle effects
        animator.SetBool("isDead", true);

        // Destroy the enemy game object after a delay
        StartCoroutine(WaitAndDestroy());

        // Could also add score increment, item drops, etc. here
    }

    IEnumerator WaitAndDestroy()
    {
        yield return new WaitForSeconds(1.0f);
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
}
