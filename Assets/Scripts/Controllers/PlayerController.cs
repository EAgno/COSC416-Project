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
    private enum WeaponType { None, FlameThrower, Glock17 }
    private WeaponType currentWeapon = WeaponType.None;

    // Add weapon switching cooldown to prevent rapid toggling
    private float weaponSwitchCooldown = 0.2f;
    private float lastWeaponSwitchTime = 0f;

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

    private Rigidbody2D rb;
    private Vector2 movement;

    private bool jumpPressed = false;

    private Animator animator;

    [Header("Ground Detection")]
    [SerializeField] private LayerMask groundLayer; // Assign your ground layers in the inspector
    [SerializeField] private float groundCheckDistance = 0.1f; // Distance to check for ground
    [SerializeField] private Vector2 groundCheckSize = new Vector2(0.5f, 0.02f); // Size of ground check box
    [SerializeField] private Vector2 groundCheckOffset = new Vector2(0f, -0.5f); // Offset from center (adjust based on your collider)

    public float getMoveSpeed()
    {
        return moveSpeed;
    }

    public void setMoveSpeed(float moveSpeed)
    {
        this.moveSpeed = moveSpeed;
    }

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

        // Handle weapon switching with Q key
        HandleWeaponSwitching();
    }

    private void HandleWeaponSwitching()
    {
        // Check if Q key was pressed and cooldown has elapsed
        if (Input.GetKeyDown(KeyCode.Q) && Time.time > lastWeaponSwitchTime + weaponSwitchCooldown)
        {
            lastWeaponSwitchTime = Time.time;
            CycleToNextWeapon();
        }
    }

    private void CycleToNextWeapon()
    {
        // Cycle to the next weapon type
        switch (currentWeapon)
        {
            case WeaponType.None:
                // Check if FlameThrower exists as a child
                if (transform.Find("FlameThrower") != null)
                {
                    SetFlameThrowerActive(true);
                }
                // If not, check if Glock17 exists
                else if (transform.Find("Glock17") != null)
                {
                    SetGlock17Active(true);
                }
                // If no weapons exist, stay at None
                break;

            case WeaponType.FlameThrower:
                // Check if Glock17 exists as a child
                if (transform.Find("Glock17") != null)
                {
                    SetGlock17Active(true);
                }
                else
                {
                    // No Glock17, return to None
                    DeactivateAllWeapons();
                }
                break;

            case WeaponType.Glock17:
                // Return to None (default attacks)
                DeactivateAllWeapons();
                break;
        }

        // Log the current weapon for debugging
        Debug.Log("Current weapon: " + currentWeapon.ToString());
    }

    // this line actually moves the player in the FixedUpdate method
    void FixedUpdate()
    {
        Move();

        // Update ground state
        bool wasGrounded = isGrounded;
        isGrounded = CheckGrounded();

        // Reset jumps when landing
        if (isGrounded && !wasGrounded)
        {
            jumpsRemaining = maxJumps;
            animator.SetBool("IsDBJumping", false);
        }

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

    // Replace OnCollisionEnter2D and OnCollisionExit2D with this method
    private bool CheckGrounded()
    {
        // Calculate the position for the ground check
        Vector2 position = (Vector2)transform.position + groundCheckOffset;

        // Use BoxCast for more reliable ground detection
        RaycastHit2D hit = Physics2D.BoxCast(
            position,
            groundCheckSize,
            0f,
            Vector2.down,
            groundCheckDistance,
            groundLayer
        );

        // Optional: Draw debug visualization in Scene view
        DrawGroundCheck(position);

        return hit.collider != null;
    }

    // Optional debug visualization
    private void DrawGroundCheck(Vector2 position)
    {
        // Only draw in editor
#if UNITY_EDITOR
        Color debugColor = isGrounded ? Color.green : Color.red;
        Debug.DrawRay(position - new Vector2(groundCheckSize.x / 2, 0), Vector2.right * groundCheckSize.x, debugColor);
        Debug.DrawRay(position - new Vector2(groundCheckSize.x / 2, 0) + Vector2.down * groundCheckDistance, Vector2.right * groundCheckSize.x, debugColor);
        Debug.DrawRay(position - new Vector2(groundCheckSize.x / 2, 0), Vector2.down * groundCheckDistance, debugColor);
        Debug.DrawRay(position + new Vector2(groundCheckSize.x / 2, 0), Vector2.down * groundCheckDistance, debugColor);
#endif
    }

    private void OnAttack()
    {
        // Handle different attack types based on current weapon
        switch (currentWeapon)
        {
            case WeaponType.None:
                // Use bomb attack
                UseDefaultBombAttack();
                Debug.Log("Player used bomb attack");
                break;

            case WeaponType.FlameThrower:
                Debug.Log("Player used FlameThrower attack");
                break;

            case WeaponType.Glock17:
                Debug.Log("Player used Glock17 attack");
                break;
        }
    }

    private void UseDefaultBombAttack()
    {
        // if the player has bombs left, and the bomb prefab is assigned
        if (bombPrefab != null && bombAttacks > 0)
        {
            GameObject bombInstance = Instantiate(bombPrefab, transform.position, Quaternion.identity);

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

    public void SetFlameThrowerActive(bool isActive)
    {
        if (isActive)
        {
            // Deactivate any other weapon first
            DeactivateAllWeapons();

            // Set current weapon type
            currentWeapon = WeaponType.FlameThrower;
        }

        Transform flamethrowerChild = gameObject.transform.Find("FlameThrower");
        if (flamethrowerChild != null)
        {
            flamethrowerChild.gameObject.SetActive(isActive);
        }
    }

    public void SetGlock17Active(bool isActive)
    {
        if (isActive)
        {
            // Deactivate any other weapon first
            DeactivateAllWeapons();

            // Set current weapon type
            currentWeapon = WeaponType.Glock17;
        }

        Transform glockChild = gameObject.transform.Find("Glock17");
        if (glockChild != null)
        {
            glockChild.gameObject.SetActive(isActive);
        }
    }

    private void DeactivateAllWeapons()
    {
        // Deactivate FlameThrower
        Transform flamethrowerChild = gameObject.transform.Find("FlameThrower");
        if (flamethrowerChild != null)
        {
            flamethrowerChild.gameObject.SetActive(false);
        }

        // Deactivate Glock17
        Transform glockChild = gameObject.transform.Find("Glock17");
        if (glockChild != null)
        {
            glockChild.gameObject.SetActive(false);
        }

        // Reset weapon type
        currentWeapon = WeaponType.None;
    }
}
