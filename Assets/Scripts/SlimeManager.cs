using System.Collections.Generic;
using UnityEngine;

public class SlimeManager : MonoBehaviour
{
    public static SlimeManager Instance { get; private set; }

    [Header("슬라임 Prefab")]
    public GameObject slimePrefab;

    [Header("감정별 스프라이트")]
    public Sprite spriteAngry;
    public Sprite spriteSad;
    public Sprite spriteFear;
    public Sprite spriteHappy;
    public Sprite spriteDisgust;
    public Sprite spriteSurprised;
    public Sprite spriteContempt;
    public Sprite spriteBlank;

    // 색상 팔레트 (game-m의 EXPRESSION_COLORS와 동일)
    static readonly Dictionary<Expression, string[]> Palette = new()
    {
        [Expression.Angry]     = new[] { "#DC2626", "#B91C1C", "#EF4444", "#C53030" },
        [Expression.Sad]       = new[] { "#2563EB", "#1D4ED8", "#3B82F6", "#1E40AF" },
        [Expression.Surprised] = new[] { "#9333EA", "#7C3AED", "#A855F7", "#6D28D9" },
        [Expression.Blank]     = new[] { "#6B7280", "#52525B", "#71717A", "#64748B" },
        [Expression.Happy]     = new[] { "#7C3AED", "#DB2777", "#D97706", "#0891B2" },
        [Expression.Fear]      = new[] { "#4F46E5", "#4338CA", "#6366F1", "#3730A3" },
        [Expression.Disgust]   = new[] { "#16A34A", "#15803D", "#22C55E", "#166534" },
        [Expression.Contempt]  = new[] { "#64748B", "#475569", "#94A3B8", "#334155" },
    };

    readonly Dictionary<string, SlimeController> slimes = new();

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        var text = PlayerPrefs.GetString("first_slime_text", "");
        if (string.IsNullOrEmpty(text)) return;

        var req = new SlimeCreateRequest
        {
            id         = "slime-first",
            text       = text,
            expression = PlayerPrefs.GetString("first_slime_expression", "blank"),
            size       = 72f,
        };
        CreateSlimeFromWeb(JsonUtility.ToJson(req));
        PlayerPrefs.DeleteKey("first_slime_text");
        PlayerPrefs.DeleteKey("first_slime_expression");
    }

    // ── React → Unity 진입점 ──────────────────────────────────

    // SendMessage("SlimeManager", "CreateSlimeFromWeb", "{...json...}")
    public void CreateSlimeFromWeb(string json)
    {
        var req = JsonUtility.FromJson<SlimeCreateRequest>(json);
        if (req == null) return;

        var expression = ParseExpression(req.expression);
        var color      = string.IsNullOrEmpty(req.color)
            ? ColorUtil.FromHex(Palette[expression][Random.Range(0, 4)])
            : ColorUtil.FromHex(req.color);

        SpawnSlime(req.id ?? $"slime-{System.DateTime.Now.Ticks}", req.text, expression, color, req.size);
    }

    // SendMessage("SlimeManager", "TriggerShake", "")
    public void TriggerShake(string _)
    {
        foreach (var s in slimes.Values) s.ApplyShake();
    }

    // SendMessage("SlimeManager", "DeleteSlime", "slime-id")
    public void DeleteSlime(string id)
    {
        if (slimes.TryGetValue(id, out var s))
        {
            slimes.Remove(id);
            Destroy(s.gameObject);
        }
    }

    // ── 내부 ─────────────────────────────────────────────────

    void SpawnSlime(string id, string text, Expression expression, Color color, float sizePx)
    {
        if (slimePrefab == null) { Debug.LogError("[SlimeManager] slimePrefab이 null — Setup Scene을 다시 실행하세요"); return; }

        float worldSize = Mathf.Lerp(0.6f, 1.2f, Mathf.InverseLerp(48f, 95f, sizePx));

        Camera cam  = Camera.main;
        float  hw   = cam.orthographicSize * cam.aspect;
        float  hh   = cam.orthographicSize;
        Vector3 pos = new(Random.Range(-hw * 0.7f, hw * 0.7f), Random.Range(-hh * 0.7f, hh * 0.7f), 0f);

        var go   = Instantiate(slimePrefab, pos, Quaternion.identity);
        var ctrl = go.GetComponent<SlimeController>();

        var sr     = go.GetComponent<SpriteRenderer>();
        var sprite = GetSprite(expression);
        if (sprite == null) Debug.LogWarning($"[SlimeManager] {expression} 스프라이트가 null — Setup Scene을 다시 실행하세요");
        sr.sprite = sprite;
        sr.color  = color;

        ctrl.Init(id, expression, color, worldSize);
        slimes[id] = ctrl;
        go.name    = $"Slime_{id}";
        Debug.Log($"[SlimeManager] 생성: {go.name} pos={pos} scale={worldSize} sprite={(sprite != null ? sprite.name : "NULL")}");
    }

    Sprite GetSprite(Expression e) => e switch
    {
        Expression.Angry     => spriteAngry,
        Expression.Sad       => spriteSad,
        Expression.Fear      => spriteFear,
        Expression.Happy     => spriteHappy,
        Expression.Disgust   => spriteDisgust,
        Expression.Surprised => spriteSurprised,
        Expression.Contempt  => spriteContempt,
        _                    => spriteBlank,
    };

    static Expression ParseExpression(string s) => s?.ToLower() switch
    {
        "angry"     => Expression.Angry,
        "sad"       => Expression.Sad,
        "fear"      => Expression.Fear,
        "happy"     => Expression.Happy,
        "disgust"   => Expression.Disgust,
        "surprised" => Expression.Surprised,
        "contempt"  => Expression.Contempt,
        _           => Expression.Blank,
    };

    public List<SlimeController> GetPeers(SlimeController me)
    {
        var result = new List<SlimeController>();
        foreach (var s in slimes.Values)
            if (s != me && s.SlimeExpression == me.SlimeExpression)
                result.Add(s);
        return result;
    }

    public void OnDragStart(string id) { }
    public void OnDragEnd(string id)   { }
}
