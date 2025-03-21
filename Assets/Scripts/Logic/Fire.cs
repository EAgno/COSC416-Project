using UnityEngine;

public class Fire : MonoBehaviour
{


    private void OnTriggerEnter2D(Collider2D other)
    {

        if (other.CompareTag("Breakable"))
        {
            var breakableComponent = other.GetComponent<Breakable>();
            if (breakableComponent != null)
            {
                breakableComponent.DestroyBlock();
            }
        }
    }
}