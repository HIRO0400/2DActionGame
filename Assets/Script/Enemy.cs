using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    #region インスペクター
    [Header("移動速度")] [SerializeField] private float speed;
    [Header("重力")] [SerializeField] private float gravity;
    [Header("画面外でも行動する")] [SerializeField] private bool nonVisibleAct;
    #endregion

    #region プライベート
    private Rigidbody2D rb = null;
    private SpriteRenderer sr = null;
    private Animator anim = null;
    private Collider2D col = null;
    private bool rightTleftF = false;
    private bool isDead = false;
    #endregion

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        anim = GetComponent<Animator>(); // 追加
        col = GetComponent<Collider2D>();
    }

    void FixedUpdate()
    {
        if (isDead) return; // 死んだら動かない

        if (sr.isVisible || nonVisibleAct)
        {
            int xVector = -1;

            if (rightTleftF)
            {
                xVector = 1;
                transform.localScale = new Vector3(-1, 1, 1);
            }
            else
            {
                transform.localScale = new Vector3(1, 1, 1);
            }

            rb.linearVelocity = new Vector2(xVector * speed, -gravity);
        }
        else
        {
            rb.Sleep();
        }
    }

    // ★ プレイヤーに踏まれた判定
    void OnCollisionEnter2D(Collision2D collision)
    {
        if (isDead) return;
        if (collision.contactCount == 0) return;

        if (collision.gameObject.CompareTag("Player"))
        {
            float y = collision.contacts[0].normal.y;

            if (y <= -0.5f)
            {
                // 上から踏まれた → 敵死亡
                Die();

                if (collision.gameObject.TryGetComponent<Player>(out var player))
                {
                    player.Bounce(10f);
                }
            }
            else
            {
                // 横 or 下 → プレイヤー死亡
                if (collision.gameObject.TryGetComponent<Player>(out var player))
                {
                    player.Die();
                }
            }
        }
    }

    void Die()
    {
        isDead = true;

        rb.linearVelocity = Vector2.zero;
        rb.gravityScale = 0;

        anim.SetTrigger("Death"); // ←ここ

        if (col != null)
        {
            col.enabled = false;
        }

        Destroy(gameObject, 1.0f);
    }

}