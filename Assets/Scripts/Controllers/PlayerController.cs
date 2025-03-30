using Unity.VisualScripting;
using UnityEngine;
using TMPro;
using System.Collections;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;


public class PlayerController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private InputManager inputManager;
    [SerializeField] private TextMeshProUGUI livesText;
    [SerializeField] private GameObject flameThrowerFramePrefab;
    [SerializeField] private GameObject glock17FramePrefab;
    [SerializeField] private Transform currentWeaponUI; // Reference to CurrentWeaponUI
    [SerializeField] private Transform groundCheck;

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

    [Header("UI")]
    [SerializeField] private GameObject bombFrame;
    [SerializeField] private GameObject flameThrowerFrame;
    [SerializeField] private GameObject glock17Frame;
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

        // Update UI
        UpdateLivesUI();
        SetCurrentFrameUI();


        // Subscribe to the attack events
        inputManager.OnAttackPressed.AddListener(OnAttackPressed);
        inputManager.OnAttackHeld.AddListener(OnAttackHeld);
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
        // Update UI
        SetCurrentFrameUI();
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
                Debug.Log("Player used bomb attack");
                break;

            case WeaponType.FlameThrower:
                if (flameThrowerAmmo >= flameThrowerAmmoPerShot)
                {
                    // Fire flamethrower
                    flameThrowerAmmo -= flameThrowerAmmoPerShot;
                    lastFireTime = Time.time;
                    Debug.Log("Player used FlameThrower attack. Ammo remaining: " + flameThrowerAmmo);

                    // the FlameThrower script is attached to the FlameThrower prefab
                    GameObject flameThrowerInstance = transform.Find("FlameThrower").gameObject;
                    FlameThrower flameThrower = flameThrowerInstance.GetComponent<FlameThrower>();
                    flameThrower.Shoot();

                    // If out of ammo, switch back to default weapon
                    if (flameThrowerAmmo <= 0)
                    {
                        Debug.Log("FlameThrower out of ammo!");
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
                    Debug.Log("Player used Glock17 attack. Ammo remaining: " + glock17Ammo);

                    // the glock script is attached to the glock prefab
                    GameObject glockInstance = transform.Find("Glock17").gameObject;
                    Glock glock = glockInstance.GetComponent<Glock>();
                    glock.Shoot();


                    // If out of ammo, switch back to default weapon
                    if (glock17Ammo <= 0)
                    {
                        Debug.Log("Glock17 out of ammo!");
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
            SceneManager.LoadScene("LoseScreen");
        }
        else
        {
            UpdateLivesUI();
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

        SetCurrentFrameUI();

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

        SetCurrentFrameUI();

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
        SetCurrentFrameUI();

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

    public void UpdateLivesUI()
    {
        livesText.text = lives.ToString();
    }

    private void SetCurrentFrameUI()
    {
        Debug.Log("Current Weapon:" + currentWeapon);
        switch (currentWeapon)
        {
            case WeaponType.None:
                bombFrame.SetActive(true);
                flameThrowerFrame.SetActive(false);
                glock17Frame.SetActive(false);
                break;
            case WeaponType.FlameThrower:
                bombFrame.SetActive(false);
                flameThrowerFrame.SetActive(true);
                glock17Frame.SetActive(false);
                break;
            case WeaponType.Glock17:
                bombFrame.SetActive(false);
                flameThrowerFrame.SetActive(false);
                glock17Frame.SetActive(true);
                break;
        }
    }
}
