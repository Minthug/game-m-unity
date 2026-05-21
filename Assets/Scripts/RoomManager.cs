using System.Collections.Generic;
using UnityEngine;

public class RoomManager : MonoBehaviour
{
    public static RoomManager Instance { get; private set; }

    [Header("카탈로그")]
    public List<RoomItem>  itemCatalog  = new();
    public List<RoomTheme> themeCatalog = new();

    [Header("프리팹")]
    public GameObject roomItemPrefab;

    const string SAVE_KEY = "room_save";

    readonly Dictionary<string, RoomItemObject> placed = new();

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start() => LoadRoom();

    // ── 배치 ─────────────────────────────────────────────────────

    public void PlaceItem(string itemId, Vector3 position)
    {
        var data = itemCatalog.Find(x => x != null && x.itemId == itemId);
        if (data == null) { Debug.LogWarning($"[RoomMgr] '{itemId}' 카탈로그에 없음"); return; }

        string instanceId = $"{itemId}-{System.DateTime.Now.Ticks}";
        SpawnItem(instanceId, data, position);
        SaveRoom();
        Debug.Log($"[RoomMgr] 배치: {data.displayName} @ {position}");
    }

    public void RemoveItem(string instanceId)
    {
        if (!placed.TryGetValue(instanceId, out var obj)) return;
        placed.Remove(instanceId);
        Destroy(obj.gameObject);
        SaveRoom();
    }

    // ── 감정 연동 ─────────────────────────────────────────────────

    public void OnEmotionChanged(Expression e)
    {
        foreach (var obj in placed.Values)
            obj.OnEmotionChange(e);
    }

    // ── 저장 / 불러오기 ───────────────────────────────────────────

    public void SaveRoom()
    {
        var data = new RoomSaveData();
        foreach (var kv in placed)
        {
            var pos = kv.Value.transform.position;
            data.placed.Add(new RoomPlacementEntry
            {
                instanceId = kv.Key,
                itemId     = kv.Value.Data.itemId,
                x          = pos.x,
                y          = pos.y,
                scale      = kv.Value.transform.localScale.x,
            });
        }
        PlayerPrefs.SetString(SAVE_KEY, JsonUtility.ToJson(data));
        PlayerPrefs.Save();
    }

    void LoadRoom()
    {
        var json = PlayerPrefs.GetString(SAVE_KEY, "");
        if (string.IsNullOrEmpty(json)) return;

        RoomSaveData data;
        try { data = JsonUtility.FromJson<RoomSaveData>(json); }
        catch { Debug.LogWarning("[RoomMgr] 저장 데이터 파싱 실패"); return; }

        if (data?.placed == null) return;

        foreach (var entry in data.placed)
        {
            var itemData = itemCatalog.Find(x => x != null && x.itemId == entry.itemId);
            if (itemData == null) continue;
            SpawnItem(entry.instanceId, itemData, new Vector3(entry.x, entry.y, 0f), entry.scale);
        }
        Debug.Log($"[RoomMgr] 불러오기 완료 ({data.placed.Count}개)");
    }

    // ── 내부 ─────────────────────────────────────────────────────

    void SpawnItem(string instanceId, RoomItem data, Vector3 position, float scale = 0f)
    {
        if (roomItemPrefab == null) { Debug.LogError("[RoomMgr] roomItemPrefab 미할당"); return; }

        var go  = Instantiate(roomItemPrefab, position, Quaternion.identity);
        var obj = go.GetComponent<RoomItemObject>();
        obj.Init(instanceId, data, scale);

        placed[instanceId] = obj;
        go.name = $"RoomItem_{data.itemId}";
    }
}
