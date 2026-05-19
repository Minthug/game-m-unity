using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using TMPro;

public class OnboardingManager : MonoBehaviour
{
    static readonly string[] Questions =
    {
        "지금 마음이\n어때요?",
        "요즘 자주\n드는 감정은요?",
        "요즘 가장\n힘든 건 뭐예요?",
    };

    static readonly (string label, string bgHex)[,] Choices =
    {
        { ("우울해요", "#060E20"), ("기뻐요", "#1C0A00"), ("아무렇지않아요", "#111116") },
        { ("지쳐있어요", "#0D0820"), ("설레요", "#0A1A0D"), ("그냥그래요", "#0F0F14") },
        { ("관계가 힘들어요", "#12060A"), ("내 자신이요", "#060E1A"), ("딱히 없어요", "#0A100A") },
    };

    Camera mainCam;
    Color targetBg;
    int step;
    bool shouldSkip;

    GameObject questionPanel;
    TextMeshProUGUI questionText;
    Button[] choiceButtons;
    TextMeshProUGUI[] choiceTexts;
    GameObject finalPanel;
    TMP_InputField inputField;
    Image fadeOverlay;

    void Awake()
    {
        shouldSkip = PlayerPrefs.GetInt("onboarding_done", 0) == 1;
        if (!shouldSkip) mainCam = Camera.main;
    }

    void Start()
    {
        if (shouldSkip) { SceneManager.LoadScene("Main"); return; }

        targetBg = HexColor("#0A0A0F");
        mainCam.backgroundColor = targetBg;
        BuildSmokeParticles();
        BuildUI();
        ShowQuestion(0);
    }

    void Update()
    {
        if (shouldSkip || mainCam == null) return;
        mainCam.backgroundColor = Color.Lerp(mainCam.backgroundColor, targetBg, Time.deltaTime * 1.5f);
    }

    // ── 질문 흐름 ───────────────────────────────────────────────
    void ShowQuestion(int idx)
    {
        questionPanel.SetActive(true);
        finalPanel.SetActive(false);
        questionText.text = Questions[idx];
        for (int i = 0; i < 3; i++)
        {
            int ci = i;
            choiceTexts[i].text = Choices[idx, i].label;
            choiceButtons[i].onClick.RemoveAllListeners();
            choiceButtons[i].onClick.AddListener(() => OnChoice(ci));
        }
    }

    void OnChoice(int ci)
    {
        targetBg = HexColor(Choices[step, ci].bgHex);
        step++;
        if (step < Questions.Length) ShowQuestion(step);
        else ShowFinal();
    }

    void ShowFinal()
    {
        questionPanel.SetActive(false);
        finalPanel.SetActive(true);
        if (inputField != null) inputField.Select();
        // WebGL 빌드에서는 React 레이어도 트리거
        WebBridge.RequestTextInput("지금 꼭 내뱉고 싶은 말은?");
    }

    // ── 완료 버튼 (Unity InputField) ────────────────────────────
    void OnSubmitButtonClicked()
    {
        var text = inputField != null ? inputField.text.Trim() : "";
        SaveAndLoad(text, skip: false);
    }

    // ── Web → Unity (WebGL SendMessage) ─────────────────────────
    public void SubmitText(string json)
    {
        var d = JsonUtility.FromJson<SubmitData>(json);
        SaveAndLoad(d.skip ? "" : d.text, d.skip);
    }

    public void SkipOnboarding()
    {
        SaveAndLoad("", skip: true);
    }

    void SaveAndLoad(string text, bool skip)
    {
        if (!skip && !string.IsNullOrWhiteSpace(text))
        {
            PlayerPrefs.SetString("first_slime_text", text);
            PlayerPrefs.SetString("first_slime_expression",
                EmotionDetector.Detect(text).ToString().ToLower());
        }
        PlayerPrefs.SetInt("onboarding_done", 1);
        PlayerPrefs.Save();
        StartCoroutine(FadeAndLoad());
    }

