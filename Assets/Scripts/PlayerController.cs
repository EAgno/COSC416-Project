using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private InputManager inputManager;

    [Header("Movement Parameters")]
    [SerializeField] private float moveSpeed = 5f;

    private Rigidbody2D rb;
    private Vector2 movement;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();

        // Make sure InputManager is assigned
        if (inputManager == null)
        {
            Debug.LogError("InputManager is not assigned to PlayerController!");
            return;
        }

        // Subscribe to the OnAttack event
        inputManager.OnAttack.AddListener(OnAttack);
    }

    void Update()
    {
        // Get input
        movement = inputManager.GetMovementInput();
    }

    void FixedUpdate()
    {
        Move();
    }

    private void Move()
    {
        if (rb != null)
        {
            rb.linearVelocity = movement * moveSpeed;
        }
    }

    private void OnAttack()
    {
        Debug.Log("Player attacked!");
    }

    // Don't forget to unsubscribe when the object is destroyed
    void OnDestroy()
    {
        if (inputManager != null)
        {
            inputManager.OnAttack.RemoveListener(OnAttack);
        }
    }
}
