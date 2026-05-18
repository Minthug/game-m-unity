using UnityEngine;

[System.Serializable]
public class SlimeCreateRequest
{
    public string id;
    public string text;
    public string expression;
    public string color;   // "#RRGGBB"
    public float  size;    // 48~95 (px 기준, 내부에서 정규화)
}

public static class ColorUtil
{
    public static Color FromHex(string hex)
    {
        if (string.IsNullOrEmpty(hex)) return Color.white;
        if (!hex.StartsWith('#')) hex = '#' + hex;
        return ColorUtility.TryParseHtmlString(hex, out var c) ? c : Color.white;
    }
}
