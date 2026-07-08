using UnityEngine;

public class Coin : MonoBehaviour
{
    [SerializeField] private int addAmount = 1;
    [SerializeField] private AudioClip coinSound;
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.TryGetComponent<Player>(out var player))
        {
            return;
        }


        if (coinSound != null)
        {
            AudioManager.Instance.PlaySE(coinSound);
        }


        player.AddCloneCapacity(addAmount);

        Destroy(gameObject);
    }
}