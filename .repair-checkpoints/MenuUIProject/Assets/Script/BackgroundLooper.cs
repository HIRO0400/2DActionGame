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

        // Capture one final camera position; apply parallax to every panel first.
        Vector3 cameraPosition = cameraTransform.position;
        foreach (Transform bg in backgrounds)
            if (bg != null && bg.TryGetComponent<ParallaxBackground>(out var panel))
                panel.ApplyCameraPosition(cameraPosition);

        float span = backgroundWidth * backgrounds.Length;
        float halfSpan = span * 0.5f;
        foreach (Transform bg in backgrounds)
        {
            if (bg == null) continue;
            float distance =
                cameraPosition.x - bg.position.x;


            // Normalize into [-halfSpan, halfSpan). A moved panel cannot immediately
            // cross the opposite threshold, even when the camera teleports.
            Vector3 offset = Vector3.right * span * Mathf.Floor((distance + halfSpan) / span);

            if (offset == Vector3.zero) continue;
            if (bg.TryGetComponent<ParallaxBackground>(out var parallax))
                parallax.ShiftLoop(offset);
            else bg.position += offset;
        }
    }

    public bool ManagesBackground(Transform panel, Transform camera)
    {
        if (cameraTransform != camera || backgrounds == null || backgrounds.Length < 2 || backgroundWidth <= 0f) return false;
        foreach (var background in backgrounds) if (background == panel) return true;
        return false;
    }
}
