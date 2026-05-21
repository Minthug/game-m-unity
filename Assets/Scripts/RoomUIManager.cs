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
    public Transform  itemGrid;
    public Button     openShopBtn;
    public Button     closeShopBtn;

    [Header("프리팹")]
    public GameObject itemButtonPrefab;

    bool shopOpen;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        RefreshShop();
        SetShopVisible(false);
    }

    // ── 상점 열기/닫기 ─────────────────────────────────────────────

    public void OpenShop()  => SetShopVisible(true);
    public void CloseShop() => SetShopVisible(false);
    public void ToggleShop() => SetShopVisible(!shopOpen);

    void SetShopVisible(bool visible)
    {
        shopOpen = visible;
        if (shopPanel != null)
            StartCoroutine(AnimatePanel(visible));
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
        if (itemGrid == null || itemButtonPrefab == null) return;

        foreach (Transform child in itemGrid)
            Destroy(child.gameObject);

        var catalog = RoomManager.Instance?.itemCatalog;
        if (catalog == null) return;

        foreach (var item in catalog)
        {
            if (item == null) continue;
            var btn = Instantiate(itemButtonPrefab, itemGrid);
            SetupItemButton(btn, item);
        }
    }

    void SetupItemButton(GameObject btn, RoomItem item)
    {
        // 아이콘
        var icon = btn.transform.Find("Icon")?.GetComponent<Image>();
        if (icon != null && item.sprite != null)
            icon.sprite = item.sprite;

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
        // 화면 중앙에 배치 후 상점 닫기
        RoomManager.Instance?.PlaceItem(item.itemId, Vector3.zero);
        CloseShop();
    }
}
