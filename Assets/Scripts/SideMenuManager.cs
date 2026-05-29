using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SideMenuManager : MonoBehaviour
{
    public static SideMenuManager Instance { get; private set; }

    Canvas         sideCanvas;
    RectTransform  sidePanel;
    GameObject     backdrop;
    bool           isOpen;

    // 설정
    Slider bgmSlider;
    Slider sfxSlider;

    // 통계
    TextMeshProUGUI   totalLabel;
    TextMeshProUGUI   todayLabel;
    TextMeshProUGUI   streakLabel;
    TextMeshProUGUI[] exprLabels;
    Image[]           exprDots;

    // 탭
    GameObject     settingsContent;
    GameObject     statsContent;
    Button         tabSettingsBtn;
    Button         tabStatsBtn;
    enum Tab { Settings, Stats }
    Tab currentTab = Tab.Settings;

    TMP_FontAsset _korFont;
    TMP_FontAsset KorFont => _korFont != null ? _korFont
        : (_korFont = Resources.Load<TMP_FontAsset>("KoreanFont"));

    const float PANEL_WIDTH  = 380f;
    const float ANIM_SECONDS = 0.22f;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start() => BuildUI();

    // ── UI 생성 ──────────────────────────────────────────────────

    void BuildUI()
    {
        var canvasGO = new GameObject("SideMenuCanvas");
        canvasGO.transform.SetParent(transform, false);
        sideCanvas = canvasGO.AddComponent<Canvas>();
        sideCanvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        sideCanvas.sortingOrder = 30; // RoomCanvas(10), AdOverlay(100) 사이

        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.matchWidthOrHeight  = 0.5f;
        canvasGO.AddComponent<GraphicRaycaster>();

        BuildHamburgerButton(canvasGO.transform);
        BuildBackdrop(canvasGO.transform);
        BuildSidePanel(canvasGO.transform);
    }

    void BuildHamburgerButton(Transform parent)
    {
        var go  = new GameObject("HamburgerBtn"); go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.color = new Color(0.28f, 0.10f, 0.60f, 0.75f);
        var btn = go.AddComponent<Button>();
        btn.onClick.AddListener(Toggle);

        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot     = new Vector2(1f, 1f);
        rt.sizeDelta        = new Vector2(110f, 110f);
        rt.anchoredPosition = new Vector2(-20f, -120f);

        var lbl = MakeTMP(go.transform, "≡", 38f);
        lbl.rectTransform.anchorMin = Vector2.zero;
        lbl.rectTransform.anchorMax = Vector2.one;
        lbl.rectTransform.offsetMin = lbl.rectTransform.offsetMax = Vector2.zero;
    }

    void BuildBackdrop(Transform parent)
    {
        var go  = new GameObject("Backdrop"); go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.color = new Color(0f, 0f, 0f, 0.5f);
        var btn = go.AddComponent<Button>();
        btn.onClick.AddListener(Close);
        btn.transition = Selectable.Transition.None;
        Stretch(go.GetComponent<RectTransform>());
        backdrop = go;
        backdrop.SetActive(false);
    }

    void BuildSidePanel(Transform parent)
    {
        var go  = new GameObject("SidePanel"); go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.color = new Color(0.07f, 0.07f, 0.10f, 0.97f);

        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(1f, 0f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot     = new Vector2(1f, 0.5f);
        rt.sizeDelta        = new Vector2(PANEL_WIDTH, 0f);
        rt.anchoredPosition = new Vector2(PANEL_WIDTH, 0f); // 화면 밖 (닫힌 상태)
        sidePanel = rt;

        BuildPanelHeader(go.transform);
        BuildTabBar(go.transform);
        BuildSettingsContent(go.transform);
        BuildStatsContent(go.transform);

        SwitchTab(Tab.Settings);
    }

    void BuildPanelHeader(Transform parent)
    {
        var go = new GameObject("Header"); go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f); rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot     = new Vector2(0.5f, 1f);
        rt.sizeDelta        = new Vector2(0f, 80f);
        rt.anchoredPosition = Vector2.zero;

        // 닫기 버튼
        var closeGO  = new GameObject("CloseBtn"); closeGO.transform.SetParent(go.transform, false);
        var closeImg = closeGO.AddComponent<Image>();
        closeImg.color = Color.clear;
        var closeBtn = closeGO.AddComponent<Button>();
        closeBtn.onClick.AddListener(Close);
        var closeRt  = closeGO.GetComponent<RectTransform>();
        closeRt.anchorMin = new Vector2(1f, 0f); closeRt.anchorMax = new Vector2(1f, 1f);
        closeRt.pivot     = new Vector2(1f, 0.5f);
        closeRt.sizeDelta        = new Vector2(60f, 0f);
        closeRt.anchoredPosition = new Vector2(-8f, 0f);
        var closeLbl = MakeTMP(closeGO.transform, "✕", 22f);
        closeLbl.rectTransform.anchorMin = Vector2.zero;
        closeLbl.rectTransform.anchorMax = Vector2.one;
        closeLbl.rectTransform.offsetMin = closeLbl.rectTransform.offsetMax = Vector2.zero;

        // 타이틀
        var titleLbl = MakeTMP(go.transform, "메뉴", 20f);
        titleLbl.fontStyle = FontStyles.Bold;
        titleLbl.rectTransform.anchorMin = new Vector2(0f, 0f);
        titleLbl.rectTransform.anchorMax = new Vector2(0.75f, 1f);
        titleLbl.rectTransform.offsetMin = new Vector2(20f, 0f);
        titleLbl.rectTransform.offsetMax = Vector2.zero;
        titleLbl.alignment = TextAlignmentOptions.MidlineLeft;
    }

    void BuildTabBar(Transform parent)
    {
        var go = new GameObject("TabBar"); go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f); rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot     = new Vector2(0.5f, 1f);
        rt.sizeDelta        = new Vector2(0f, 52f);
        rt.anchoredPosition = new Vector2(0f, -80f);

        var hlg = go.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing              = 0f;
        hlg.childForceExpandWidth  = true;
        hlg.childForceExpandHeight = true;

        tabSettingsBtn = MakeTabButton(go.transform, "설정", () => SwitchTab(Tab.Settings));
        tabStatsBtn    = MakeTabButton(go.transform, "통계", () => SwitchTab(Tab.Stats));
    }

    Button MakeTabButton(Transform parent, string label, UnityEngine.Events.UnityAction onClick)
    {
        var go  = new GameObject($"Tab_{label}"); go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.color = new Color(0.20f, 0.20f, 0.26f, 1f);
        var btn = go.AddComponent<Button>();
        btn.onClick.AddListener(onClick);
        btn.transition = Selectable.Transition.None;
        var lbl = MakeTMP(go.transform, label, 15f);
        lbl.fontStyle = FontStyles.Bold;
        lbl.rectTransform.anchorMin = Vector2.zero;
        lbl.rectTransform.anchorMax = Vector2.one;
        lbl.rectTransform.offsetMin = lbl.rectTransform.offsetMax = Vector2.zero;
        return btn;
    }

    void BuildSettingsContent(Transform parent)
    {
        settingsContent = new GameObject("SettingsContent");
        settingsContent.transform.SetParent(parent, false);
        var rt = settingsContent.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 0f); rt.anchorMax = new Vector2(1f, 1f);
        rt.offsetMin = new Vector2(0f, 0f);
        rt.offsetMax = new Vector2(0f, -132f); // header(80) + tabbar(52)

        float y = -24f;

        // BGM 볼륨
        y = AddSliderRow(settingsContent.transform, "BGM 볼륨", y,
            SettingsManager.Instance?.BgmVolume ?? 1f,
            v => SettingsManager.Instance?.SetBgmVolume(v),
            out bgmSlider);

        // 효과음 볼륨
        y = AddSliderRow(settingsContent.transform, "효과음 볼륨", y,
            SettingsManager.Instance?.SfxVolume ?? 1f,
            v => SettingsManager.Instance?.SetSfxVolume(v),
            out sfxSlider);

        // 구분선
        y -= 24f;
        AddDivider(settingsContent.transform, y);
        y -= 20f;

        // 데이터 초기화 버튼
        AddResetButton(settingsContent.transform, y, "전체 데이터 초기화",
            new Color(0.55f, 0.08f, 0.08f, 1f),
            () => {
                StatsManager.Instance?.ResetStats();
                SettingsManager.Instance?.ResetAllData();
            });
    }

    float AddSliderRow(Transform parent, string label, float yPos,
                       float initialValue, System.Action<float> onChange, out Slider slider)
    {
        // 레이블
        var lblGO = new GameObject($"Lbl_{label}"); lblGO.transform.SetParent(parent, false);
        var lblTmp = lblGO.AddComponent<TextMeshProUGUI>();
        if (KorFont != null) lblTmp.font = KorFont;
        lblTmp.text      = label;
        lblTmp.fontSize  = 14f;
        lblTmp.color     = new Color(0.75f, 0.75f, 0.82f, 1f);
        lblTmp.alignment = TextAlignmentOptions.MidlineLeft;
        var lblRt = lblGO.GetComponent<RectTransform>();
        lblRt.anchorMin = new Vector2(0f, 1f); lblRt.anchorMax = new Vector2(1f, 1f);
        lblRt.pivot     = new Vector2(0.5f, 1f);
        lblRt.sizeDelta        = new Vector2(-40f, 30f);
        lblRt.anchoredPosition = new Vector2(0f, yPos);

        yPos -= 34f;

        // 슬라이더
        slider = BuildSlider(parent, yPos, initialValue, onChange);
        yPos -= 56f;

        return yPos;
    }

    Slider BuildSlider(Transform parent, float yPos, float value, System.Action<float> onChange)
    {
        var go = new GameObject("Slider"); go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f); rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot     = new Vector2(0.5f, 1f);
        rt.sizeDelta        = new Vector2(-40f, 40f);
        rt.anchoredPosition = new Vector2(0f, yPos);

        // Background
        var bgGO  = new GameObject("Background"); bgGO.transform.SetParent(go.transform, false);
        var bgImg = bgGO.AddComponent<Image>();
        bgImg.color = new Color(0.15f, 0.15f, 0.20f, 1f);
        var bgRt = bgGO.GetComponent<RectTransform>();
        bgRt.anchorMin = new Vector2(0f, 0.25f); bgRt.anchorMax = new Vector2(1f, 0.75f);
        bgRt.offsetMin = bgRt.offsetMax = Vector2.zero;

        // Fill Area
        var fillAreaGO = new GameObject("Fill Area"); fillAreaGO.transform.SetParent(go.transform, false);
        var fillAreaRt = fillAreaGO.AddComponent<RectTransform>();
        fillAreaRt.anchorMin = new Vector2(0f, 0.25f); fillAreaRt.anchorMax = new Vector2(1f, 0.75f);
        fillAreaRt.offsetMin = new Vector2(5f, 0f); fillAreaRt.offsetMax = new Vector2(-15f, 0f);

        var fillGO  = new GameObject("Fill"); fillGO.transform.SetParent(fillAreaGO.transform, false);
        var fillImg = fillGO.AddComponent<Image>();
        fillImg.color = new Color(0.45f, 0.22f, 0.93f, 1f);
        var fillRt = fillGO.GetComponent<RectTransform>();
        fillRt.anchorMin = Vector2.zero; fillRt.anchorMax = new Vector2(0f, 1f);
        fillRt.offsetMin = fillRt.offsetMax = Vector2.zero;

        // Handle Slide Area
        var handleAreaGO = new GameObject("Handle Slide Area"); handleAreaGO.transform.SetParent(go.transform, false);
        var handleAreaRt = handleAreaGO.AddComponent<RectTransform>();
        handleAreaRt.anchorMin = Vector2.zero; handleAreaRt.anchorMax = Vector2.one;
        handleAreaRt.offsetMin = new Vector2(10f, 0f); handleAreaRt.offsetMax = new Vector2(-10f, 0f);

        var handleGO  = new GameObject("Handle"); handleGO.transform.SetParent(handleAreaGO.transform, false);
        var handleImg = handleGO.AddComponent<Image>();
        handleImg.color = Color.white;
        var handleRt = handleGO.GetComponent<RectTransform>();
        handleRt.anchorMin = new Vector2(0f, 0f); handleRt.anchorMax = new Vector2(0f, 1f);
        handleRt.sizeDelta        = new Vector2(20f, 0f);
        handleRt.anchoredPosition = Vector2.zero;

        var slider = go.AddComponent<Slider>();
        slider.fillRect    = fillRt;
        slider.handleRect  = handleRt;
        slider.targetGraphic = handleImg;
        slider.direction   = Slider.Direction.LeftToRight;
        slider.minValue    = 0f;
        slider.maxValue    = 1f;
        slider.value       = value;
        slider.onValueChanged.AddListener(v => onChange?.Invoke(v));

        return slider;
    }

    void AddDivider(Transform parent, float yPos)
    {
        var go  = new GameObject("Divider"); go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.color = new Color(1f, 1f, 1f, 0.08f);
        var rt  = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f); rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot     = new Vector2(0.5f, 1f);
        rt.sizeDelta        = new Vector2(-32f, 1f);
        rt.anchoredPosition = new Vector2(0f, yPos);
    }

    void AddResetButton(Transform parent, float yPos, string label, Color color, System.Action onClick)
    {
        var go  = new GameObject("ResetBtn"); go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.color = color;
        var btn = go.AddComponent<Button>();
        btn.onClick.AddListener(() => onClick());
        var rt  = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f); rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot     = new Vector2(0.5f, 1f);
        rt.sizeDelta        = new Vector2(-40f, 48f);
        rt.anchoredPosition = new Vector2(0f, yPos);
        var lbl = MakeTMP(go.transform, label, 13f);
        lbl.rectTransform.anchorMin = Vector2.zero;
        lbl.rectTransform.anchorMax = Vector2.one;
        lbl.rectTransform.offsetMin = lbl.rectTransform.offsetMax = Vector2.zero;
    }

    void BuildStatsContent(Transform parent)
    {
        statsContent = new GameObject("StatsContent");
        statsContent.transform.SetParent(parent, false);
        var rt = statsContent.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 0f); rt.anchorMax = new Vector2(1f, 1f);
        rt.offsetMin = new Vector2(0f, 0f);
        rt.offsetMax = new Vector2(0f, -132f);

        float y = -24f;

        totalLabel  = AddStatRow(statsContent.transform, "총 생성",   "0마리", ref y);
        todayLabel  = AddStatRow(statsContent.transform, "오늘 생성", "0마리", ref y);
        streakLabel = AddStatRow(statsContent.transform, "연속 기록", "0일",   ref y);

        y -= 20f;
        AddDivider(statsContent.transform, y);
        y -= 28f;

        // 감정 도감 헤더
        var hdr = MakeTMP(statsContent.transform, "감정 도감", 13f);
        hdr.color     = new Color(0.6f, 0.6f, 0.7f, 1f);
        hdr.alignment = TextAlignmentOptions.MidlineLeft;
        var hdrRt = hdr.rectTransform;
        hdrRt.anchorMin = new Vector2(0f, 1f); hdrRt.anchorMax = new Vector2(1f, 1f);
        hdrRt.pivot     = new Vector2(0.5f, 1f);
        hdrRt.sizeDelta        = new Vector2(-40f, 28f);
        hdrRt.anchoredPosition = new Vector2(0f, y);
        y -= 36f;

        // 감정별 색상 (SlimeManager Palette 상단색 기준)
        var exprs = new[]
        {
            (Expression.Angry,     "분노",   new Color(0.86f, 0.15f, 0.15f, 1f)),
            (Expression.Sad,       "슬픔",   new Color(0.15f, 0.39f, 0.92f, 1f)),
            (Expression.Happy,     "기쁨",   new Color(0.49f, 0.23f, 0.93f, 1f)),
            (Expression.Surprised, "놀람",   new Color(0.58f, 0.20f, 0.92f, 1f)),
            (Expression.Fear,      "두려움", new Color(0.31f, 0.27f, 0.90f, 1f)),
            (Expression.Disgust,   "혐오",   new Color(0.09f, 0.64f, 0.29f, 1f)),
            (Expression.Contempt,  "경멸",   new Color(0.39f, 0.46f, 0.55f, 1f)),
            (Expression.Blank,     "무감정", new Color(0.42f, 0.45f, 0.50f, 1f)),
        };

        exprLabels = new TextMeshProUGUI[exprs.Length];
        exprDots   = new Image[exprs.Length];
        for (int i = 0; i < exprs.Length; i++)
        {
            var (_, name, color) = exprs[i];
            (exprLabels[i], exprDots[i]) = AddExprRow(statsContent.transform, name, color, ref y);
        }
    }

    TextMeshProUGUI AddStatRow(Transform parent, string labelText, string valueText, ref float y)
    {
        var go  = new GameObject($"Row_{labelText}"); go.transform.SetParent(parent, false);
        var rt  = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f); rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot     = new Vector2(0.5f, 1f);
        rt.sizeDelta        = new Vector2(-40f, 52f);
        rt.anchoredPosition = new Vector2(0f, y);
        y -= 58f;

        var bgImg = go.AddComponent<Image>();
        bgImg.color = new Color(1f, 1f, 1f, 0.04f);

        var nameLbl = MakeTMP(go.transform, labelText, 13f);
        nameLbl.color     = new Color(0.65f, 0.65f, 0.75f, 1f);
        nameLbl.alignment = TextAlignmentOptions.MidlineLeft;
        nameLbl.rectTransform.anchorMin = new Vector2(0f, 0f);
        nameLbl.rectTransform.anchorMax = new Vector2(0.5f, 1f);
        nameLbl.rectTransform.offsetMin = new Vector2(16f, 0f);
        nameLbl.rectTransform.offsetMax = Vector2.zero;

        var valLbl = MakeTMP(go.transform, valueText, 22f);
        valLbl.fontStyle  = FontStyles.Bold;
        valLbl.alignment  = TextAlignmentOptions.MidlineRight;
        valLbl.rectTransform.anchorMin = new Vector2(0.5f, 0f);
        valLbl.rectTransform.anchorMax = new Vector2(1f, 1f);
        valLbl.rectTransform.offsetMin = Vector2.zero;
        valLbl.rectTransform.offsetMax = new Vector2(-16f, 0f);
        return valLbl;
    }

    (TextMeshProUGUI label, Image dot) AddExprRow(Transform parent, string exprName, Color dotColor, ref float y)
    {
        var go  = new GameObject($"Expr_{exprName}"); go.transform.SetParent(parent, false);
        var rt  = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f); rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot     = new Vector2(0.5f, 1f);
        rt.sizeDelta        = new Vector2(-40f, 38f);
        rt.anchoredPosition = new Vector2(0f, y);
        y -= 42f;

        // 색상 도트
        var dotGO  = new GameObject("Dot"); dotGO.transform.SetParent(go.transform, false);
        var dotImg = dotGO.AddComponent<Image>();
        dotImg.color = dotColor;
        var dotRt = dotGO.GetComponent<RectTransform>();
        dotRt.anchorMin        = new Vector2(0f, 0.5f);
        dotRt.anchorMax        = new Vector2(0f, 0.5f);
        dotRt.pivot            = new Vector2(0f, 0.5f);
        dotRt.sizeDelta        = new Vector2(10f, 10f);
        dotRt.anchoredPosition = new Vector2(8f, 0f);

        var nameLbl = MakeTMP(go.transform, exprName, 13f);
        nameLbl.color     = new Color(0.75f, 0.75f, 0.82f, 1f);
        nameLbl.alignment = TextAlignmentOptions.MidlineLeft;
        nameLbl.rectTransform.anchorMin = new Vector2(0f, 0f);
        nameLbl.rectTransform.anchorMax = new Vector2(0.55f, 1f);
        nameLbl.rectTransform.offsetMin = new Vector2(26f, 0f);
        nameLbl.rectTransform.offsetMax = Vector2.zero;

        var valLbl = MakeTMP(go.transform, "미발견", 13f);
        valLbl.color      = new Color(0.4f, 0.4f, 0.45f, 1f);
        valLbl.fontStyle  = FontStyles.Bold;
        valLbl.alignment  = TextAlignmentOptions.MidlineRight;
        valLbl.rectTransform.anchorMin = new Vector2(0.55f, 0f);
        valLbl.rectTransform.anchorMax = new Vector2(1f, 1f);
        valLbl.rectTransform.offsetMin = Vector2.zero;
        valLbl.rectTransform.offsetMax = new Vector2(-8f, 0f);
        return (valLbl, dotImg);
    }

    // ── 탭 전환 ──────────────────────────────────────────────────

    void SwitchTab(Tab tab)
    {
        currentTab = tab;
        settingsContent?.SetActive(tab == Tab.Settings);
        statsContent?.SetActive(tab == Tab.Stats);

        var active   = new Color(0.45f, 0.22f, 0.93f, 1f);
        var inactive = new Color(0.20f, 0.20f, 0.26f, 1f);
        SetBtnColor(tabSettingsBtn, tab == Tab.Settings ? active : inactive);
        SetBtnColor(tabStatsBtn,    tab == Tab.Stats    ? active : inactive);

        if (tab == Tab.Stats) RefreshStats();
    }

    static void SetBtnColor(Button btn, Color c)
    {
        if (btn == null) return;
        var img = btn.GetComponent<Image>();
        if (img != null) img.color = c;
    }

    // ── 열기/닫기 ────────────────────────────────────────────────

    public void Toggle() { if (isOpen) Close(); else Open(); }

    public void Open()
    {
        isOpen = true;
        backdrop?.SetActive(true);
        StopAllCoroutines();
        StartCoroutine(Animate(0f));
        if (currentTab == Tab.Stats) RefreshStats();
    }

    public void Close()
    {
        isOpen = false;
        StopAllCoroutines();
        StartCoroutine(Animate(PANEL_WIDTH));
    }

    IEnumerator Animate(float targetX)
    {
        float startX = sidePanel.anchoredPosition.x;
        for (float t = 0f; t < 1f; t += Time.deltaTime / ANIM_SECONDS)
        {
            sidePanel.anchoredPosition = new Vector2(Mathf.Lerp(startX, targetX, t), 0f);
            yield return null;
        }
        sidePanel.anchoredPosition = new Vector2(targetX, 0f);
        if (!isOpen) backdrop?.SetActive(false);
    }

    // ── 통계 갱신 ────────────────────────────────────────────────

    public void RefreshStats()
    {
        var stats = StatsManager.Instance;
        if (stats == null) return;

        if (totalLabel  != null) totalLabel.text  = $"{stats.TotalCount}마리";
        if (todayLabel  != null) todayLabel.text  = $"{stats.TodayCount}마리";
        if (streakLabel != null) streakLabel.text = stats.Streak > 0 ? $"{stats.Streak}일 🔥" : "0일";

        if (exprLabels == null) return;
        var exprs = new[]
        {
            Expression.Angry, Expression.Sad,     Expression.Happy,    Expression.Surprised,
            Expression.Fear,  Expression.Disgust, Expression.Contempt, Expression.Blank,
        };
        for (int i = 0; i < exprs.Length && i < exprLabels.Length; i++)
        {
            int count = stats.GetExpressionCount(exprs[i]);
            if (exprLabels[i] != null)
            {
                if (count == 0)
                {
                    exprLabels[i].text  = "미발견";
                    exprLabels[i].color = new Color(0.4f, 0.4f, 0.45f, 1f);
                }
                else
                {
                    exprLabels[i].text  = $"{count}번";
                    exprLabels[i].color = Color.white;
                }
            }
            // 미발견이면 도트 dim 처리
            if (exprDots != null && i < exprDots.Length && exprDots[i] != null)
            {
                var c = exprDots[i].color;
                exprDots[i].color = new Color(c.r, c.g, c.b, count == 0 ? 0.25f : 1f);
            }
        }
    }

    // ── 유틸 ────────────────────────────────────────────────────

    TextMeshProUGUI MakeTMP(Transform parent, string text, float size)
    {
        var go  = new GameObject("Txt"); go.transform.SetParent(parent, false);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        if (KorFont != null) tmp.font = KorFont;
        tmp.text      = text;
        tmp.fontSize  = size;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color     = Color.white;
        return tmp;
    }

    static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }
}
