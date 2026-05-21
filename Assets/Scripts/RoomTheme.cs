using UnityEngine;

[CreateAssetMenu(menuName = "Game-M/Room Theme")]
public class RoomTheme : ScriptableObject
{
    public string themeId;
    public string displayName;
    public Color  backgroundColor = Color.white;
    public Sprite wallSprite;
    public Sprite floorSprite;
    public bool   isUnlocked = true;
    public int    price      = 0;
}
