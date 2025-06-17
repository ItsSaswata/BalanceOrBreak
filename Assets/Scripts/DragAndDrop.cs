using UnityEngine;

public class DragAndDrop : MonoBehaviour
{
    private bool isDragging = false;
    private Camera mainCamera;
    private Vector3 offset;
    private Rigidbody rb;
    private float fixedDepth; // Fixed Z position relative to camera

    void Start()
    {
        mainCamera = Camera.main;
        rb = GetComponent<Rigidbody>();

        // Make sure the cube has a Rigidbody
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }

        // Store the initial depth (distance from camera) to maintain it
        fixedDepth = Vector3.Dot(transform.position - mainCamera.transform.position, mainCamera.transform.forward);
    }

    void OnMouseDown()
    {
        isDragging = true;
        // Freeze physics while dragging
        rb.isKinematic = true;

        // Calculate offset between mouse and object center
        Vector3 mousePos = GetMouseWorldPosition();
        offset = transform.position - mousePos;
    }

    void OnMouseDrag()
    {
        if (isDragging)
        {
            Vector3 mousePos = GetMouseWorldPosition();
            Vector3 newPosition = mousePos + offset;

            // Constrain the position to maintain fixed depth from camera
            Vector3 cameraToNewPos = newPosition - mainCamera.transform.position;
            Vector3 projectedPos = Vector3.ProjectOnPlane(cameraToNewPos, mainCamera.transform.forward);

            transform.position = mainCamera.transform.position + projectedPos + (mainCamera.transform.forward * fixedDepth);
        }
    }

    void OnMouseUp()
    {
        if (isDragging)
        {
            isDragging = false;
            // Re-enable physics when dropped
            rb.isKinematic = false;

            // Optional: Add a small downward velocity for satisfying drop
            // Use the camera's up vector to determine "down" direction
            rb.linearVelocity = -mainCamera.transform.up * 2f;
        }
    }

    Vector3 GetMouseWorldPosition()
    {
        Vector3 mouseScreenPos = Input.mousePosition;

        // For orthographic camera, use a fixed distance from camera
        mouseScreenPos.z = fixedDepth;

        return mainCamera.ScreenToWorldPoint(mouseScreenPos);
    }
}