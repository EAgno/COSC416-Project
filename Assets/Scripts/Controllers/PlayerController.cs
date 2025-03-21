using Unity.VisualScripting;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private InputManager inputManager;

    [Header("Movement Parameters")]
    [SerializeField] private float moveSpeed = 5f;

    [Header("Player Weapons")]
    [SerializeField] private GameObject bombPrefab;

    [Header("Player Stats")]
    [SerializeField]
    // this acts as a "limit" for the player to place bombs
    private int bombAttacks = 3;
    [SerializeField]
    private int explosionPower = 1;

    private Rigidbody2D rb;
    private Vector2 movement;


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

    // this line actually moves the player in the FixedUpdate method
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
}
