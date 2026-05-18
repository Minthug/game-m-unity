using UnityEngine;

public class BackgroundManager : MonoBehaviour
{
    public static BackgroundManager Instance { get; private set; }

    Camera mainCam;

    // 테마별 파티클 오브젝트
    GameObject psDefault;
    GameObject psDeepSea;
    GameObject psLava;
    GameObject psStorm;
    GameObject psFogForest;

    string currentTheme = "default";

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        mainCam  = Camera.main;
    }

    void Start()
    {
        BuildAllParticles();
        SetTheme("default");
    }

    // ── 테마 전환 (React → Unity SendMessage) ─────────────────
    public void SetThemeFromWeb(string themeId) => SetTheme(themeId);

    public void SetTheme(string themeId)
    {
        currentTheme = themeId;

        psDefault.SetActive(themeId == "default");
        psDeepSea.SetActive(themeId == "deep_sea");
        psLava.SetActive(themeId == "lava");
        psStorm.SetActive(themeId == "storm");
        psFogForest.SetActive(themeId == "fog_forest");

        ApplyBgColor(themeId);
    }

    // ── 배경색 ─────────────────────────────────────────────────
    void ApplyBgColor(string id)
    {
        if (mainCam == null) return;
        mainCam.backgroundColor = id switch
        {
            "deep_sea"   => HexColor("#020C1B"),
            "lava"       => HexColor("#120100"),
            "storm"      => HexColor("#060610"),
            "fog_forest" => HexColor("#0A1A0D"),
            _            => HexColor("#F0EDE6"),
        };
    }

    // ── 파티클 생성 ────────────────────────────────────────────
    void BuildAllParticles()
    {
        psDefault   = BuildDefault();
        psDeepSea   = BuildDeepSea();
        psLava      = BuildLava();
        psStorm     = BuildStorm();
        psFogForest = BuildFogForest();
    }

    // 기본: 느리게 떠다니는 부드러운 원형 점
    GameObject BuildDefault()
    {
        var go = NewPS("PS_Default");
        var ps = go.GetComponent<ParticleSystem>();
        var main = ps.main;
        main.loop             = true;
        main.duration         = 10f;
        main.startLifetime    = new ParticleSystem.MinMaxCurve(6f, 10f);
        main.startSpeed       = new ParticleSystem.MinMaxCurve(0.05f, 0.18f);
        main.startSize        = new ParticleSystem.MinMaxCurve(0.3f, 0.9f);
        main.startColor       = new ParticleSystem.MinMaxGradient(
            new Color(0.47f, 0.39f, 0.31f, 0.12f),
            new Color(0.55f, 0.47f, 0.38f, 0.20f));
        main.maxParticles     = 18;
        main.simulationSpace  = ParticleSystemSimulationSpace.World;
        main.gravityModifier  = 0f;

        var emission = ps.emission;
        emission.rateOverTime = 1.5f;

        var shape = ps.shape;
        shape.enabled   = true;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale     = new Vector3(ScreenW() * 2f, ScreenH() * 2f, 0f);

        var vel = ps.velocityOverLifetime;
        vel.enabled = true;
        vel.space   = ParticleSystemSimulationSpace.World;
        vel.x       = new ParticleSystem.MinMaxCurve(-0.05f, 0.05f);
        vel.y       = new ParticleSystem.MinMaxCurve(0.05f, 0.15f);

        var fade = ps.colorOverLifetime;
        fade.enabled = true;
        var grad = new Gradient();
        grad.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.2f), new GradientAlphaKey(1f, 0.8f), new GradientAlphaKey(0f, 1f) });
        fade.color = grad;

        return go;
    }

    // 심해: 위로 올라오는 파란 물방울
    GameObject BuildDeepSea()
    {
        var go = NewPS("PS_DeepSea");
        var ps = go.GetComponent<ParticleSystem>();
        var main = ps.main;
        main.loop             = true;
        main.duration         = 8f;
        main.startLifetime    = new ParticleSystem.MinMaxCurve(5f, 9f);
        main.startSpeed       = new ParticleSystem.MinMaxCurve(0.4f, 1.0f);
        main.startSize        = new ParticleSystem.MinMaxCurve(0.04f, 0.18f);
        main.startColor       = new ParticleSystem.MinMaxGradient(
            HexColor("#00D4FF", 0.8f), HexColor("#7FE8FF", 0.9f));
        main.maxParticles     = 30;
        main.simulationSpace  = ParticleSystemSimulationSpace.World;
        main.gravityModifier  = -0.05f;

        var emission = ps.emission;
        emission.rateOverTime = 3f;

        var shape = ps.shape;
        shape.enabled   = true;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale     = new Vector3(ScreenW() * 2f, 0.2f, 0f);
        shape.position  = new Vector3(0f, -ScreenH(), 0f);

        var vel = ps.velocityOverLifetime;
        vel.enabled = true;
        vel.space   = ParticleSystemSimulationSpace.World;
        vel.x       = new ParticleSystem.MinMaxCurve(-0.08f, 0.08f);

        SetFade(ps);
        return go;
    }

    // 용암: 위로 튀어오르는 붉은 불씨
    GameObject BuildLava()
    {
        var go = NewPS("PS_Lava");
        var ps = go.GetComponent<ParticleSystem>();
        var main = ps.main;
        main.loop             = true;
        main.duration         = 5f;
        main.startLifetime    = new ParticleSystem.MinMaxCurve(2f, 5f);
        main.startSpeed       = new ParticleSystem.MinMaxCurve(0.8f, 2.5f);
        main.startSize        = new ParticleSystem.MinMaxCurve(0.03f, 0.12f);
        main.startColor       = new ParticleSystem.MinMaxGradient(
            HexColor("#FF4500", 0.9f), HexColor("#FFB800", 0.95f));
        main.maxParticles     = 40;
        main.simulationSpace  = ParticleSystemSimulationSpace.World;
        main.gravityModifier  = -0.3f;

        var emission = ps.emission;
        emission.rateOverTime = 6f;

        var shape = ps.shape;
        shape.enabled   = true;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale     = new Vector3(ScreenW() * 2f, 0.2f, 0f);
        shape.position  = new Vector3(0f, -ScreenH(), 0f);

        var vel = ps.velocityOverLifetime;
        vel.enabled = true;
        vel.space   = ParticleSystemSimulationSpace.World;
        vel.x       = new ParticleSystem.MinMaxCurve(-0.3f, 0.3f);

        SetFade(ps);
        return go;
    }

    // 폭풍: 대각선으로 내리는 빗줄기
    GameObject BuildStorm()
    {
        var go = NewPS("PS_Storm");
        var ps = go.GetComponent<ParticleSystem>();
        var main = ps.main;
        main.loop             = true;
        main.duration         = 3f;
        main.startLifetime    = new ParticleSystem.MinMaxCurve(0.8f, 1.6f);
        main.startSpeed       = new ParticleSystem.MinMaxCurve(6f, 10f);
        main.startSize        = new ParticleSystem.MinMaxCurve(0.02f, 0.05f);
        main.startRotation    = new ParticleSystem.MinMaxCurve(-0.35f, -0.25f); // 약간 기울어진 빗줄기
        main.startColor       = new ParticleSystem.MinMaxGradient(
            new Color(0.78f, 0.84f, 1f, 0.5f), new Color(0.86f, 0.90f, 1f, 0.65f));
        main.maxParticles     = 80;
        main.simulationSpace  = ParticleSystemSimulationSpace.World;
        main.gravityModifier  = 1.2f;

        var emission = ps.emission;
        emission.rateOverTime = 25f;

        var shape = ps.shape;
        shape.enabled   = true;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale     = new Vector3(ScreenW() * 2.5f, 0.2f, 0f);
        shape.position  = new Vector3(0f, ScreenH() + 1f, 0f);

        var renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Stretch;
        renderer.lengthScale = 6f;

        SetFade(ps);
        return go;
    }

    // 안개숲: 떠다니는 초록 연기
    GameObject BuildFogForest()
    {
        var go = NewPS("PS_FogForest");
        var ps = go.GetComponent<ParticleSystem>();
        var main = ps.main;
        main.loop             = true;
        main.duration         = 12f;
        main.startLifetime    = new ParticleSystem.MinMaxCurve(8f, 14f);
        main.startSpeed       = new ParticleSystem.MinMaxCurve(0.03f, 0.12f);
        main.startSize        = new ParticleSystem.MinMaxCurve(0.8f, 2.5f);
        main.startColor       = new ParticleSystem.MinMaxGradient(
            HexColor("#00C850", 0.15f), HexColor("#80E8B0", 0.25f));
        main.maxParticles     = 20;
        main.simulationSpace  = ParticleSystemSimulationSpace.World;
        main.gravityModifier  = 0f;

        var emission = ps.emission;
        emission.rateOverTime = 1.2f;

        var shape = ps.shape;
        shape.enabled   = true;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale     = new Vector3(ScreenW() * 2f, ScreenH() * 2f, 0f);

        var vel = ps.velocityOverLifetime;
        vel.enabled = true;
        vel.space   = ParticleSystemSimulationSpace.World;
        vel.x       = new ParticleSystem.MinMaxCurve(-0.06f, 0.06f);
        vel.y       = new ParticleSystem.MinMaxCurve(0.02f, 0.08f);

        SetFade(ps);
        return go;
    }

    // ── 유틸 ───────────────────────────────────────────────────
    GameObject NewPS(string name)
    {
        var go = new GameObject(name);
        go.transform.SetParent(transform);
        go.transform.localPosition = Vector3.zero;
        go.AddComponent<ParticleSystem>();
        var r = go.GetComponent<ParticleSystemRenderer>();
        r.sortingOrder = -10; // 슬라임 뒤
        return go;
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
    static float ScreenH() => Camera.main != null ? Camera.main.orthographicSize : 9f;

    static Color HexColor(string hex, float a = 1f)
    {
        ColorUtility.TryParseHtmlString(hex, out var c);
        c.a = a;
        return c;
    }
}
