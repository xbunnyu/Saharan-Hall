using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// ระบบจัดการเสียงส่วนกลางของเกม (Central Audio Manager)
/// จัดหมวดหมู่เสียงทั้งหมดของเกมเป็นสัดส่วนชัดเจนใน Inspector:
/// 1. ระดับเสียงภาพรวม (Master & SFX Volume) — แถบสไลเดอร์ปรับลดเสียงเอฟเฟกต์ส่วนรวมทั้งหมด
/// 2. เพลงและบรรยากาศพื้นหลัง (BGM & Ambiance)
/// 3. เสียงกดปุ่ม UI (Button Click Sounds)
/// 4. เสียงการเคลื่อนไหวของผู้เล่น (Player Movement & Footsteps)
/// 5. เสียงเอฟเฟกต์ในเกม (Game SFX เช่น กุมารทอง, ผีหลอก)
/// 6. Audio Sources (ระบบจัดการให้อัตโนมัติ)
/// </summary>
public class AudioManager : MonoBehaviour
{
    private static AudioManager _instance;
    public static AudioManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<AudioManager>();
                if (_instance == null && Application.isPlaying)
                {
                    GameObject go = new GameObject("[AudioManager]");
                    _instance = go.AddComponent<AudioManager>();
                    DontDestroyOnLoad(go);
                    Debug.Log("[AudioManager] 🔊 Auto-created [AudioManager] in DontDestroyOnLoad for current scene.");
                }
            }
            return _instance;
        }
        private set
        {
            _instance = value;
        }
    }

    // ════════════════════════════════════════════════════════════════
    // หมวดที่ 0: ระดับเสียงภาพรวมและเอฟเฟกต์ส่วนรวม (Global Volume Controls)
    // ════════════════════════════════════════════════════════════════
    [Header("=== 0. ระดับเสียงส่วนรวม (Global Volume Controls) ===")]
    [Tooltip("ระดับเสียงเอฟเฟกต์ส่วนรวมทั้งหมดในเกม (SFX Master) — ปรับลดแถบนี้จะลดเสียงเอฟเฟกต์ทุกอย่างพร้อมกันทันที")]
    [Range(0f, 1f)]
    public float globalSFXVolume = 1.0f;

    // ════════════════════════════════════════════════════════════════
    // หมวดที่ 1: เพลงและเสียงบรรยากาศพื้นหลัง (Background Music / Ambiance)
    // ════════════════════════════════════════════════════════════════
    [Header("=== 1. เพลงและบรรยากาศพื้นหลัง (BGM & Ambiance) ===")]
    [Tooltip("เปิด/ปิดเพลงบรรยากาศพื้นหลัง")]
    public bool enableBGM = true;

    [Tooltip("ระดับความดังของเพลงบรรยากาศ (0.0 - 1.0)")]
    [Range(0f, 1f)]
    public float bgmVolume = 0.85f;

    [Tooltip("เพลงบรรยากาศเริ่มต้น (เช่น เสียงจิ้งหรีดยามค่ำคืนในศาลา/หมู่บ้าน - จิ้งหรีด.mp3)")]
    public AudioClip defaultBGM;

    [Tooltip("เพลงบรรยากาศป่า (สำหรับฉาก Forest / Forest 2)")]
    public AudioClip forestBGM;

    [Tooltip("เพลงบรรยากาศหลอน / สยองขวัญ (สำหรับฉากพิเศษ/กลางคืน)")]
    public AudioClip horrorBGM;

    // ════════════════════════════════════════════════════════════════
    // หมวดที่ 2: เสียงกดปุ่ม UI (UI Button Click Sounds)
    // ════════════════════════════════════════════════════════════════
    [Header("=== 2. เสียงกดปุ่ม UI (Button Click Sounds) ===")]
    [Tooltip("เปิด/ปิดเสียงกดปุ่ม UI")]
    public bool enableButtonClickSound = true;

    [Tooltip("ระดับความดังเฉพาะของเสียงกดปุ่ม (0.0 - 1.0)")]
    [Range(0f, 1f)]
    public float buttonClickVolume = 0.85f;

    [Tooltip("เสียงคลิกปุ่มหลักสำหรับปุ่ม UI ทั้งหมดในเกม (สามารถกด Delete ลบเป็น None ได้อิสระ)")]
    public AudioClip defaultButtonClickSound;

    [Tooltip("เสียงคลิกแบบสวิตช์ (ทางเลือก สามารถกด Delete ลบเป็น None ได้อิสระ)")]
    public AudioClip switchClickSound;

    [Tooltip("ผูกเสียงคลิกกับปุ่ม Button ทั้งหมดในหน้าจอให้อัตโนมัติหรือไม่")]
    public bool autoHookAllButtons = true;

    // ════════════════════════════════════════════════════════════════
    // หมวดที่ 3: เสียงการเคลื่อนไหวของผู้เล่น (Player Movement & Footsteps)
    // ════════════════════════════════════════════════════════════════
    [Header("=== 3. เสียงการเคลื่อนไหวของผู้เล่น (Player Movement) ===")]
    [Tooltip("เสียงเดินทั่วไปบนพื้นศาลา/พื้นไม้/พื้นปูน (FootStep.wav)")]
    public AudioClip playerFootstepSound;

    [Tooltip("เสียงเดินบนใบไม้/หญ้า/พื้นป่า (footstep-leaves-2.wav)")]
    public AudioClip playerFoliageFootstepSound;

    // ════════════════════════════════════════════════════════════════
    // หมวดที่ 4: เสียงเอฟเฟกต์ในเกม (Game SFX & Events)
    // ════════════════════════════════════════════════════════════════
    [Header("=== 4. เสียงเอฟเฟกต์ในเกม (Game SFX & Events) ===")]
    [Tooltip("เสียงศาลกุมารทองเมื่อถวายของเซ่นไหว้สำเร็จ (ใส่กุมาร.mp3)")]
    public AudioClip kumanThongSound;

    [Tooltip("เสียงผีหลอก / ตกใจ / Jumpscare (jumpscaly.mp3)")]
    public AudioClip jumpscareSound;

    [Tooltip("เสียงบรรยากาศหลอนลึกๆ เสริม (surlaound.wav)")]
    public AudioClip ambientDroneSound;

    // ════════════════════════════════════════════════════════════════
    // หมวดที่ 5: AudioSources (ระบบจัดการให้อัตโนมัติ)
    // ════════════════════════════════════════════════════════════════
    [Header("=== 5. Audio Sources (ระบบจัดการให้อัตโนมัติ) ===")]
    [Tooltip("AudioSource สำหรับเล่นเพลง BGM (Loop)")]
    public AudioSource bgmAudioSource;

    [Tooltip("AudioSource สำหรับเล่นเสียง SFX และเสียงปุ่ม (ไม่ Loop)")]
    public AudioSource sfxAudioSource;

    // Internal state
    private AudioClip currentBGMClip;
    private HashSet<Button> registeredButtons = new HashSet<Button>();
    private float lastClickSoundTime = -1f;

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
            return;
        }

        SetupAudioSources();
    }

    private void Start()
    {
        // ปลดล็อคระบบเสียงของ Unity
        AudioListener.pause = false;
        if (AudioListener.volume <= 0.05f) AudioListener.volume = 1f;

        // เริ่มเล่น BGM ทันทีเมื่อเข้าเกม
        PlayBGMForCurrentScene();

        // เชื่อมเสียงคลิกเข้ากับปุ่มทั้งหมดในหน้าจอ
        if (autoHookAllButtons)
        {
            RegisterAllSceneButtons();
        }
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        PlayBGMForCurrentScene();

        if (autoHookAllButtons)
        {
            RegisterAllSceneButtons();
        }
    }

    /// <summary>
    /// ตั้งค่า AudioSource ให้ถูกต้อง ป้องกันปัญหาเสียง Loop รัวๆ และทำให้เสียงชัดเจน
    /// แยก AudioSource ของ BGM และ SFX ออกเป็น Child GameObject เพื่อไม่ให้ชนกัน
    /// </summary>
    private void SetupAudioSources()
    {
        // 1. จัดการ BGM AudioSource บน Child GameObject แยกต่างหาก
        Transform bgmChild = transform.Find("BGM_Source");
        if (bgmChild == null)
        {
            GameObject go = new GameObject("BGM_Source");
            go.transform.SetParent(transform);
            bgmChild = go.transform;
        }
        bgmAudioSource = bgmChild.GetComponent<AudioSource>();
        if (bgmAudioSource == null) bgmAudioSource = bgmChild.gameObject.AddComponent<AudioSource>();

        bgmAudioSource.playOnAwake = false;
        bgmAudioSource.loop = true;          // เพลง BGM วนลูป
        bgmAudioSource.spatialBlend = 0f;    // 2D Audio (ได้ยินชัดเจนทุกทิศทาง)
        bgmAudioSource.ignoreListenerPause = true;

        // 2. จัดการ SFX AudioSource บน Child GameObject แยกต่างหาก
        Transform sfxChild = transform.Find("SFX_Source");
        if (sfxChild == null)
        {
            GameObject go = new GameObject("SFX_Source");
            go.transform.SetParent(transform);
            sfxChild = go.transform;
        }
        sfxAudioSource = sfxChild.GetComponent<AudioSource>();
        if (sfxAudioSource == null) sfxAudioSource = sfxChild.gameObject.AddComponent<AudioSource>();

        sfxAudioSource.playOnAwake = false;
        sfxAudioSource.loop = false;         // *** SFX และเสียงปุ่มห้าม Loop เด็ดขาด! ***
        sfxAudioSource.spatialBlend = 0f;    // 2D Audio
        sfxAudioSource.ignoreListenerPause = true;

        // 3. ปิดเสียงและปิด Loop ของ AudioSource ทั้งหมดที่ติดอยู่บนตัว Root GameObject
        // เพื่อแก้ปัญหา AudioSource ที่ผู้ใช้เผลอสร้างไว้แล้วติ๊ก Loop ค้างไว้
        AudioSource[] rootSources = GetComponents<AudioSource>();
        foreach (var s in rootSources)
        {
            s.Stop();
            s.loop = false;
            s.playOnAwake = false;
        }
    }

    // ─────────────────────────────────────────────────────────────
    // ระบบเล่น BGM
    // ─────────────────────────────────────────────────────────────

    /// <summary>
    /// ตรวจสอบ Scene ปัจจุบันและเล่น BGM ที่เหมาะสมให้อัตโนมัติ
    /// </summary>
    public void PlayBGMForCurrentScene()
    {
        if (!enableBGM) return;

        string sceneName = SceneManager.GetActiveScene().name.ToLower();
        AudioClip clipToPlay = defaultBGM;

        if (sceneName.Contains("forest") && forestBGM != null)
        {
            clipToPlay = forestBGM;
        }

#if UNITY_EDITOR
        // Fallback อัตโนมัติหากยังไม่ได้ลากไฟล์ใส่ใน Inspector
        if (clipToPlay == null)
        {
            if (sceneName.Contains("forest"))
            {
                forestBGM = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Sounds/877424__3to1video__outdoors_ambiance_forest_insects_birds_distant_car.wav");
                clipToPlay = forestBGM;
            }
            if (clipToPlay == null)
            {
                defaultBGM = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Sounds/จิ้งหรีด.mp3");
                clipToPlay = defaultBGM;
            }
        }
#endif

        if (clipToPlay != null)
        {
            PlayBGM(clipToPlay);
        }
        else
        {
            Debug.LogWarning("[AudioManager] ยังไม่ได้ลากไฟล์เสียงใส่ช่อง Default BGM หรือ Forest BGM ใน Inspector!");
        }
    }

    /// <summary>
    /// สั่งเล่นเพลง BGM ที่กำหนด
    /// </summary>
    public void PlayBGM(AudioClip clip)
    {
        if (clip == null || !enableBGM) return;

        // ระบบป้องกัน: หากคลิปที่ใส่ใน BGM มีความยาวสั้นมาก (< 3 วินาที) เช่น เสียงคลิกปุ่ม
        // ระบบจะแจ้งเตือนและสลับไปใช้เสียงบรรยากาศจิ้งหรีดแทน เพื่อไม่ให้เสียงปุ่มวนลูป
        if (clip.length < 3.0f)
        {
            Debug.LogWarning($"[AudioManager] คลิป '{clip.name}' สั้นเกินไปสำหรับ BGM ({clip.length:F2} วินาที) ซึ่งเป็นเสียงคลิกปุ่ม! ระบบสลับไปใช้เสียงบรรยากาศ 'จิ้งหรีด.mp3' อัตโนมัติ");
            AudioClip properBGM = null;
#if UNITY_EDITOR
            properBGM = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Sounds/จิ้งหรีด.mp3");
#endif
            if (properBGM != null)
            {
                clip = properBGM;
            }
            else
            {
                return; // ป้องกันไม่ให้เล่นเสียงปุ่มแบบ Loop
            }
        }

        if (bgmAudioSource == null)
        {
            SetupAudioSources();
        }

        currentBGMClip = clip;
        bgmAudioSource.clip = clip;
        bgmAudioSource.loop = true;
        bgmAudioSource.spatialBlend = 0f;
        bgmAudioSource.volume = GetCalculatedBGMVolume();

        if (!bgmAudioSource.isPlaying || bgmAudioSource.clip != clip)
        {
            bgmAudioSource.Play();
            Debug.Log($"[AudioManager] 🎵 กำลังเล่นเพลงบรรยากาศ: {clip.name} (Volume: {bgmAudioSource.volume:F2})");
        }
    }

    /// <summary>
    /// หยุดเพลง BGM
    /// </summary>
    public void StopBGM()
    {
        if (bgmAudioSource != null && bgmAudioSource.isPlaying)
        {
            bgmAudioSource.Stop();
        }
    }

    /// <summary>
    /// อัปเดตความดัง BGM ทันที (เรียกใช้เมื่อผู้เล่นเลื่อน Slider ในหน้าตั้งค่า)
    /// </summary>
    public void UpdateVolume()
    {
        if (bgmAudioSource != null)
        {
            bgmAudioSource.volume = GetCalculatedBGMVolume();
        }
    }

    /// <summary>
    /// ตั้งค่าความดังเพลง BGM (0.0 - 1.0) — ผูกกับ Slider ในหน้า Setting (Dynamic float) ได้ทันที
    /// </summary>
    public void SetBGMVolume(float volume)
    {
        bgmVolume = Mathf.Clamp01(volume);
        PlayerPrefs.SetFloat("BGMVolume", volume);
        PlayerPrefs.Save();
        UpdateVolume();
    }

    /// <summary>
    /// ตั้งค่าความดังเสียงเอฟเฟกต์ส่วนรวม (0.0 - 1.0) — ผูกกับ Slider ในหน้า Setting (Dynamic float) ได้ทันที
    /// </summary>
    public void SetGlobalSFXVolume(float volume)
    {
        globalSFXVolume = Mathf.Clamp01(volume);
        PlayerPrefs.SetFloat("SFXVolume", volume);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// ตั้งค่า Master Volume รวมทั้งเกม (0.0 - 1.0) — ผูกกับ Slider ในหน้า Setting ได้
    /// </summary>
    public void SetMasterVolume(float volume)
    {
        AudioListener.volume = Mathf.Clamp01(volume);
        PlayerPrefs.SetFloat("MasterVolume", volume);
        PlayerPrefs.Save();
    }

    public float GetCalculatedBGMVolume()
    {
        float prefVol = PlayerPrefs.GetFloat("BGMVolume", 0.8f);
        if (prefVol <= 0.05f) prefVol = 0.8f;
        return Mathf.Clamp01(bgmVolume * prefVol);
    }

    /// <summary>
    /// คำนวณความดังของเสียงเอฟเฟกต์ส่วนรวม (Global SFX Volume)
    /// ใช้ร่วมกับ PlayerPrefs("SFXVolume") และตัวปรับลดเสียงส่วนรวม globalSFXVolume
    /// </summary>
    public float GetCalculatedSFXVolume()
    {
        float sfxPref = PlayerPrefs.GetFloat("SFXVolume", 1.0f);
        if (sfxPref <= 0.05f) sfxPref = 1.0f;
        return Mathf.Clamp01(globalSFXVolume * sfxPref);
    }

    // ─────────────────────────────────────────────────────────────
    // ระบบเสียงกดปุ่ม UI
    // ─────────────────────────────────────────────────────────────

    /// <summary>
    /// เล่นเสียงคลิกปุ่ม UI (มีระบบ Debounce ป้องกันเสียงดังซ้ำรัวๆ)
    /// </summary>
    public void PlayButtonClickSound(AudioClip customClip = null)
    {
        if (!enableButtonClickSound) return;

        // ป้องกันเสียงดังรัวๆ (เว้นระยะห่างขั้นต่ำ 0.12 วินาทีระหว่างแต่ละคลิก)
        if (Time.unscaledTime - lastClickSoundTime < 0.12f) return;
        lastClickSoundTime = Time.unscaledTime;

        AudioClip clipToPlay = customClip != null ? customClip : defaultButtonClickSound;
        if (clipToPlay == null) clipToPlay = switchClickSound;
        if (clipToPlay == null) return; // ถ้าลบเป็น None จะไม่เล่นเสียง

        if (sfxAudioSource == null)
        {
            SetupAudioSources();
        }

        float vol = Mathf.Clamp01(buttonClickVolume * GetCalculatedSFXVolume());
        sfxAudioSource.pitch = Random.Range(0.97f, 1.03f);
        sfxAudioSource.PlayOneShot(clipToPlay, vol);
    }

    /// <summary>
    /// ค้นหาปุ่ม Button ทั้งหมดในหน้าจอเพื่อผูกเสียงคลิกให้อัตโนมัติ
    /// </summary>
    public void RegisterAllSceneButtons()
    {
        if (!autoHookAllButtons || !enableButtonClickSound) return;

        Button[] buttons = FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var btn in buttons)
        {
            if (btn == null) continue;
            if (registeredButtons.Contains(btn)) continue;

            registeredButtons.Add(btn);

            Button currentBtn = btn;
            currentBtn.onClick.AddListener(() =>
            {
                if (currentBtn != null && currentBtn.interactable && currentBtn.isActiveAndEnabled)
                {
                    PlayButtonClickSound();
                }
            });
        }
    }

    // ─────────────────────────────────────────────────────────────
    // ระบบเสียงเอฟเฟกต์ในเกม (SFX Events เช่น กุมารทอง, ผี)
    // ─────────────────────────────────────────────────────────────

    /// <summary>
    /// เล่นเสียงถวายของกุมารทอง (ใส่กุมาร.mp3)
    /// </summary>
    public void PlayKumanThongSound()
    {
        AudioClip clip = kumanThongSound;
#if UNITY_EDITOR
        if (clip == null)
        {
            clip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Sounds/ใส่กุมาร.mp3");
        }
#endif
        if (clip != null)
        {
            PlaySFX(clip, 1.0f);
        }
    }

    /// <summary>
    /// เล่นเสียงเอฟเฟกต์ทั่วไปผ่าน 2D SFX Source โดยคำนวณตามระดับเสียงเอฟเฟกต์ส่วนรวม (Global SFX Volume)
    /// </summary>
    public void PlaySFX(AudioClip clip, float volumeScale = 1.0f)
    {
        if (clip == null) return;

        if (sfxAudioSource == null)
        {
            SetupAudioSources();
        }

        float finalVol = Mathf.Clamp01(volumeScale * GetCalculatedSFXVolume());
        sfxAudioSource.pitch = 1.0f;
        sfxAudioSource.PlayOneShot(clip, finalVol);
    }

