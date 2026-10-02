using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using System.Diagnostics;

public class Player : MonoBehaviour
{
    // ======================
    // 移動
    // ======================
    [Header("移動")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float airControl = 0.6f;

    private Vector2 moveInput;

    // ======================
    // ジャンプ
    // ======================
    [Header("ジャンプ")]
    [SerializeField] private float jumpForce = 6f;
    //[SerializeField] private float jumpHoldForce = 10f;
    [SerializeField] private float maxJumpTime = 0.3f;

    private bool jumpPressed;
    private bool jumpHeld;
    private bool isJumping;
    private float jumpTimeCounter;
    private float coyoteTime = 0.1f;
    private float coyoteCounter;

    // ======================
    // クローン
    // ======================
    [Header("クローン")]
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private int maxClones = 1;

    private GameObject[] clones;
    private int currentClones;
    private InputAction cloneAction;

    private bool isRespawning;
    private bool canCloneInput = true;
    private bool isGroundLocked = false;
    private bool inputInitialized;

    // ======================
    // 無敵
    // ======================
    private bool isBlinkInvincible;
    private bool isInvincible => isRespawning || isGroundLocked || isBlinkInvincible;
    [SerializeField] private float blinkInterval = 0.05f;
    [SerializeField] private float fallDeathDistance = 20f;

    // ======================
    // 落下
    // ======================
    [Header("落下")]
    [SerializeField] private float fallMultiplier = 2.5f;
    [SerializeField] private float lowJumpMultiplier = 2f;

    // ======================
    // 接地
    // ======================
    [Header("接地")]
    [SerializeField] private Transform footCheck;
    [SerializeField] private Vector2 groundCheckSize = new Vector2(0.8f, 0.15f);
    [SerializeField] private LayerMask footLayer;

    private readonly ContactPoint2D[] wallContacts = new ContactPoint2D[32];
    private Collider2D currentGround;
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
    // オーディオ
    // ======================
    [SerializeField] private AudioClip jumpSound;
    [SerializeField] private AudioClip cloneSound;

    // ======================
    // 初期化
    // ======================
    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponentInChildren<SpriteRenderer>();
        playerInput = GetComponent<PlayerInput>();

        if (rb == null || sr == null || playerInput == null || playerInput.actions == null)
        {
            enabled = false;
            return;
        }

        if (footCheck == null ||  playerPrefab == null)
        {
            enabled = false;
            return;
        }

        moveAction = playerInput.actions["Move"];
        jumpAction = playerInput.actions["Jump"];
        cloneAction = playerInput.actions["Clone"];
    }

    void Start()
    {
        maxClones = Mathf.Max(1, maxClones);
        clones = new GameObject[maxClones];
        lastGroundPosition = transform.position;
        canCloneInput = false;
    }

    // ======================
    // Update / FixedUpdate
    // ======================
    void Update()
    {
        if (!inputInitialized)
        {
            inputInitialized = true;
            return;
        }

        CheckGround();

        if (!isRespawning && transform.position.y < lastGroundPosition.y - fallDeathDistance)
        {
            // Recover from a pit even while waiting for the respawn landing.
            if (isGroundLocked)
            {
                isGroundLocked = false;
                StartRespawn();
                StartGroundLock();
            }
            else Die();
            return;
        }

        if (isGroundLocked)
        {
            CheckGroundLanding();
            return;
        }

        if (!isRespawning && canCloneInput && !isGroundLocked)
        {
            CreateClone();
            canCloneInput = false;
        }

        if (cloneAction.WasReleasedThisFrame())
        {
            canCloneInput = true;
        }

        ReadInput();
        HandleJump();
        HandleFlip();

        jumpPressed = false;
    }

    void FixedUpdate()
    {
        Move();
        ApplyGravity();
        ResolveWallCollision();
    }

    // ======================
    // 入力
    // ======================
    void ReadInput()
    {
        if (isRespawning)
        {
            moveInput = Vector2.zero;
            return;
        }

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
        Vector2 v = rb.linearVelocity;

        float multiplier = isGrounded ? 1f : airControl;
        float inputX = moveInput.x * moveSpeed * multiplier;

        v.x = inputX;
        rb.linearVelocity = v;
    }

    void HandleFlip()
    {
        if (moveInput.x > 0) sr.flipX = false;
        else if (moveInput.x < 0) sr.flipX = true;
    }

    void ResolveWallCollision()
    {
        int count = rb.GetContacts(wallContacts);
        for (int i = 0; i < count; i++)
        {
            ContactPoint2D contact = wallContacts[i];
            if (Mathf.Abs(contact.normal.x) > 0.5f &&
                contact.normal.x * rb.linearVelocity.x < 0f)
            {
                rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
                return;
            }
        }
    }

    // ======================
    // ジャンプ
    // ======================
    void HandleJump()
    {
        if (jumpPressed && coyoteCounter > 0f){
            coyoteCounter = 0f;
            isJumping = true;
            if (AudioManager.Instance != null) AudioManager.Instance.PlaySE(jumpSound);
            jumpTimeCounter = maxJumpTime;

            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
        }

        if (jumpHeld && isJumping && jumpTimeCounter > 0){
            jumpTimeCounter -= Time.deltaTime;

            rb.linearVelocity += Vector2.up * 10f * Time.deltaTime;
        }

        if (!jumpHeld) isJumping = false;
    }

    // ======================
    // 重力
    // ======================
    void ApplyGravity()
    {
        float deltaTime = Time.fixedDeltaTime;

        if (rb.linearVelocity.y < 0)
        {
            rb.linearVelocity += Vector2.up * Physics2D.gravity.y * (fallMultiplier - 1) * deltaTime;
        }
        else if (rb.linearVelocity.y > 0 && !jumpHeld)
        {
            rb.linearVelocity += Vector2.up * Physics2D.gravity.y * (lowJumpMultiplier - 1) * deltaTime;
        }
    }

