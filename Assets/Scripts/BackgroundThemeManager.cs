using System.Collections.Generic;
using UnityEngine;

public class BackgroundThemeManager : MonoBehaviour
{
    public static BackgroundThemeManager Instance { get; private set; }

    public List<BackgroundTheme> catalog = new();

    string          activeThemeId;
    SpriteRenderer  bgSr;
    HashSet<string> unlocked = new();

    const string PREF_UNLOCKED = "bg_unlocked_v1";
    const string PREF_ACTIVE   = "bg_active";

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;

        foreach (var id in PlayerPrefs.GetString(PREF_UNLOCKED, "").Split(','))
            if (!string.IsNullOrWhiteSpace(id)) unlocked.Add(id.Trim());

        // Inspector 카탈로그가 비어있으면 Resources에서 자동 로드
        if (catalog.Count == 0)
            AutoLoadFromResources();
    }

    void AutoLoadFromResources()
    {
        var loaded = Resources.LoadAll<BackgroundTheme>("BackgroundThemes");
        catalog = new List<BackgroundTheme>(loaded);
        catalog.Sort((a, b) => {
            if (a.themeId == "default") return -1;
            if (b.themeId == "default") return  1;
            return string.Compare(a.themeId, b.themeId, System.StringComparison.Ordinal);
        });
        Debug.Log($"[BgTheme] Resources 자동 로드: {catalog.Count}개");
    }

    void Start()
    {
        var go = new GameObject("BgThemeSprite");
        go.transform.SetParent(transform, false);
        bgSr = go.AddComponent<SpriteRenderer>();
        bgSr.sortingOrder = -30; // 파티클(-10)보다 뒤
        bgSr.color = Color.clear;

        var saved = PlayerPrefs.GetString(PREF_ACTIVE, "");
        if (!string.IsNullOrEmpty(saved)) ApplyTheme(saved, persist: false);
    }

    void ScaleBgToScreen()
    {
        var cam = Camera.main;
        if (cam == null || bgSr == null || bgSr.sprite == null) return;
        float h    = cam.orthographicSize * 2f * 1.1f;
        float w    = h * cam.aspect;
        var   sp   = bgSr.sprite;
        float natW = sp.rect.width  / sp.pixelsPerUnit;
        float natH = sp.rect.height / sp.pixelsPerUnit;
        bgSr.transform.localScale = new Vector3(w / natW, h / natH, 1f);
    }

    // 세로 그라디언트 텍스처 생성 (top = 화면 위쪽)
    static Sprite MakeGradientSprite(Color top, Color bottom, int h = 256)
    {
        var tex = new Texture2D(2, h, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode   = TextureWrapMode.Clamp;
        for (int y = 0; y < h; y++)
        {
            float t = (float)y / (h - 1);
            var c = Color.Lerp(bottom, top, t);
            tex.SetPixel(0, y, c);
            tex.SetPixel(1, y, c);
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, 2, h), new Vector2(0.5f, 0.5f), 1f);
    }

    public bool IsUnlocked(string themeId)
    {
        var t = Find(themeId);
        if (t == null) return false;
        return !t.isAdUnlock || t.defaultUnlocked || unlocked.Contains(themeId);
    }

    public bool IsActive(string themeId) => activeThemeId == themeId;

    public void ApplyTheme(string themeId, bool persist = true)
    {
        if (themeId == "default")
        {
            ClearTheme();
            return;
        }

        var t = Find(themeId);
        if (t == null || !IsUnlocked(themeId)) return;

        activeThemeId = themeId;
        if (bgSr != null)
        {
            if (t.bgSprite != null)
            {
                bgSr.sprite = t.bgSprite;
                bgSr.color  = Color.white;
            }
            else
            {
                // 상단/하단 색으로 그라디언트 생성 (alpha=0.92)
                var top    = new Color(t.bgColor.r,       t.bgColor.g,       t.bgColor.b,       0.92f);
                var bottom = new Color(t.bgColorBottom.r, t.bgColorBottom.g, t.bgColorBottom.b, 0.92f);
                bgSr.sprite = MakeGradientSprite(top, bottom);
                bgSr.color  = Color.white;
            }
            ScaleBgToScreen();
        }

        BackgroundManager.Instance?.SetThemeColorOverride(t.bgColor);
        if (persist) PlayerPrefs.SetString(PREF_ACTIVE, themeId);

        RoomUIManager.Instance?.RefreshBgShop();
    }

    public void ClearTheme()
    {
        activeThemeId = null;
        if (bgSr != null) { bgSr.color = Color.clear; }
        BackgroundManager.Instance?.ClearThemeColorOverride();
        PlayerPrefs.DeleteKey(PREF_ACTIVE);
        RoomUIManager.Instance?.RefreshBgShop();
    }

    public void UnlockByAd(string themeId)
    {
        unlocked.Add(themeId);
        PlayerPrefs.SetString(PREF_UNLOCKED, string.Join(",", unlocked));
        PlayerPrefs.Save();
        ApplyTheme(themeId);
    }

    BackgroundTheme Find(string themeId) =>
        catalog.Find(t => t != null && t.themeId == themeId);
}
