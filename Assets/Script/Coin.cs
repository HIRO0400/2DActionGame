using UnityEngine;

public class Coin : MonoBehaviour
{
    public int addAmount = 1;
    public AudioSource audioSource;
    public AudioClip coinSound;
    private void OnTriggerEnter2D(Collider2D collision){
        Player player = collision.GetComponent<Player>();

        if (player != null)
        {
            audioSource.PlayOneShot(coinSound);
            player.AddCloneCapacity(addAmount);
            Destroy(gameObject);
        }
    }
}
