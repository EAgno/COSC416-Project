using UnityEngine;

public class Bullet : MonoBehaviour
{
    [Header("Bullet Settings")]
    [SerializeField] private GameObject _destroyEffect;
    [SerializeField] private float _destroyEffectDuration = 1;
    [SerializeField] private float _playerIgnoreTime = 0.1f; // Time to ignore player collisions

    private GameObject _player;
    private float _spawnTime;

    private void Start()
    {
        _player = GameObject.FindGameObjectWithTag("Player");
        _spawnTime = Time.time;
    }

    // if it collides with anything at all,
    // destroy the bullet and spawn the destroy effect
    private void OnTriggerEnter2D(Collider2D other)
    {
        // Ignore collisions with the player for a short time after spawning
        if (other.gameObject == _player && Time.time < _spawnTime + _playerIgnoreTime)
        {
            return;
        }

        // Ignore collisions with other bullets and destroy effects
        if (other.CompareTag("Bullet") || other.CompareTag("Fire"))
        {
            return;
        }

        DestroyBullet();
    }

    private void DestroyBullet()
    {
        // Spawn the destroy effect
        if (_destroyEffect != null)
        {
            GameObject destroyEffect = Instantiate(_destroyEffect, transform.position, Quaternion.identity);
            // Tag the destroy effect so bullets don't collide with it
            destroyEffect.tag = "Fire";
            Destroy(destroyEffect, _destroyEffectDuration);
        }

        // Destroy the bullet
        Destroy(gameObject);
    }
}