using UnityEngine;
using Unity.Cinemachine;

public class ParallaxBackground : MonoBehaviour
{
    [SerializeField]
    private Transform cameraTransform;

    [SerializeField]
    private float parallaxFactor = 0.5f;

    private Vector3 startPosition;
    private Vector3 startCameraPosition;
    private Vector3 loopOffset;
    private BackgroundLooper looper;


    private void Awake()
    {
        startPosition = transform.position;
        looper = GetComponentInParent<BackgroundLooper>();
        if (cameraTransform != null) startCameraPosition = cameraTransform.position;
    }


    private void OnEnable()
    {
        CinemachineCore.CameraUpdatedEvent.AddListener(OnCameraUpdated);
    }

    private void OnDisable()
    {
        CinemachineCore.CameraUpdatedEvent.RemoveListener(OnCameraUpdated);
    }

    private void OnCameraUpdated(CinemachineBrain brain)
    {
        // Use only the configured rendering camera, after Cinemachine applies its final pose.
        if (cameraTransform == null || brain == null || brain.OutputCamera == null ||
            brain.OutputCamera.transform != cameraTransform)
        {
            return;
        }

        // A configured looper updates all its panels together before wrapping them.
        if (looper != null && looper.isActiveAndEnabled && looper.ManagesBackground(transform, cameraTransform)) return;
        ApplyCameraPosition(cameraTransform.position);
    }

    public void ApplyCameraPosition(Vector3 cameraPosition)
    {
        Vector3 cameraOffset = (cameraPosition - startCameraPosition) * parallaxFactor;

        transform.position =
            startPosition + loopOffset + new Vector3(
                cameraOffset.x,
                cameraOffset.y,
                0
            );
    }

    public void ShiftLoop(Vector3 offset)
    {
        loopOffset += offset;
        transform.position += offset;
    }
}
