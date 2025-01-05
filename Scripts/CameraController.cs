using UnityEngine;

public class CameraController : MonoBehaviour
{
    // Movement speed
    public float moveSpeed = 5f;
    public float sprintMultiplier = 2f; // Multiplier for sprinting

    // Zooming parameters
    public float zoomSpeed = 2f;
    public float minZoom = 2f;
    public float maxZoom = 10f;

    // Update is called once per frame
    void Update()
    {
        HandleMovement();
        HandleZoom();
    }

    void HandleMovement()
    {
        // Get input for vertical and horizontal movement
        float verticalInput = Input.GetAxis("Vertical");
        float horizontalInput = Input.GetAxis("Horizontal");

        // Check if Shift is held for sprinting
        float currentSpeed = Input.GetKey(KeyCode.LeftShift) ? moveSpeed * sprintMultiplier : moveSpeed;

        // Move the camera up, down, left, and right
        Vector3 movement = new Vector3(horizontalInput * currentSpeed * Time.deltaTime, verticalInput * currentSpeed * Time.deltaTime, 0);
        transform.position += movement;
    }

    void HandleZoom()
    {
        // Get input from the mouse scroll wheel
        float scrollInput = Input.GetAxis("Mouse ScrollWheel");

        // Adjust the camera's orthographic size (for 2D) or field of view (for 3D)
        Camera camera = GetComponent<Camera>();
        if (camera.orthographic)
        {
            camera.orthographicSize -= scrollInput * zoomSpeed;
            camera.orthographicSize = Mathf.Clamp(camera.orthographicSize, minZoom, maxZoom);
        }
        else
        {
            camera.fieldOfView -= scrollInput * zoomSpeed;
            camera.fieldOfView = Mathf.Clamp(camera.fieldOfView, minZoom, maxZoom);
        }
    }
}
