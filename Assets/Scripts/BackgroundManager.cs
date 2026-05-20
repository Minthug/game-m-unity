using System.Collections.Generic;
using UnityEngine;

public class BackgroundManager : MonoBehaviour
{
    public static BackgroundManager Instance { get; private set; }

    Camera mainCam;
    Color  targetBgColor;

    Expression currentEmotion = Expression.Blank;
    float      checkTimer     = 0f;
    const float CHECK_INTERVAL = 2f;

    readonly Dictionary<Expression, GameObject> psMap = new();

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        mainCam  = Camera.main;
        targetBgColor = Hex("#0A0A0F");
    }

    void Start()
    {
        BuildAllParticles();
        ApplyEmotion(Expression.Blank);
    }

    void Update()
    {
        // 배경색 부드럽게 전환
        if (mainCam != null)
            mainCam.backgroundColor = Color.Lerp(mainCam.backgroundColor, targetBgColor, Time.deltaTime * 1.0f);

        // 지배적 감정 주기 체크
        checkTimer += Time.deltaTime;
        if (checkTimer < CHECK_INTERVAL) return;
        checkTimer = 0f;

        if (SlimeManager.Instance == null) return;
        var next = SlimeManager.Instance.GetDominantExpression();
        if (next == currentEmotion) return;
        currentEmotion = next;
        ApplyEmotion(currentEmotion);
    }

    void ApplyEmotion(Expression e)
    {
        targetBgColor = EmotionBgColor(e);
        foreach (var kv in psMap)
            kv.Value.SetActive(kv.Key == e);
        Debug.Log($"[BG] 감정 전환 → {e}");
    }

    // React 호환 (기존 SendMessage 유지)
    public void SetThemeFromWeb(string themeId) { }
    public void SetTheme(string themeId)         { }

    // ── 감정별 배경색 ─────────────────────────────────────────
    static Color EmotionBgColor(Expression e) => e switch
    {
        Expression.Angry     => Hex("#130000"),
        Expression.Sad       => Hex("#00040F"),
        Expression.Fear      => Hex("#070010"),
        Expression.Happy     => Hex("#0D0818"),
        Expression.Disgust   => Hex("#010C02"),
        Expression.Surprised => Hex("#0A0020"),
        Expression.Contempt  => Hex("#060810"),
        _                    => Hex("#0A0A0F"),
    };

    // ── 파티클 빌드 ─────────────────────────────────────────────
    void BuildAllParticles()
    {
        psMap[Expression.Angry]     = BuildAngry();
        psMap[Expression.Sad]       = BuildSad();
        psMap[Expression.Fear]      = BuildFear();
        psMap[Expression.Happy]     = BuildHappy();
        psMap[Expression.Disgust]   = BuildDisgust();
        psMap[Expression.Surprised] = BuildSurprised();
        psMap[Expression.Contempt]  = BuildContempt();
        psMap[Expression.Blank]     = BuildBlank();

        foreach (var go in psMap.Values) go.SetActive(false);
    }

    // 분노: 붉은 불씨가 빠르게 위로 솟구침
    GameObject BuildAngry()
    {
        var ps = NewPS("PS_Angry", out var go);
        var m  = ps.main;
        m.startLifetime   = new ParticleSystem.MinMaxCurve(1.5f, 3.5f);
        m.startSpeed      = new ParticleSystem.MinMaxCurve(1.0f, 2.8f);
        m.startSize       = new ParticleSystem.MinMaxCurve(0.03f, 0.10f);
        m.startColor      = new ParticleSystem.MinMaxGradient(Hex("#FF2200", 0.9f), Hex("#FF8800", 0.95f));
        m.maxParticles    = 50;
        m.gravityModifier = -0.3f;
        ps.emission.rateOverTime = 9f;
        SetShape(ps, bottom: true);
        var vel = ps.velocityOverLifetime;
        vel.enabled = true;
        vel.x = new ParticleSystem.MinMaxCurve(-0.4f, 0.4f);
        SetFade(ps);
        ps.Play();
        return go;
    }

    // 슬픔: 파란 방울이 천천히 아래로 떨어짐
    GameObject BuildSad()
    {
        var ps = NewPS("PS_Sad", out var go);
        var m  = ps.main;
        m.startLifetime   = new ParticleSystem.MinMaxCurve(5f, 9f);
        m.startSpeed      = new ParticleSystem.MinMaxCurve(0.3f, 0.9f);
        m.startSize       = new ParticleSystem.MinMaxCurve(0.03f, 0.12f);
        m.startColor      = new ParticleSystem.MinMaxGradient(Hex("#1E5FFF", 0.7f), Hex("#88BBFF", 0.85f));
        m.maxParticles    = 35;
        m.gravityModifier = 0.08f;
        ps.emission.rateOverTime = 3.5f;
        SetShape(ps, bottom: false, top: true);
        SetFade(ps);
        ps.Play();
        return go;
    }

    // 공포: 보라 연기가 노이즈로 불규칙하게 흐름
    GameObject BuildFear()
    {
        var ps = NewPS("PS_Fear", out var go);
        var m  = ps.main;
        m.startLifetime   = new ParticleSystem.MinMaxCurve(7f, 13f);
        m.startSpeed      = new ParticleSystem.MinMaxCurve(0.08f, 0.35f);
        m.startSize       = new ParticleSystem.MinMaxCurve(0.6f, 2.0f);
        m.startColor      = new ParticleSystem.MinMaxGradient(Hex("#440088", 0.18f), Hex("#9944CC", 0.28f));
        m.maxParticles    = 20;
        m.gravityModifier = 0f;
        ps.emission.rateOverTime = 1.5f;
        SetShape(ps, fullscreen: true);
        var vel = ps.velocityOverLifetime;
        vel.enabled = true;
        vel.x = new ParticleSystem.MinMaxCurve(-0.12f, 0.12f);
        vel.y = new ParticleSystem.MinMaxCurve(-0.06f, 0.10f);
        var noise = ps.noise;
        noise.enabled     = true;
        noise.strength    = 0.4f;
        noise.frequency   = 0.5f;
        noise.scrollSpeed = 0.2f;
        SetFade(ps);
        ps.Play();
        return go;
    }

    // 행복: 황금/핑크 반짝이가 위로 떠오름
    GameObject BuildHappy()
    {
        var ps = NewPS("PS_Happy", out var go);
        var m  = ps.main;
        m.startLifetime   = new ParticleSystem.MinMaxCurve(3f, 6f);
        m.startSpeed      = new ParticleSystem.MinMaxCurve(0.5f, 1.5f);
        m.startSize       = new ParticleSystem.MinMaxCurve(0.02f, 0.08f);
        m.startColor      = new ParticleSystem.MinMaxGradient(Hex("#FFD700", 0.9f), Hex("#FF69B4", 0.95f));
        m.maxParticles    = 60;
        m.gravityModifier = -0.1f;
        ps.emission.rateOverTime = 10f;
        SetShape(ps, fullscreen: true);
        var vel = ps.velocityOverLifetime;
        vel.enabled = true;
        vel.x = new ParticleSystem.MinMaxCurve(-0.2f, 0.2f);
        vel.y = new ParticleSystem.MinMaxCurve(0.3f, 0.8f);
        SetFade(ps);
        ps.Play();
        return go;
    }

    // 혐오: 어두운 녹색 안개가 느리게 흐름
    GameObject BuildDisgust()
    {
        var ps = NewPS("PS_Disgust", out var go);
        var m  = ps.main;
        m.startLifetime   = new ParticleSystem.MinMaxCurve(9f, 15f);
        m.startSpeed      = new ParticleSystem.MinMaxCurve(0.03f, 0.12f);
        m.startSize       = new ParticleSystem.MinMaxCurve(0.7f, 2.2f);
        m.startColor      = new ParticleSystem.MinMaxGradient(Hex("#003300", 0.20f), Hex("#00AA44", 0.30f));
        m.maxParticles    = 18;
        m.gravityModifier = 0f;
        ps.emission.rateOverTime = 1.2f;
        SetShape(ps, fullscreen: true);
        var vel = ps.velocityOverLifetime;
        vel.enabled = true;
        vel.x = new ParticleSystem.MinMaxCurve(-0.05f, 0.08f);
        vel.y = new ParticleSystem.MinMaxCurve(-0.03f, 0.05f);
        SetFade(ps);
        ps.Play();
        return go;
    }

    // 놀람: 흰/노란 반짝임이 사방으로 터짐
    GameObject BuildSurprised()
    {
        var ps = NewPS("PS_Surprised", out var go);
        var m  = ps.main;
        m.startLifetime   = new ParticleSystem.MinMaxCurve(0.8f, 2.0f);
        m.startSpeed      = new ParticleSystem.MinMaxCurve(1.5f, 4.0f);
        m.startSize       = new ParticleSystem.MinMaxCurve(0.02f, 0.07f);
        m.startColor      = new ParticleSystem.MinMaxGradient(Hex("#FFFFFF", 0.85f), Hex("#FFFF44", 0.95f));
        m.maxParticles    = 40;
        m.gravityModifier = 0.05f;
        ps.emission.rateOverTime = 8f;
        SetShape(ps, fullscreen: true);
        var vel = ps.velocityOverLifetime;
        vel.enabled = true;
        vel.x = new ParticleSystem.MinMaxCurve(-1.5f, 1.5f);
        vel.y = new ParticleSystem.MinMaxCurve(-1.5f, 1.5f);
        SetFade(ps);
        ps.Play();
        return go;
    }

    // 경멸: 차가운 회색이 한 방향으로 천천히 흐름
    GameObject BuildContempt()
    {
        var ps = NewPS("PS_Contempt", out var go);
        var m  = ps.main;
        m.startLifetime   = new ParticleSystem.MinMaxCurve(7f, 12f);
        m.startSpeed      = new ParticleSystem.MinMaxCurve(0.05f, 0.2f);
        m.startSize       = new ParticleSystem.MinMaxCurve(0.1f, 0.5f);
        m.startColor      = new ParticleSystem.MinMaxGradient(Hex("#556677", 0.15f), Hex("#99AABB", 0.25f));
        m.maxParticles    = 15;
        m.gravityModifier = 0f;
        ps.emission.rateOverTime = 1.2f;
        SetShape(ps, fullscreen: true);
        var vel = ps.velocityOverLifetime;
        vel.enabled = true;
        vel.x = new ParticleSystem.MinMaxCurve(0.06f, 0.18f);
        vel.y = new ParticleSystem.MinMaxCurve(-0.02f, 0.02f);
        SetFade(ps);
        ps.Play();
        return go;
    }

    // 무표정: 미묘한 회색 먼지
    GameObject BuildBlank()
    {
        var ps = NewPS("PS_Blank", out var go);
        var m  = ps.main;
        m.startLifetime   = new ParticleSystem.MinMaxCurve(6f, 10f);
        m.startSpeed      = new ParticleSystem.MinMaxCurve(0.04f, 0.14f);
        m.startSize       = new ParticleSystem.MinMaxCurve(0.2f, 0.7f);
        m.startColor      = new ParticleSystem.MinMaxGradient(
            new Color(0.4f, 0.4f, 0.45f, 0.10f),
            new Color(0.5f, 0.5f, 0.55f, 0.18f));
        m.maxParticles    = 14;
        m.gravityModifier = 0f;
        ps.emission.rateOverTime = 1.2f;
        SetShape(ps, fullscreen: true);
        var vel = ps.velocityOverLifetime;
        vel.enabled = true;
        vel.x = new ParticleSystem.MinMaxCurve(-0.04f, 0.04f);
        vel.y = new ParticleSystem.MinMaxCurve(0.04f, 0.12f);
        SetFade(ps);
        ps.Play();
        return go;
    }

    // ── 유틸 ────────────────────────────────────────────────────
    ParticleSystem NewPS(string name, out GameObject go)
    {
        go = new GameObject(name);
        go.transform.SetParent(transform);
        go.transform.localPosition = Vector3.zero;
        var ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = ps.main;
        main.loop            = true;
        main.duration        = 10f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        var r = go.GetComponent<ParticleSystemRenderer>();
        r.sortingOrder = -10;
        return ps;
    }

    static void SetShape(ParticleSystem ps, bool bottom = false, bool top = false, bool fullscreen = false)
    {
        var shape = ps.shape;
        shape.enabled   = true;
        shape.shapeType = ParticleSystemShapeType.Box;
        if (fullscreen)
        {
            shape.scale    = new Vector3(ScreenW() * 2f, ScreenH() * 2f, 0f);
            shape.position = Vector3.zero;
        }
        else if (bottom)
        {
            shape.scale    = new Vector3(ScreenW() * 2f, 0.2f, 0f);
            shape.position = new Vector3(0f, -ScreenH(), 0f);
        }
        else if (top)
        {
            shape.scale    = new Vector3(ScreenW() * 2f, 0.2f, 0f);
            shape.position = new Vector3(0f, ScreenH() + 0.5f, 0f);
        }
    }

    static void SetFade(ParticleSystem ps)
    {
        var fade = ps.colorOverLifetime;
        fade.enabled = true;
        var grad = new Gradient();
        grad.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.15f), new GradientAlphaKey(1f, 0.85f), new GradientAlphaKey(0f, 1f) });
        fade.color = grad;
    }

    static float ScreenW() => Camera.main != null ? Camera.main.orthographicSize * Camera.main.aspect : 5f;
    static float ScreenH() => Camera.main != null ? Camera.main.orthographicSize : 5f;

    static Color Hex(string hex, float a = 1f)
    {
        ColorUtility.TryParseHtmlString(hex, out var c);
        c.a = a;
        return c;
    }
}
