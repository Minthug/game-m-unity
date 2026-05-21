using System.Collections.Generic;
using UnityEngine;

public class EnvironmentManager : MonoBehaviour
{
    public static EnvironmentManager Instance { get; private set; }

    [Header("환경 아이템 카탈로그")]
    public List<EnvironmentItem> catalog = new();

    readonly Dictionary<string, GameObject> activeObjs = new();

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        foreach (var item in catalog)
            if (item != null && item.isUnlocked)
                Activate(item.itemId);
    }

    public void Activate(string itemId)
    {
        if (activeObjs.ContainsKey(itemId)) return;

        var item = catalog.Find(x => x != null && x.itemId == itemId);
        if (item == null || item.sprite == null)
        {
            Debug.LogWarning($"[EnvMgr] '{itemId}' 없음 또는 스프라이트 미할당");
            return;
        }

        var cam   = Camera.main;
        float hH  = cam.orthographicSize;
        float hW  = hH * cam.aspect;

        // 스케일: 화면을 최소한 덮도록 (높이 기준, 너비도 보장)
        float sprH  = item.sprite.bounds.size.y;
        float sprW  = item.sprite.bounds.size.x;
        float sclH  = (hH * 2f * item.scale) / sprH;
        float sclW  = (hW * 2f) / sprW;
        float scl   = Mathf.Max(sclH, sclW);
        float wldW  = sprW * scl; // 레이어 1장의 월드 너비

        // 부모 오브젝트: x = -wldW 에서 시작 (두 레이어가 [-wldW, +wldW] 커버)
        var go = new GameObject($"EnvItem_{itemId}");
        go.transform.SetParent(transform, false);
        go.transform.position = new Vector3(-wldW, 0f, 0f);

        // 레이어 2장 나란히 → 끊김 없는 스크롤
        for (int i = 0; i < 2; i++)
        {
            var child = new GameObject($"L{i}");
            child.transform.SetParent(go.transform, false);
            child.transform.localPosition = new Vector3(wldW * i, 0f, 0f);
            child.transform.localScale    = Vector3.one * scl;

            var sr          = child.AddComponent<SpriteRenderer>();
            sr.sprite       = item.sprite;
            sr.sortingOrder = item.sortingOrder;
            sr.color        = new Color(1f, 1f, 1f, item.alpha);
        }

        var scroller = go.AddComponent<EnvItemScroller>();
        scroller.Init(item, wldW);

        activeObjs[itemId] = go;
        Debug.Log($"[EnvMgr] 활성화: {itemId}");
    }

    public void Deactivate(string itemId)
    {
        if (activeObjs.TryGetValue(itemId, out var go))
        {
            activeObjs.Remove(itemId);
            Destroy(go);
            Debug.Log($"[EnvMgr] 비활성화: {itemId}");
        }
    }

    public void Toggle(string itemId)
    {
        if (activeObjs.ContainsKey(itemId)) Deactivate(itemId);
        else                                Activate(itemId);
    }

    // 카탈로그 첫 번째 아이템 토글 (테스트용)
    public void ToggleFirst()
    {
        if (catalog.Count == 0) { Debug.LogWarning("[EnvMgr] catalog 비어있음 — Setup Scene 재실행 필요"); return; }
        var item = catalog[0];
        Debug.Log($"[EnvMgr] ToggleFirst: itemId={item?.itemId} sprite={(item?.sprite != null ? item.sprite.name : "NULL")}");
        Toggle(item.itemId);
    }
}
