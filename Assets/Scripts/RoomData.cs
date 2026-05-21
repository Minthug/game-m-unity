// 방 배치 상태 저장용 데이터 구조

[System.Serializable]
public class RoomPlacementEntry
{
    public string instanceId;
    public string itemId;
    public float  x, y;
    public float  scale;
}

[System.Serializable]
public class RoomSaveData
{
    public string themeId = "default";
    public System.Collections.Generic.List<RoomPlacementEntry> placed = new();
}
