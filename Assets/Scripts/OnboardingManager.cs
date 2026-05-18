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
        WebBridge.RequestTextInput("지금 꼭 내뱉고 싶은 말은?");
    }

    // ── Web → Unity ─────────────────────────────────────────────
    // SendMessage("OnboardingManager", "SubmitText", "{\"text\":\"...\",\"skip\":false}")
    public void SubmitText(string json)
    {
        var d = JsonUtility.FromJson<SubmitData>(json);
        if (!d.skip && !string.IsNullOrWhiteSpace(d.text))
        {
            PlayerPrefs.SetString("first_slime_text", d.text);
            PlayerPrefs.SetString("first_slime_expression",
                EmotionDetector.Detect(d.text).ToString().ToLower());
        }
        PlayerPrefs.SetInt("onboarding_done", 1);
        PlayerPrefs.Save();
        SceneManager.LoadScene("Main");
    }

    public void SkipOnboarding()
    {
        PlayerPrefs.SetInt("onboarding_done", 1);
        PlayerPrefs.Save();
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

        var waitTMP = MakeTMP(finalPanel.transform, "WaitText", 26f);
        waitTMP.text      = "잠깐만요...\n입력창이 열립니다";
        waitTMP.alignment = TextAlignmentOptions.Center;
        SetAnchors(waitTMP.rectTransform, 0.1f, 0.45f, 0.9f, 0.65f);

        var (skipBtn, skipLbl) = MakeButton(finalPanel.transform, "SkipButton",
            0.2f, 0.25f, 0.8f, 0.36f);
        skipLbl.text  = "건너뛸게요";
        skipLbl.color = new Color(0.7f, 0.7f, 0.7f);
        skipBtn.onClick.AddListener(SkipOnboarding);
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

    static TMP_FontAsset LoadKoreanFont()
    {
        return Resources.Load<TMP_FontAsset>("KoreanFont");
    }

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
