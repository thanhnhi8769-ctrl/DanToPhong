using UnityEngine;

/// <summary>Lets UI overlays, chat, and gameplay claim one Escape press in a frame.</summary>
public static class InputEscapeUtility
{
    private static int consumedFrame = -1;

    public static bool TryConsume()
    {
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
        var keyboard = UnityEngine.InputSystem.Keyboard.current;
        bool pressed = keyboard != null && keyboard.escapeKey.wasPressedThisFrame;
#else
        bool pressed = Input.GetKeyDown(KeyCode.Escape);
#endif
        if (!pressed || consumedFrame == Time.frameCount) return false;
        consumedFrame = Time.frameCount;
        return true;
    }
}
