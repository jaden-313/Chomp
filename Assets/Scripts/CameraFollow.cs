using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform player;
    public float heightAbovePlayer = 12f;
    public float baseOrthographicSize = 5.5f;
    public float zoomOutPerSize = 2.5f;
    public float minOrthographicSize = 5.5f;
    public float maxOrthographicSize = 14f;
    public float smoothSpeed = 5f;

    private Camera followCamera;

    void Start()
    {
        followCamera = GetComponent<Camera>();

        if (followCamera != null)
        {
            followCamera.orthographic = true;
        }
    }

    void LateUpdate()
    {
        if (player == null) return;

        Vector3 desiredPosition = new Vector3(
            player.position.x,
            player.position.y + heightAbovePlayer,
            player.position.z
        );

        transform.position = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);

        // Straight down. No tilt, no diagonal offset.
        transform.rotation = Quaternion.Euler(90f, 0f, 0f);

        if (followCamera != null)
        {
            float playerSize = player.localScale.x;
            float targetSize = baseOrthographicSize + Mathf.Max(0f, playerSize - 0.5f) * zoomOutPerSize;

            targetSize = Mathf.Clamp(targetSize, minOrthographicSize, maxOrthographicSize);
            followCamera.orthographicSize = Mathf.Lerp(followCamera.orthographicSize, targetSize, smoothSpeed * Time.deltaTime);
        }
    }
}
