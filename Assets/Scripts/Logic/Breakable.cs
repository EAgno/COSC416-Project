using UnityEngine;

public class Breakable : MonoBehaviour
{
    [Header("Destroy Effect")]
    [Tooltip("The effect that will be spawned when the block is destroyed")]
    [SerializeField] private GameObject _destroyEffect;
    [SerializeField] private int destroyEffectDuration = 1;

    public void DestroyBlock()
    {
        Destroy(gameObject);
        SpawnDestroyEffect(transform.position, destroyEffectDuration);
    }

    public void SpawnDestroyEffect(Vector2 position, float duration)
    {
        if (_destroyEffect != null)
        {
            GameObject destroyEffect = Instantiate(_destroyEffect, position, Quaternion.identity);
            Destroy(destroyEffect, duration);
        }
    }


}