#if UNITY_EDITOR
    // ให้ผู้ใช้คลิกขวาที่ Inspector แล้วเลือกคำสั่งนี้เองเมื่อต้องการ Auto-Fill เท่านั้น
    // ไม่ผูกไว้กับ OnValidate() เพื่อให้ผู้ใช้สามารถกดลบเป็น None ได้อิสระ 100%!
    [ContextMenu("Auto Assign Default Sounds")]
    public void AutoAssignClips()
    {
        if (defaultBGM == null)
        {
            defaultBGM = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Sounds/จิ้งหรีด.mp3");
        }

        if (forestBGM == null)
        {
            forestBGM = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Sounds/877424__3to1video__outdoors_ambiance_forest_insects_birds_distant_car.wav");
        }

        if (horrorBGM == null)
        {
            horrorBGM = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Sounds/680740__christmaskrumble666__horror-ultra-fear-loop.mp3");
        }

        if (defaultButtonClickSound == null)
        {
            defaultButtonClickSound = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Sounds/152535__dwoboyle__electronics-flashlight-plastic-button-03.wav");
        }

        if (switchClickSound == null)
        {
            switchClickSound = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Sounds/842400__moulaythami__switch-010.wav");
        }

        if (playerFootstepSound == null)
        {
            playerFootstepSound = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Sounds/FootStep.wav");
        }

        if (playerFoliageFootstepSound == null)
        {
            playerFoliageFootstepSound = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Sounds/842187__aardsreal__footstep-leaves-2.wav");
        }

        if (kumanThongSound == null)
        {
            kumanThongSound = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Sounds/ใส่กุมาร.mp3");
        }

        if (jumpscareSound == null)
        {
            jumpscareSound = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Sounds/jumpscaly.mp3");
        }

        if (ambientDroneSound == null)
        {
            ambientDroneSound = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Sounds/surlaound.wav");
        }

        UnityEditor.EditorUtility.SetDirty(this);
    }
#endif
}
