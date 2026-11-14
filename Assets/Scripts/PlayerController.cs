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

    [Header("Footstep & Movement Audio")]
    [Tooltip("เปิด/ปิดระบบเสียงเดิน")]
    public bool enableFootsteps = true;

    [Tooltip("คลิปเสียงเดินทั่วไป (เช่น เดินบนพื้นไม้/พื้นศาลา/พื้นปูน) สามารถใส่ได้หลายคลิปเพื่อสุ่ม")]
    public AudioClip[] footstepSounds;

    [Tooltip("คลิปเสียงเดินบนใบไม้/หญ้า/พื้นป่า (Optional)")]
    public AudioClip[] foliageFootstepSounds;

    [Tooltip("คลิปเสียงตกกระทบพื้นเมื่อลงมาจากการกระโดด (เว้นว่างไว้จะใช้เสียงเดินโทนทุ้มแทน)")]
    public AudioClip landSound;

    [Tooltip("AudioSource สำหรับเล่นเสียง (ถ้าเว้นว่างไว้ สคริปต์จะค้นหาหรือสร้างให้อัตโนมัติ)")]
    public AudioSource footstepAudioSource;

    [Header("Footstep Tuning")]
    [Tooltip("ระยะห่างเวลาระหว่างแต่ละก้าว (วินาที) ค่ามาตรฐานประมาณ 0.45")]
    [Range(0.2f, 1.0f)]
    public float stepInterval = 0.45f;

    [Tooltip("ระดับความดังเสียงเดิน")]
    [Range(0.0f, 1.0f)]
    public float footstepVolume = 0.85f;

    [Tooltip("ระดับความดังเสียงลงพื้น")]
    [Range(0.0f, 1.0f)]
    public float landVolume = 0.7f;

    [Tooltip("ช่วงการสุ่ม Pitch ต่ำสุด-สูงสุด เพื่อให้เสียงก้าวเท้าฟังดูเป็นธรรมชาติไม่ซ้ำซาก")]
    [Range(0.8f, 1.0f)]
    public float pitchRandomMin = 0.92f;
    [Range(1.0f, 1.3f)]
    public float pitchRandomMax = 1.08f;

    private CharacterController controller;
    private PlayerInteraction playerInteraction;
    private Vector3 velocity;
    private float verticalRotation = 0f;

    // ตัวแปรจับจังหวะเสียงก้าวเท้า
    private float stepTimer = 0f;
    private bool wasGroundedLastFrame = true;
    private int lastFootstepIndex = -1;

    void Awake()
    {
#if UNITY_EDITOR
        AutoAssignDefaultSounds();
#endif
    }

    void Start()
    {
        if (stepInterval < 0.2f) stepInterval = 0.45f;
        controller = GetComponent<CharacterController>();
        playerInteraction = GetComponent<PlayerInteraction>() ?? GetComponentInChildren<PlayerInteraction>() ?? FindFirstObjectByType<PlayerInteraction>();
        InitAudio();

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

    [Header("State Flags")]
    [Tooltip("หากเป็น true ตัวละครจะถูกล็อคไม่ให้เดิน/กระโดด แต่ยังสามารถหมุนกล้องและโต้ตอบ (Interact) ได้ (ใช้สำหรับนั่งเก้าอี้)")]
    public bool isMovementLocked = false;

    void Update()
    {
        // หากบทสนทนาเควสกำลังทำงานอยู่, กำลังเล่นมินิเกม Smash Key, กำลังอ่านข้อมูล หรือเปิดกระเป๋า ห้ามรับ Input การหันกล้องหรือเดิน
        if (QuestUIManager.Instance != null && QuestUIManager.Instance.IsDialogActive()) return;
        if (SmashKeyMinigame.Instance != null && SmashKeyMinigame.Instance.IsActive) return;
        if (playerInteraction != null && (playerInteraction.IsReading() || playerInteraction.IsInventoryOpen())) return;

        HandleLook();
        
        if (!isMovementLocked)
        {
            HandleMovement();
        }
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

        // ล็อคเมาส์กลับมาเมื่อคลิก (กรณีหลุดออกมา)
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame && Cursor.lockState != CursorLockMode.Locked)
        {
            // หากร้านค้า มินิเกม ศาลกุมารทอง Pause Menu หรือบทสนทนาเควสเปิดอยู่ ห้ามล็อคเมาส์กลับ
            if (ShopController.Instance != null && ShopController.Instance.isShopOpen) return;
            if (MinigameManager.Instance != null && MinigameManager.Instance.isMinigameActive) return;
            if (KumanThongUIController.Instance != null && KumanThongUIController.Instance.isUIOpen) return;
            if (PauseMenuManager.Instance != null && PauseMenuManager.Instance.isPaused) return;
            if (QuestUIManager.Instance != null && QuestUIManager.Instance.IsDialogActive()) return;

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

        // เก็บสถานะก่อนขยับ เพื่อใช้ตรวจจับการตกกระทบพื้น (Landing)
        bool wasGroundedBefore = wasGroundedLastFrame;
        float fallSpeedBefore = velocity.y;

        // เช็คว่าตัวละครเหยียบพื้นอยู่หรือไม่
        bool isGrounded = controller.isGrounded;
        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f; // ดึงตัวละครติดพื้นไว้เล็กน้อย
        }

        // ระบบกระโดด (ไม่มีเสียงกระโดด)
        if (jumpPressed && isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        // จำลองแรงโน้มถ่วงเมื่ออยู่กลางอากาศ
        velocity.y += gravity * Time.deltaTime;

        // รวมการเคลื่อนที่ทั้งแนวระนาบ (เดิน) และแนวดิ่ง (กระโดด/แรงโน้มถ่วง) เป็นการ Move ครั้งเดียว
        // เพื่อให้ controller.velocity สะท้อนความเร็วการเดินจริงได้อย่างถูกต้อง
        Vector3 totalMovement = (move * moveSpeed + velocity) * Time.deltaTime;
        controller.Move(totalMovement);

        bool isGroundedNow = controller.isGrounded;

        // ตรวจจับการตกกระทบพื้น (Landing) หลังตกจากที่สูง
        if (!wasGroundedBefore && isGroundedNow && fallSpeedBefore < -3.5f)
        {
            PlayLandSound();
        }
        wasGroundedLastFrame = isGroundedNow;

        // ตรวจสอบการเดินก้าวเท้า (Footsteps)
        // ทำงานเมื่อผู้เล่นกำลังกดปุ่มเคลื่อนที่ (WASD หรือ Stick) และไม่ได้กำลังกระโดดหรือตกเหว
        bool hasMoveInput = inputDir.sqrMagnitude > 0.05f;
        bool isAirborne = velocity.y > 0.5f || velocity.y < -4.0f;
        bool isMoving = hasMoveInput && !isAirborne && !isMovementLocked;

        if (isMoving)
        {
            float speedRatio = moveSpeed > 0 ? (moveSpeed / 5.0f) : 1.0f;
            stepTimer += Time.deltaTime * speedRatio;

            float minInterval = Mathf.Max(0.25f, stepInterval);
            if (stepTimer >= minInterval)
            {
                stepTimer = 0f;
                PlayFootstepSound();
            }
        }
        else
        {
            // เมื่อหยุดเดิน รีเซ็ตให้ก้าวแรกดังทันทีเมื่อเริ่มก้าวเดินใหม่
            stepTimer = Mathf.Max(0.25f, stepInterval) * 0.75f;
        }
    }

    private void InitAudio()
    {
        AudioListener.pause = false;
        if (AudioListener.volume <= 0.05f) AudioListener.volume = 1f;

        if (footstepAudioSource == null)
        {
            footstepAudioSource = GetComponent<AudioSource>();
            if (footstepAudioSource == null)
            {
                footstepAudioSource = gameObject.AddComponent<AudioSource>();
            }
        }

        if (footstepAudioSource != null)
        {
            footstepAudioSource.playOnAwake = false;
            footstepAudioSource.loop = false;
            footstepAudioSource.spatialBlend = 0f; // 2D Audio ได้ยินชัดทั้งสองข้างหู
            footstepAudioSource.volume = 1f;
            footstepAudioSource.mute = false;
            footstepAudioSource.enabled = true;
        }

#if UNITY_EDITOR
        AutoAssignDefaultSounds();
#endif

        // โหลดข้อมูลเสียงเข้า Memory ล่วงหน้า
        PreloadClips(footstepSounds);
        PreloadClips(foliageFootstepSounds);
        if (landSound != null && landSound.loadState != AudioDataLoadState.Loaded)
        {
            landSound.LoadAudioData();
        }
    }

    private void PreloadClips(AudioClip[] clips)
    {
        if (clips == null) return;
        foreach (var c in clips)
        {
            if (c != null && c.loadState != AudioDataLoadState.Loaded)
            {
                c.LoadAudioData();
            }
        }
    }

    private void PlayFootstepSound()
    {
        if (!enableFootsteps) return;

        if (footstepAudioSource == null)
        {
            footstepAudioSource = GetComponent<AudioSource>();
            if (footstepAudioSource == null)
            {
                footstepAudioSource = gameObject.AddComponent<AudioSource>();
            }
            footstepAudioSource.spatialBlend = 0f;
            footstepAudioSource.playOnAwake = false;
            footstepAudioSource.loop = false;
            footstepAudioSource.volume = 1f;
        }

#if UNITY_EDITOR
        if (footstepSounds == null || footstepSounds.Length == 0)
        {
            AutoAssignDefaultSounds();
        }
#endif

        AudioClip[] clipsToUse = footstepSounds;
        if (IsFoliageSurface())
        {
            if (foliageFootstepSounds != null && foliageFootstepSounds.Length > 0)
            {
                clipsToUse = foliageFootstepSounds;
            }
            else if (AudioManager.Instance != null && AudioManager.Instance.playerFoliageFootstepSound != null)
            {
                clipsToUse = new AudioClip[] { AudioManager.Instance.playerFoliageFootstepSound };
            }
        }

        if (clipsToUse == null || clipsToUse.Length == 0)
        {
            if (AudioManager.Instance != null && AudioManager.Instance.playerFootstepSound != null)
            {
                clipsToUse = new AudioClip[] { AudioManager.Instance.playerFootstepSound };
            }
        }

        if (clipsToUse == null || clipsToUse.Length == 0)
        {
            Debug.LogWarning("[PlayerController] ไม่พบคลิปเสียงเดินใน footstepSounds! กำลังตรวจสอบ...");
            return;
        }

        AudioClip clip = GetRandomClip(clipsToUse, ref lastFootstepIndex);
        if (clip == null) return;

        if (clip.loadState != AudioDataLoadState.Loaded)
        {
            clip.LoadAudioData();
        }

        float sfxVol = AudioManager.Instance != null ? AudioManager.Instance.GetCalculatedSFXVolume() : PlayerPrefs.GetFloat("SFXVolume", 1.0f);
        if (sfxVol <= 0.05f) sfxVol = 1.0f; // ป้องกันกรณีค่า SFX ใน Settings ถูกตั้งไว้ที่ 0

        float currentVol = Mathf.Clamp01(footstepVolume * sfxVol);
        footstepAudioSource.pitch = Random.Range(pitchRandomMin, pitchRandomMax);
        footstepAudioSource.PlayOneShot(clip, currentVol);

        Debug.Log($"[Footstep] 👣 เดินก้าวเท้า: {clip.name} (Vol: {currentVol:F2}, Pitch: {footstepAudioSource.pitch:F2})");
    }

    private void PlayLandSound()
    {
        if (!enableFootsteps || footstepAudioSource == null) return;

        AudioClip clip = landSound;
        if (clip == null)
        {
            if (IsFoliageSurface() && foliageFootstepSounds != null && foliageFootstepSounds.Length > 0)
            {
                clip = foliageFootstepSounds[0];
            }
            else if (footstepSounds != null && footstepSounds.Length > 0)
            {
                clip = footstepSounds[0];
            }
        }

        if (clip == null) return;

        if (clip.loadState != AudioDataLoadState.Loaded)
        {
            clip.LoadAudioData();
        }

        float sfxVol = AudioManager.Instance != null ? AudioManager.Instance.GetCalculatedSFXVolume() : PlayerPrefs.GetFloat("SFXVolume", 1.0f);
        if (sfxVol <= 0.05f) sfxVol = 1.0f;

        footstepAudioSource.pitch = Random.Range(0.85f, 0.95f);
        footstepAudioSource.PlayOneShot(clip, landVolume * sfxVol);
    }

    private AudioClip GetRandomClip(AudioClip[] clips, ref int lastIndex)
    {
        if (clips.Length == 1)
        {
            lastIndex = 0;
            return clips[0];
        }

        int index = Random.Range(0, clips.Length);
        if (index == lastIndex)
        {
            index = (index + 1) % clips.Length;
        }
        lastIndex = index;
        return clips[index];
    }

    /// <summary>
    /// ยิง Raycast ลงพื้นเพื่อตรวจสอบว่าเดินอยู่บนพื้นหญ้า / ใบไม้ / พื้นป่า หรือไม่
    /// ใช้ RaycastAll เพื่อข้าม Collider ของตัว Player เอง
    /// </summary>
    private bool IsFoliageSurface()
    {
        RaycastHit[] hits = Physics.RaycastAll(transform.position + Vector3.up * 0.5f, Vector3.down, 2.5f);
        foreach (var hit in hits)
        {
            if (hit.collider.transform.root == transform.root) continue; // ข้าม collider ของตัวผู้เล่นเอง

            string hitName = hit.collider.gameObject.name.ToLower();
            string tag = hit.collider.gameObject.tag.ToLower();
            if (hitName.Contains("grass") || hitName.Contains("leaf") || hitName.Contains("leaves") ||
                hitName.Contains("dirt") || hitName.Contains("forest") || hitName.Contains("terrain") ||
                tag.Contains("grass") || tag.Contains("terrain") || hit.collider is TerrainCollider)
            {
                return true;
            }
            if (hit.collider.sharedMaterial != null)
            {
                string matName = hit.collider.sharedMaterial.name.ToLower();
                if (matName.Contains("grass") || matName.Contains("leaf") || matName.Contains("leaves") ||
                    matName.Contains("dirt") || matName.Contains("forest"))
                {
                    return true;
                }
            }
        }
        return false;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        AutoAssignDefaultSounds();
    }

    private void AutoAssignDefaultSounds()
    {
        if (footstepSounds == null || footstepSounds.Length == 0)
        {
            var defaultStep = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Sounds/FootStep.wav");
            if (defaultStep != null)
            {
                footstepSounds = new AudioClip[] { defaultStep };
            }
        }

        if (foliageFootstepSounds == null || foliageFootstepSounds.Length == 0)
        {
            var leafStep = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Sounds/842187__aardsreal__footstep-leaves-2.wav");
            if (leafStep != null)
            {
                foliageFootstepSounds = new AudioClip[] { leafStep };
            }
        }
    }
#endif
}


