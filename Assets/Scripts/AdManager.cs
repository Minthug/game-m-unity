using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class AdManager : MonoBehaviour
{
    public static AdManager Instance { get; private set; }

    Canvas          adCanvas;
    TextMeshProUGUI countdownTmp;
    bool            showing;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        BuildUI();
    }

    void BuildUI()
    {
        var go = new GameObject("AdOverlay");
        go.transform.SetParent(transform, false);

        adCanvas = go.AddComponent<Canvas>();
        adCanvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        adCanvas.sortingOrder = 100;

        var scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        go.AddComponent<GraphicRaycaster>();

        var bgGO  = new GameObject("Bg"); bgGO.transform.SetParent(go.transform, false);
        var bgImg = bgGO.AddComponent<Image>();
        bgImg.color = new Color(0f, 0f, 0f, 0.88f);
        Stretch(bgGO.GetComponent<RectTransform>());

        var lblGO = new GameObject("Countdown"); lblGO.transform.SetParent(go.transform, false);
        countdownTmp           = lblGO.AddComponent<TextMeshProUGUI>();
        countdownTmp.fontSize  = 30f;
        countdownTmp.alignment = TextAlignmentOptions.Center;
        countdownTmp.color     = Color.white;
        var korFont = Resources.Load<TMP_FontAsset>("KoreanFont");
        if (korFont != null) countdownTmp.font = korFont;
        var lblRt = lblGO.GetComponent<RectTransform>();
        lblRt.anchorMin = new Vector2(0.1f, 0.42f);
        lblRt.anchorMax = new Vector2(0.9f, 0.58f);
        lblRt.offsetMin = lblRt.offsetMax = Vector2.zero;

        go.SetActive(false);
    }

    public void ShowRewardedAd(System.Action<bool> onComplete)
    {
        if (showing) return;
        StartCoroutine(Simulate(onComplete));
    }

    IEnumerator Simulate(System.Action<bool> onComplete)
    {
        showing = true;
        adCanvas.gameObject.SetActive(true);

        for (int i = 3; i > 0; i--)
        {
            countdownTmp.text = $"광고 시청 중... {i}초";
            yield return new WaitForSeconds(1f);
        }
        countdownTmp.text = "완료! 🎉";
        yield return new WaitForSeconds(0.6f);

        adCanvas.gameObject.SetActive(false);
        showing = false;
        onComplete?.Invoke(true);
    }

    static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }
}
