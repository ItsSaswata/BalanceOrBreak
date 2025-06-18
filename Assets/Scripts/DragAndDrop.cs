using UnityEngine;

public class DragAndDrop : MonoBehaviour
{
    private bool isDragging = false;
    private Camera mainCamera;
    private Vector3 dragStartWorldPos;
    private Vector3 dragStartMousePos;
    private Rigidbody rb;
    private float minY = 0f;

    [Header("Drag Settings")]
    [SerializeField] private float dragSensitivity = 1f;
    [SerializeField] private LayerMask groundLayer = -1;

    void Start()
    {
        mainCamera = Camera.main;
        rb = GetComponent<Rigidbody>();
        if (rb == null)
            rb = gameObject.AddComponent<Rigidbody>();

        minY = FindGroundLevel();
    }

    void OnMouseDown()
    {
        isDragging = true;
        rb.isKinematic = true;

        dragStartWorldPos = transform.position;
        dragStartMousePos = Input.mousePosition;
    }

    void OnMouseDrag()
    {
        if (isDragging)
        {
            Vector3 mouseDelta = Input.mousePosition - dragStartMousePos;
            Vector3 worldDelta = GetIsometricMovement(mouseDelta);
            Vector3 newPosition = dragStartWorldPos + worldDelta;

            // Ensure minimum Y constraint
            newPosition.y = Mathf.Max(newPosition.y, minY + 0.5f);

            transform.position = newPosition;
        }
    }

    void OnMouseUp()
    {
        if (isDragging)
        {
            isDragging = false;
            rb.isKinematic = false;
            rb.linearVelocity = Vector3.down * 2f;
        }
    }

    Vector3 GetIsometricMovement(Vector3 screenDelta)
    {
        float worldUnitsPerPixel = mainCamera.orthographicSize * 2f / Screen.height;

        // For isometric stacking games:
        // Screen X (left-right) = combination of world X and Z movement
        // Screen Y (up-down) = ONLY world Y movement (true vertical)

        // Calculate horizontal movement (X-Z plane only)
        Vector3 cameraRight = mainCamera.transform.right;
        // Remove any Y component to keep movement in X-Z plane only
        cameraRight.y = 0;
        cameraRight.Normalize();

        // Vertical movement is pure world Y
        Vector3 horizontalMovement = cameraRight * (screenDelta.x * worldUnitsPerPixel * dragSensitivity);
        Vector3 verticalMovement = Vector3.up * (screenDelta.y * worldUnitsPerPixel * dragSensitivity);

        return horizontalMovement + verticalMovement;
    }

    float FindGroundLevel()
    {
        RaycastHit hit;
        if (Physics.Raycast(transform.position, Vector3.down, out hit, Mathf.Infinity, groundLayer))
        {
            return hit.point.y;
        }
        return 0f; // Your plane's Y position
    }

    void OnDrawGizmosSelected()
    {
        if (mainCamera != null)
        {
            Vector3 cameraRight = mainCamera.transform.right;
            cameraRight.y = 0;
            cameraRight.Normalize();

            // Draw movement axes
            Gizmos.color = Color.red;
            Gizmos.DrawRay(transform.position, cameraRight * 2f); // Horizontal movement

            Gizmos.color = Color.green;
            Gizmos.DrawRay(transform.position, Vector3.up * 2f); // Vertical movement

            Gizmos.color = Color.blue;
            Gizmos.DrawWireCube(new Vector3(transform.position.x, minY, transform.position.z),
                               new Vector3(1f, 0.1f, 1f));
        }
    }
}