using Unity.VisualScripting;
using UnityEngine;
using System.Collections;

public class PlayerController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private InputManager inputManager;

    [Header("Movement Parameters")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float jumpForce = 5f;  // Add this line
    private bool isGrounded;  // Add this line

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

    private Rigidbody2D rb;
    private Vector2 movement;


    private Animator animator;

    public int getLives()
    {
        return this.lives;
    }

    public void setLives(int lives)
    {
        this.lives = lives;
    }

    public int getExplosionPower()
    {
        return this.explosionPower;
    }

    public void setExplosionPower(int explosionPower)
    {
        this.explosionPower = explosionPower;
    }

    public int getBombAttacks()
    {
        return this.bombAttacks;
    }

    public void setBombAttacks(int bombAttacks)
    {
        this.bombAttacks = bombAttacks;
    }

    public bool IsInvulnerable()
    {
        return this.isInvulnerable;
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


        // Subscribe to the OnAttack event
        inputManager.OnAttack.AddListener(OnAttack);
    }

    void Update()
    {
        // Get input
        movement = inputManager.GetMovementInput();


        animator.SetFloat("Speed", Mathf.Abs(movement.x));  // Set Speed parameter

        // Handle jump input
        if (isGrounded && movement.y > 0)
        {
            Jump();
        }


        // Update jumping animation
        animator.SetBool("IsJumping", !isGrounded);
    }

    // this line actually moves the player in the FixedUpdate method
    void FixedUpdate()
    {
        Move();
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
        rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
        isGrounded = false;
    }

    // Add ground detection
    private void OnCollisionEnter2D(Collision2D collision)
    {
        // Check if there are any contact points
        if (collision.contacts.Length > 0)
        {
            // Check if the collision is below the player
            if (collision.contacts[0].normal.y > 0.7f)
            {
                isGrounded = true;
            }
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        // Check if there are any contact points
        if (collision.contacts.Length > 0)
        {
            // Check if we're leaving ground contact
            if (collision.contacts[0].normal.y > 0.7f)
            {
                isGrounded = false;
            }
        }
        else
        {
            // If no contact points, assume we're leaving the ground
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
            // Game Over
            Debug.Log("Game Over");
        }
        else
        {
            // give the player invulnerability for a short duration
            StartCoroutine(InvulnerabilityCoroutine());
        }
    }
}
