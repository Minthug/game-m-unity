using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D), typeof(SpriteRenderer))]
public class SlimeController : MonoBehaviour
{
    public string SlimeId { get; private set; }
    public Expression SlimeExpression { get; private set; }

    Rigidbody2D  rb;
    SpriteRenderer sr;
    Camera mainCam;

    bool    isDragging;
    Vector2 dragOffset;
    float   originalSize;

    // 물리 파라미터
    const float MAX_SPEED  = 1.8f;
    const float DAMPING    = 0.95f;
    const float DRIFT      = 0.03f;
    const float COHESION   = 0.021f;

    // 스케일 애니메이션
    Vector3 targetScale;
    const float SQUISH_SPEED = 8f;

    void Awake()
    {
        rb      = GetComponent<Rigidbody2D>();
        sr      = GetComponent<SpriteRenderer>();
        mainCam = Camera.main;

        rb.gravityScale = 0f;
        rb.linearDamping       = 0f;
        rb.angularDamping  = 5f;
        rb.constraints  = RigidbodyConstraints2D.FreezeRotation;

        // 초기 랜덤 속도
        rb.linearVelocity = new Vector2(
            (Random.value - 0.5f) * 0.4f,
            (Random.value - 0.5f) * 0.4f
        );
    }

    public void Init(string id, Expression expression, Color color, float worldSize)
    {
        SlimeId         = id;
        SlimeExpression = expression;
        sr.color        = color;
        originalSize         = worldSize;
        targetScale          = Vector3.one * worldSize;
        transform.localScale = targetScale;
        GetComponent<CircleCollider2D>().radius = 0.42f;
    }

    void FixedUpdate()
    {
        if (isDragging) return;

        Vector2 vel = rb.linearVelocity;

        // 무작위 표류
        vel += new Vector2(
            (Random.value - 0.5f) * DRIFT,
            (Random.value - 0.5f) * DRIFT
        );

        // 같은 감정 군집
        var peers = SlimeManager.Instance?.GetPeers(this);
        if (peers != null && peers.Count > 0)
        {
            Vector2 center = Vector2.zero;
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

        // 속도 제한
        if (vel.magnitude > MAX_SPEED)
            vel = vel.normalized * MAX_SPEED;

        vel *= DAMPING;
        rb.linearVelocity = vel;

        // 화면 경계 반사
        ClampToBounds(ref vel);

        // 스케일 스쿼시 애니메이션
        transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.fixedDeltaTime * SQUISH_SPEED);
    }

    void ClampToBounds(ref Vector2 vel)
    {
        if (mainCam == null) return;
        float h = mainCam.orthographicSize;
        float w = mainCam.orthographicSize * mainCam.aspect;
        float r = transform.localScale.x * 0.5f;

        Vector3 pos = transform.position;
        if (pos.x - r < -w) { pos.x = -w + r; vel.x =  Mathf.Abs(vel.x) * 0.5f; }
        else if (pos.x + r >  w) { pos.x =  w - r; vel.x = -Mathf.Abs(vel.x) * 0.5f; }
        if (pos.y - r < -h) { pos.y = -h + r; vel.y =  Mathf.Abs(vel.y) * 0.5f; }
        else if (pos.y + r >  h) { pos.y =  h - r; vel.y = -Mathf.Abs(vel.y) * 0.5f; }

        transform.position  = pos;
        rb.linearVelocity        = vel;
    }

    void OnCollisionEnter2D(Collision2D col)
    {
        // 충돌 시 스쿼시 효과
        targetScale = new Vector3(
            transform.localScale.x * 1.15f,
            transform.localScale.y * 0.88f,
            1f
        );
        Invoke(nameof(ResetScale), 0.12f);
    }

    void ResetScale() => targetScale = Vector3.one * originalSize;

    // --- 드래그 ---

    void OnMouseDown()
    {
        isDragging = true;
        rb.linearVelocity  = Vector2.zero;
        dragOffset = (Vector2)transform.position - GetMouseWorld();
        SlimeManager.Instance?.OnDragStart(SlimeId);
    }

    void OnMouseDrag()
    {
        if (!isDragging) return;
        transform.position = (Vector3)(GetMouseWorld() + dragOffset);
    }

    void OnMouseUp()
    {
        isDragging = false;
        rb.linearVelocity  = (GetMouseWorld() - (Vector2)transform.position) * 2f;
        SlimeManager.Instance?.OnDragEnd(SlimeId);
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
