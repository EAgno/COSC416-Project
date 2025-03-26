using UnityEngine;

public class EnemyController : MonoBehaviour
{
    [Header("Enemy Stats")]
    [SerializeField] private int health = 100;
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private int attackDamage = 1;
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
                // Jump with force proportional to the height difference (within limits)
                float adjustedJumpForce = Mathf.Clamp(jumpForce * (heightDifference / 2f), jumpForce, jumpForce * 1.5f);

                Debug.Log("Jumping to reach player. Height difference: " + heightDifference +
                          ", Jump force: " + adjustedJumpForce);

                rb.AddForce(Vector2.up * adjustedJumpForce, ForceMode2D.Impulse);
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

        // Check all hits to see if we hit an obstacle before the player
        foreach (RaycastHit2D hit in hits)
        {
            Debug.Log("CircleCast hit: " + hit.collider.gameObject.name + " with tag: " + hit.collider.tag);

            // If we hit the player first, we have line of sight
            if (hit.collider.CompareTag("Player"))
            {
                return true;
            }

            // If we hit something else first, we don't have line of sight
            if (hit.collider.gameObject != gameObject) // Ignore self
            {
                return false;
            }
        }

        // We didn't hit the player
        return false;
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

        // Add debug for movement
        Debug.Log("Moving towards player. Direction: " + direction);

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
