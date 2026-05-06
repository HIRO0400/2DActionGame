using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;

public class CameraController : MonoBehaviour
{
    public CinemachineCamera vcam;
    public Transform cameraTarget;
    public Rigidbody2D playerRb;

    [Header("Input System")]
    public InputActionReference moveAction; // ← 追加（Horizontal入力）

    public float lookAheadAmount = 2f;
    public float smoothSpeed = 5f;

    private float currentOffsetX;
    private float currentOffsetY;

    public float fallOffsetY = -1f;
    public float fallThreshold = -2f;

    void OnEnable()
    {
        moveAction.action.Enable();
    }

    void OnDisable()
    {
        moveAction.action.Disable();
    }

    void Update()
    {
        HandleLookAhead();
        HandleFall();
        ApplyOffset();
    }

    void HandleLookAhead()
    {
        Vector2 input = moveAction.action.ReadValue<Vector2>();

        float horizontal = input.x;

        float target = 0f;

        if (horizontal > 0) target = lookAheadAmount;
        else if (horizontal < 0) target = -lookAheadAmount;

        currentOffsetX = Mathf.Lerp(currentOffsetX, target, Time.deltaTime * smoothSpeed);
    }

    void HandleFall()
    {
        float targetY = 0f;

        if (playerRb.linearVelocity.y < fallThreshold)
            targetY = fallOffsetY;

        currentOffsetY = Mathf.Lerp(currentOffsetY, targetY, Time.deltaTime * smoothSpeed);
    }

    void ApplyOffset()
    {
        cameraTarget.localPosition = new Vector3(
            currentOffsetX,
            currentOffsetY,
            0
        );
    }
}