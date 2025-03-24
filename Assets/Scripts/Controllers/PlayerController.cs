using Unity.VisualScripting;
using UnityEngine;
using System.Collections;

public class PlayerController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private InputManager inputManager;

    [Header("Movement Parameters")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float jumpForce = 5f;
    [SerializeField] private int maxJumps = 2; // 2 for double jump, 3 for triple jump
    private int jumpsRemaining;
    private bool isGrounded;
    private bool isFalling;

    [Header("Player Weapons")]
    [SerializeField] private GameObject bombPrefab;

    [Header("Player Stats")]
    [SerializeField]
    // this acts as a "limit" for the player to place bombs
    private int bombAttacks = 3;
    [SerializeField]
    private int explosionPower = 1;
    [SerializeField] private int lives = 3;

    [Header("Invulnerability Settings")]
    [SerializeField] private float invulnerabilityDuration = 2f;
    [SerializeField] private float flickerInterval = 0.1f;
    private bool isInvulnerable = false;
    private SpriteRenderer spriteRenderer;

    [Header("Ground Detection")]
    [SerializeField] private float groundCheckDistance = 0.1f;
    [SerializeField] private LayerMask groundLayer; // Assign your ground/tilemap layer in the Inspector

    private Rigidbody2D rb;
    private Vector2 movement;

    private bool jumpPressed = false;

    private Animator animator;

    public int getLives()
    {
        return lives;
    }

    public void setLives(int lives)
    {
        this.lives = lives;
    }

    public int getExplosionPower()
    {
        return explosionPower;
    }

    public void setExplosionPower(int explosionPower)
    {
        this.explosionPower = explosionPower;
    }

    public int getBombAttacks()
    {
        return bombAttacks;
    }

    public void setBombAttacks(int bombAttacks)
    {
        this.bombAttacks = bombAttacks;
    }

    public bool IsInvulnerable()
    {
        return isInvulnerable;
    }

    // Coroutine to make the player invulnerable for a short duration when hit
    private IEnumerator InvulnerabilityCoroutine()
    {
        isInvulnerable = true;
        float elapsedTime = 0f;

        Color oldColor = spriteRenderer.color;

        while (elapsedTime < invulnerabilityDuration)
        {
            // Toggle transparency between 0.3 and 1.0
            spriteRenderer.color = new Color(1f, 1f, 1f,
                spriteRenderer.color.a == 1f ? 0.3f : 1f);

            yield return new WaitForSeconds(flickerInterval);
            elapsedTime += flickerInterval;
        }

        // Reset to fully visible
        spriteRenderer.color = oldColor;
        isInvulnerable = false;
    }

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();
        jumpsRemaining = maxJumps; // Initialize jumps

        // Subscribe to the OnAttack event
        inputManager.OnAttack.AddListener(OnAttack);
    }

    void Update()
    {
        // Get input
        movement = inputManager.GetMovementInput();

        animator.SetFloat("Speed", Mathf.Abs(movement.x));  // Set Speed parameter

        // Check ground status with raycast
        CheckGroundStatus();

        // Check for new jump button press
        bool jumpButtonDown = movement.y > 0;

        // Only jump if button is pressed (not held) or first frame on ground
        if (jumpButtonDown && !jumpPressed)
        {
            Jump();
        }

        // Update jump button state
        jumpPressed = jumpButtonDown;

        // Update jumping animation
        animator.SetBool("IsJumping", !isGrounded);
    }

    private void CheckGroundStatus()
    {
        // Cast rays from slightly inside the character's collider
        Vector2 raycastOrigin = transform.position;
        raycastOrigin.y -= GetComponent<Collider2D>().bounds.extents.y;

        // Main center ray
        bool hitGround = Physics2D.Raycast(raycastOrigin, Vector2.down, groundCheckDistance, groundLayer);

        // Left and right foot rays for better detection
        bool hitGroundLeft = Physics2D.Raycast(
            new Vector2(raycastOrigin.x - 0.25f, raycastOrigin.y),
            Vector2.down, groundCheckDistance, groundLayer);

        bool hitGroundRight = Physics2D.Raycast(
            new Vector2(raycastOrigin.x + 0.25f, raycastOrigin.y),
            Vector2.down, groundCheckDistance, groundLayer);

        // Debug rays to visualize in Scene view
        Debug.DrawRay(raycastOrigin, Vector2.down * groundCheckDistance, Color.red);
        Debug.DrawRay(new Vector2(raycastOrigin.x - 0.25f, raycastOrigin.y), Vector2.down * groundCheckDistance, Color.red);
        Debug.DrawRay(new Vector2(raycastOrigin.x + 0.25f, raycastOrigin.y), Vector2.down * groundCheckDistance, Color.red);

        // We're grounded if any ray hits
        bool wasGrounded = isGrounded;
        isGrounded = hitGround || hitGroundLeft || hitGroundRight;

        // Reset jumps when landing
        if (!wasGrounded && isGrounded)
        {
            jumpsRemaining = maxJumps;
            animator.SetBool("IsDBJumping", false);
        }
    }

    // this line actually moves the player in the FixedUpdate method
    void FixedUpdate()
    {
        Move();
        CheckFallingState();
    }

    private void Move()
    {
        if (rb != null)
        {
            // Only use horizontal movement
            float horizontalMovement = movement.x * moveSpeed;
            rb.linearVelocity = new Vector2(horizontalMovement, rb.linearVelocity.y);

            if (spriteRenderer != null)
            {
                if (horizontalMovement > 0)
                {
                    spriteRenderer.flipX = false;  // Face right
                }
                else if (horizontalMovement < 0)
                {
                    spriteRenderer.flipX = true;   // Face left
                }
            }
        }
    }

    private void Jump()
    {
        // Only allow jump if we have jumps remaining
        if (jumpsRemaining > 0)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0); // Reset vertical velocity for consistent jumps
            rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
            jumpsRemaining--;

            if (!isGrounded)
            {
                animator.SetBool("IsDBJumping", true);
            }
            else
            {
                // This is a regular jump from the ground
                animator.SetBool("IsDBJumping", false);
            }

            isGrounded = false;
        }
    }

    // Rounds position to the nearest integer grid point
    // so that we can place bombs in the 1x1 grids
    Vector2 RoundToGrid(Vector2 position)
    {
        Vector2 roundedPos = new Vector2(Mathf.Round(position.x), Mathf.Round(position.y));
        return roundedPos;
    }

    private void OnAttack()
    {
        // if the player has bombs left, and the bomb prefab is assigned
        if (bombPrefab != null && bombAttacks > 0)
        {
            // Spawn the bomb at the player's position
            Vector2 roundedPosition = RoundToGrid(transform.position);
            GameObject bombInstance = Instantiate(bombPrefab, roundedPosition, Quaternion.identity);

            // Get the Bomb script from the spawned bomb
            Bomb bombScript = bombInstance.GetComponent<Bomb>();

            // Pass this PlayerController to the Bomb script
            if (bombScript != null)
            {
                bombScript.SetPlayerReference(this);
            }
            bombAttacks--;
        }
    }

    // unsubscribe when the object is destroyed
    void OnDestroy()
    {
        if (inputManager != null)
        {
            inputManager.OnAttack.RemoveListener(OnAttack);
        }
    }

    public void Die()
    {
        if (isInvulnerable) return;

        lives--;
        if (lives <= 0)
        {
            Debug.Log("Game Over");
        }
        else
        {
            StartCoroutine(InvulnerabilityCoroutine());
        }
    }

    private void CheckFallingState()
    {
        if (rb != null)
        {
            isFalling = rb.linearVelocity.y < -0.1f; // Small negative threshold to account for minor fluctuations

            // Update animator if you want to show falling animation
            if (animator != null)
            {
                animator.SetBool("IsFalling", isFalling);
            }
        }
    }
    public bool IsFalling()
    {
        return isFalling;
    }
}
