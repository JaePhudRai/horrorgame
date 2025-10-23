using UnityEngine;

public class FirstPersonCamera : MonoBehaviour
{
    [Header("Mouse Settings")]
    public float mouseSensitivity = 100f;

    [Header("References")]
    public Transform playerBody;       // ลาก Capsule มาวางตรงนี้
    public Transform cameraPosition;   // จุดที่กล้องจะตาม (เช่นหัวผู้เล่น)

    private float xRotation = 0f;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked; // ล็อกเมาส์ไว้กลางจอ
    }

    void Update()
    {
        // ----- หมุนกล้องด้วยเมาส์ -----
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f); // จำกัดมุมมองแนวตั้ง

        transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        playerBody.Rotate(Vector3.up * mouseX);

        // ----- ล็อกตำแหน่งกล้องให้ตาม player -----
        if (cameraPosition != null)
        {
            transform.position = cameraPosition.position;
        }
    }
}