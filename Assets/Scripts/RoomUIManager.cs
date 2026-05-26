using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class RoomUIManager : MonoBehaviour
{
    public static RoomUIManager Instance { get; private set; }

    [Header("UI 참조 (Setup Scene이 자동 생성)")]
    public Canvas     rootCanvas;
    public GameObject shopPanel;
    public RectTransform itemGrid;
    public Button     openShopBtn;
    public Button     closeShopBtn;

    [Header("프리팹")]
    public GameObject itemButtonPrefab;

    bool       shopOpen;
    Expression currentEmotion = Expression.Blank;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        // SceneSetup의 AddListener는 씬에 저장 안 되므로 런타임에 연결
        if (openShopBtn  != null) openShopBtn.onClick.AddListener(OpenShop);
        if (closeShopBtn != null) closeShopBtn.onClick.AddListener(CloseShop);

        RefreshShop();      // 버튼 먼저 생성
        ApplyKoreanFont();  // 생성된 버튼 포함 전체 폰트 적용

        // 애니메이션 없이 즉시 숨김 — AnimatePanel 사용 시 0.2s flash 발생
        if (shopPanel != null)
        {
            var rect = shopPanel.GetComponent<RectTransform>();
            float h = rect.sizeDelta.y > 0 ? rect.sizeDelta.y : 420f;
            rect.anchoredPosition = new Vector2(0f, -h);
            shopPanel.SetActive(false);
        }
        shopOpen = false;
    }

    void ApplyKoreanFont()
    {
        var font = Resources.Load<TMPro.TMP_FontAsset>("KoreanFont");
        if (font == null) { Debug.LogWarning("[RoomUI] KoreanFont 없음 — Game-M/0. Setup Korean Font 먼저 실행"); return; }

        // RoomUIManager는 Canvas의 자식 — Canvas 전체에서 검색해야 형제 오브젝트도 포함됨
        var root = rootCanvas != null ? rootCanvas.transform : transform.root;
        foreach (var tmp in root.GetComponentsInChildren<TMPro.TextMeshProUGUI>(true))
            tmp.font = font;
    }

    // ── 상점 열기/닫기 ─────────────────────────────────────────────

    public void OpenShop()
    {
        RefreshShop();       // 열 때마다 최신 카탈로그 반영
        ApplyKoreanFont();
        SetShopVisible(true);
    }
    public void CloseShop()  => SetShopVisible(false);
    public void ToggleShop() { if (shopOpen) CloseShop(); else OpenShop(); }

    void SetShopVisible(bool visible)
    {
        shopOpen = visible;
        if (shopPanel != null)
        {
            StopAllCoroutines();
            StartCoroutine(AnimatePanel(visible));
        }
    }

    IEnumerator AnimatePanel(bool visible)
    {
        var rect  = shopPanel.GetComponent<RectTransform>();
        float h   = rect.rect.height > 0 ? rect.rect.height : 400f;
        float from = visible ? -h : 0f;
        float to   = visible ? 0f : -h;

        shopPanel.SetActive(true);
        for (float t = 0f; t < 1f; t += Time.deltaTime / 0.2f)
        {
            rect.anchoredPosition = new Vector2(0f, Mathf.Lerp(from, to, t));
            yield return null;
        }
        rect.anchoredPosition = new Vector2(0f, to);
        if (!visible) shopPanel.SetActive(false);
    }

    // ── 상점 목록 갱신 ────────────────────────────────────────────

    public void RefreshShop()
    {
        Debug.Log($"[RoomUI] RefreshShop — itemGrid:{itemGrid != null}, prefab:{itemButtonPrefab != null}, RoomMgr:{RoomManager.Instance != null}");
        if (itemGrid == null) { Debug.LogError("[RoomUI] itemGrid 없음 — Setup Scene 재실행 필요"); return; }
        if (itemButtonPrefab == null) { Debug.LogError("[RoomUI] itemButtonPrefab 없음 — Setup Scene 재실행 필요"); return; }

        foreach (Transform child in itemGrid)
            Destroy(child.gameObject);

        var catalog = RoomManager.Instance?.itemCatalog;
        if (catalog == null || catalog.Count == 0)
        {
            Debug.LogWarning("[RoomUI] 카탈로그 비어있음 — Game-M/Refresh Room Catalog 실행 필요");
            return;
        }
        Debug.Log($"[RoomUI] 상점 갱신: {catalog.Count}개");

        foreach (var item in catalog)
        {
            if (item == null) continue;
            var btn = Instantiate(itemButtonPrefab, itemGrid);
            SetupItemButton(btn, item);
        }

        // 레이아웃 즉시 재계산 (ContentSizeFitter가 다음 프레임까지 기다리지 않도록)
        UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(itemGrid);
    }

    void SetupItemButton(GameObject btn, RoomItem item)
    {
        // 카드 배경색을 현재 감정 색상으로
        var bg = btn.GetComponent<Image>();
        if (bg != null) { bg.sprite = null; bg.color = CurrentCardColor(); }

        // 아이콘 (previewIcon 우선, 없으면 sprite, 둘 다 없으면 placeholder)
        var icon = btn.transform.Find("Icon")?.GetComponent<Image>();
        if (icon != null)
        {
            if (item.ShopIcon != null)
            {
                icon.sprite = item.ShopIcon;
                icon.color  = Color.white;
            }
            else
            {
                icon.sprite = null;
                icon.color  = new Color(0.25f, 0.25f, 0.30f); // 아이콘 없는 3D 아이템 placeholder
            }
        }

        // 이름
        var label = btn.transform.Find("Label")?.GetComponent<TextMeshProUGUI>();
        if (label != null)
            label.text = item.displayName;

        // 가격
        var price = btn.transform.Find("Price")?.GetComponent<TextMeshProUGUI>();
        if (price != null)
            price.text = item.price == 0 ? "무료" : $"{item.price}원";

        // 잠금 오버레이
        var lockObj = btn.transform.Find("Lock")?.gameObject;
        if (lockObj != null)
            lockObj.SetActive(!item.isUnlocked);

        // 버튼 클릭
        var button = btn.GetComponent<Button>();
        if (button != null)
        {
            var captured = item;
            button.onClick.AddListener(() => OnItemClicked(captured));
            button.interactable = item.isUnlocked;
        }
    }

    void OnItemClicked(RoomItem item)
    {
        var cam = Camera.main;
        float hw = cam != null ? cam.orthographicSize * cam.aspect * 0.5f : 2f;
        float hh = cam != null ? cam.orthographicSize * 0.5f : 2f;
        var pos = new Vector3(
            UnityEngine.Random.Range(-hw, hw),
            UnityEngine.Random.Range(-hh, hh),
            0f);
        RoomManager.Instance?.PlaceItem(item.itemId, pos);
        CloseShop();
    }

    // ── 감정 연동 색상 ────────────────────────────────────────────

    public void ApplyEmotion(Expression e)
    {
        currentEmotion = e;

        var (panel, btn, btnClose, card) = EmotionUIColors(e);

        if (shopPanel != null)
        {
            var img = shopPanel.GetComponent<Image>();
            if (img != null) { img.sprite = null; img.color = panel; }
        }
        if (openShopBtn != null)
        {
            var img = openShopBtn.GetComponent<Image>();
            if (img != null) { img.sprite = null; img.color = btn; }
        }
        if (closeShopBtn != null)
        {
            var img = closeShopBtn.GetComponent<Image>();
            if (img != null) { img.sprite = null; img.color = btnClose; }
        }

        // 열려있는 상태라면 아이템 카드도 즉시 갱신
        if (shopOpen) RefreshShop();
    }

    Color CurrentCardColor()
    {
        var (_, _, _, card) = EmotionUIColors(currentEmotion);
        return card;
    }

    static (Color panel, Color btn, Color btnClose, Color card) EmotionUIColors(Expression e) => e switch
    {
        Expression.Angry     => (Hex("#200500", 0.97f), Hex("#3A0800"), Hex("#220400"), Hex("#2E0600")),
        Expression.Sad       => (Hex("#001030", 0.97f), Hex("#001844"), Hex("#000D20"), Hex("#001438")),
        Expression.Fear      => (Hex("#100020", 0.97f), Hex("#1A0038"), Hex("#0D0028"), Hex("#150030")),
        Expression.Happy     => (Hex("#140C28", 0.97f), Hex("#1E1040"), Hex("#110830"), Hex("#1A1035")),
        Expression.Disgust   => (Hex("#041208", 0.97f), Hex("#061A0A"), Hex("#030C05"), Hex("#051408")),
        Expression.Surprised => (Hex("#0E0030", 0.97f), Hex("#160048"), Hex("#0A001C"), Hex("#120038")),
        Expression.Contempt  => (Hex("#0A0E18", 0.97f), Hex("#121828"), Hex("#080C14"), Hex("#0E1420")),
        _                    => (Hex("#0F0F18", 0.97f), Hex("#181820"), Hex("#101018"), Hex("#141420")),
    };

    static Color Hex(string hex, float a = 1f)
    {
        ColorUtility.TryParseHtmlString(hex, out var c);
        c.a = a;
        return c;
    }
}