    // ======================
    // 接地
    // ======================
    void CheckGround()
    {
        if (footCheck == null) return;

        currentGround = null;
        // A downward surface test rejects nearby walls and trigger volumes.
        RaycastHit2D[] hits = Physics2D.BoxCastAll(
            footCheck.position + Vector3.up * 0.15f,
            new Vector2(groundCheckSize.x, 0.05f), 0f,
            Vector2.down, 0.25f, footLayer);
        float nearest = float.PositiveInfinity;
        foreach (RaycastHit2D candidate in hits)
        {
            if (candidate.collider == null || candidate.collider.isTrigger ||
                candidate.normal.y < 0.5f || candidate.distance <= 0f) continue;
            if (candidate.distance < nearest)
            {
                nearest = candidate.distance;
                currentGround = candidate.collider;
            }
        }
        isGrounded = currentGround != null && rb.linearVelocity.y <= 0.1f;
        if (isGrounded) coyoteCounter = coyoteTime;
        else coyoteCounter = Mathf.Max(0f, coyoteCounter - Time.deltaTime);

        if (isGrounded && currentGround != null && !isRespawning && !isGroundLocked &&
            rb.linearVelocity.y <= 0.1f)
        {
            if (currentGround.gameObject.layer == LayerMask.NameToLayer("Ground"))
                lastGroundPosition = footCheck.position;
        }
    }

    void OnDrawGizmosSelected()
    {
        if (footCheck == null) return;
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(footCheck.position, groundCheckSize);
    }

    bool IsStandingOnClone()
    {
        if (currentGround == null) return false;
        return currentGround.gameObject.layer == LayerMask.NameToLayer("Clone");
    }

    // ======================
    // クローン
    // ======================
    void CreateClone()
    {
        if (isRespawning || !canCloneInput) return;
        if (IsStandingOnClone()) return;
        if (maxClones <= 0 || clones == null || playerPrefab == null) return;

        if (AudioManager.Instance != null) AudioManager.Instance.PlaySE(cloneSound);

        int index = currentClones % maxClones;

        if (clones[index] != null)
            Destroy(clones[index]);

        GameObject clone = Instantiate(playerPrefab, transform.position, Quaternion.identity);

        clones[index] = clone;
        currentClones++;

        StartRespawn();
        StartGroundLock();
    }

    void CreateCloneForce(){
    if (maxClones <= 0 || clones == null || playerPrefab == null) return;
    int index = currentClones % maxClones;
    if (AudioManager.Instance != null) AudioManager.Instance.PlaySE(cloneSound);

    if (clones[index] != null)
    {
        Destroy(clones[index]);
    }

    GameObject clone = Instantiate(playerPrefab, transform.position, Quaternion.identity);

    clones[index] = clone;
    currentClones++;

    StartRespawn();
    StartGroundLock();
}

    void StartRespawn()
    {
        if (isRespawning) return;
        StartCoroutine(RespawnRoutine());
    }

    IEnumerator RespawnRoutine()
    {
        isRespawning = true;

        moveInput = Vector2.zero;
        jumpHeld = false;
        jumpPressed = false;
        isJumping = false;
        jumpTimeCounter = 0f;
        coyoteCounter = 0f;
        canCloneInput = false;

        rb.linearVelocity = Vector2.zero;

        float respownHeight = 3.0f;

        Vector2 respawnPosition = new Vector2(
            lastGroundPosition.x,
            lastGroundPosition.y + respownHeight
        );
        // Do not interpolate across a teleport from the death position.
        RigidbodyInterpolation2D interpolation = rb.interpolation;
        rb.interpolation = RigidbodyInterpolation2D.None;
        rb.position = respawnPosition;
        transform.position = respawnPosition;
        Physics2D.SyncTransforms();

        yield return new WaitForFixedUpdate();
        rb.interpolation = interpolation;
        yield return new WaitForSeconds(0.1f);

        isRespawning = false;
    }

    void StartGroundLock()
    {
        isGroundLocked = true;
    }

    void CheckGroundLanding()
    {
        if (isRespawning || !isGrounded || currentGround == null) return;

        if (((1 << currentGround.gameObject.layer) & footLayer) != 0)
        {
            isGroundLocked = false;
        }
    }

    // ======================
    // 接触判定
    // ======================
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.layer == LayerMask.NameToLayer("Spike"))
        {
            Die();
        }
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        if (collision.gameObject.layer == LayerMask.NameToLayer("Spike")) Die();
    }

    public void Die()
    {
        if (isInvincible) return;

        CreateCloneForce();
        StartCoroutine(InvincibleRoutine());
    }

    IEnumerator InvincibleRoutine()
    {
        isBlinkInvincible = true;
        float t = 0f;

        while (t < 1f)
        {
            Color c = sr.color;
            c.a = (c.a == 1f) ? 0.2f : 1f;
            sr.color = c;

            float interval = Mathf.Max(0.01f, blinkInterval);
            yield return new WaitForSeconds(interval);
            t += interval;
        }

        sr.color = Color.white;
        isBlinkInvincible = false;
    }

    public void Bounce(float force)
    {
        if (rb == null) return;

        coyoteCounter = 0f;
        isJumping = false;

        rb.linearVelocity = new Vector2(rb.linearVelocity.x, force);
    }
    public void AddCloneCapacity(int amount){
    if (amount <= 0) return;

    int oldMax = maxClones;
    maxClones += amount;

    // 配列を拡張
    GameObject[] newClones = new GameObject[maxClones];

    // 既存データをコピー
    for (int i = 0; i < oldMax; i++)
    {
        newClones[i] = clones[i];
    }

    clones = newClones;
    }
}
