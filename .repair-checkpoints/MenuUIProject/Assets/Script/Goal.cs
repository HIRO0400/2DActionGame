using UnityEngine;

public class Goal : MonoBehaviour
{
    private bool isGoal = false;
    [SerializeField] private StageManager stageManager;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (isGoal) return;

        if (collision.CompareTag("Player"))
        {
            isGoal = true;

            // プレイヤー操作停止（任意）
            if (collision.TryGetComponent<Player>(out var player))
            {
                player.enabled = false;
                if (player.TryGetComponent<Rigidbody2D>(out var body))
                {
                    body.linearVelocity = Vector2.zero;
                    body.simulated = false;
                }
            }

            // StageManagerに通知
            if (stageManager == null)
            {
                stageManager = FindFirstObjectByType<StageManager>();
            }

            if (stageManager != null)
            {
                stageManager.OnStageClear();
            }
        }
    }
}
