using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// อ่านปุ่มแบบ Input.GetKeyDown(KeyCode...) แต่ใช้ระบบ Input System ใหม่
/// โปรเจกต์ตั้ง Active Input Handling = Input System อย่างเดียว → Input.GetKeyDown แบบเก่าจะ error ทุกเฟรม และกดไม่ติด
/// ใช้แทนได้ตรงๆ: Input.GetKeyDown(KeyCode.Space) → InputCompat.Down(KeyCode.Space)
/// </summary>
public static class InputCompat
{
    public static bool Down(KeyCode code)
    {
        var kb = Keyboard.current;
        if (kb == null) return false;
        Key key = ToKey(code);
        if (key == Key.None) return false;
        return kb[key].wasPressedThisFrame;
    }

    /// <summary>0 = ซ้าย, 1 = ขวา, 2 = กลาง</summary>
    public static bool MouseDown(int button)
    {
        var m = Mouse.current;
        if (m == null) return false;
        switch (button)
        {
            case 0: return m.leftButton.wasPressedThisFrame;
            case 1: return m.rightButton.wasPressedThisFrame;
            case 2: return m.middleButton.wasPressedThisFrame;
            default: return false;
        }
    }

    static Key ToKey(KeyCode code)
    {
        if (code >= KeyCode.Alpha0 && code <= KeyCode.Alpha9) return Key.Digit0 + (code - KeyCode.Alpha0);
        if (code >= KeyCode.Keypad0 && code <= KeyCode.Keypad9) return Key.Numpad0 + (code - KeyCode.Keypad0);
        switch (code)
        {
            case KeyCode.Return: return Key.Enter;
            case KeyCode.KeypadEnter: return Key.NumpadEnter;
            case KeyCode.LeftControl: return Key.LeftCtrl;
            case KeyCode.RightControl: return Key.RightCtrl;
            case KeyCode.BackQuote: return Key.Backquote;
        }
        Key k;
        return System.Enum.TryParse(code.ToString(), true, out k) ? k : Key.None;
    }
}
