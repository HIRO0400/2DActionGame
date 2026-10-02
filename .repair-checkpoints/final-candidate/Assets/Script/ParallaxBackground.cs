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


    private void Awake()
    {
        startPosition = transform.position;
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

        Vector3 cameraOffset =
            (cameraTransform.position - startCameraPosition) * parallaxFactor;

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