    IEnumerator FadeAndLoad()
    {
        if (fadeOverlay != null)
        {
            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime * 2.5f;
                fadeOverlay.color = new Color(0f, 0f, 0f, Mathf.Clamp01(t));
                yield return null;
            }
        }
        SceneManager.LoadScene("Main");
    }

    // ── UI 빌드 (런타임) ────────────────────────────────────────
    void BuildUI()
    {
        var canvasGO = new GameObject("Canvas");
        var cv = canvasGO.AddComponent<Canvas>();
        cv.renderMode = RenderMode.ScreenSpaceOverlay;
        cv.sortingOrder = 10;
        var cs = canvasGO.AddComponent<CanvasScaler>();
        cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        cs.referenceResolution = new Vector2(390f, 844f);
        cs.matchWidthOrHeight = 0.5f;
        canvasGO.AddComponent<GraphicRaycaster>();

        if (FindFirstObjectByType<EventSystem>() == null)
        {
            var esGO = new GameObject("EventSystem");
            esGO.AddComponent<EventSystem>();
            esGO.AddComponent<InputSystemUIInputModule>();
        }

        // QuestionPanel
        questionPanel = MakePanel(canvasGO.transform, "QuestionPanel");

        questionText = MakeTMP(questionPanel.transform, "QuestionText", 32f);
        SetAnchors(questionText.rectTransform, 0.1f, 0.55f, 0.9f, 0.85f);
        questionText.alignment = TextAlignmentOptions.Center;

        choiceButtons = new Button[3];
        choiceTexts   = new TextMeshProUGUI[3];
        float[] yMins = { 0.30f, 0.17f, 0.04f };
        for (int i = 0; i < 3; i++)
        {
            var (btn, lbl) = MakeButton(questionPanel.transform, $"Choice{i}",
                0.1f, yMins[i], 0.9f, yMins[i] + 0.10f);
            choiceButtons[i] = btn;
            choiceTexts[i]   = lbl;
        }

        // FinalPanel
        finalPanel = MakePanel(canvasGO.transform, "FinalPanel");
        finalPanel.SetActive(false);

        var titleTMP = MakeTMP(finalPanel.transform, "FinalTitle", 28f);
        titleTMP.text      = "지금 꼭\n내뱉고 싶은 말은?";
        titleTMP.alignment = TextAlignmentOptions.Center;
        SetAnchors(titleTMP.rectTransform, 0.1f, 0.68f, 0.9f, 0.88f);

        inputField = BuildInputField(finalPanel.transform, 0.08f, 0.50f, 0.92f, 0.65f);

        var (submitBtn, submitLbl) = MakeButton(finalPanel.transform, "SubmitButton",
            0.1f, 0.35f, 0.9f, 0.47f);
        submitLbl.text = "완료";
        submitBtn.onClick.AddListener(OnSubmitButtonClicked);

        var (skipBtn, skipLbl) = MakeButton(finalPanel.transform, "SkipButton",
            0.2f, 0.20f, 0.8f, 0.31f);
        skipLbl.text  = "건너뛸게요";
        skipLbl.color = new Color(0.7f, 0.7f, 0.7f);
        skipBtn.onClick.AddListener(SkipOnboarding);

        // 페이드 오버레이 (씬 전환용, 맨 위 레이어)
        var fadeGO = new GameObject("FadeOverlay");
        fadeGO.transform.SetParent(canvasGO.transform, false);
        var fadeRT = fadeGO.AddComponent<RectTransform>();
        fadeRT.anchorMin = Vector2.zero;
        fadeRT.anchorMax = Vector2.one;
        fadeRT.offsetMin = fadeRT.offsetMax = Vector2.zero;
        fadeOverlay = fadeGO.AddComponent<Image>();
        fadeOverlay.color = new Color(0f, 0f, 0f, 0f);
        fadeOverlay.raycastTarget = false;
    }

    TMP_InputField BuildInputField(Transform parent, float xMin, float yMin, float xMax, float yMax)
    {
        var go = new GameObject("InputField");
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        SetAnchors(rt, xMin, yMin, xMax, yMax);

        var bg = go.AddComponent<Image>();
        bg.color = new Color(1f, 1f, 1f, 0.10f);

        var field = go.AddComponent<TMP_InputField>();
        field.lineType = TMP_InputField.LineType.MultiLineSubmit;

        var font = LoadKoreanFont();

        // TextArea (clipping mask)
        var areaGO = new GameObject("Text Area");
        areaGO.transform.SetParent(go.transform, false);
        var areaRT = areaGO.AddComponent<RectTransform>();
        areaRT.anchorMin = Vector2.zero;
        areaRT.anchorMax = Vector2.one;
        areaRT.offsetMin = new Vector2(10f, 6f);
        areaRT.offsetMax = new Vector2(-10f, -6f);
        areaGO.AddComponent<RectMask2D>();

        // 실제 텍스트
        var textGO = new GameObject("Text");
        textGO.transform.SetParent(areaGO.transform, false);
        var textRT = textGO.AddComponent<RectTransform>();
        textRT.anchorMin = Vector2.zero;
        textRT.anchorMax = Vector2.one;
        textRT.offsetMin = textRT.offsetMax = Vector2.zero;
        var textTMP = textGO.AddComponent<TextMeshProUGUI>();
        textTMP.fontSize  = 20f;
        textTMP.color     = Color.white;
        textTMP.alignment = TextAlignmentOptions.TopLeft;
        if (font != null) textTMP.font = font;

        // 플레이스홀더
        var phGO = new GameObject("Placeholder");
        phGO.transform.SetParent(areaGO.transform, false);
        var phRT = phGO.AddComponent<RectTransform>();
        phRT.anchorMin = Vector2.zero;
        phRT.anchorMax = Vector2.one;
        phRT.offsetMin = phRT.offsetMax = Vector2.zero;
        var phTMP = phGO.AddComponent<TextMeshProUGUI>();
        phTMP.fontSize  = 20f;
        phTMP.color     = new Color(1f, 1f, 1f, 0.35f);
        phTMP.text      = "여기에 입력하세요...";
        phTMP.alignment = TextAlignmentOptions.TopLeft;
        if (font != null) phTMP.font = font;

        field.textViewport   = areaRT;
        field.textComponent  = textTMP;
        field.placeholder    = phTMP;
        field.targetGraphic  = bg;
        field.onSubmit.AddListener(_ => OnSubmitButtonClicked());

        return field;
    }

    // ── 연기 파티클 ─────────────────────────────────────────────
    void BuildSmokeParticles()
    {
        var go = new GameObject("PS_Smoke");
        go.transform.SetParent(transform);
        go.transform.localPosition = Vector3.zero;
        var ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        go.GetComponent<ParticleSystemRenderer>().sortingOrder = -10;

        var main = ps.main;
        main.loop            = true;
        main.duration        = 10f;
        main.startLifetime   = new ParticleSystem.MinMaxCurve(6f, 12f);
        main.startSpeed      = new ParticleSystem.MinMaxCurve(0.04f, 0.14f);
        main.startSize       = new ParticleSystem.MinMaxCurve(1.2f, 3.5f);
        main.startColor      = new ParticleSystem.MinMaxGradient(
            new Color(0.55f, 0.55f, 0.60f, 0.10f),
            new Color(0.70f, 0.70f, 0.75f, 0.18f));
        main.maxParticles    = 20;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.gravityModifier = 0f;

        var em = ps.emission;
        em.rateOverTime = 1.2f;

        float sw = mainCam.orthographicSize * mainCam.aspect;
        float sh = mainCam.orthographicSize;
        var shape = ps.shape;
        shape.enabled   = true;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale     = new Vector3(sw * 2.5f, sh * 2.5f, 0f);

        var vel = ps.velocityOverLifetime;
        vel.enabled = true;
        vel.space   = ParticleSystemSimulationSpace.World;
        vel.x       = new ParticleSystem.MinMaxCurve(-0.05f, 0.05f);
        vel.y       = new ParticleSystem.MinMaxCurve(0.03f, 0.10f);
        vel.z       = new ParticleSystem.MinMaxCurve(0f, 0f);

        var fade = ps.colorOverLifetime;
        fade.enabled = true;
        var grad = new Gradient();
        grad.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.15f),
                    new GradientAlphaKey(1f, 0.85f), new GradientAlphaKey(0f, 1f) });
        fade.color = grad;
        ps.Play();
    }

    // ── UI 헬퍼 ─────────────────────────────────────────────────
    static GameObject MakePanel(Transform parent, string name)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        return go;
    }

    static TMP_FontAsset LoadKoreanFont() => Resources.Load<TMP_FontAsset>("KoreanFont");

    static TextMeshProUGUI MakeTMP(Transform parent, string name, float size)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.fontSize = size;
        tmp.color    = Color.white;
        var font = LoadKoreanFont();
        if (font != null) tmp.font = font;
        return tmp;
    }

    static (Button, TextMeshProUGUI) MakeButton(
        Transform parent, string name,
        float xMin, float yMin, float xMax, float yMax)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        SetAnchors(rt, xMin, yMin, xMax, yMax);

        var img = go.AddComponent<Image>();
        img.color = new Color(1f, 1f, 1f, 0.08f);

        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        var bc = btn.colors;
        bc.normalColor      = new Color(1f, 1f, 1f, 0.08f);
        bc.highlightedColor = new Color(1f, 1f, 1f, 0.20f);
        bc.pressedColor     = new Color(1f, 1f, 1f, 0.35f);
        btn.colors = bc;

        var lblGO = new GameObject("Label");
        lblGO.transform.SetParent(go.transform, false);
        lblGO.AddComponent<RectTransform>();
        var lbl = lblGO.AddComponent<TextMeshProUGUI>();
        lbl.fontSize  = 18f;
        lbl.color     = Color.white;
        lbl.alignment = TextAlignmentOptions.Center;
        var font = LoadKoreanFont();
        if (font != null) lbl.font = font;
        SetAnchors(lbl.rectTransform, 0f, 0f, 1f, 1f);

        return (btn, lbl);
    }

    static void SetAnchors(RectTransform rt, float xMin, float yMin, float xMax, float yMax)
    {
        rt.anchorMin = new Vector2(xMin, yMin);
        rt.anchorMax = new Vector2(xMax, yMax);
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    static Color HexColor(string hex)
    {
        ColorUtility.TryParseHtmlString(hex, out var c);
        return c;
    }

    [System.Serializable]
    class SubmitData { public string text; public bool skip; }
}
