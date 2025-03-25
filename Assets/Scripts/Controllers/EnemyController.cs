using UnityEngine;

public class EnemyController : MonoBehaviour
{
    public float speed = 5f;
    public float attackRange = 1f;
    public float attackCooldown = 1f;
    public float attackDamage = 10f;
    public float jumpForce = 5f;
    private float nextJumpTime;
    private float jumpInterval = 1f;
    private bool isGrounded;
    public float detectionRadius = 5f; // Detection radius for player
    public LayerMask playerLayer; // Layer mask for player detection
    public bool drawDebugRays = true; // Toggle debug visualization


    private Transform player;
    private float lastAttackTime;
    private SpriteRenderer spriteRenderer;
    private Animator animator;
    private Rigidbody2D rb;

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player").transform;
        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
        nextJumpTime = Time.time + jumpInterval;
    }

    void Update()
    {
        if (player == null) return;

        // Check if player is in sight
        float distance = Vector3.Distance(transform.position, player.position);
        UpdateAnimations();

        // Check if it's time to jump
        if (Time.time >= nextJumpTime && isGrounded)
        {
            Jump();
            nextJumpTime = Time.time + jumpInterval;
        }

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


}
