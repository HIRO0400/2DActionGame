
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
    private float coyoteTime = 0.1f;
    private float coyoteCounter;

    // ======================
    // クローン
    // ======================
    [Header("クローン")]
    public GameObject playerPrefab;
    public int maxClones = 3;

    private GameObject[] clones;
    private int currentClones;
    private InputAction cloneAction;
    private bool isRespawning;
    private bool canClone = true;

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
    public Vector2 groundCheckSize = new Vector2(0.8f, 0.15f);
    public LayerMask footLayer;
    private bool isTouchingWall;
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

    // 初期化
    void Awake(){
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponentInChildren<SpriteRenderer>();
        playerInput = GetComponent<PlayerInput>();

        moveAction = playerInput.actions["Move"];
        jumpAction = playerInput.actions["Jump"];
        cloneAction = playerInput.actions["Clone"];
    }

    void Start(){
        clones = new GameObject[maxClones];
        lastGroundPosition = transform.position;
    }

    void Update(){
    CheckGround();
    // ★ クローンは常に禁止（ここで制御）
    if (!isRespawning && cloneAction.WasPressedThisFrame() && canClone){
        CreateClone();
        canClone = false;
    }
    if (cloneAction.WasReleasedThisFrame()){
        canClone = true;
    }
    ReadInput();
    HandleJump();
    HandleFlip();
    jumpPressed = false;
}

    void FixedUpdate(){
    Move();
    ApplyGravity();
    ResolveWallCollision();
}

    // 入力
    void ReadInput(){
        if (isRespawning){
        moveInput = Vector2.zero;
        return;
    }
        Vector2 input = moveAction.ReadValue<Vector2>();
        moveInput.x = Mathf.Abs(input.x) > 0.1f ? input.x : 0;

        if (jumpAction.WasPressedThisFrame()){
            jumpPressed = true;
            jumpHeld = true;
        }

        if (jumpAction.WasReleasedThisFrame()){
            jumpHeld = false;
        }
    }

    // 移動
    void Move()
{
    Vector2 v = rb.linearVelocity;

    float multiplier = isGrounded ? 1f : airControl;

    float inputX = moveInput.x * moveSpeed * multiplier;

    v.x = inputX;

    rb.linearVelocity = v;
}
void ResolveWallCollision()
{
    if (!isTouchingWall) return;

    RaycastHit2D hit = Physics2D.Raycast(
        transform.position,
        Vector2.right * Mathf.Sign(rb.linearVelocity.x),
        0.3f,
        footLayer
    );

    if (hit.collider != null)
    {
        Vector2 v = rb.linearVelocity;
        v.x = 0;
        rb.linearVelocity = v;
    }
}
    void HandleFlip(){
        if (moveInput.x > 0) sr.flipX = false;
        else if (moveInput.x < 0) sr.flipX = true;
    }

    // ジャンプ
    void HandleJump(){
    if (jumpPressed && coyoteCounter > 0f){
        isJumping = true;
        jumpTimeCounter = maxJumpTime;

        Vector2 v = rb.linearVelocity;
        v.y = jumpForce;
        rb.linearVelocity = v;

        jumpPressed = false;
        coyoteCounter = 0f;
    }

    if (jumpHeld && isJumping){
        if (jumpTimeCounter > 0){
            Vector2 v = rb.linearVelocity;
            v.y = jumpHoldForce;
            rb.linearVelocity = v;

            jumpTimeCounter -= Time.deltaTime;
        }
        else{
            isJumping = false;
        }
    }

    if (!jumpHeld) isJumping = false;
}

    // 重力
    void ApplyGravity()
    {
        if (rb.linearVelocity.y < 0){
            rb.linearVelocity += Vector2.up * Physics2D.gravity.y * (fallMultiplier - 1) * Time.deltaTime;
        }
        else if (rb.linearVelocity.y > 0 && !jumpHeld){
            rb.linearVelocity += Vector2.up * Physics2D.gravity.y * (lowJumpMultiplier - 1) * Time.deltaTime;
        }
    }

    // 接地
    void CheckGround(){
        if (isGrounded) coyoteCounter = coyoteTime;
        else coyoteCounter -= Time.deltaTime;

        Collider2D hit = Physics2D.OverlapBox(
        footCheck.position,
        groundCheckSize,
        0f,
        footLayer
    );

    bool wasGrounded = isGrounded;
    isGrounded = hit != null;

    currentGround = hit;

    if (!wasGrounded && isGrounded && hit != null)
    {
        lastGroundPosition = new Vector2(
            transform.position.x,
            hit.bounds.max.y
        );
    }
}
void OnDrawGizmosSelected()
{
    Gizmos.color = Color.red;
    Gizmos.DrawWireCube(footCheck.position, groundCheckSize);
}
bool IsStandingOnClone()
{
    if (currentGround == null) return false;

    return currentGround.gameObject.layer == LayerMask.NameToLayer("Clone");
}
void OnCollisionStay2D(Collision2D collision)
{
    foreach (ContactPoint2D contact in collision.contacts)
    {
        if (Mathf.Abs(contact.normal.x) > 0.5f)
        {
            isTouchingWall = true;
            return;
        }
    }

    isTouchingWall = false;
}

    // クローン
    void CreateClone(){
        if (isRespawning || !canClone) return;
        if (IsStandingOnClone()) return;
    int index = currentClones % maxClones;
    if (clones[index] != null){
        Destroy(clones[index]);
    }

    GameObject clone = Instantiate(playerPrefab, transform.position, Quaternion.identity);

    clones[index] = clone;
    currentClones++;

    Respawn();
}

    void Respawn(){
        StartCoroutine(RespawnRoutine());
    }
    IEnumerator RestoreCollision(){
    yield return new WaitForSeconds(0.1f);

    Physics2D.IgnoreLayerCollision(
        LayerMask.NameToLayer("Player"),
        LayerMask.NameToLayer("Clone"),
        false
    );
    }
    IEnumerator RespawnRoutine()
{
    isRespawning = true;
    isInvincible = true;

    moveInput = Vector2.zero;
    jumpHeld = false;
    jumpPressed = false;

    // ★ 完全に入力停止
    playerInput.enabled = false;

    float respawnHeight = 2.0f;

    transform.position = new Vector2(
        lastGroundPosition.x,
        lastGroundPosition.y + respawnHeight
    );

    rb.linearVelocity = Vector2.zero;

    while (!isGrounded)
    {
        yield return null;
    }

    yield return new WaitForSeconds(0.1f);

    // ★ 入力復帰
    playerInput.enabled = true;

    isInvincible = false;
    isRespawning = false;
}

    // トゲ判定
    private void OnTriggerEnter2D(Collider2D collision){
        if (isRespawning) return;

        if (collision.gameObject.layer == LayerMask.NameToLayer("Spike")){
        Die();
    }
    }

    public void Die(){
        if (isInvincible) return;

        CreateClone();
        StartCoroutine(InvincibleRoutine());
    }

    IEnumerator InvincibleRoutine(){
        isInvincible = true;
        float t = 0f;
        while (t < 1f){
            Color c = sr.color;
            c.a = (c.a == 1f) ? 0.2f : 1f;
            sr.color = c;

            yield return new WaitForSeconds(blinkInterval);
            t += blinkInterval;
        }
        sr.color = Color.white;
        isInvincible = false;
    }
    public void Bounce(float force){
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, force);
    }
}