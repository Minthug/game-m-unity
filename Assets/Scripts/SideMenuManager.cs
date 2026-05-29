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

    // 통계
    TextMeshProUGUI   totalLabel;
    TextMeshProUGUI   todayLabel;
    TextMeshProUGUI   streakLabel;
    TextMeshProUGUI[] exprLabels;
    Image[]           exprDots;

    // 탭
    GameObject     statsContent;
    GameObject     diaryContent;
    RectTransform  diaryScrollContent;
    Button         tabStatsBtn;
    Button         tabDiaryBtn;
    enum Tab { Stats, Diary }
    Tab currentTab = Tab.Stats;

    // 설정 오버레이
    RectTransform settingsOverlay;
    GameObject    settingsBackdrop;
    bool          isSettingsOpen;
    Slider        bgmSlider;
    Slider        sfxSlider;

    TMP_FontAsset _korFont;
    TMP_FontAsset KorFont => _korFont != null ? _korFont
        : (_korFont = Resources.Load<TMP_FontAsset>("KoreanFont"));

    const float PANEL_WIDTH  = 270f;
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

        // Safe Area 컨테이너 — 노치/다이나믹 아일랜드 자동 회피
        var safeGO = new GameObject("SafeArea");
        safeGO.transform.SetParent(canvasGO.transform, false);
        var safeRt = safeGO.AddComponent<RectTransform>();
        var area   = Screen.safeArea;
        safeRt.anchorMin = new Vector2(area.x / Screen.width, area.y / Screen.height);
        safeRt.anchorMax = new Vector2((area.x + area.width) / Screen.width,
                                       (area.y + area.height) / Screen.height);
        safeRt.offsetMin = safeRt.offsetMax = Vector2.zero;

        BuildHamburgerButton(safeGO.transform);
        BuildGearButton(safeGO.transform);
        BuildBackdrop(canvasGO.transform);
        BuildSidePanel(canvasGO.transform);
        BuildSettingsOverlay(canvasGO.transform);
    }

    void BuildHamburgerButton(Transform parent)
    {
        var go  = new GameObject("HamburgerBtn"); go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.color = new Color(0.28f, 0.10f, 0.60f, 0.75f);
        var btn = go.AddComponent<Button>();
        btn.onClick.AddListener(Toggle);

        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(1f, 0f);
        rt.pivot     = new Vector2(1f, 0f);
        rt.sizeDelta        = new Vector2(110f, 110f);
        rt.anchoredPosition = new Vector2(-20f, 647f);

        var lbl = MakeTMP(go.transform, "메뉴", 22f);
        lbl.rectTransform.anchorMin = Vector2.zero;
        lbl.rectTransform.anchorMax = Vector2.one;
        lbl.rectTransform.offsetMin = lbl.rectTransform.offsetMax = Vector2.zero;
    }

    void BuildGearButton(Transform parent)
    {
        var go  = new GameObject("GearBtn"); go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.color = new Color(0.28f, 0.10f, 0.60f, 0.75f);
        var btn = go.AddComponent<Button>();
        btn.onClick.AddListener(ToggleSettings);

        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(1f, 0f);
        rt.pivot     = new Vector2(1f, 0f);
        rt.sizeDelta        = new Vector2(110f, 110f);
        rt.anchoredPosition = new Vector2(-20f, 395f);

        var lbl = MakeTMP(go.transform, "설정", 22f);
        lbl.rectTransform.anchorMin = Vector2.zero;
        lbl.rectTransform.anchorMax = Vector2.one;
        lbl.rectTransform.offsetMin = lbl.rectTransform.offsetMax = Vector2.zero;
    }

    void BuildSettingsOverlay(Transform parent)
    {
        // 반투명 배경
        var bdGO = new GameObject("SettingsBackdrop"); bdGO.transform.SetParent(parent, false);
        bdGO.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.5f);
        var bdBtn = bdGO.AddComponent<Button>();
        bdBtn.onClick.AddListener(CloseSettings);
        bdBtn.transition = Selectable.Transition.None;
        Stretch(bdGO.GetComponent<RectTransform>());
        settingsBackdrop = bdGO;
        settingsBackdrop.SetActive(false);

        // 바텀시트 패널
        var panelGO = new GameObject("SettingsPanel"); panelGO.transform.SetParent(parent, false);
        panelGO.AddComponent<Image>().color = new Color(0.07f, 0.07f, 0.10f, 0.97f);
        var panelRt = panelGO.GetComponent<RectTransform>();
        panelRt.anchorMin        = new Vector2(0f, 0f);
        panelRt.anchorMax        = new Vector2(1f, 0f);
        panelRt.pivot            = new Vector2(0.5f, 0f);
        panelRt.sizeDelta        = new Vector2(0f, 420f);
        panelRt.anchoredPosition = new Vector2(0f, -420f); // 화면 밖
        settingsOverlay = panelRt;

        // 헤더
        var hdrGO = new GameObject("Header"); hdrGO.transform.SetParent(panelGO.transform, false);
        var hdrRt = hdrGO.AddComponent<RectTransform>();
        hdrRt.anchorMin = new Vector2(0f, 1f); hdrRt.anchorMax = new Vector2(1f, 1f);
        hdrRt.pivot     = new Vector2(0.5f, 1f);
        hdrRt.sizeDelta = new Vector2(0f, 72f);
        hdrRt.anchoredPosition = Vector2.zero;

        var titleLbl = MakeTMP(hdrGO.transform, "설정", 20f);
        titleLbl.fontStyle = FontStyles.Bold;
        titleLbl.alignment = TextAlignmentOptions.MidlineLeft;
        titleLbl.rectTransform.anchorMin = new Vector2(0f, 0f);
        titleLbl.rectTransform.anchorMax = new Vector2(0.75f, 1f);
        titleLbl.rectTransform.offsetMin = new Vector2(24f, 0f);
        titleLbl.rectTransform.offsetMax = Vector2.zero;

        var closeGO = new GameObject("CloseBtn"); closeGO.transform.SetParent(hdrGO.transform, false);
        closeGO.AddComponent<Image>().color = Color.clear;
        closeGO.AddComponent<Button>().onClick.AddListener(CloseSettings);
        var closeRt = closeGO.GetComponent<RectTransform>();
        closeRt.anchorMin = new Vector2(1f, 0f); closeRt.anchorMax = new Vector2(1f, 1f);
        closeRt.pivot     = new Vector2(1f, 0.5f);
        closeRt.sizeDelta = new Vector2(60f, 0f);
        closeRt.anchoredPosition = new Vector2(-8f, 0f);
        var closeLbl = MakeTMP(closeGO.transform, "X", 22f);
        closeLbl.rectTransform.anchorMin = Vector2.zero;
        closeLbl.rectTransform.anchorMax = Vector2.one;
        closeLbl.rectTransform.offsetMin = closeLbl.rectTransform.offsetMax = Vector2.zero;

        // 콘텐츠 (헤더 아래, 하단 홈 인디케이터 Safe Area 반영)
        float safeBottom = Screen.safeArea.y > 0
            ? (Screen.safeArea.y / Screen.height) * 1920f
            : 40f; // 홈 인디케이터 기본 여백
        var contentGO = new GameObject("Content"); contentGO.transform.SetParent(panelGO.transform, false);
        var contentRt = contentGO.AddComponent<RectTransform>();
        contentRt.anchorMin = new Vector2(0f, 0f);
        contentRt.anchorMax = new Vector2(1f, 1f);
        contentRt.offsetMin = new Vector2(0f, safeBottom);
        contentRt.offsetMax = new Vector2(0f, -72f);

        float y = -24f;
        y = AddSliderRow(contentGO.transform, "BGM 볼륨", y,
            SettingsManager.Instance?.BgmVolume ?? 1f,
            v => SettingsManager.Instance?.SetBgmVolume(v),
            out bgmSlider);
        y = AddSliderRow(contentGO.transform, "효과음 볼륨", y,
            SettingsManager.Instance?.SfxVolume ?? 1f,
            v => SettingsManager.Instance?.SetSfxVolume(v),
            out sfxSlider);
        y -= 24f;
        AddDivider(contentGO.transform, y);
        y -= 20f;
        AddResetButton(contentGO.transform, y, "전체 데이터 초기화",
            new Color(0.55f, 0.08f, 0.08f, 1f),
            () => {
                StatsManager.Instance?.ResetStats();
                SettingsManager.Instance?.ResetAllData();
            });
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
        rt.anchorMin = new Vector2(1f, 0.10f);
        rt.anchorMax = new Vector2(1f, 0.90f);
        rt.pivot     = new Vector2(1f, 0.5f);
        rt.sizeDelta        = new Vector2(PANEL_WIDTH, 0f);
        rt.anchoredPosition = new Vector2(PANEL_WIDTH, 0f); // 화면 밖 (닫힌 상태)
        sidePanel = rt;

        BuildPanelHeader(go.transform);
        BuildTabBar(go.transform);
        BuildStatsContent(go.transform);
        BuildDiaryContent(go.transform);

        SwitchTab(Tab.Stats);
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
        var closeLbl = MakeTMP(closeGO.transform, "X", 22f);
        closeLbl.rectTransform.anchorMin = Vector2.zero;
        closeLbl.rectTransform.anchorMax = Vector2.one;
        closeLbl.rectTransform.offsetMin = closeLbl.rectTransform.offsetMax = Vector2.zero;

        // 타이틀
        var titleLbl = MakeTMP(go.transform, "메뉴", 28f);
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

        tabStatsBtn = MakeTabButton(go.transform, "통계", () => SwitchTab(Tab.Stats));
        tabDiaryBtn = MakeTabButton(go.transform, "일기", () => SwitchTab(Tab.Diary));
    }

    Button MakeTabButton(Transform parent, string label, UnityEngine.Events.UnityAction onClick)
    {
        var go  = new GameObject($"Tab_{label}"); go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.color = new Color(0.20f, 0.20f, 0.26f, 1f);
        var btn = go.AddComponent<Button>();
        btn.onClick.AddListener(onClick);
        btn.transition = Selectable.Transition.None;
        var lbl = MakeTMP(go.transform, label, 20f);
        lbl.fontStyle = FontStyles.Bold;
        lbl.rectTransform.anchorMin = Vector2.zero;
        lbl.rectTransform.anchorMax = Vector2.one;
        lbl.rectTransform.offsetMin = lbl.rectTransform.offsetMax = Vector2.zero;
        return btn;
    }

    float AddSliderRow(Transform parent, string label, float yPos,
                       float initialValue, System.Action<float> onChange, out Slider slider)
    {
        // 레이블
        var lblGO = new GameObject($"Lbl_{label}"); lblGO.transform.SetParent(parent, false);
        var lblTmp = lblGO.AddComponent<TextMeshProUGUI>();
        if (KorFont != null) lblTmp.font = KorFont;
        lblTmp.text      = label;
        lblTmp.fontSize  = 18f;
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
        var lbl = MakeTMP(go.transform, label, 17f);
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
        var hdr = MakeTMP(statsContent.transform, "감정 도감", 17f);
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

        var nameLbl = MakeTMP(go.transform, labelText, 17f);
        nameLbl.color     = new Color(0.65f, 0.65f, 0.75f, 1f);
        nameLbl.alignment = TextAlignmentOptions.MidlineLeft;
        nameLbl.rectTransform.anchorMin = new Vector2(0f, 0f);
        nameLbl.rectTransform.anchorMax = new Vector2(0.5f, 1f);
        nameLbl.rectTransform.offsetMin = new Vector2(16f, 0f);
        nameLbl.rectTransform.offsetMax = Vector2.zero;

        var valLbl = MakeTMP(go.transform, valueText, 26f);
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

        var nameLbl = MakeTMP(go.transform, exprName, 16f);
        nameLbl.color     = new Color(0.75f, 0.75f, 0.82f, 1f);
        nameLbl.alignment = TextAlignmentOptions.MidlineLeft;
        nameLbl.rectTransform.anchorMin = new Vector2(0f, 0f);
        nameLbl.rectTransform.anchorMax = new Vector2(0.55f, 1f);
        nameLbl.rectTransform.offsetMin = new Vector2(26f, 0f);
        nameLbl.rectTransform.offsetMax = Vector2.zero;

        var valLbl = MakeTMP(go.transform, "미발견", 16f);
        valLbl.color      = new Color(0.4f, 0.4f, 0.45f, 1f);
        valLbl.fontStyle  = FontStyles.Bold;
        valLbl.alignment  = TextAlignmentOptions.MidlineRight;
        valLbl.rectTransform.anchorMin = new Vector2(0.55f, 0f);
        valLbl.rectTransform.anchorMax = new Vector2(1f, 1f);
        valLbl.rectTransform.offsetMin = Vector2.zero;
        valLbl.rectTransform.offsetMax = new Vector2(-8f, 0f);
        return (valLbl, dotImg);
    }

    // ── 일기 탭 ──────────────────────────────────────────────────

    void BuildDiaryContent(Transform parent)
    {
        var scrollGO = new GameObject("ScrollView_Diary");
        scrollGO.transform.SetParent(parent, false);
        var scrollRect = scrollGO.AddComponent<ScrollRect>();
        var scrollRt   = scrollGO.GetComponent<RectTransform>();
        scrollRt.anchorMin = new Vector2(0f, 0f);
        scrollRt.anchorMax = new Vector2(1f, 1f);
        scrollRt.offsetMin = new Vector2(0f, 0f);
        scrollRt.offsetMax = new Vector2(0f, -132f);

        var vpGO = new GameObject("Viewport");
        vpGO.transform.SetParent(scrollGO.transform, false);
        vpGO.AddComponent<RectMask2D>();
        var vpRt = vpGO.GetComponent<RectTransform>();
        vpRt.anchorMin = Vector2.zero;
        vpRt.anchorMax = Vector2.one;
        vpRt.offsetMin = vpRt.offsetMax = Vector2.zero;

        var contentGO = new GameObject("Content");
        contentGO.transform.SetParent(vpGO.transform, false);
        var contentRt = contentGO.AddComponent<RectTransform>();
        contentRt.anchorMin = new Vector2(0f, 1f);
        contentRt.anchorMax = new Vector2(1f, 1f);
        contentRt.pivot     = new Vector2(0.5f, 1f);
        contentRt.sizeDelta = Vector2.zero;

        var vlg = contentGO.AddComponent<VerticalLayoutGroup>();
        vlg.spacing               = 8f;
        vlg.padding               = new RectOffset(12, 12, 12, 12);
        vlg.childControlHeight    = true;
        vlg.childForceExpandHeight = false;
        vlg.childControlWidth     = true;
        vlg.childForceExpandWidth = true;
        contentGO.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scrollRect.content    = contentRt;
        scrollRect.viewport   = vpRt;
        scrollRect.horizontal = false;

        diaryScrollContent = contentRt;
        diaryContent = scrollGO;
        diaryContent.SetActive(false);
    }

    void RefreshDiary()
    {
        if (diaryScrollContent == null) return;

        while (diaryScrollContent.childCount > 0)
            DestroyImmediate(diaryScrollContent.GetChild(0).gameObject);

        BuildWeeklyCard(diaryScrollContent);

        var entries = StatsManager.Instance?.GetDiaryEntries();
        if (entries == null || entries.Length == 0)
        {
            var emptyGO = new GameObject("Empty");
            emptyGO.transform.SetParent(diaryScrollContent, false);
            var le = emptyGO.AddComponent<LayoutElement>();
            le.preferredHeight = 80f;
            var tmp = emptyGO.AddComponent<TextMeshProUGUI>();
            if (KorFont != null) tmp.font = KorFont;
            tmp.text      = "아직 기록이 없어요";
            tmp.fontSize  = 17f;
            tmp.color     = new Color(0.5f, 0.5f, 0.6f, 1f);
            tmp.alignment = TextAlignmentOptions.Center;
            return;
        }

        foreach (var entry in entries)
            BuildDiaryCard(diaryScrollContent, entry);
    }

    void BuildWeeklyCard(RectTransform parent)
    {
        var report   = StatsManager.Instance?.GetWeeklyReport();
        int barCount = (report != null && report.totalCount > 0)
            ? Mathf.Min(report.breakdown.Count, 5) : 0;
        float cardH  = barCount > 0 ? 60f + barCount * 32f : 72f;

        var cardGO = new GameObject("WeeklyCard");
        cardGO.transform.SetParent(parent, false);
        cardGO.AddComponent<Image>().color = new Color(0.18f, 0.12f, 0.34f, 0.90f);
        var le = cardGO.AddComponent<LayoutElement>();
        le.preferredHeight = cardH;

        // 금색 좌측 바
        var barGO = new GameObject("GoldBar");
        barGO.transform.SetParent(cardGO.transform, false);
        barGO.AddComponent<Image>().color = new Color(0.95f, 0.78f, 0.18f, 1f);
        var barRt = barGO.GetComponent<RectTransform>();
        barRt.anchorMin = new Vector2(0f, 0f); barRt.anchorMax = new Vector2(0f, 1f);
        barRt.pivot = new Vector2(0f, 0.5f);
        barRt.sizeDelta = new Vector2(4f, 0f);
        barRt.anchoredPosition = Vector2.zero;

        // 헤더
        var now = System.DateTime.Now;
        var hdrTmp = MakeTMP(cardGO.transform, "이번 주 감정 리포트", 17f);
        hdrTmp.fontStyle = FontStyles.Bold;
        hdrTmp.color     = new Color(0.96f, 0.84f, 0.36f, 1f);
        hdrTmp.alignment = TextAlignmentOptions.TopLeft;
        var hdrRt = hdrTmp.rectTransform;
        hdrRt.anchorMin = new Vector2(0f, 1f); hdrRt.anchorMax = new Vector2(1f, 1f);
        hdrRt.pivot     = new Vector2(0.5f, 1f);
        hdrRt.sizeDelta        = new Vector2(0f, 30f);
        hdrRt.anchoredPosition = Vector2.zero;
        hdrRt.offsetMin = new Vector2(14f, hdrRt.offsetMin.y);

        if (report == null || report.totalCount == 0)
        {
            var emptyTmp = MakeTMP(cardGO.transform, "이번 주 아직 기록이 없어요", 15f);
            emptyTmp.color     = new Color(0.62f, 0.58f, 0.78f, 1f);
            emptyTmp.alignment = TextAlignmentOptions.TopLeft;
            var eRt = emptyTmp.rectTransform;
            eRt.anchorMin = new Vector2(0f, 1f); eRt.anchorMax = new Vector2(1f, 1f);
            eRt.pivot     = new Vector2(0.5f, 1f);
            eRt.sizeDelta        = new Vector2(0f, 26f);
            eRt.anchoredPosition = new Vector2(0f, -30f);
            eRt.offsetMin = new Vector2(14f, eRt.offsetMin.y);
            return;
        }

        // 총 N번 라인
        var totalTmp = MakeTMP(cardGO.transform, $"총 {report.totalCount}번 털어냈어요", 15f);
        totalTmp.color     = new Color(0.74f, 0.70f, 0.90f, 1f);
        totalTmp.alignment = TextAlignmentOptions.TopLeft;
        var totRt = totalTmp.rectTransform;
        totRt.anchorMin = new Vector2(0f, 1f); totRt.anchorMax = new Vector2(1f, 1f);
        totRt.pivot     = new Vector2(0.5f, 1f);
        totRt.sizeDelta        = new Vector2(0f, 24f);
        totRt.anchoredPosition = new Vector2(0f, -30f);
        totRt.offsetMin = new Vector2(14f, totRt.offsetMin.y);

        // 바 차트
        int maxCnt = report.breakdown[0].count;
        float rowY = -56f;
        for (int i = 0; i < Mathf.Min(report.breakdown.Count, 5); i++)
        {
            var (expr, cnt) = report.breakdown[i];
            BuildBarRow(cardGO.transform, expr, cnt, maxCnt, rowY);
            rowY -= 32f;
        }
    }

    void BuildBarRow(Transform parent, string expr, int count, int maxCount, float y)
    {
        var rowGO = new GameObject($"Row_{expr}");
        rowGO.transform.SetParent(parent, false);
        var rowRt = rowGO.AddComponent<RectTransform>();
        rowRt.anchorMin = new Vector2(0f, 1f); rowRt.anchorMax = new Vector2(1f, 1f);
        rowRt.pivot     = new Vector2(0.5f, 1f);
        rowRt.sizeDelta        = new Vector2(0f, 26f);
        rowRt.anchoredPosition = new Vector2(0f, y);
        rowRt.offsetMin = new Vector2(14f, rowRt.offsetMin.y);
        rowRt.offsetMax = new Vector2(-14f, rowRt.offsetMax.y);

        // 감정 이름
        var nameTmp = MakeTMP(rowGO.transform, ExprKorean(expr), 14f);
        nameTmp.color     = new Color(0.80f, 0.78f, 0.95f, 1f);
        nameTmp.alignment = TextAlignmentOptions.MidlineLeft;
        var nameRt = nameTmp.rectTransform;
        nameRt.anchorMin = new Vector2(0f, 0f); nameRt.anchorMax = new Vector2(0f, 1f);
        nameRt.pivot     = new Vector2(0f, 0.5f);
        nameRt.sizeDelta        = new Vector2(58f, 0f);
        nameRt.anchoredPosition = Vector2.zero;

        // 바 배경
        var bgGO = new GameObject("BG");
        bgGO.transform.SetParent(rowGO.transform, false);
        bgGO.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0.07f);
        var bgRt = bgGO.GetComponent<RectTransform>();
        bgRt.anchorMin = new Vector2(0f, 0.15f); bgRt.anchorMax = new Vector2(1f, 0.85f);
        bgRt.offsetMin = new Vector2(62f, 0f);
        bgRt.offsetMax = new Vector2(-38f, 0f);

        // 바 채우기
        var fillGO = new GameObject("Fill");
        fillGO.transform.SetParent(bgGO.transform, false);
        fillGO.AddComponent<Image>().color = ExprColor(expr);
        var fillRt = fillGO.GetComponent<RectTransform>();
        fillRt.anchorMin = Vector2.zero;
        fillRt.anchorMax = new Vector2((float)count / maxCount, 1f);
        fillRt.offsetMin = fillRt.offsetMax = Vector2.zero;

        // 횟수
        var cntTmp = MakeTMP(rowGO.transform, $"{count}회", 14f);
        cntTmp.color     = new Color(0.65f, 0.62f, 0.80f, 1f);
        cntTmp.alignment = TextAlignmentOptions.MidlineRight;
        var cntRt = cntTmp.rectTransform;
        cntRt.anchorMin = new Vector2(1f, 0f); cntRt.anchorMax = new Vector2(1f, 1f);
        cntRt.pivot     = new Vector2(1f, 0.5f);
        cntRt.sizeDelta        = new Vector2(34f, 0f);
        cntRt.anchoredPosition = Vector2.zero;
    }

    void BuildDiaryCard(RectTransform parent, StatsManager.DiaryEntry entry)
    {
        var cardGO = new GameObject("DiaryCard");
        cardGO.transform.SetParent(parent, false);
        var cardImg = cardGO.AddComponent<Image>();
        cardImg.color = new Color(1f, 1f, 1f, 0.05f);
        var le = cardGO.AddComponent<LayoutElement>();
        le.preferredHeight = 84f;

        // 좌측 감정 색상 바
        var barGO = new GameObject("Bar");
        barGO.transform.SetParent(cardGO.transform, false);
        var barImg = barGO.AddComponent<Image>();
        barImg.color = ExprColor(entry.expression);
        var barRt = barGO.GetComponent<RectTransform>();
        barRt.anchorMin = new Vector2(0f, 0f);
        barRt.anchorMax = new Vector2(0f, 1f);
        barRt.pivot     = new Vector2(0f, 0.5f);
        barRt.sizeDelta        = new Vector2(4f, 0f);
        barRt.anchoredPosition = Vector2.zero;

        // 감정명 + 시간
        var hdrTmp = MakeTMP(cardGO.transform,
            $"{ExprKorean(entry.expression)}  ·  {FormatTime(entry.timestamp)}", 14f);
        hdrTmp.color     = new Color(0.60f, 0.60f, 0.72f, 1f);
        hdrTmp.alignment = TextAlignmentOptions.TopLeft;
        var hdrRt = hdrTmp.rectTransform;
        hdrRt.anchorMin = new Vector2(0f, 1f);
        hdrRt.anchorMax = new Vector2(1f, 1f);
        hdrRt.pivot     = new Vector2(0.5f, 1f);
        hdrRt.sizeDelta        = new Vector2(0f, 26f);
        hdrRt.anchoredPosition = Vector2.zero;
        hdrRt.offsetMin = new Vector2(14f, hdrRt.offsetMin.y);

        // 본문 텍스트
        var preview = entry.text.Length > 52 ? entry.text.Substring(0, 49) + "…" : entry.text;
        var txtTmp  = MakeTMP(cardGO.transform, preview, 16f);
        txtTmp.color              = new Color(0.90f, 0.88f, 1f, 0.92f);
        txtTmp.alignment          = TextAlignmentOptions.TopLeft;
        txtTmp.enableWordWrapping = true;
        var txtRt = txtTmp.rectTransform;
        txtRt.anchorMin = new Vector2(0f, 0f);
        txtRt.anchorMax = new Vector2(1f, 1f);
        txtRt.offsetMin = new Vector2(14f, 8f);
        txtRt.offsetMax = new Vector2(-8f, -26f);
    }

    static Color ExprColor(string expr) => expr switch
    {
        "angry"     => new Color(0.86f, 0.15f, 0.15f),
        "sad"       => new Color(0.15f, 0.39f, 0.92f),
        "happy"     => new Color(0.58f, 0.22f, 0.90f),
        "surprised" => new Color(0.58f, 0.20f, 0.92f),
        "fear"      => new Color(0.31f, 0.27f, 0.90f),
        "disgust"   => new Color(0.09f, 0.64f, 0.29f),
        "contempt"  => new Color(0.39f, 0.46f, 0.55f),
        _           => new Color(0.42f, 0.45f, 0.50f),
    };

    static string ExprKorean(string expr) => expr switch
    {
        "angry"     => "분노",
        "sad"       => "슬픔",
        "happy"     => "기쁨",
        "surprised" => "놀람",
        "fear"      => "두려움",
        "disgust"   => "혐오",
        "contempt"  => "경멸",
        _           => "무감정",
    };

    static string FormatTime(long ms)
    {
        var epoch = new System.DateTime(1970, 1, 1, 0, 0, 0, System.DateTimeKind.Utc);
        var dt    = epoch.AddMilliseconds(ms).ToLocalTime();
        string today     = System.DateTime.Now.ToString("yyyy-MM-dd");
        string yesterday = System.DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd");
        string date      = dt.ToString("yyyy-MM-dd");
        if (date == today)     return dt.ToString("오늘 HH:mm");
        if (date == yesterday) return dt.ToString("어제 HH:mm");
        return dt.ToString("M월 d일 HH:mm");
    }

    // ── 탭 전환 ──────────────────────────────────────────────────

    void SwitchTab(Tab tab)
    {
        currentTab = tab;
        statsContent?.SetActive(tab == Tab.Stats);
        diaryContent?.SetActive(tab == Tab.Diary);

        var active   = new Color(0.45f, 0.22f, 0.93f, 1f);
        var inactive = new Color(0.20f, 0.20f, 0.26f, 1f);
        SetBtnColor(tabStatsBtn, tab == Tab.Stats  ? active : inactive);
        SetBtnColor(tabDiaryBtn, tab == Tab.Diary  ? active : inactive);

        if (tab == Tab.Stats) RefreshStats();
        if (tab == Tab.Diary) RefreshDiary();
    }

    static void SetBtnColor(Button btn, Color c)
    {
        if (btn == null) return;
        var img = btn.GetComponent<Image>();
        if (img != null) img.color = c;
    }

    // ── 열기/닫기 ────────────────────────────────────────────────

    public void Toggle() { if (isOpen) Close(); else Open(); }

    public void ToggleSettings() { if (isSettingsOpen) CloseSettings(); else OpenSettings(); }

    public void OpenSettings()
    {
        if (isOpen) Close();
        isSettingsOpen = true;
        settingsBackdrop?.SetActive(true);
        StartCoroutine(AnimateSettings(0f));
    }

    public void CloseSettings()
    {
        isSettingsOpen = false;
        StartCoroutine(AnimateSettings(-420f));
    }

    IEnumerator AnimateSettings(float targetY)
    {
        float startY = settingsOverlay.anchoredPosition.y;
        for (float t = 0f; t < 1f; t += Time.deltaTime / ANIM_SECONDS)
        {
            settingsOverlay.anchoredPosition = new Vector2(0f, Mathf.Lerp(startY, targetY, t));
            yield return null;
        }
        settingsOverlay.anchoredPosition = new Vector2(0f, targetY);
        if (!isSettingsOpen) settingsBackdrop?.SetActive(false);
    }

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
