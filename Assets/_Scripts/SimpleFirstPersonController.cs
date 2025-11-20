using UnityEngine;

public class SimpleFirstPersonController : MonoBehaviour
{
    [Header("Movimento")]
    public float speed = 5f;
    public float sprintMultiplier = 1.5f;

    [Header("Camera")]
    public Transform cameraTransform;
    public float lookSensitivity = 3f;

    private bool isSprinting;
    private float xRotation = 0f;

    void Start()
    {
        Utilities.SetCursorLocked(true);
    }

    void Update()
    {
        Sprint();
        Move();
        look();
    }

    void Move()
    {
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        Vector3 inputOir = new Vector3(horizontal, 0f, vertical);

    }
}
