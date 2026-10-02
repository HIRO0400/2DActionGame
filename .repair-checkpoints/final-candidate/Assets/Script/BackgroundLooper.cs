using UnityEngine;
using Unity.Cinemachine;

public class BackgroundLooper : MonoBehaviour
{
    [SerializeField]
    private Transform cameraTransform;

    [SerializeField]
    private Transform[] backgrounds;

    [SerializeField]
    private float backgroundWidth;


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
        if (cameraTransform == null || backgrounds == null || backgrounds.Length < 2 ||
            backgroundWidth <= 0f || brain == null || brain.OutputCamera == null ||
            brain.OutputCamera.transform != cameraTransform) return;

        foreach (Transform bg in backgrounds)
        {
            if (bg == null) continue;
            float distance =
                cameraTransform.position.x - bg.position.x;


            float span = backgroundWidth * backgrounds.Length;
            Vector3 offset = Vector3.zero;
            if (distance > backgroundWidth)
            {
                offset = Vector3.right * span * Mathf.Ceil((distance - backgroundWidth) / span);
            }
            else if (distance < -backgroundWidth)
                offset = Vector3.left * span * Mathf.Ceil((-distance - backgroundWidth) / span);

            if (offset == Vector3.zero) continue;
            if (bg.TryGetComponent<ParallaxBackground>(out var parallax))
                parallax.ShiftLoop(offset);
            else bg.position += offset;
        }
    }
}
