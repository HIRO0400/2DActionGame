using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;

public class CameraController : MonoBehaviour
{
    [SerializeField] private CinemachineCamera vcam;
    [SerializeField] private Transform cameraTarget;
    [SerializeField] private Rigidbody2D playerRb;

    [Header("Input System")]
    [SerializeField] private InputActionReference moveAction;

    [SerializeField] private float lookAheadAmount = 2f;
    [SerializeField] private float smoothSpeed = 5f;

    private float currentOffsetX;
    private float currentOffsetY;

    [SerializeField] private float fallOffsetY = -1f;
    [SerializeField] private float fallThreshold = -2f;

    void OnEnable()
    {
        if (moveAction != null && moveAction.action != null)
        {
            moveAction.action.Enable();
        }
    }

    void OnDisable()
    {
        if (moveAction != null && moveAction.action != null)
        {
            moveAction.action.Disable();
        }
    }

    void Update()
    {
        if (moveAction == null || playerRb == null || cameraTarget == null) return;

        HandleLookAhead();
        HandleFall();
        ApplyOffset();
    }

    void HandleLookAhead()
    {
        if (moveAction == null || moveAction.action == null) return;

        Vector2 input = moveAction.action.ReadValue<Vector2>();

        float horizontal = input.x;

        float target = 0f;

        if (horizontal > 0) target = lookAheadAmount;
        else if (horizontal < 0) target = -lookAheadAmount;

        currentOffsetX = Mathf.Lerp(currentOffsetX, target, Time.deltaTime * smoothSpeed);
    }

    void HandleFall()
    {
        if (playerRb == null) return;

        float targetY = 0f;

        if (playerRb.linearVelocity.y < fallThreshold)
            targetY = fallOffsetY;

        currentOffsetY = Mathf.Lerp(currentOffsetY, targetY, Time.deltaTime * smoothSpeed);
    }

    void ApplyOffset()
    {
        if (cameraTarget == null) return;

        cameraTarget.localPosition = new Vector3(
            currentOffsetX,
            currentOffsetY,
            0
        );
    }
}