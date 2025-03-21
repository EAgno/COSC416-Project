using UnityEngine;

public class Fire : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        // this is the fire spawned by the bomb, so if it hits a breakable block, destroy it
        if (other.CompareTag("Breakable"))
        {
            var breakableComponent = other.GetComponent<Breakable>();
            if (breakableComponent != null)
            {
                breakableComponent.DestroyBlock();
            }
        }
        // if the fire hits the player, kill the player
        if (other.CompareTag("Player"))
        {
            var playerComponent = other.GetComponent<PlayerController>();
            if (playerComponent != null)
            {
                playerComponent.Die();
            }
        }
    }
}