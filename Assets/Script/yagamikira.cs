using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Killer : MonoBehaviour
{
    [SerializeField] private float speed = 8f;
    [SerializeField] private float explosionScale = 2f;

    private Rigidbody2D rb;
    private Animator anim;
    private Collider2D col;

    private Vector2 moveDir;

    private bool isExploded = false;
    private bool isFalling = false;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        col = GetComponent<Collider2D>();

        rb.gravityScale = 0;

        // 通常は物理衝突あり
        col.isTrigger = false;
    }

    public void Init(Vector3 playerPos)
    {
        moveDir = (playerPos - transform.position).normalized;
    }

    void Start()
    {
        if (moveDir == Vector2.zero)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                Init(player.transform.position);
            }
        }
    }

    void FixedUpdate()
    {
        if (isExploded || isFalling) return;

        rb.linearVelocity = moveDir * speed;

        float angle = Mathf.Atan2(moveDir.y, moveDir.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle + 180f);
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (isExploded) return;
        if (collision.contactCount == 0) return;

        // ★ プレイヤー
        if (collision.gameObject.CompareTag("Player"))
        {
            if (isFalling) return;

            float y = collision.contacts[0].normal.y;

            if (y <= -0.5f)
            {
                // 上から踏まれた → 墜落
                Fall();

                if (collision.gameObject.TryGetComponent<Player>(out var player))
                {
                    player.Bounce(12f);
                }
            }
            else
            {
                // 横・下 → 爆発
                Explode();
            }
        }

        // ★ 地面（墜落中は爆発しない）
        if (!isFalling && collision.gameObject.layer == LayerMask.NameToLayer("Ground"))
        {
            Explode();
        }
    }

    void Fall()
    {
        isFalling = true;

        rb.linearVelocity = new Vector2(rb.linearVelocity.x * 0.5f, 2f);
        rb.gravityScale = 3f;

        rb.angularVelocity = Random.Range(200f, 400f);

        // 踏まれた後は当たり判定OFF
        col.enabled = false;
    }

    void Explode()
    {
        if (isExploded) return;

        isExploded = true;

        rb.linearVelocity = Vector2.zero;
        rb.gravityScale = 0;
        rb.angularVelocity = 0;

        // 爆発用にTrigger化
        col.isTrigger = true;

        // Colliderだけ拡大（見た目はそのまま）
        if (col is BoxCollider2D box)
        {
            box.size *= explosionScale;
        }

        anim.SetTrigger("Bakuretsu");

        Destroy(gameObject, 0.5f);
    }

    void OnTriggerStay2D(Collider2D collision)
    {
        if (!isExploded) return;

        if (collision.CompareTag("Player"))
        {
            if (collision.TryGetComponent<Player>(out var player))
            {
                player.Die();
            }
        }
    }

    void Update()
    {
        // 墜落時のみ画面外で削除
        if (isFalling && transform.position.y < -10f)
        {
            Destroy(gameObject);
        }
    }
}