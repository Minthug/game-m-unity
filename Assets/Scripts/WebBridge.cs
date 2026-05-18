using UnityEngine;
using System.Runtime.InteropServices;

public static class WebBridge
{
#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")] static extern void RequestTextInputJS(string placeholder);
#endif

    public static void RequestTextInput(string placeholder)
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        RequestTextInputJS(placeholder);
#else
        Debug.Log($"[WebBridge] RequestTextInput: {placeholder}");
#endif
    }
}
