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

        // 1×1 흰색 텍스처 → 단색 풀스크린 배경으로 사용
        var tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();
        bgSr.sprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
        bgSr.color  = Color.clear;
        ScaleBgToScreen();

        var saved = PlayerPrefs.GetString(PREF_ACTIVE, "");
        if (!string.IsNullOrEmpty(saved)) ApplyTheme(saved, persist: false);
    }

    void ScaleBgToScreen()
    {
        var cam = Camera.main;
        if (cam == null || bgSr == null) return;
        float h = cam.orthographicSize * 2f * 1.1f;
        float w = h * cam.aspect;
        bgSr.transform.localScale = new Vector3(w, h, 1f);
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
            // bgSprite가 있으면 그걸 쓰고, 없으면 단색 풀스크린 배경
            if (t.bgSprite != null)
            {
                bgSr.sprite = t.bgSprite;
                bgSr.color  = Color.white;
            }
            else
            {
                var c = t.bgColor;
                bgSr.color = new Color(c.r, c.g, c.b, 0.88f);
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
