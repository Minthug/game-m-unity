using UnityEngine;

[CreateAssetMenu(menuName = "Game-M/Background Theme")]
public class BackgroundTheme : ScriptableObject
{
    public string themeId;
    public string displayName;
    public Color  bgColor         = new Color(0.04f, 0.04f, 0.06f);
    public Sprite bgSprite;
    public bool   isAdUnlock      = true;
    public bool   defaultUnlocked = false;
}
