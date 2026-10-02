using UnityEngine;

public class BackgroundLooper : MonoBehaviour
{
    [SerializeField]
    private Transform cameraTransform;

    [SerializeField]
    private Transform[] backgrounds;

    [SerializeField]
    private float backgroundWidth;


    private void Update()
    {
        foreach (Transform bg in backgrounds)
        {
            float distance =
                cameraTransform.position.x - bg.position.x;


            if (distance > backgroundWidth)
            {
                bg.position += Vector3.right * backgroundWidth * backgrounds.Length;
            }
        }
    }
}