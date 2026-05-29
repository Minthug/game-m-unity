using UnityEngine;

/// <summary>
/// 런타임에 RectTransform을 Screen.safeArea에 맞게 조정.
/// 노치 / 다이나믹 아일랜드 / 홈 인디케이터 영역 자동 회피.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class SafeAreaFitter : MonoBehaviour
{
    void Start()
    {
        Apply();
    }

    public void Apply()
    {
        var rt   = GetComponent<RectTransform>();
        var area = Screen.safeArea;

        rt.anchorMin = new Vector2(area.x / Screen.width,  area.y / Screen.height);
        rt.anchorMax = new Vector2((area.x + area.width)  / Screen.width,
                                   (area.y + area.height) / Screen.height);
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }
}
