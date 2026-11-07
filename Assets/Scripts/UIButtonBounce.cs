using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// คอมโพเนนต์ทำให้ปุ่ม UI มีเอฟเฟกต์ "เด้งตอบรับ" (Juicy Bounce Effect) เมื่อเอาเมาส์ไปชี้หรือกดคลิก
/// - ขยายขนาดอย่างนุ่มนวลพร้อมจังหวะเด้งดึ๋ง (Hover Bounce / Elastic)
/// - ยุบลงเล็กน้อยเมื่อกดคลิก (Click Press)
/// - เล่นเสียง Hover / Click ได้
/// - รองรับทั้งเมาส์ (Pointer) และจอยสติ๊ก/คีย์บอร์ด (Navigation Select)
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class UIButtonBounce : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler, ISelectHandler, IDeselectHandler
{
    [Header("=== การตั้งค่าขนาดและจังหวะเด้ง (Scale Settings) ===")]
    [Tooltip("ขนาดขยายเมื่อเอาเมาส์มาชี้ (1.0 = ปกติ, 1.1 = ขยาย 10%, 1.15 = ขยาย 15%)")]
    [Range(1.0f, 1.5f)]
    public float hoverScale = 1.10f;

    [Tooltip("ขนาดยุบตัวเมื่อกดคลิกเมาส์ลงไป")]
    [Range(0.8f, 1.0f)]
    public float clickScale = 0.94f;

    [Tooltip("ความเร็วในการเคลื่อนไหว (ยิ่งมากยิ่งตอบสนองเร็ว)")]
    public float animationSpeed = 14f;

    [Tooltip("เปิดเอฟเฟกต์เด้งดึ๋งแบบ Elastic Bounce (ขยายเกินนิดๆ แล้วเด้งกลับเข้าที่อย่างสวยงาม)")]
    public bool useElasticBounce = true;

    [Tooltip("ระยะ Overshoot ของจังหวะเด้งดึ๋ง")]
    [Range(1.0f, 1.3f)]
    public float bounceOvershoot = 1.14f;

    [Header("=== สีและไฮไลต์เสริม (Optional Color Tint) ===")]
    [Tooltip("เปลี่ยนสีภาพพื้นหลังให้สว่างขึ้นตอนชี้เมาส์หรือไม่")]
    public bool enableColorTint = false;
    public Image targetGraphic;
    public Color hoverColorMultiplier = new Color(1.15f, 1.15f, 1.15f, 1.0f);

    [Header("=== เสียงประกอบ (Audio Feedback) ===")]
    [Tooltip("เสียงเอฟเฟกต์เบาๆ เมื่อเลื่อนเมาส์มาชี้ (Optional)")]
    public AudioClip hoverSound;
    [Tooltip("เสียงเมื่อกดคลิกปุ่ม (Optional)")]
    public AudioClip clickSound;

    // Runtime state
    private RectTransform rectTransform;
    private Button button;
    private Vector3 originalScale;
    private Vector3 targetScale;
    private Color originalColor;
    private Coroutine activeScaleRoutine;
    private bool isHovered = false;
    private bool isPressed = false;

    public bool IsHovered => isHovered;
    public bool IsPressed => isPressed;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        button = GetComponent<Button>();

        if (rectTransform != null)
        {
            originalScale = rectTransform.localScale;
            // ถ้า scale เริ่มต้นเป็น 0 ให้ตั้งเป็น 1
            if (originalScale == Vector3.zero) originalScale = Vector3.one;
        }
        else
        {
            originalScale = Vector3.one;
        }

        targetScale = originalScale;

        if (targetGraphic == null)
        {
            targetGraphic = GetComponent<Image>();
        }

        if (targetGraphic != null)
        {
            originalColor = targetGraphic.color;
        }
    }

    private void OnEnable()
    {
        // คืนค่าขนาดเดิมเสมอเมื่อเปิด Panel ขึ้นมาใหม่
        if (rectTransform != null)
        {
            rectTransform.localScale = originalScale;
        }
        targetScale = originalScale;
        isHovered = false;
        isPressed = false;

        if (targetGraphic != null && enableColorTint)
        {
            targetGraphic.color = originalColor;
        }
    }

    private void OnDisable()
    {
        if (activeScaleRoutine != null)
        {
            StopCoroutine(activeScaleRoutine);
            activeScaleRoutine = null;
        }

        if (rectTransform != null)
        {
            rectTransform.localScale = originalScale;
        }

        if (targetGraphic != null && enableColorTint)
        {
            targetGraphic.color = originalColor;
        }

        isHovered = false;
        isPressed = false;
    }

    // ──────────────────────────────────────────────────────────
    // Event Triggers (Pointer & Select)
    // ──────────────────────────────────────────────────────────

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (button != null && !button.interactable) return;

        isHovered = true;
        PlayHoverSound();
        ApplyColorTint(true);

        if (useElasticBounce)
        {
            StartScaleAnimation(originalScale * hoverScale, true);
        }
        else
        {
            StartScaleAnimation(originalScale * hoverScale, false);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;
        ApplyColorTint(false);
        StartScaleAnimation(originalScale, false);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (button != null && !button.interactable) return;

        isPressed = true;
        PlayClickSound();
        StartScaleAnimation(originalScale * clickScale, false);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        isPressed = false;
        if (isHovered)
        {
            StartScaleAnimation(originalScale * hoverScale, useElasticBounce);
        }
        else
        {
            StartScaleAnimation(originalScale, false);
        }
    }

    public void OnSelect(BaseEventData eventData)
    {
        if (button != null && !button.interactable) return;
        isHovered = true;
        StartScaleAnimation(originalScale * hoverScale, useElasticBounce);
    }

    public void OnDeselect(BaseEventData eventData)
    {
        isHovered = false;
        isPressed = false;
        StartScaleAnimation(originalScale, false);
    }

    // ──────────────────────────────────────────────────────────
    // Animation Routine
    // ──────────────────────────────────────────────────────────

    private void StartScaleAnimation(Vector3 target, bool withElasticBounce)
    {
        if (rectTransform == null) return;

        if (activeScaleRoutine != null)
        {
            StopCoroutine(activeScaleRoutine);
        }

        if (gameObject.activeInHierarchy)
        {
            activeScaleRoutine = StartCoroutine(AnimateScaleRoutine(target, withElasticBounce));
        }
        else
        {
            rectTransform.localScale = target;
        }
    }

    private IEnumerator AnimateScaleRoutine(Vector3 destination, bool withElasticBounce)
    {
        // หากต้องการเอฟเฟกต์เด้งดึ๋งแบบ Juicy ให้เด้งขยายเกินนิดหนึ่งก่อน (Overshoot)
        if (withElasticBounce)
        {
            Vector3 overshootDestination = originalScale * bounceOvershoot;
            float overshootElapsed = 0f;
            float overshootDuration = 0.10f;
            Vector3 startScale = rectTransform.localScale;

            while (overshootElapsed < overshootDuration)
            {
                overshootElapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(overshootElapsed / overshootDuration);
                // กราฟเร่งช่วงแรก
                float ease = Mathf.Sin(t * Mathf.PI * 0.5f);
                rectTransform.localScale = Vector3.LerpUnclamped(startScale, overshootDestination, ease);
                yield return null;
            }

            // จังหวะดึงกลับเข้าสู่ขนาดเป้าหมาย
            float settleElapsed = 0f;
            float settleDuration = 0.12f;
            Vector3 fromOvershoot = rectTransform.localScale;

            while (settleElapsed < settleDuration)
            {
                settleElapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(settleElapsed / settleDuration);
                // Smooth step curve
                float ease = Mathf.SmoothStep(0f, 1f, t);
                rectTransform.localScale = Vector3.Lerp(fromOvershoot, destination, ease);
                yield return null;
            }
        }
        else
        {
            // เคลื่อนไหวสมูทตามปกติ
            while (Vector3.Distance(rectTransform.localScale, destination) > 0.002f)
            {
                rectTransform.localScale = Vector3.Lerp(rectTransform.localScale, destination, Time.unscaledDeltaTime * animationSpeed);
                yield return null;
            }
        }

        rectTransform.localScale = destination;
        activeScaleRoutine = null;
    }

    private void ApplyColorTint(bool hovered)
    {
        if (!enableColorTint || targetGraphic == null) return;
        targetGraphic.color = hovered ? originalColor * hoverColorMultiplier : originalColor;
    }

    private void PlayHoverSound()
    {
        if (hoverSound != null)
        {
            AudioSource.PlayClipAtPoint(hoverSound, Camera.main != null ? Camera.main.transform.position : Vector3.zero);
        }
    }

    private void PlayClickSound()
    {
        if (clickSound != null)
        {
            AudioSource.PlayClipAtPoint(clickSound, Camera.main != null ? Camera.main.transform.position : Vector3.zero);
        }
    }

    /// <summary>
    /// ฟังก์ชันสำหรับเรียกคืนขนาดเดิมทันที
    /// </summary>
    public void ResetToOriginal()
    {
        if (activeScaleRoutine != null)
        {
            StopCoroutine(activeScaleRoutine);
            activeScaleRoutine = null;
        }

        if (rectTransform != null)
        {
            rectTransform.localScale = originalScale;
        }
    }
}
