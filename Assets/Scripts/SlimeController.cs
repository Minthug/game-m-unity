using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D), typeof(SpriteRenderer))]
public class SlimeController : MonoBehaviour
{
    public string     SlimeId         { get; private set; }
    public string     SlimeText       { get; private set; }
    public Expression SlimeExpression { get; private set; }
    public int        Stage           { get; private set; }
    public Color      SlimeColor      { get; private set; }
    public long       CreatedAt       { get; private set; }

    Rigidbody2D    rb;
    SpriteRenderer sr;
    Camera         mainCam;

    bool    isMouseHeld;
    bool    isDragging;
    Vector2 dragOffset;
    float   originalSize;
    float   mouseDownTime;
    Vector2 mouseDownPos;
    bool    mergeCooldown = true;
    Coroutine holdRoutine;

    // 물리 파라미터
    const float MAX_SPEED = 1.8f;
    const float DAMPING   = 0.95f;
    const float DRIFT     = 0.03f;
    const float COHESION  = 0.021f;

    // 스케일 애니메이션
    Vector3 targetScale;
    const float SQUISH_SPEED = 8f;

    // 호흡 애니메이션
    float _breathOffset;        // 슬라임마다 다른 위상
    bool  _overrideAnim;        // SpawnAnimation / PopAnimation 이 제어 중일 때 true

    // ── 초기화 ───────────────────────────────────────────────────

    void Awake()
    {
        rb      = GetComponent<Rigidbody2D>();
        sr      = GetComponent<SpriteRenderer>();
        mainCam = Camera.main;

        rb.gravityScale   = 0f;
        rb.linearDamping  = 0f;
        rb.angularDamping = 5f;
        rb.constraints    = RigidbodyConstraints2D.FreezeRotation;

        rb.linearVelocity = new Vector2(
            (Random.value - 0.5f) * 0.4f,
            (Random.value - 0.5f) * 0.4f
        );

        _breathOffset = Random.Range(0f, Mathf.PI * 2f);
    }

    public void Init(string id, string text, Expression expression, Color color,
                     float worldSize, int stage = 1, long createdAt = 0)
    {
        SlimeId         = id;
        SlimeText       = text;
        SlimeExpression = expression;
        SlimeColor      = color;
        Stage           = Mathf.Clamp(stage, 1, 3);
        CreatedAt       = createdAt > 0 ? createdAt : ColorUtil.NowMs();
        sr.color        = Color.white;
        originalSize    = worldSize;
        targetScale     = Vector3.one * worldSize;
        GetComponent<CircleCollider2D>().radius = 0.42f;

        mergeCooldown = true;
        Invoke(nameof(EnableMerge), 1.5f);

        // 저장 불러오기(createdAt > 0)면 팝인 없이 바로 등장
        if (createdAt <= 0)
            StartCoroutine(SpawnAnimation());
        else
            transform.localScale = targetScale;

        Debug.Log($"[Slime] Init | id={id} stage={Stage} scale={worldSize}");
    }

    void EnableMerge() => mergeCooldown = false;

    void OnBecameVisible()   => Debug.Log($"[Slime] {SlimeId} 화면에 나타남");
    void OnBecameInvisible() => Debug.LogWarning($"[Slime] {SlimeId} 화면에서 사라짐");
    void OnDisable()  => Debug.LogWarning($"[Slime] {SlimeId} OnDisable");
    void OnDestroy()  => Debug.LogWarning($"[Slime] {SlimeId} OnDestroy");

    // ── 생성 팝인 애니메이션 ─────────────────────────────────────

