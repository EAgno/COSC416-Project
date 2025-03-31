using UnityEngine;

public class Glock : MonoBehaviour
{
    [Header("Glock Settings")]
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private Transform bulletSpawnPoint;
    [SerializeField] private float bulletSpeed = 15f;

    public void Shoot()
    {
        AudioManager.instance.PlaySFX("Shots");
        GameObject bullet = Instantiate(bulletPrefab, bulletSpawnPoint.position, bulletSpawnPoint.rotation);
        Rigidbody2D rb = bullet.GetComponent<Rigidbody2D>();

        // Check if the gun is flipped (facing left)
        bool isFacingLeft = transform.localScale.x < 0;

        // Apply velocity in the correct direction
        if (isFacingLeft)
        {
            rb.linearVelocity = -bulletSpawnPoint.right * bulletSpeed;
        }
        else
        {
            rb.linearVelocity = bulletSpawnPoint.right * bulletSpeed;
        }
    }
}
