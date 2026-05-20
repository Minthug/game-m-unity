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
        var e0 = ps.emission; e0.rateOverTime = 9f;
        SetShape(ps, bottom: true);
        var vel = ps.velocityOverLifetime;
        vel.enabled = true;
        vel.x = new ParticleSystem.MinMaxCurve(-0.4f, 0.4f);
        vel.y = new ParticleSystem.MinMaxCurve(0f, 0f);
        vel.z = new ParticleSystem.MinMaxCurve(0f, 0f);
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
        var e1 = ps.emission; e1.rateOverTime = 3.5f;
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
        var e2 = ps.emission; e2.rateOverTime = 1.5f;
        SetShape(ps, fullscreen: true);
        var vel = ps.velocityOverLifetime;
        vel.enabled = true;
        vel.x = new ParticleSystem.MinMaxCurve(-0.12f, 0.12f);
        vel.y = new ParticleSystem.MinMaxCurve(-0.06f, 0.10f);
        vel.z = new ParticleSystem.MinMaxCurve(0f, 0f);
        var noise = ps.noise;
        noise.enabled     = true;
        noise.strength    = 0.4f;
        noise.frequency   = 0.5f;
        noise.scrollSpeed = 0.2f;
        SetFade(ps);
        ps.Play();
        return go;
    }

    // 행복: 알록달록 별 꽃가루가 살랑살랑 위로 떠오름
    GameObject BuildHappy()
    {
        var ps = NewPS("PS_Happy", out var go);
        go.GetComponent<ParticleSystemRenderer>().material = GetStarMat();
        var m  = ps.main;
        m.startLifetime   = new ParticleSystem.MinMaxCurve(4f, 7f);
        m.startSpeed      = new ParticleSystem.MinMaxCurve(0.3f, 1.0f);
        m.startSize       = new ParticleSystem.MinMaxCurve(0.05f, 0.14f);
        m.startRotation   = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        m.maxParticles    = 80;
        m.gravityModifier = -0.06f;

        // 무지개 색상 — 파티클마다 랜덤 선택
        var grad = new Gradient();
        grad.mode = GradientMode.Fixed;
        grad.SetKeys(
            new GradientColorKey[]
            {
                new(new Color(1.0f, 0.25f, 0.25f), 0.00f), // 빨
                new(new Color(1.0f, 0.60f, 0.10f), 0.14f), // 주
                new(new Color(1.0f, 1.00f, 0.15f), 0.28f), // 노
                new(new Color(0.2f, 0.90f, 0.30f), 0.42f), // 초
                new(new Color(0.2f, 0.65f, 1.00f), 0.57f), // 파
                new(new Color(0.7f, 0.25f, 1.00f), 0.71f), // 보
                new(new Color(1.0f, 0.40f, 0.85f), 0.85f), // 핑크
                new(new Color(1.0f, 1.00f, 1.00f), 1.00f), // 흰
            },
            new GradientAlphaKey[] { new(1f, 0f), new(1f, 1f) }
        );
        var colorRange = new ParticleSystem.MinMaxGradient(grad);
        colorRange.mode = ParticleSystemGradientMode.RandomColor;
        m.startColor = colorRange;

        var e3 = ps.emission; e3.rateOverTime = 14f;
        SetShape(ps, fullscreen: true);

        // 살랑살랑 회전 (꽃가루 느낌)
        var rot = ps.rotationOverLifetime;
        rot.enabled = true;
        rot.z = new ParticleSystem.MinMaxCurve(-1.5f, 1.5f);

        var vel = ps.velocityOverLifetime;
        vel.enabled = true;
        vel.x = new ParticleSystem.MinMaxCurve(-0.25f, 0.25f);
        vel.y = new ParticleSystem.MinMaxCurve(0.25f, 0.85f);
        vel.z = new ParticleSystem.MinMaxCurve(0f, 0f);

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
        var e4 = ps.emission; e4.rateOverTime = 1.2f;
        SetShape(ps, fullscreen: true);
        var vel = ps.velocityOverLifetime;
        vel.enabled = true;
        vel.x = new ParticleSystem.MinMaxCurve(-0.05f, 0.08f);
        vel.y = new ParticleSystem.MinMaxCurve(-0.03f, 0.05f);
        vel.z = new ParticleSystem.MinMaxCurve(0f, 0f);
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
        var e5 = ps.emission; e5.rateOverTime = 8f;
        SetShape(ps, fullscreen: true);
        var vel = ps.velocityOverLifetime;
        vel.enabled = true;
        vel.x = new ParticleSystem.MinMaxCurve(-1.5f, 1.5f);
        vel.y = new ParticleSystem.MinMaxCurve(-1.5f, 1.5f);
        vel.z = new ParticleSystem.MinMaxCurve(0f, 0f);
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
        var e6 = ps.emission; e6.rateOverTime = 1.2f;
        SetShape(ps, fullscreen: true);
        var vel = ps.velocityOverLifetime;
        vel.enabled = true;
        vel.x = new ParticleSystem.MinMaxCurve(0.06f, 0.18f);
        vel.y = new ParticleSystem.MinMaxCurve(-0.02f, 0.02f);
        vel.z = new ParticleSystem.MinMaxCurve(0f, 0f);
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
        var e7 = ps.emission; e7.rateOverTime = 1.2f;
        SetShape(ps, fullscreen: true);
        var vel = ps.velocityOverLifetime;
        vel.enabled = true;
        vel.x = new ParticleSystem.MinMaxCurve(-0.04f, 0.04f);
        vel.y = new ParticleSystem.MinMaxCurve(0.04f, 0.12f);
        vel.z = new ParticleSystem.MinMaxCurve(0f, 0f);
        SetFade(ps);
        ps.Play();
        return go;
    }

    // ── 유틸 ────────────────────────────────────────────────────

    Material _particleMat;
    Material _starMat;

    Material GetParticleMat()
    {
        if (_particleMat != null) return _particleMat;
        _particleMat = MakeMat(MakeCircleTex(32));
        return _particleMat;
    }

    Material GetStarMat()
    {
        if (_starMat != null) return _starMat;
        _starMat = MakeMat(MakeStarTex(32));
        return _starMat;
    }

    Material MakeMat(Texture2D tex)
    {
        var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit")
                  ?? Shader.Find("Sprites/Default");
        var mat = new Material(shader);
        mat.SetTexture("_BaseMap", tex);
        mat.mainTexture = tex;
        return mat;
    }

    static Texture2D MakeCircleTex(int size)
    {
        var tex  = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.wrapMode   = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;
        float half = size * 0.5f;
        var pixels = new Color[size * size];
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float dx   = x + 0.5f - half;
            float dy   = y + 0.5f - half;
            float dist = Mathf.Sqrt(dx * dx + dy * dy);
            float t    = Mathf.Clamp01(1f - dist / half);
            float a    = t * t * (3f - 2f * t);
            pixels[y * size + x] = new Color(1f, 1f, 1f, a);
        }
        tex.SetPixels(pixels);
        tex.Apply();
        return tex;
    }

    static Texture2D MakeStarTex(int size)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.wrapMode   = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;
        float half = size * 0.5f;
        var pixels = new Color[size * size];
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float dx = (x + 0.5f - half) / half;
            float dy = (y + 0.5f - half) / half;
            float r  = Mathf.Sqrt(dx * dx + dy * dy);
            float angle = Mathf.Atan2(dy, dx);
            // 5각 별: 각도마다 threshold 변동
            float threshold = 0.38f + 0.28f * Mathf.Abs(Mathf.Cos(angle * 2.5f));
            float a = Mathf.Clamp01((threshold - r) / 0.08f);
            a = a * a * (3f - 2f * a);
            pixels[y * size + x] = new Color(1f, 1f, 1f, a);
        }
        tex.SetPixels(pixels);
        tex.Apply();
        return tex;
    }

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
        r.material = GetParticleMat();
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
