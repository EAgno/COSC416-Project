using UnityEngine;
public class InputManager : MonoBehaviour
{
    public static InputManager Instance;

    private Vector2 movementInput;
    public bool jumpPressed;
    public bool attackPressed;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Update()
    {
        // Capture movement input (WASD or Arrow keys)
        movementInput.x = Input.GetAxisRaw("Horizontal");
        movementInput.y = Input.GetAxisRaw("Vertical");

        // Capture jump input (Spacebar or Joystick button 0)
        jumpPressed = Input.GetButtonDown("Jump");

        // Capture attack input (Left Mouse Button or Joystick button 1)
        attackPressed = Input.GetButtonDown("Fire1");
    }

    public Vector2 GetMovementInput()
    {
        return movementInput.normalized;
    }
}
