using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5.0f;
    public float jumpHeight = 2.0f;
    public float gravity = -9.81f;

    [Header("Camera & Look Settings")]
    [Tooltip("ลากวัตถุ Camera มาใส่ หรือถ้าเว้นว่างไว้จะค้นหากล้องให้อัตโนมัติ")]
    public Transform playerCamera;
    public float mouseSensitivity = 0.15f;
    public float gamepadLookSpeed = 120.0f;
    public float minVerticalAngle = -85f; // ก้มได้สูงสุด
    public float maxVerticalAngle = 85f;  // เงยได้สูงสุด
    public bool lockCursor = true;

    private CharacterController controller;
    private Vector3 velocity;
    private float verticalRotation = 0f;

    void Start()
    {
        controller = GetComponent<CharacterController>();

        // โหลดความไวเมาส์จากการตั้งค่าใน Main Menu
        if (PlayerPrefs.HasKey("MouseSensitivity"))
        {
            mouseSensitivity = PlayerPrefs.GetFloat("MouseSensitivity", mouseSensitivity);
        }

        // หากยังไม่ได้กำหนดกล้อง ให้หากล้องที่เป็น Child หรือ Main Camera
        if (playerCamera == null)
        {
            Camera cam = GetComponentInChildren<Camera>();
            if (cam != null)
            {
                playerCamera = cam.transform;
            }
            else if (Camera.main != null)
            {
                playerCamera = Camera.main.transform;
            }
        }

        // ซ่อนและล็อคเคอร์เซอร์เมาส์ไว้กลางหน้าจอสำหรับการเล่นมุมมองบุคคลที่หนึ่ง
        if (lockCursor)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        if (playerCamera != null)
        {
            verticalRotation = playerCamera.localEulerAngles.x;
            if (verticalRotation > 180f) verticalRotation -= 360f;
        }
    }

    void Update()
    {
        HandleLook();
        HandleMovement();
    }

    private void HandleLook()
    {
        float lookX = 0f;
        float lookY = 0f;

        // รับค่าการขยับเมาส์ (New Input System)
        if (Mouse.current != null && Cursor.lockState == CursorLockMode.Locked)
        {
            Vector2 mouseDelta = Mouse.current.delta.ReadValue();
            lookX += mouseDelta.x * mouseSensitivity;
            lookY += mouseDelta.y * mouseSensitivity;
        }

        // รับค่าจาก Right Stick ของ Gamepad
        if (Gamepad.current != null)
        {
            Vector2 rightStick = Gamepad.current.rightStick.ReadValue();
            lookX += rightStick.x * gamepadLookSpeed * Time.deltaTime;
            lookY += rightStick.y * gamepadLookSpeed * Time.deltaTime;
        }

        // หมุนตัวละครซ้าย-ขวาตามการมอง
        transform.Rotate(Vector3.up * lookX);

        // หมุนกล้องก้ม-เงย (Vertical Pitch)
        if (playerCamera != null)
        {
            verticalRotation -= lookY;
            verticalRotation = Mathf.Clamp(verticalRotation, minVerticalAngle, maxVerticalAngle);
            playerCamera.localRotation = Quaternion.Euler(verticalRotation, 0f, 0f);
        }

        // กด Escape เพื่อปลดล็อคเมาส์ (กรณีต้องการปรับของใน Editor) และคลิกเพื่อล็อคเมาส์กลับมา
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame && Cursor.lockState != CursorLockMode.Locked)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    private void HandleMovement()
    {
        float x = 0f;
        float z = 0f;
        bool jumpPressed = false;

        // ตรวจสอบ Input จาก Keyboard
        if (Keyboard.current != null)
        {
            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) x -= 1f;
            if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) x += 1f;
            if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) z += 1f;
            if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) z -= 1f;

            if (Keyboard.current.spaceKey.wasPressedThisFrame)
            {
                jumpPressed = true;
            }
        }

        // ตรวจสอบ Input จาก Gamepad
        if (Gamepad.current != null)
        {
            Vector2 stick = Gamepad.current.leftStick.ReadValue();
            if (Mathf.Abs(stick.x) > 0.1f) x += stick.x;
            if (Mathf.Abs(stick.y) > 0.1f) z += stick.y;

            if (Gamepad.current.buttonSouth.wasPressedThisFrame)
            {
                jumpPressed = true;
            }
        }

        // ปรับขนาดเวกเตอร์ไม่ให้เดินทแยงแล้วเร็วเกินไป
        Vector2 inputDir = Vector2.ClampMagnitude(new Vector2(x, z), 1f);

        // คำนวณทิศทางการเดินอิงตามมุมมองและทิศทางของตัวละคร
        Vector3 move = transform.right * inputDir.x + transform.forward * inputDir.y;

        // สั่งให้ตัวละครเดิน
        controller.Move(move * moveSpeed * Time.deltaTime);

        // เช็คว่าตัวละครเหยียบพื้นอยู่หรือไม่
        if (controller.isGrounded && velocity.y < 0)
        {
            velocity.y = -2f; // ดึงตัวละครติดพื้นไว้เล็กน้อย
        }

        // ระบบกระโดด
        if (jumpPressed && controller.isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        // จำลองแรงโน้มถ่วงเมื่ออยู่กลางอากาศ
        velocity.y += gravity * Time.deltaTime;

        // อัปเดตการเคลื่อนที่ในแกน Y (แรงโน้มถ่วงและการกระโดด)
        controller.Move(velocity * Time.deltaTime);
    }
}


