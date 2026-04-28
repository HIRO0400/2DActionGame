
using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class Player : MonoBehaviour
{
    // ======================
    // 移動
    // ======================
    [Header("移動")]
    public float moveSpeed = 5f;
    public float airControl = 0.6f;

    private Vector2 moveInput;

    // ======================
    // ジャンプ
    // ======================
    [Header("ジャンプ")]
    public float jumpForce = 4f;
    public float jumpHoldForce = 6f;
    public float maxJumpTime = 0.3f;

    private bool jumpPressed;
    private bool jumpHeld;
    private bool isJumping;
    private float jumpTimeCounter;

    // ======================
    // クローン
    // ======================
    [Header("クローン")]
    public GameObject playerPrefab;
    public int maxClones = 3;

    private GameObject[] clones;
    private int currentClones;
    private InputAction cloneAction;

    // ======================
    // 無敵
    // ======================
    private bool isInvincible;
    public float blinkInterval = 0.05f;

    // ======================
    // 落下
    // ======================
    [Header("落下")]
    public float fallMultiplier = 2.5f;
    public float lowJumpMultiplier = 2f;

    // ======================
    // 接地
    // ======================
    [Header("接地")]
    public Transform footCheck;
    public float checkRadius = 0.2f;
    public LayerMask footLayer;

    private bool isGrounded;
    private Vector3 lastGroundPosition;

    // ======================
    // コンポーネント
    // ======================
    private Rigidbody2D rb;
    private SpriteRenderer sr;
    private PlayerInput playerInput;

    private InputAction moveAction;
    private InputAction jumpAction;

    // ======================
    // 初期化
    // ======================
    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponentInChildren<SpriteRenderer>();
        playerInput = GetComponent<PlayerInput>();

        moveAction = playerInput.actions["Move"];
        jumpAction = playerInput.actions["Jump"];
        cloneAction = playerInput.actions["Clone"];
    }

    void Start()
    {
        clones = new GameObject[maxClones];
        lastGroundPosition = transform.position;
    }

    // ======================
    // メインループ
    // ======================
    void Update()
    {
        ReadInput();
        CheckGround();
        HandleJump();
        HandleFlip();

        if (cloneAction.WasPressedThisFrame())
        {
            CreateClone();
        }
    }

    void FixedUpdate()
    {
        Move();
        ApplyGravity();
    }

    // ======================
    // 入力
    // ======================
    void ReadInput()
    {
        Vector2 input = moveAction.ReadValue<Vector2>();
        moveInput.x = Mathf.Abs(input.x) > 0.1f ? input.x : 0;

        if (jumpAction.WasPressedThisFrame())
        {
            jumpPressed = true;
            jumpHeld = true;
        }

        if (jumpAction.WasReleasedThisFrame())
        {
            jumpHeld = false;
        }
    }

    // ======================
    // 移動
    // ======================
    void Move()
    {
        float multiplier = isGrounded ? 1f : airControl;

        Vector2 v = rb.linearVelocity;
        v.x = moveInput.x * moveSpeed * multiplier;
        rb.linearVelocity = v;
    }

    void HandleFlip()
    {
        if (moveInput.x > 0) sr.flipX = false;
        else if (moveInput.x < 0) sr.flipX = true;
    }

    // ======================
    // ジャンプ
    // ======================
    void HandleJump()
    {
        if (jumpPressed && isGrounded)
        {
            isJumping = true;
            jumpTimeCounter = maxJumpTime;

            Vector2 v = rb.linearVelocity;
            v.y = jumpForce;
            rb.linearVelocity = v;

            jumpPressed = false;
        }

        if (jumpHeld && isJumping)
        {
            if (jumpTimeCounter > 0)
            {
                Vector2 v = rb.linearVelocity;
                v.y = jumpHoldForce;
                rb.linearVelocity = v;

                jumpTimeCounter -= Time.deltaTime;
            }
            else
            {
                isJumping = false;
            }
        }

        if (!jumpHeld) isJumping = false;
    }

    // ======================
    // 重力
    // ======================
    void ApplyGravity()
    {
        if (rb.linearVelocity.y < 0)
        {
            rb.linearVelocity += Vector2.up * Physics2D.gravity.y * (fallMultiplier - 1) * Time.deltaTime;
        }
        else if (rb.linearVelocity.y > 0 && !jumpHeld)
        {
            rb.linearVelocity += Vector2.up * Physics2D.gravity.y * (lowJumpMultiplier - 1) * Time.deltaTime;
        }
    }

    // ======================
    // 接地
    // ======================
    void CheckGround()
    {
        Collider2D hit = Physics2D.OverlapCircle(
            footCheck.position,
            checkRadius,
            footLayer
        );


        isGrounded = hit != null;

        if (isGrounded && hit.gameObject.layer == LayerMask.NameToLayer("Ground"))
        {
            lastGroundPosition = transform.position;
        }
    }

    // ======================
    // クローン
    // ======================
    void CreateClone()
{
    int index = currentClones % maxClones;

    if (clones[index] != null)
    {
        Destroy(clones[index]);
    }

    GameObject clone = Instantiate(playerPrefab, transform.position, Quaternion.identity);
    float direction = sr.flipX ? -1f : 1f;

// 進行方向に1.0fずらす
    clone.transform.position = new Vector2(
    transform.position.x + direction * 1.0f,
    transform.position.y
);
    clones[index] = clone;
    currentClones++;

    Respawn();
    }

    void Respawn()
    {
        transform.position = new Vector2(lastGroundPosition.x, transform.position.y);
        rb.linearVelocity = Vector2.zero;
    }
    IEnumerator RestoreCollision(){
    yield return new WaitForSeconds(0.1f);

    Physics2D.IgnoreLayerCollision(
        LayerMask.NameToLayer("Player"),
        LayerMask.NameToLayer("Clone"),
        false
    );
    }

    // ======================
    // トゲ判定
    // ======================
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.layer == LayerMask.NameToLayer("Spike"))
        {
            Die();
        }
    }

    public void Die()
    {
        if (isInvincible) return;

        CreateClone();
        StartCoroutine(InvincibleRoutine());
    }

    IEnumerator InvincibleRoutine()
    {
        isInvincible = true;

        float t = 0f;

        while (t < 1f)
        {
            Color c = sr.color;
            c.a = (c.a == 1f) ? 0.2f : 1f;
            sr.color = c;

            yield return new WaitForSeconds(blinkInterval);
            t += blinkInterval;
        }

        sr.color = Color.white;
        isInvincible = false;
    }
    public void Bounce(float force)
    {
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, force);
    }
}