    IEnumerator SpawnAnimation()
    {
        _overrideAnim = true;
        transform.localScale = Vector3.zero;
        targetScale          = Vector3.zero;

        // 키프레임: t → scale 배율
        float[] tKeys = { 0f,   0.35f, 0.55f, 0.75f, 1.0f };
        float[] sKeys = { 0f,   1.25f, 0.88f, 1.06f, 1.0f };
        float   dur   = 0.5f;
        float   elapsed = 0f;

        while (elapsed < dur)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / dur);
            float s = SampleCurve(t, tKeys, sKeys) * originalSize;
            transform.localScale = targetScale = new Vector3(s, s, 1f);
            yield return null;
        }

        transform.localScale = targetScale = Vector3.one * originalSize;
        _overrideAnim = false;
    }

    // ── 물리 ─────────────────────────────────────────────────────

    void FixedUpdate()
    {
        if (isDragging || isMouseHeld) return;

        Vector2 vel = rb.linearVelocity;

        vel += new Vector2(
            (Random.value - 0.5f) * DRIFT,
            (Random.value - 0.5f) * DRIFT
        );

        var peers = SlimeManager.Instance?.GetPeers(this);
        if (peers != null && peers.Count > 0)
        {
            Vector2 center  = Vector2.zero;
            float   avgDist = 0f;
            foreach (var p in peers)
            {
                center  += (Vector2)p.transform.position;
                avgDist += (transform.localScale.x + p.transform.localScale.x) * 0.42f;
            }
            center  /= peers.Count;
            avgDist /= peers.Count;

            Vector2 toCenter = center - (Vector2)transform.position;
            float   dist     = toCenter.magnitude;

            if (dist > avgDist * 0.5f && dist < 6f)
            {
                float force = Mathf.Min((dist - avgDist) / 2.5f, 1f) * COHESION;
                vel += toCenter.normalized * force;
            }
        }

        if (vel.magnitude > MAX_SPEED) vel = vel.normalized * MAX_SPEED;
        vel *= DAMPING;
        rb.linearVelocity = vel;

        ClampToBounds(ref vel);

        transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.fixedDeltaTime * SQUISH_SPEED);
    }

    void ClampToBounds(ref Vector2 vel)
    {
        if (mainCam == null) return;
        float h = mainCam.orthographicSize;
        float w = mainCam.orthographicSize * mainCam.aspect;
        float r = transform.localScale.x * 0.5f;

        Vector3 pos = transform.position;
        if (pos.x - r < -w)     { pos.x = -w + r; vel.x =  Mathf.Abs(vel.x) * 0.5f; }
        else if (pos.x + r > w) { pos.x =  w - r; vel.x = -Mathf.Abs(vel.x) * 0.5f; }
        if (pos.y - r < -h)     { pos.y = -h + r; vel.y =  Mathf.Abs(vel.y) * 0.5f; }
        else if (pos.y + r > h) { pos.y =  h - r; vel.y = -Mathf.Abs(vel.y) * 0.5f; }

        transform.position = pos;
        rb.linearVelocity  = vel;
    }

    void OnCollisionEnter2D(Collision2D col)
    {
        // 충돌 스쿼시
        if (!_overrideAnim)
        {
            targetScale = new Vector3(
                originalSize * 1.15f,
                originalSize * 0.87f, 1f);
            Invoke(nameof(ResetScale), 0.14f);
        }

        if (Stage >= 3 || mergeCooldown) return;
        var other = col.gameObject.GetComponent<SlimeController>();
        if (other == null || other.mergeCooldown) return;
        if (other.Stage != Stage || other.SlimeExpression != SlimeExpression) return;

        if (string.Compare(SlimeId, other.SlimeId, System.StringComparison.Ordinal) < 0)
            SlimeManager.Instance?.TryMerge(SlimeId, other.SlimeId);
    }

    void ResetScale() { /* 호흡 애니메이션이 Update에서 targetScale을 관리함 */ }

    // ── 업데이트 (입력 + 호흡) ───────────────────────────────────

    static SlimeController pressedSlime;
    CircleCollider2D col2d;
    void Start() => col2d = GetComponent<CircleCollider2D>();

    void Update()
    {
        HandleInput();

        // 호흡 애니메이션 — 외부 코루틴이 제어 중이거나 드래그/홀드 중일 때 건너뜀
        if (!_overrideAnim && !isMouseHeld)
        {
            float breath = Mathf.Sin(Time.time * 1.3f + _breathOffset) * 0.034f;
            targetScale = new Vector3(
                originalSize * (1f - breath * 0.45f),   // 가로는 살짝 반대
                originalSize * (1f + breath),             // 세로로 호흡
                1f
            );
        }
    }

    void HandleInput()
    {
        var mouse = Mouse.current;
        if (mouse == null) return;

        Vector2 mouseWorld = GetMouseWorld();

        // 버튼 누름
        if (mouse.leftButton.wasPressedThisFrame && pressedSlime == null)
        {
            var hit = Physics2D.OverlapPoint(mouseWorld);
            if (hit != null && hit.gameObject == gameObject)
            {
                pressedSlime  = this;
                isMouseHeld   = true;
                isDragging    = false;
                mouseDownTime = Time.time;
                mouseDownPos  = mouseWorld;
                rb.linearVelocity = Vector2.zero;
                dragOffset    = (Vector2)transform.position - mouseWorld;
                SlimeManager.Instance?.OnDragStart(SlimeId);
                holdRoutine = StartCoroutine(HoldTimer());
            }
        }

        // 드래그 중
        if (isMouseHeld && mouse.leftButton.isPressed)
        {
            if (!isDragging && (mouseWorld - mouseDownPos).magnitude > 0.12f)
            {
                isDragging = true;
                if (holdRoutine != null) { StopCoroutine(holdRoutine); holdRoutine = null; }
            }
            if (isDragging)
                transform.position = (Vector3)(mouseWorld + dragOffset);
        }

        // 버튼 뗌
        if (isMouseHeld && mouse.leftButton.wasReleasedThisFrame)
        {
            if (holdRoutine != null) { StopCoroutine(holdRoutine); holdRoutine = null; }

            bool wasDragging = isDragging;
            isMouseHeld  = false;
            isDragging   = false;
            pressedSlime = null;

            SlimeManager.Instance?.OnDragEnd(SlimeId);

            if (wasDragging)
            {
                rb.linearVelocity = (mouseWorld - (Vector2)transform.position) * 2f;
            }
            else
            {
                float elapsed = Time.time - mouseDownTime;
                float moved   = (mouseWorld - mouseDownPos).magnitude;
                bool  quickTap = elapsed < 0.25f && moved < 0.3f;
                if (quickTap && Stage > 1)
                    SlimeManager.Instance?.SplitSlime(SlimeId);
            }
        }
    }

    // ── 홀드 타이머 (경고 떨림 포함) ────────────────────────────

    IEnumerator HoldTimer()
    {
        float elapsed     = 0f;
        float holdDuration = 0.6f;

        while (elapsed < holdDuration)
        {
            if (!isMouseHeld || isDragging) yield break;
            elapsed += Time.deltaTime;

            // 홀드 진행도에 따라 진동이 커짐
            float t      = elapsed / holdDuration;
            float wobble = Mathf.Sin(elapsed * 30f) * t * 0.12f;
            targetScale = new Vector3(
                originalSize * (1f + wobble),
                originalSize * (1f - wobble * 0.8f),
                1f
            );
            yield return null;
        }

        if (isMouseHeld && !isDragging)
            TriggerPop();
    }

    // ── 팝 ───────────────────────────────────────────────────────

    public void TriggerPop()
    {
        if (holdRoutine != null) { StopCoroutine(holdRoutine); holdRoutine = null; }
        isMouseHeld  = false;
        isDragging   = false;
        pressedSlime = null;
        AudioManager.Instance?.PlayPop();
        StartCoroutine(PopAnimation());
    }

    IEnumerator PopAnimation()
    {
        _overrideAnim = true;
        rb.linearVelocity = Vector2.zero;
        rb.simulated      = false;

        // 1. 좌우 흔들기
        Vector3 origin = transform.position;
        float[] oxList = { 0.18f, -0.18f, 0.14f, -0.14f, 0.10f, -0.10f, 0.06f, -0.06f, 0.03f, -0.03f, 0f };
        foreach (float ox in oxList)
        {
            transform.position = origin + new Vector3(ox, 0f, 0f);
            yield return new WaitForSeconds(0.028f);
        }
        transform.position = origin;

        // 2. 스케일 wobble (4회)
        for (int i = 0; i < 4; i++)
        {
            for (float t = 0f; t < 1f; t += Time.deltaTime / 0.04f)
            {
                transform.localScale = new Vector3(
                    originalSize * Mathf.Lerp(1f, 1.18f, t),
                    originalSize * Mathf.Lerp(1f, 0.82f, t), 1f);
                yield return null;
            }
            for (float t = 0f; t < 1f; t += Time.deltaTime / 0.04f)
            {
                transform.localScale = new Vector3(
                    originalSize * Mathf.Lerp(1.18f, 0.82f, t),
                    originalSize * Mathf.Lerp(0.82f, 1.18f, t), 1f);
                yield return null;
            }
        }

        // 3. 납작하게 눌림
        for (float t = 0f; t < 1f; t += Time.deltaTime / 0.18f)
        {
            transform.localScale = new Vector3(
                originalSize * Mathf.Lerp(1f, 1.6f, t),
                originalSize * Mathf.Lerp(1f, 0.15f, t), 1f);
            yield return null;
        }

        // 4. 파티클 폭발
        PopEffect.Spawn(transform.position, SlimeColor, sr.sprite, originalSize);

        // 5. 줄어들며 소멸
        Vector3 flatScale = transform.localScale;
        for (float t = 0f; t < 1f; t += Time.deltaTime / 0.22f)
        {
            transform.localScale = Vector3.Lerp(flatScale, Vector3.zero, t);
            sr.color = new Color(1f, 1f, 1f, 1f - t);
            yield return null;
        }

        SlimeManager.Instance?.DeleteSlime(SlimeId);
    }

    // ── 유틸 ─────────────────────────────────────────────────────

    public void ApplyShake()
    {
        float angle = Random.value * Mathf.PI * 2f;
        float power = 2.5f + Random.value * 2.5f;
        rb.linearVelocity = new Vector2(Mathf.Cos(angle) * power, Mathf.Sin(angle) * power);
    }

    Vector2 GetMouseWorld()
    {
        Vector2 mp = Mouse.current != null
            ? Mouse.current.position.ReadValue()
            : (Vector2)Input.mousePosition;
        Vector3 wp = new(mp.x, mp.y, -mainCam.transform.position.z);
        return mainCam.ScreenToWorldPoint(wp);
    }

    // 키프레임 보간 (SpawnAnimation용)
    static float SampleCurve(float t, float[] tKeys, float[] sKeys)
    {
        for (int i = 0; i < tKeys.Length - 1; i++)
        {
            if (t >= tKeys[i] && t <= tKeys[i + 1])
            {
                float lt = (t - tKeys[i]) / (tKeys[i + 1] - tKeys[i]);
                return Mathf.Lerp(sKeys[i], sKeys[i + 1], lt);
            }
        }
        return sKeys[sKeys.Length - 1];
    }
}
