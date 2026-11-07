#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

/// <summary>
/// เมนูอำนวยความสะดวกสำหรับทดสอบระบบอีเวนต์จดหมายเชิญไปป่า (Forest Invitation Event) ใน Unity Editor
/// </summary>
public static class ForestInvitationEditorTool
{
    [MenuItem("Tools/Saharan Hall/✉️ ระบบจดหมายเชิญไปป่า/1. บังคับส่งจดหมายเชิญทันที (Force Deliver)", false, 1)]
    public static void ForceDeliverLetter()
    {
        if (!Application.isPlaying)
        {
            EditorUtility.DisplayDialog("แจ้งเตือน", "กรุณากด Play เกมก่อนเพื่อทดสอบการส่งจดหมายและแสดงผลในฉากจริง", "ตกลง");
            return;
        }

        if (ForestInvitationManager.Instance != null)
        {
            ForestInvitationManager.Instance.TestForceDeliverLetter();
            EditorUtility.DisplayDialog("สำเร็จ", "สั่งส่งจดหมายเชิญไปป่าสำเร็จ! ตรวจดูที่โต๊ะรับแขกในตำหนัก", "ตกลง");
        }
        else
        {
            EditorUtility.DisplayDialog("ข้อผิดพลาด", "ไม่พบ ForestInvitationManager ในเกมขณะนี้", "ตกลง");
        }
    }

    [MenuItem("Tools/Saharan Hall/✉️ ระบบจดหมายเชิญไปป่า/2. เปิดหน้าต่างอีเวนต์จดหมายเชิญ (Open Event Dialog)", false, 2)]
    public static void OpenEventDialog()
    {
        if (!Application.isPlaying)
        {
            EditorUtility.DisplayDialog("แจ้งเตือน", "กรุณากด Play เกมก่อนเพื่อทดสอบหน้าต่างอีเวนต์", "ตกลง");
            return;
        }

        if (ForestInvitationManager.Instance != null)
        {
            ForestInvitationManager.Instance.OpenInvitationEventUI();
        }
        else
        {
            EditorUtility.DisplayDialog("ข้อผิดพลาด", "ไม่พบ ForestInvitationManager ในเกมขณะนี้", "ตกลง");
        }
    }

    [MenuItem("Tools/Saharan Hall/✉️ ระบบจดหมายเชิญไปป่า/3. บังคับตอบรับคำเชิญทันที (Force Accept)", false, 3)]
    public static void ForceAcceptInvitation()
    {
        if (!Application.isPlaying)
        {
            EditorUtility.DisplayDialog("แจ้งเตือน", "กรุณากด Play เกมก่อนเพื่อทดสอบการตอบรับคำเชิญ", "ตกลง");
            return;
        }

        if (ForestInvitationManager.Instance != null)
        {
            ForestInvitationManager.Instance.TestForceAcceptInvitation();
            EditorUtility.DisplayDialog("สำเร็จ", "ตอบรับคำเชิญเรียบร้อยแล้ว! รถยนต์หน้าตำหนักถูกปลดล็อคแล้ว", "ตกลง");
        }
        else
        {
            EditorUtility.DisplayDialog("ข้อผิดพลาด", "ไม่พบ ForestInvitationManager ในเกมขณะนี้", "ตกลง");
        }
    }

    [MenuItem("Tools/Saharan Hall/✉️ ระบบจดหมายเชิญไปป่า/4. รีเซ็ตสถานะจดหมายเชิญ (Reset State)", false, 4)]
    public static void ResetInvitationState()
    {
        if (!Application.isPlaying)
        {
            EditorUtility.DisplayDialog("แจ้งเตือน", "กรุณากด Play เกมก่อนเพื่อรีเซ็ตสถานะ", "ตกลง");
            return;
        }

        if (ForestInvitationManager.Instance != null)
        {
            ForestInvitationManager.Instance.TestResetInvitation();
            EditorUtility.DisplayDialog("สำเร็จ", "รีเซ็ตสถานะจดหมายเชิญและล็อครถยนต์กลับสู่สถานะเริ่มต้นเรียบร้อยแล้ว", "ตกลง");
        }
        else
        {
            EditorUtility.DisplayDialog("ข้อผิดพลาด", "ไม่พบ ForestInvitationManager ในเกมขณะนี้", "ตกลง");
        }
    }
}
#endif

