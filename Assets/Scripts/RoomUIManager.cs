using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class RoomUIManager : MonoBehaviour
{
    public static RoomUIManager Instance { get; private set; }

    [Header("UI 참조 (Setup Scene이 자동 생성)")]
    public Canvas        rootCanvas;
    public GameObject    shopPanel;
    public RectTransform itemGrid;
    public Button        openShopBtn;
    public Button        closeShopBtn;

    [Header("탭")]
    public Button     tabDecorBtn;
    public Button     tabBgBtn;
    public GameObject scrollViewDecor;
    public GameObject scrollViewBg;

    [Header("배경 테마")]
    public RectTransform bgItemGrid;
    public GameObject    bgThemeButtonPrefab;

    [Header("프리팹")]
    public GameObject itemButtonPrefab;

    enum ShopTab { Decor, BgTheme }
    ShopTab    currentTab    = ShopTab.Decor;
    bool       shopOpen;
    Expression currentEmotion = Expression.Blank;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        // 방꾸미기 버튼 비활성화 (핵심 경험 집중)
        if (openShopBtn != null) openShopBtn.gameObject.SetActive(false);

        // SceneSetup의 AddListener는 씬에 저장 안 되므로 런타임에 연결
        if (closeShopBtn != null) closeShopBtn.onClick.AddListener(CloseShop);
        if (tabDecorBtn  != null) tabDecorBtn.onClick.AddListener(() => SwitchTab(ShopTab.Decor));
        if (tabBgBtn     != null) tabBgBtn.onClick.AddListener(() => SwitchTab(ShopTab.BgTheme));

        UpdateTabColors();  // 초기 탭 색상 설정
        RefreshShop();      // 버튼 먼저 생성
        ApplyKoreanFont();  // 생성된 버튼 포함 전체 폰트 적용

        // 애니메이션 없이 즉시 숨김 — AnimatePanel 사용 시 0.2s flash 발생
        if (shopPanel != null)
        {
            var rect = shopPanel.GetComponent<RectTransform>();
            float h = rect.sizeDelta.y > 0 ? rect.sizeDelta.y : 420f;
            rect.anchoredPosition = new Vector2(0f, -h);
            shopPanel.SetActive(false);
        }
        shopOpen = false;
    }

    TMPro.TMP_FontAsset _korFont;

    TMPro.TMP_FontAsset KorFont =>
        _korFont != null ? _korFont : (_korFont = Resources.Load<TMPro.TMP_FontAsset>("KoreanFont"));

    void ApplyKoreanFont()
    {
        if (KorFont == null) { Debug.LogWarning("[RoomUI] KoreanFont 없음 — Game-M/0. Setup Korean Font 먼저 실행"); return; }
        var root = rootCanvas != null ? rootCanvas.transform : transform.root;
        foreach (var tmp in root.GetComponentsInChildren<TMPro.TextMeshProUGUI>(true))
            tmp.font = KorFont;
    }

    // 한글 텍스트 설정 전에 개별 오브젝트에 폰트 적용
    void ApplyKoreanFontTo(GameObject go)
    {
        if (KorFont == null) return;
        foreach (var tmp in go.GetComponentsInChildren<TMPro.TextMeshProUGUI>(true))
            tmp.font = KorFont;
    }

    static Sprite MakeGradientSprite(Color top, Color bottom, int h = 64)
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

    // ── 상점 열기/닫기 ─────────────────────────────────────────────

    public void OpenShop()
    {
        if (currentTab == ShopTab.Decor) RefreshShop();
        else                             RefreshBgShop();
        ApplyKoreanFont();
        SetShopVisible(true);
    }
    public void CloseShop()  => SetShopVisible(false);
    public void ToggleShop() { if (shopOpen) CloseShop(); else OpenShop(); }

    void SetShopVisible(bool visible)
    {
        shopOpen = visible;
        if (shopPanel != null)
        {
            StopAllCoroutines();
            StartCoroutine(AnimatePanel(visible));
        }
    }

    IEnumerator AnimatePanel(bool visible)
    {
        var rect  = shopPanel.GetComponent<RectTransform>();
        float h   = rect.rect.height > 0 ? rect.rect.height : 400f;
        float from = visible ? -h : 0f;
        float to   = visible ? 0f : -h;

        shopPanel.SetActive(true);
        for (float t = 0f; t < 1f; t += Time.deltaTime / 0.2f)
        {
            rect.anchoredPosition = new Vector2(0f, Mathf.Lerp(from, to, t));
            yield return null;
        }
        rect.anchoredPosition = new Vector2(0f, to);
        if (!visible) shopPanel.SetActive(false);
    }

    // ── 상점 목록 갱신 ────────────────────────────────────────────

    public void RefreshShop()
    {
        Debug.Log($"[RoomUI] RefreshShop — itemGrid:{itemGrid != null}, prefab:{itemButtonPrefab != null}, RoomMgr:{RoomManager.Instance != null}");
        if (itemGrid == null) { Debug.LogError("[RoomUI] itemGrid 없음 — Setup Scene 재실행 필요"); return; }
        if (itemButtonPrefab == null) { Debug.LogError("[RoomUI] itemButtonPrefab 없음 — Setup Scene 재실행 필요"); return; }

        foreach (Transform child in itemGrid)
            Destroy(child.gameObject);

        var catalog = RoomManager.Instance?.itemCatalog;
        if (catalog == null || catalog.Count == 0)
        {
            Debug.LogWarning("[RoomUI] 카탈로그 비어있음 — Game-M/Refresh Room Catalog 실행 필요");
            return;
        }
        Debug.Log($"[RoomUI] 상점 갱신: {catalog.Count}개");

        foreach (var item in catalog)
        {
            if (item == null) continue;
            var btn = Instantiate(itemButtonPrefab, itemGrid);
            ApplyKoreanFontTo(btn); // 한글 텍스트 설정 전에 폰트 먼저 적용
            SetupItemButton(btn, item);
        }

        // 레이아웃 즉시 재계산 (ContentSizeFitter가 다음 프레임까지 기다리지 않도록)
        UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(itemGrid);
    }

    void SetupItemButton(GameObject btn, RoomItem item)
    {
        // 카드 배경색을 현재 감정 색상으로
        var bg = btn.GetComponent<Image>();
        if (bg != null) { bg.sprite = null; bg.color = CurrentCardColor(); }

        // 아이콘 (previewIcon 우선, 없으면 sprite, 둘 다 없으면 placeholder)
        var icon = btn.transform.Find("Icon")?.GetComponent<Image>();
        if (icon != null)
        {
            if (item.ShopIcon != null)
            {
                icon.sprite = item.ShopIcon;
                icon.color  = Color.white;
            }
            else
            {
                icon.sprite = null;
                icon.color  = new Color(0.25f, 0.25f, 0.30f); // 아이콘 없는 3D 아이템 placeholder
            }
        }

        // 이름
        var label = btn.transform.Find("Label")?.GetComponent<TextMeshProUGUI>();
        if (label != null)
            label.text = item.displayName;

        // 가격
        var price = btn.transform.Find("Price")?.GetComponent<TextMeshProUGUI>();
        if (price != null)
            price.text = item.price == 0 ? "무료" : $"{item.price}원";

        // 잠금 오버레이
        var lockObj = btn.transform.Find("Lock")?.gameObject;
        if (lockObj != null)
            lockObj.SetActive(!item.isUnlocked);

        // 버튼 클릭
        var button = btn.GetComponent<Button>();
        if (button != null)
        {
            var captured = item;
            button.onClick.AddListener(() => OnItemClicked(captured));
            button.interactable = item.isUnlocked;
        }
    }

    void OnItemClicked(RoomItem item)
    {
        var cam = Camera.main;
        float hw = cam != null ? cam.orthographicSize * cam.aspect * 0.5f : 2f;
        float hh = cam != null ? cam.orthographicSize * 0.5f : 2f;
        var pos = new Vector3(
            UnityEngine.Random.Range(-hw, hw),
            UnityEngine.Random.Range(-hh, hh),
            0f);
        RoomManager.Instance?.PlaceItem(item.itemId, pos);
        CloseShop();
    }

    // ── 탭 전환 ──────────────────────────────────────────────────

    void SwitchTab(ShopTab tab)
    {
        currentTab = tab;
        if (scrollViewDecor != null) scrollViewDecor.SetActive(tab == ShopTab.Decor);
        if (scrollViewBg    != null) scrollViewBg.SetActive(tab == ShopTab.BgTheme);

        UpdateTabColors();

        if (tab == ShopTab.Decor) RefreshShop();
        else                      RefreshBgShop();
    }

    void UpdateTabColors()
    {
        var active   = new Color(0.45f, 0.22f, 0.93f, 1f);
        var inactive = new Color(0.20f, 0.20f, 0.26f, 1f);

        SetBtnColor(tabDecorBtn, currentTab == ShopTab.Decor   ? active : inactive);
        SetBtnColor(tabBgBtn,    currentTab == ShopTab.BgTheme ? active : inactive);
    }

    static void SetBtnColor(Button btn, Color c)
    {
        if (btn == null) return;
        var img = btn.GetComponent<Image>();
        if (img != null) img.color = c;
    }

    // ── 배경 테마 탭 ─────────────────────────────────────────────

    public void RefreshBgShop()
    {
        if (bgItemGrid == null || bgThemeButtonPrefab == null) return;

        // BackgroundThemeManager가 씬에 없으면 자동 생성
        if (BackgroundThemeManager.Instance == null)
            new GameObject("BackgroundThemeManager").AddComponent<BackgroundThemeManager>();

        foreach (Transform child in bgItemGrid)
            Destroy(child.gameObject);

        var catalog = BackgroundThemeManager.Instance?.catalog;
        if (catalog == null || catalog.Count == 0)
        {
            Debug.LogWarning("[RoomUI] 배경 테마 카탈로그 비어있음 — Assets/Resources/BackgroundThemes/ 확인 필요");
            return;
        }

        foreach (var theme in catalog)
        {
            if (theme == null) continue;
            var btn = Instantiate(bgThemeButtonPrefab, bgItemGrid);
            ApplyKoreanFontTo(btn); // 한글 텍스트 설정 전에 폰트 먼저 적용
            SetupBgThemeButton(btn, theme);
        }

        UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(bgItemGrid);
        ApplyKoreanFont();
    }

    void SetupBgThemeButton(GameObject btn, BackgroundTheme theme)
    {
        bool isUnlocked = BackgroundThemeManager.Instance?.IsUnlocked(theme.themeId) ?? false;
        bool isActive   = BackgroundThemeManager.Instance?.IsActive(theme.themeId) ?? false;

        // 카드 배경 = 테마 그라디언트 프리뷰
        var bg = btn.GetComponent<Image>();
        if (bg != null)
        {
            bg.sprite = MakeGradientSprite(theme.bgColor, theme.bgColorBottom, 64);
            bg.color  = Color.white;
        }

        // 카드 루트 Button은 raycast만 막지 않도록 비활성화
        var rootBtn = btn.GetComponent<Button>();
        if (rootBtn != null) rootBtn.enabled = false;

        // 이름
        var label = btn.transform.Find("Label")?.GetComponent<TextMeshProUGUI>();
        if (label != null) label.text = theme.displayName;

        // 잠금 오버레이 — raycastTarget 끄기 (ActionBtn 클릭 가로채지 않도록)
        var lockObj = btn.transform.Find("Lock");
        if (lockObj != null)
        {
            lockObj.gameObject.SetActive(!isUnlocked);
            foreach (var img in lockObj.GetComponentsInChildren<Image>(true))
                img.raycastTarget = false;
        }

        // 액션 버튼
        var actionBtn   = btn.transform.Find("ActionBtn")?.GetComponent<Button>();
        var actionLabel = btn.transform.Find("ActionBtn/Text")?.GetComponent<TextMeshProUGUI>();

        if (actionBtn == null)
        {
            Debug.LogWarning("[RoomUI] BgThemeButton에 'ActionBtn' 자식이 없음 — Recreate Room UI 필요");
            return;
        }
        actionBtn.onClick.RemoveAllListeners();

        if (isActive)
        {
            if (actionLabel != null) actionLabel.text = "✓ 사용 중";
            actionBtn.interactable = false;
        }
        else if (isUnlocked)
        {
            if (actionLabel != null) actionLabel.text = theme.themeId == "default" ? "기본으로" : "적용";
            actionBtn.interactable = true;
            var captured = theme;
            actionBtn.onClick.AddListener(() =>
                BackgroundThemeManager.Instance?.ApplyTheme(captured.themeId));
        }
        else if (theme.isAdUnlock)
        {
            if (actionLabel != null) actionLabel.text = "광고 보기";
            actionBtn.interactable = true;
            var captured = theme;
            actionBtn.onClick.AddListener(() => {
                // AdManager가 없으면 자동 생성
                if (AdManager.Instance == null)
                    new GameObject("AdManager").AddComponent<AdManager>();
                AdManager.Instance.ShowRewardedAd(success => {
                    if (success) BackgroundThemeManager.Instance?.UnlockByAd(captured.themeId);
                });
            });
        }
    }

    // ── 감정 연동 색상 ────────────────────────────────────────────

    public void ApplyEmotion(Expression e)
    {
        currentEmotion = e;

        var (panel, btn, btnClose, card) = EmotionUIColors(e);

        if (shopPanel != null)
        {
            var img = shopPanel.GetComponent<Image>();
            if (img != null) { img.sprite = null; img.color = panel; }
        }
        if (openShopBtn != null)
        {
            var img = openShopBtn.GetComponent<Image>();
            if (img != null) { img.sprite = null; img.color = btn; }
        }
        if (closeShopBtn != null)
        {
            var img = closeShopBtn.GetComponent<Image>();
            if (img != null) { img.sprite = null; img.color = btnClose; }
        }

        // 열려있는 상태라면 아이템 카드도 즉시 갱신
        if (shopOpen) RefreshShop();
    }

    Color CurrentCardColor()
    {
        var (_, _, _, card) = EmotionUIColors(currentEmotion);
        return card;
    }

    static (Color panel, Color btn, Color btnClose, Color card) EmotionUIColors(Expression e) => e switch
    {
        Expression.Angry     => (Hex("#200500", 0.97f), Hex("#3A0800"), Hex("#220400"), Hex("#2E0600")),
        Expression.Sad       => (Hex("#001030", 0.97f), Hex("#001844"), Hex("#000D20"), Hex("#001438")),
        Expression.Fear      => (Hex("#100020", 0.97f), Hex("#1A0038"), Hex("#0D0028"), Hex("#150030")),
        Expression.Happy     => (Hex("#140C28", 0.97f), Hex("#1E1040"), Hex("#110830"), Hex("#1A1035")),
        Expression.Disgust   => (Hex("#041208", 0.97f), Hex("#061A0A"), Hex("#030C05"), Hex("#051408")),
        Expression.Surprised => (Hex("#0E0030", 0.97f), Hex("#160048"), Hex("#0A001C"), Hex("#120038")),
        Expression.Contempt  => (Hex("#0A0E18", 0.97f), Hex("#121828"), Hex("#080C14"), Hex("#0E1420")),
        _                    => (Hex("#0F0F18", 0.97f), Hex("#181820"), Hex("#101018"), Hex("#141420")),
    };

    static Color Hex(string hex, float a = 1f)
    {
        ColorUtility.TryParseHtmlString(hex, out var c);
        c.a = a;
        return c;
    }
}
