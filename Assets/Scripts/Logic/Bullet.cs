using UnityEngine;

public class Bullet : MonoBehaviour
{
    [Header("Bullet Settings")]
    [SerializeField] private GameObject _destroyEffect;
    [SerializeField] private float _destroyEffectDuration = 0.5f;
    [SerializeField] private float _playerIgnoreTime = 0.1f; // Time to ignore player collisions

    private GameObject _player;
    private float _spawnTime;

    private void Start()
    {
        _player = GameObject.FindGameObjectWithTag("Player");
        _spawnTime = Time.time;

        // Ignore collisions between bullets
        Physics2D.IgnoreLayerCollision(gameObject.layer, gameObject.layer);
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

        DestroyBullet();
    }

    private void DestroyBullet()
    {
        // Spawn the destroy effect
        if (_destroyEffect != null)
        {
            GameObject destroyEffect = Instantiate(_destroyEffect, transform.position, Quaternion.identity);
            Destroy(destroyEffect, _destroyEffectDuration);
        }

        // Destroy the bullet
        Destroy(gameObject);
    }
}
