using UnityEngine;
using UnityEngine.Events;


public class InputManager : MonoBehaviour
{
    public UnityEvent<Vector2> OnMove = new();
    public UnityEvent OnAttack = new();



    public Vector2 GetMovementInput()
    {
        float horizontalInput = Input.GetAxisRaw("Horizontal");
        float verticalInput = Input.GetAxisRaw("Vertical");

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
        if (Input.GetKeyDown(KeyCode.Space))
        {
            OnAttack?.Invoke();
        }
    }
}
