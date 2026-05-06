using UnityEngine;

public class Coin : MonoBehaviour
{
    public int addAmount = 1;
    
    private void OnTriggerEnter2D(Collider2D collision){
        Player player = collision.GetComponent<Player>();

        if (player != null)
        {
            player.AddCloneCapacity(addAmount);
            Destroy(gameObject);
        }
    }
}
