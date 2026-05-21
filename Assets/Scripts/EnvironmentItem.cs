using UnityEngine;

[CreateAssetMenu(menuName = "Game-M/Environment Item")]
public class EnvironmentItem : ScriptableObject
{
    public string  itemId       = "";
    public string  displayName  = "";
    public Sprite  sprite;

    [Range(0f, 1f)]
    public float   alpha        = 0.25f;
    public float   scale        = 1.2f;      // 화면 높이 대비 배율
    public float   scrollSpeedX = 0.15f;     // 초당 유닛, 음수=왼쪽
    public int     sortingOrder = 5;         // 슬라임(0) 위 오버레이

    public bool    isUnlocked   = true;
    public int     price        = 0;
}
