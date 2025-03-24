using System;
using UnityEngine;

public class Pickable : MonoBehaviour
{
    [Header("Levitation Settings")]
    [SerializeField] private float levitationHeight = 0.2f;  // Maximum height of levitation
    [SerializeField] private float levitationSpeed = 2.0f;   // Speed of the levitation cycle

    [Header("Immunity Settings")]
    [SerializeField] private float immunityDuration = 1.0f;  // Immunity time after spawning

    private Vector3 startPosition;
    private bool isImmune = true;

    private void Start()
    {
        // Store the initial position
        startPosition = transform.position;

        // Set initial immunity
        isImmune = true;

        // Turn off immunity after the delay
        Invoke(nameof(DisableImmunity), immunityDuration);
    }

    private void DisableImmunity()
    {
        isImmune = false;
    }

    private void Update()
    {
        // Create a smooth levitation using a sine wave
        float levitationOffset = Mathf.Sin(Time.time * levitationSpeed) * levitationHeight;
        transform.position = startPosition + new Vector3(0, levitationOffset, 0);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Handle fire collisions - destroy the pickable if not immune
        if (other.gameObject.CompareTag("Fire") && !isImmune)
        {
            Destroy(gameObject);
            return;
        }

        // Check if the player has collided with the pickable object
        if (other.CompareTag("Player"))
        {
            // check what tag the script is attached to
            Debug.Log("Player picked up " + gameObject.tag);

            PlayerController player = other.GetComponent<PlayerController>();

            switch (gameObject.tag)
            {
                case "ExtraSpeed":
                    player.setMoveSpeed(player.getMoveSpeed() + 0.5f);
                    break;
                case "ExtraLife":
                    player.setLives(player.getLives() + 1);
                    break;
                case "ExtraBomb":
                    player.setBombAttacks(player.getBombAttacks() + 1);
                    break;
                case "ExtraPower":
                    player.setExplosionPower(player.getExplosionPower() + 1);
                    break;
            }

            Destroy(gameObject);
        }
    }
}