using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// ระบบจัดการการเดินทางด้วยรถและเฟดหน้าจอสีดำข้ามซีน (Persistent Singleton)
/// เนื่องจากอยู่บน DontDestroyOnLoad จึงทำงานต่อเนื่องข้ามซีนได้ 100% ไม่ถูกทำลายและไม่ติดจอดำค้าง
/// </summary>
public class CarSceneFader : MonoBehaviour
{
    private static CarSceneFader instance;
    public static CarSceneFader Instance
    {
        get
        {
            if (instance == null)
            {
                GameObject go = new GameObject("CarSceneFader_Persistent");
                DontDestroyOnLoad(go);
                instance = go.AddComponent<CarSceneFader>();
                instance.InitUI();
            }
            return instance;
        }
    }

    public bool IsTransitioning { get; private set; } = false;

    private Canvas fadeCanvas;
    private Image fadeImage;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            InitUI();
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }

    private void InitUI()
    {
        if (fadeCanvas != null) return;

        fadeCanvas = gameObject.GetComponent<Canvas>();
        if (fadeCanvas == null) fadeCanvas = gameObject.AddComponent<Canvas>();
        fadeCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        fadeCanvas.sortingOrder = 9999;

        CanvasScaler scaler = gameObject.GetComponent<CanvasScaler>();
        if (scaler == null) scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        GraphicRaycaster raycaster = gameObject.GetComponent<GraphicRaycaster>();
        if (raycaster == null) gameObject.AddComponent<GraphicRaycaster>();

        Transform existingImg = transform.Find("FadeOverlay");
        GameObject imgObj;
        if (existingImg != null)
        {
            imgObj = existingImg.gameObject;
        }
        else
        {
            imgObj = new GameObject("FadeOverlay");
            imgObj.transform.SetParent(transform, false);
        }

        RectTransform rt = imgObj.GetComponent<RectTransform>();
        if (rt == null) rt = imgObj.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        fadeImage = imgObj.GetComponent<Image>();
        if (fadeImage == null) fadeImage = imgObj.AddComponent<Image>();
        fadeImage.color = new Color(0f, 0f, 0f, 0f);
        fadeImage.raycastTarget = false;

        imgObj.SetActive(false);
    }

    /// <summary>
    /// สั่งเดินทางข้ามซีนด้วยรถ พร้อมจัดการเฟดดำและย้ายตำแหน่งผู้เล่นอัตโนมัติ
    /// </summary>
    public void Travel(string targetSceneName, string targetSpawnPointName, float fadeDuration, AudioClip carSound, Vector3 soundPosition)
    {
        if (IsTransitioning) return;
        StartCoroutine(TravelRoutine(targetSceneName, targetSpawnPointName, fadeDuration, carSound, soundPosition));
    }

    private IEnumerator TravelRoutine(string targetSceneName, string targetSpawnPointName, float fadeDuration, AudioClip carSound, Vector3 soundPosition)
    {
        IsTransitioning = true;

        // 1. เล่นเสียงรถ
        if (carSound != null)
        {
            AudioSource.PlayClipAtPoint(carSound, soundPosition);
        }

        // 2. ล็อกผู้เล่นในซีนปัจจุบัน
        PlayerController currentPc = FindFirstObjectByType<PlayerController>();
        if (currentPc != null) currentPc.enabled = false;

        // 3. ค่อยๆ เฟดจอดำ
        yield return FadeOut(fadeDuration);

        // 4. บันทึกข้อมูลจุดเกิดข้ามซีน
        CarTravelData.hasTargetSpawn = !string.IsNullOrEmpty(targetSpawnPointName);
        CarTravelData.targetSpawnPointName = targetSpawnPointName;

        Time.timeScale = 1f;

        // 5. โหลดซีนเป้าหมาย
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(targetSceneName);
        if (asyncLoad != null)
        {
            while (!asyncLoad.isDone)
            {
                yield return null;
            }
        }

        // รอ 1 เฟรมให้ซีนใหม่ Initialized อ็อบเจ็กต์ครบถ้วน
        yield return null;

        // 6. จัดตำแหน่งผู้เล่นไปยังจุดเกิดที่กำหนด
        try
        {
            if (CarTravelData.hasTargetSpawn && !string.IsNullOrEmpty(CarTravelData.targetSpawnPointName))
            {
                Transform spawnTarget = SceneSpawnPoint.FindSpawnPoint(CarTravelData.targetSpawnPointName);
                PlayerController newPc = FindFirstObjectByType<PlayerController>();

                if (spawnTarget != null && newPc != null)
                {
                    CharacterController cc = newPc.GetComponent<CharacterController>();
                    if (cc != null) cc.enabled = false;

                    newPc.transform.position = spawnTarget.position;
                    newPc.transform.rotation = spawnTarget.rotation;

                    if (newPc.playerCamera != null)
                    {
                        newPc.playerCamera.localRotation = Quaternion.identity;
                    }

                    if (cc != null) cc.enabled = true;

                    Debug.Log($"[CarTravel] 🚗 ย้ายผู้เล่นไปยังจุดเกิด '{spawnTarget.name}' ({spawnTarget.position}) ในซีน '{targetSceneName}' สำเร็จ!");
                }
                else if (spawnTarget == null)
                {
                    Debug.LogWarning($"[CarTravel] ⚠️ ไม่พบจุดเกิด '{CarTravelData.targetSpawnPointName}' ในซีน '{targetSceneName}'! ผู้เล่นจะยืนที่จุดเริ่มต้นเดิมของซีน");
                }

                CarTravelData.hasTargetSpawn = false;
                CarTravelData.targetSpawnPointName = "";
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[CarTravel] เกิดข้อผิดพลาดขณะย้ายตำแหน่งผู้เล่น: {ex.Message}");
        }

        // 7. เฟดสว่างกลับขึ้นมาเสมอ (ป้องกันจอดำค้าง 100%)
        yield return FadeIn(fadeDuration);

        // 8. ปลดล็อกผู้เล่นในซีนใหม่
        PlayerController finalPc = FindFirstObjectByType<PlayerController>();
        if (finalPc != null) finalPc.enabled = true;

        IsTransitioning = false;
    }

    public IEnumerator FadeOut(float duration)
    {
        if (fadeImage == null) InitUI();
        fadeImage.gameObject.SetActive(true);

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Clamp01(elapsed / duration);
            if (fadeImage != null) fadeImage.color = new Color(0f, 0f, 0f, alpha);
            yield return null;
        }
        if (fadeImage != null) fadeImage.color = new Color(0f, 0f, 0f, 1f);
    }

    public IEnumerator FadeIn(float duration)
    {
        if (fadeImage == null) InitUI();
        fadeImage.gameObject.SetActive(true);

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float alpha = 1f - Mathf.Clamp01(elapsed / duration);
            if (fadeImage != null) fadeImage.color = new Color(0f, 0f, 0f, alpha);
            yield return null;
        }
        if (fadeImage != null)
        {
            fadeImage.color = new Color(0f, 0f, 0f, 0f);
            fadeImage.gameObject.SetActive(false);
        }
    }
}
