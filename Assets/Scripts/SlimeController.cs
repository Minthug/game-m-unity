using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D), typeof(SpriteRenderer))]
public class SlimeController : MonoBehaviour
{
    public string     SlimeId         { get; private set; }
    public Expression SlimeExpression { get; private set; }
    public int        Stage           { get; private set; }
    public Color      SlimeColor      { get; private set; }

    Rigidbody2D    rb;
    SpriteRenderer sr;
    Camera         mainCam;

    // 입력 상태 (isMouseHeld: 버튼 누름, isDragging: 실제 이동 중)
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
    }

    public void Init(string id, Expression expression, Color color, float worldSize, int stage = 1)
    {
        SlimeId         = id;
        SlimeExpression = expression;
        SlimeColor      = color;
        Stage           = Mathf.Clamp(stage, 1, 3);
        sr.color        = Color.white;
        originalSize    = worldSize;
        targetScale     = Vector3.one * worldSize;
        transform.localScale = targetScale;
        GetComponent<CircleCollider2D>().radius = 0.42f;

        mergeCooldown = true;
        Invoke(nameof(EnableMerge), 1.5f);

        Debug.Log($"[Slime] Init 완료 | id={id} stage={Stage} pos={transform.position} scale={worldSize}");
    }

    void EnableMerge() => mergeCooldown = false;

    void OnBecameVisible()   => Debug.Log($"[Slime] {SlimeId} 화면에 나타남");
    void OnBecameInvisible() => Debug.LogWarning($"[Slime] {SlimeId} 화면에서 사라짐");
    void OnDisable()  => Debug.LogWarning($"[Slime] {SlimeId} OnDisable");
    void OnDestroy()  => Debug.LogWarning($"[Slime] {SlimeId} OnDestroy");

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
        if (pos.x - r < -w)      { pos.x = -w + r; vel.x =  Mathf.Abs(vel.x) * 0.5f; }
        else if (pos.x + r > w)  { pos.x =  w - r; vel.x = -Mathf.Abs(vel.x) * 0.5f; }
        if (pos.y - r < -h)      { pos.y = -h + r; vel.y =  Mathf.Abs(vel.y) * 0.5f; }
        else if (pos.y + r > h)  { pos.y =  h - r; vel.y = -Mathf.Abs(vel.y) * 0.5f; }

        transform.position    = pos;
        rb.linearVelocity     = vel;
    }

    void OnCollisionEnter2D(Collision2D col)
    {
        targetScale = new Vector3(
            transform.localScale.x * 1.15f,
            transform.localScale.y * 0.88f,
            1f
        );
        Invoke(nameof(ResetScale), 0.12f);

        if (Stage >= 3 || mergeCooldown) return;
        var other = col.gameObject.GetComponent<SlimeController>();
        if (other == null || other.mergeCooldown) return;
        if (other.Stage != Stage || other.SlimeExpression != SlimeExpression) return;

        if (string.Compare(SlimeId, other.SlimeId, System.StringComparison.Ordinal) < 0)
            SlimeManager.Instance?.TryMerge(SlimeId, other.SlimeId);
    }

    void ResetScale() => targetScale = Vector3.one * originalSize;

    // ── 입력 ─────────────────────────────────────────────────

    void OnMouseDown()
    {
        isMouseHeld   = true;
        isDragging    = false;
        mouseDownTime = Time.time;
        mouseDownPos  = GetMouseWorld();
        rb.linearVelocity = Vector2.zero;
        dragOffset    = (Vector2)transform.position - mouseDownPos;
        SlimeManager.Instance?.OnDragStart(SlimeId);

        holdRoutine = StartCoroutine(HoldTimer());
    }

    void OnMouseDrag()
    {
        if (!isMouseHeld) return;

        var mousePos = GetMouseWorld();

        // 이동 감지 → 드래그로 전환, 홀드 타이머 취소
        if (!isDragging && (mousePos - mouseDownPos).magnitude > 0.12f)
        {
            isDragging = true;
            if (holdRoutine != null) { StopCoroutine(holdRoutine); holdRoutine = null; }
        }

        if (isDragging)
            transform.position = (Vector3)(mousePos + dragOffset);
    }

    void OnMouseUp()
    {
        if (holdRoutine != null) { StopCoroutine(holdRoutine); holdRoutine = null; }

        bool wasDragging = isDragging;
        isMouseHeld = false;
        isDragging  = false;

        var releasePos = GetMouseWorld();
        SlimeManager.Instance?.OnDragEnd(SlimeId);

        if (wasDragging)
        {
            rb.linearVelocity = (releasePos - (Vector2)transform.position) * 2f;
        }
        else
        {
            // 탭 → 2단계 이상 분리
            float elapsed    = Time.time - mouseDownTime;
            float moved      = (releasePos - mouseDownPos).magnitude;
            bool  isQuickTap = elapsed < 0.25f && moved < 0.3f;
            Debug.Log($"[Slime] tap: elapsed={elapsed:F3}s moved={moved:F3} stage={Stage} quickTap={isQuickTap}");
            if (isQuickTap && Stage > 1)
                SlimeManager.Instance?.SplitSlime(SlimeId);
        }
    }

    IEnumerator HoldTimer()
    {
        yield return new WaitForSeconds(0.6f);
        if (isMouseHeld && !isDragging)
            StartCoroutine(PopAnimation());
    }

    IEnumerator PopAnimation()
    {
        isMouseHeld = false;
        isDragging  = false;
        rb.linearVelocity = Vector2.zero;
        rb.simulated      = false;

        // 1. 흔들기
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

    Vector2 GetMouseWorld()
    {
        Vector2 mp = Mouse.current != null
            ? Mouse.current.position.ReadValue()
            : (Vector2)Input.mousePosition;
        Vector3 wp = new(mp.x, mp.y, -mainCam.transform.position.z);
        return mainCam.ScreenToWorldPoint(wp);
    }

    public void ApplyShake()
    {
        float angle = Random.value * Mathf.PI * 2f;
        float power = 2.5f + Random.value * 2.5f;
        rb.linearVelocity = new Vector2(Mathf.Cos(angle) * power, Mathf.Sin(angle) * power);
    }
}
