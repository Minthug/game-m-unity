using UnityEngine;

// 환경 아이템 하나의 스크롤 + 알파 펄스를 담당
public class EnvItemScroller : MonoBehaviour
{
    float          scrollSpeedX;
    float          baseAlpha;
    float          worldW;
    float          originX;
    SpriteRenderer[] srs;

    public void Init(EnvironmentItem item, float layerWorldWidth)
    {
        scrollSpeedX = item.scrollSpeedX;
        baseAlpha    = item.alpha;
        worldW       = layerWorldWidth;
        originX      = transform.position.x; // Activate에서 -worldW로 설정됨
        srs          = GetComponentsInChildren<SpriteRenderer>();
    }

    void Update()
    {
        if (scrollSpeedX != 0f)
        {
            Vector3 pos = transform.position;
            pos.x += scrollSpeedX * Time.deltaTime;

            // [originX, originX + worldW) 범위 안에서 순환
            if      (pos.x >= originX + worldW) pos.x -= worldW;
            else if (pos.x <  originX)          pos.x += worldW;

            transform.position = pos;
        }

        // 부드러운 알파 펄스
        float a = baseAlpha + Mathf.Sin(Time.time * 0.4f) * 0.06f;
        a = Mathf.Clamp01(a);
        foreach (var sr in srs)
            sr.color = new Color(1f, 1f, 1f, a);
    }
}
