using UnityEngine;
using UnityEngine.InputSystem;
public class Player : MonoBehaviour
{
    [Header("移動")]
    public float moveSpeed = 5f;
    public float airControl = 0.6f;

    [Header("ジャンプ")]
    public float jumpForce = 4f;
    public float jumpHoldForce = 6f;
    public float maxJumpTime = 0.3f;

    [Header("落下調整")]
    public float fallMultiplier = 2.5f;
    public float lowJumpMultiplier = 2f;

    private Rigidbody2D rb;
    private bool isGrounded;
    private bool isJumping;
    private float jumpTimeCounter;

    private Vector2 moveInput;
    private bool jumpPressed;
    private bool jumpHeld;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        Move();
        Jump();
        BetterFall();
    }

    // ======================
    // Input Systemから呼ばれる
    // ======================
    public void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }

    public void OnJump(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            jumpPressed = true;
            jumpHeld = true;
        }
        if (context.canceled)
        {
            jumpHeld = false;
        }
    }

    // ======================
    // 移動
    // ======================
    void Move()
    {
        float controlMultiplier = isGrounded ? 1f : airControl;
        rb.linearVelocity = new Vector2(moveInput.x * moveSpeed * controlMultiplier, rb.linearVelocity.y);
    }

    // ======================
    // ジャンプ
    // ======================
    void Jump()
    {
        if (jumpPressed && isGrounded)
        {
            isJumping = true;
            jumpTimeCounter = maxJumpTime;
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            jumpPressed = false;
        }

        if (jumpHeld && isJumping)
        {
            if (jumpTimeCounter > 0)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpHoldForce);
                jumpTimeCounter -= Time.deltaTime;
            }
            else
            {
                isJumping = false;
            }
        }

        if (!jumpHeld)
        {
            isJumping = false;
        }
    }

    // ======================
    // 落下強化
    // ======================
    void BetterFall()
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
    // 接地判定
    // ======================
    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            isGrounded = true;
        }
    }

    void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            isGrounded = false;
        }
    }
}
