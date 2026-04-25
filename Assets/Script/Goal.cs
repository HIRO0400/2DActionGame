using UnityEngine;

public class Goal : MonoBehaviour
{
    private bool isGoal = false;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (isGoal) return;

        if (collision.CompareTag("Player"))
        {
            isGoal = true;

            // プレイヤー操作停止（任意）
            Player player = collision.GetComponent<Player>();
            if (player != null)
            {
                player.enabled = false;
            }

            // StageManagerに通知
            FindObjectOfType<StageManager>().OnStageClear();
        }
    }
}