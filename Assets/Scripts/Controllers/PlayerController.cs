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
    [SerializeField] private int jumpsRemaining;
    private bool isGrounded;
    private bool isFalling;

    [Header("Player Weapons")]
    [SerializeField] private GameObject bombPrefab;
    private enum WeaponType { None, FlameThrower, Glock17 }
    private WeaponType currentWeapon = WeaponType.None;
    [SerializeField] private int flameThrowerAmmo = 100;
    [SerializeField] private int flameThrowerAmmoPerShot = 1;
    [SerializeField] private int glock17Ammo = 30;
    [SerializeField] private int glock17AmmoPerShot = 1;
    [SerializeField] private float flamethrowerFireRate = 0.1f;
    [SerializeField] private float glock17FireRate = 0.2f;
    private float lastFireTime = 0f;

    // Track which weapons have been collected
    private bool hasFlameThrower = false;
    private bool hasGlock17 = false;

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

    [Header("Wall Jump Detection")]
    [SerializeField] private LayerMask wallLayer; // Assign your wall layers in the inspector
    [SerializeField] private float wallCheckDistance = 0.1f; // Distance to check for walls
    [SerializeField] private Vector2 wallCheckSize = new Vector2(0.02f, 0.5f); // Size of wall check box (narrow width, decent height)
    [SerializeField] private Vector2 leftWallCheckOffset = new Vector2(-0.5f, 0f); // Offset for left wall check
    [SerializeField] private Vector2 rightWallCheckOffset = new Vector2(0.5f, 0f); // Offset for right wall check
    [SerializeField] private float wallJumpForceX = 5f; // Horizontal force when wall jumping
    [SerializeField] private float wallJumpForceY = 7f; // Vertical force when wall jumping
    [SerializeField] private float wallSlideSpeed = 1.5f; // How fast player slides down walls
    private bool isOnWall = false;
    private bool isWallOnLeft = false; // To know which direction to jump

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

        // Ensure all weapons are disabled at start
        DeactivateAllWeapons();

        // Subscribe to the attack events
        inputManager.OnAttackPressed.AddListener(OnAttackPressed);
        inputManager.OnAttackHeld.AddListener(OnAttackHeld);

        animator.SetBool("IsSpawned", true);
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

        // Check for wall jump
        if (jumpButtonDown && !jumpPressed && isOnWall && !isGrounded)
        {
            WallJump();
        }

        // Update jumping/sliding animations
        animator.SetBool("IsJumping", !isGrounded);
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
                // Only switch to FlameThrower if collected and has ammo
                if (hasFlameThrower && flameThrowerAmmo > 0)
                {
                    SetFlameThrowerActive(true);
                }
                // If no FlameThrower but has Glock17 with ammo, switch to that
                else if (hasGlock17 && glock17Ammo > 0)
                {
                    SetGlock17Active(true);
                }
                // If no weapons collected or all out of ammo, stay at None
                break;

            case WeaponType.FlameThrower:
                // Only switch to Glock17 if collected and has ammo
                if (hasGlock17 && glock17Ammo > 0)
                {
                    SetGlock17Active(true);
                }
                else
                {
                    // No Glock17 or out of ammo, return to None
                    DeactivateAllWeapons();
                }
                break;

            case WeaponType.Glock17:
                // Return to None (default attacks)
                DeactivateAllWeapons();
                break;
        }
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

        // Update wall state
        isOnWall = CheckWallContact();

        // Apply wall sliding
        if (isOnWall && !isGrounded && rb.linearVelocity.y < 0)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, Mathf.Max(rb.linearVelocity.y, -wallSlideSpeed));
        }
    }


    private bool lastFacingLeft = false;  // Track the last direction faced

    private void Move()
    {
        if (rb != null)
        {
            // Only use horizontal movement
            float horizontalMovement = movement.x * moveSpeed;
            rb.linearVelocity = new Vector2(horizontalMovement, rb.linearVelocity.y);
            if (spriteRenderer != null)
            {
                // Only update direction when there is significant movement
                if (Mathf.Abs(horizontalMovement) > 0.1f)
                {
                    if (horizontalMovement > 0)
                    {
                        spriteRenderer.flipX = false;  // Face right
                        lastFacingLeft = false;
                    }
                    else if (horizontalMovement < 0)
                    {
                        spriteRenderer.flipX = true;   // Face left
                        lastFacingLeft = true;
                    }
                }

                // Always use lastFacingLeft to flip weapon sprites
                FlipWeaponSprites(lastFacingLeft);
            }
        }
    }

    private void FlipWeaponSprites(bool facingLeft)
    {
        // Handle Glock17 and its nested sprites by flipping the entire parent
        Transform glockTransform = transform.Find("Glock17");
        if (glockTransform != null && glockTransform.gameObject.activeSelf)
        {
            Vector3 scale = glockTransform.localScale;
            scale.x = facingLeft ? -Mathf.Abs(scale.x) : Mathf.Abs(scale.x);
            glockTransform.localScale = scale;

            // You may need to adjust position offset when flipping
            Vector3 position = glockTransform.localPosition;
            position.x = facingLeft ? -Mathf.Abs(position.x) : Mathf.Abs(position.x);
            glockTransform.localPosition = position;
        }

        // Do the same for FlameThrower
        Transform flameThrowerTransform = transform.Find("FlameThrower");
        if (flameThrowerTransform != null && flameThrowerTransform.gameObject.activeSelf)
        {
            Vector3 scale = flameThrowerTransform.localScale;
            scale.x = facingLeft ? -Mathf.Abs(scale.x) : Mathf.Abs(scale.x);
            flameThrowerTransform.localScale = scale;

            // You may need to adjust position offset when flipping
            Vector3 position = flameThrowerTransform.localPosition;
            position.x = facingLeft ? -Mathf.Abs(position.x) : Mathf.Abs(position.x);
            flameThrowerTransform.localPosition = position;
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

    // New method for when attack button is first pressed - all weapons use this
    private void OnAttackPressed()
    {
        // For all weapon types, we'll process the initial attack
        ProcessAttack();
    }

    // New method for when attack button is held - only automatic weapons use this
    private void OnAttackHeld()
    {
        // Only process continuous attacks for automatic weapons (not for bombs)
        if (currentWeapon != WeaponType.None)
        {
            ProcessAttack();
        }
        // For bombs (WeaponType.None), we do nothing when space is held
    }

    // Renamed from OnAttack to ProcessAttack - handles the actual attack logic
    private void ProcessAttack()
    {
        if (Time.time < lastFireTime + GetCurrentWeaponFireRate())
            return;

        // Handle different attack types based on current weapon
        switch (currentWeapon)
        {
            case WeaponType.None:
                // Use bomb attack - one bomb at a time
                UseDefaultBombAttack();
                break;

            case WeaponType.FlameThrower:
                if (flameThrowerAmmo >= flameThrowerAmmoPerShot)
                {
                    // Fire flamethrower
                    flameThrowerAmmo -= flameThrowerAmmoPerShot;
                    lastFireTime = Time.time;

                    // the FlameThrower script is attached to the FlameThrower prefab
                    GameObject flameThrowerInstance = transform.Find("FlameThrower").gameObject;
                    FlameThrower flameThrower = flameThrowerInstance.GetComponent<FlameThrower>();
                    flameThrower.Shoot();

                    // If out of ammo, switch back to default weapon
                    if (flameThrowerAmmo <= 0)
                    {
                        hasFlameThrower = false; // Mark as not collected anymore
                        DeactivateAllWeapons();
                    }
                }
                break;

            case WeaponType.Glock17:
                if (glock17Ammo >= glock17AmmoPerShot)
                {
                    // Fire glock
                    glock17Ammo -= glock17AmmoPerShot;
                    lastFireTime = Time.time;

                    // the glock script is attached to the glock prefab
                    GameObject glockInstance = transform.Find("Glock17").gameObject;
                    Glock glock = glockInstance.GetComponent<Glock>();
                    glock.Shoot();


                    // If out of ammo, switch back to default weapon
                    if (glock17Ammo <= 0)
                    {
                        hasGlock17 = false; // Mark as not collected anymore
                        DeactivateAllWeapons();
                    }
                }
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

    // Add this helper method to get the fire rate for the current weapon
    private float GetCurrentWeaponFireRate()
    {
        switch (currentWeapon)
        {
            case WeaponType.FlameThrower:
                return flamethrowerFireRate;
            case WeaponType.Glock17:
                return glock17FireRate;
            default:
                return 0f; // No cooldown for bombs
        }
    }

    // unsubscribe when the object is destroyed
    void OnDestroy()
    {
        if (inputManager != null)
        {
            inputManager.OnAttackPressed.RemoveListener(OnAttackPressed);
            inputManager.OnAttackHeld.RemoveListener(OnAttackHeld);
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

    // Modify the weapon activation methods to set the fixed ammo when picking up weapons
    public void SetFlameThrowerActive(bool isActive)
    {
        // Mark as collected
        hasFlameThrower = true;

        // Set fixed ammo amount when picking up the flamethrower
        flameThrowerAmmo = 100;

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
        // Mark as collected
        hasGlock17 = true;

        // Set fixed ammo amount when picking up the Glock17
        glock17Ammo = 30;

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

    // Add these public methods to allow adding ammo
    public bool HasFlameThrower()
    {
        return hasFlameThrower;
    }

    public bool HasGlock17()
    {
        return hasGlock17;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("DamageBreakable"))
        {
            Die();
        }
    }

    // Improved wall contact check
    private bool CheckWallContact()
    {
        // Check left wall
        Vector2 leftCheckPosition = (Vector2)transform.position + leftWallCheckOffset;
        RaycastHit2D leftHit = Physics2D.BoxCast(
            leftCheckPosition,
            wallCheckSize,
            0f,
            Vector2.left,
            wallCheckDistance,
            wallLayer
        );

        // Draw debug visualization for left check regardless of hit
        DrawWallCheck(leftCheckPosition, true, leftHit.collider != null);

        if (leftHit.collider != null)
        {
            isWallOnLeft = true;
            return true;
        }

        // Check right wall
        Vector2 rightCheckPosition = (Vector2)transform.position + rightWallCheckOffset;
        RaycastHit2D rightHit = Physics2D.BoxCast(
            rightCheckPosition,
            wallCheckSize,
            0f,
            Vector2.right,
            wallCheckDistance,
            wallLayer
        );

        // Draw debug visualization for right check regardless of hit
        DrawWallCheck(rightCheckPosition, false, rightHit.collider != null);

        if (rightHit.collider != null)
        {
            isWallOnLeft = false;
            return true;
        }

        return false;
    }

    // Improved debug visualization with better visibility and wall contact indicator
    private void DrawWallCheck(Vector2 position, bool isLeftWall, bool hasContact)
    {
#if UNITY_EDITOR
        // Use different colors based on contact state
        Color debugColor = hasContact ? Color.green : Color.red;
        Vector2 direction = isLeftWall ? Vector2.left : Vector2.right;

        // Draw the outer bounds of the box
        float halfHeight = wallCheckSize.y / 2;
        float halfWidth = wallCheckSize.x / 2;

        // Top edge
        Debug.DrawLine(
            position + new Vector2(-halfWidth, halfHeight),
            position + new Vector2(halfWidth, halfHeight),
            debugColor
        );

        // Bottom edge
        Debug.DrawLine(
            position + new Vector2(-halfWidth, -halfHeight),
            position + new Vector2(halfWidth, -halfHeight),
            debugColor
        );

        // Left edge
        Debug.DrawLine(
            position + new Vector2(-halfWidth, -halfHeight),
            position + new Vector2(-halfWidth, halfHeight),
            debugColor
        );

        // Right edge
        Debug.DrawLine(
            position + new Vector2(halfWidth, -halfHeight),
            position + new Vector2(halfWidth, halfHeight),
            debugColor
        );

        // Draw the cast ray in the middle
        Debug.DrawRay(position, direction * wallCheckDistance, debugColor);

        // Add some diagonal lines to make it more visible when contact occurs
        if (hasContact)
        {
            Debug.DrawLine(
                position + new Vector2(-halfWidth, -halfHeight),
                position + new Vector2(halfWidth, halfHeight),
                Color.yellow
            );

            Debug.DrawLine(
                position + new Vector2(halfWidth, -halfHeight),
                position + new Vector2(-halfWidth, halfHeight),
                Color.yellow
            );
        }
#endif
    }

    // Perform wall jump
    private void WallJump()
    {
        // Reset velocity
        rb.linearVelocity = Vector2.zero;

        // Apply force in opposite direction of wall
        float xForce = isWallOnLeft ? wallJumpForceX : -wallJumpForceX;
        rb.AddForce(new Vector2(xForce, wallJumpForceY), ForceMode2D.Impulse);

        // Reset jumps to max instead of just adding one
        jumpsRemaining = maxJumps;

        // Play animation if needed
        animator.SetTrigger("WallJump");
    }

}
