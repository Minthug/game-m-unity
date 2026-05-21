using UnityEngine;

[System.Serializable]
public class SlimeCreateRequest
{
    public string id;
    public string text;
    public string expression;
    public string color;   // "#RRGGBB"
    public float  size;    // 48~95 (px 기준, 내부에서 정규화)
    public int    stage;   // 1·2·3 (0이면 기본 1단계)
}

[System.Serializable]
public class SlimeSaveEntry
{
    public string id;
    public string expression;
    public string color;
    public int    stage;
    public string text;
    public long   createdAt; // Unix ms
}

[System.Serializable]
public class SlimeSaveData
{
    public System.Collections.Generic.List<SlimeSaveEntry> slimes = new();
}

public static class ColorUtil
{
    public static Color FromHex(string hex)
    {
        if (string.IsNullOrEmpty(hex)) return Color.white;
        if (!hex.StartsWith('#')) hex = '#' + hex;
        return ColorUtility.TryParseHtmlString(hex, out var c) ? c : Color.white;
    }

    public static string ToHex(Color c) => "#" + ColorUtility.ToHtmlStringRGB(c);

    public static long NowMs() =>
        (long)(System.DateTime.UtcNow - new System.DateTime(1970, 1, 1, 0, 0, 0, System.DateTimeKind.Utc)).TotalMilliseconds;
}
