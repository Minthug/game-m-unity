using UnityEngine;

[CreateAssetMenu(menuName = "Game-M/Room Item")]
public class RoomItem : ScriptableObject
{
    public string  itemId;
    public string  displayName;
    public Sprite  sprite;
    public string  category     = "furniture"; // furniture / decoration / plant
    [Range(0.1f, 3f)]
    public float   defaultScale = 1f;
    public int     sortingOrder = 3;
    public bool    isUnlocked   = false;
    public int     price        = 0;
}
