using UnityEngine;
using UnityEngine.Events;

public class InputManager : MonoBehaviour
{
    public UnityEvent<Vector2> OnMove = new();
    public UnityEvent OnAttackPressed = new(); // Renamed from OnAttack (key first pressed)
    public UnityEvent OnAttackHeld = new();    // New event for continuous attack (key held)
    public UnityEvent OnAttackReleased = new(); // Optional: for when attack button is released

    public Vector2 GetMovementInput()
    {
        float horizontalInput = Input.GetAxisRaw("Horizontal");
        float verticalInput = Input.GetAxisRaw("Vertical");

        // Only pass vertical input if it's the up direction (jump)
        if (verticalInput < 0) verticalInput = 0;

        return new Vector2(horizontalInput, verticalInput).normalized;
    }

    // Update is called once per frame
    void Update()
    {
        Vector2 input = GetMovementInput();
        if (input != Vector2.zero)
        {
            OnMove?.Invoke(input);
        }

        // Handle different attack input states
        if (Input.GetKeyDown(KeyCode.Space))
        {
            OnAttackPressed?.Invoke();
        }

        if (Input.GetKey(KeyCode.Space)) // Check if key is being held
        {
            OnAttackHeld?.Invoke();
        }

        if (Input.GetKeyUp(KeyCode.Space)) // Optional: detect when key is released
        {
            OnAttackReleased?.Invoke();
        }
    }
}
