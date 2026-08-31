using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5.0f;
    public float jumpHeight = 2.0f;
    public float gravity = -9.81f;

    private CharacterController controller;
    private Vector3 velocity;

    void Start()
    {
        controller = GetComponent<CharacterController>();
    }

    void Update()
    {
        // รับค่า input จากคีย์บอร์ด (WASD หรือ ลูกศร)
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");

        // คำนวณทิศทางการเดินอิงตามมุมมองของตัวละคร
        Vector3 move = transform.right * x + transform.forward * z;

        // สั่งให้ตัวละครเดิน
        controller.Move(move * moveSpeed * Time.deltaTime);

        // เช็คว่าตัวละครเหยียบพื้นอยู่หรือไม่
        if (controller.isGrounded && velocity.y < 0)
        {
            velocity.y = -2f; // ดึงตัวละครติดพื้นไว้เล็กน้อย
        }

        // ระบบกระโดด
        if (Input.GetButtonDown("Jump") && controller.isGrounded)
        {
            // คำนวณแรงกระโดดจากความสูงที่ต้องการ
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        // จำลองแรงโน้มถ่วงเมื่ออยู่กลางอากาศ
        velocity.y += gravity * Time.deltaTime;

        // อัปเดตการเคลื่อนที่ในแกน Y (แรงโน้มถ่วงและการกระโดด)
        controller.Move(velocity * Time.deltaTime);
    }
}
