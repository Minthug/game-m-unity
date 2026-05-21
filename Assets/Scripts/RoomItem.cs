using UnityEngine;

[CreateAssetMenu(menuName = "Game-M/Room Item")]
public class RoomItem : ScriptableObject
{
    public string     itemId;
    public string     displayName;

    [Header("2D 스프라이트 (2D 아이템)")]
    public Sprite     sprite;

    [Header("3D 프리팹 (3D 가구 등)")]
    public GameObject prefab;

    [Header("상점 미리보기 아이콘 (비우면 sprite 사용)")]
    public Sprite     previewIcon;

    public string  category     = "furniture";
    [Range(0.1f, 3f)]
    public float   defaultScale = 1f;
    public int     sortingOrder = 3;
    public bool    isUnlocked   = false;
    public int     price        = 0;

    public Sprite  ShopIcon => previewIcon != null ? previewIcon : sprite;
    public bool    Is3D     => prefab != null;
}
