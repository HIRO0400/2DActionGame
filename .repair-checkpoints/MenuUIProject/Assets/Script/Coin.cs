using UnityEngine;

public class Coin : MonoBehaviour
{
    [SerializeField] private int addAmount = 1;
    [SerializeField] private AudioClip coinSound;
    private bool collected;
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collected || !collision.TryGetComponent<Player>(out var player))
        {
            return;
        }


        collected = true;
        if (coinSound != null && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySE(coinSound);
        }


        player.AddCloneCapacity(addAmount);

        Destroy(gameObject);
    }
}
