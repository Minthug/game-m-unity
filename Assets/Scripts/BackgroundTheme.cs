using UnityEngine;

[CreateAssetMenu(menuName = "Game-M/Background Theme")]
public class BackgroundTheme : ScriptableObject
{
    public string themeId;
    public string displayName;
    public Color  bgColor         = new Color(0.04f, 0.04f, 0.06f); // 상단 (대표색)
    public Color  bgColorBottom   = new Color(0.02f, 0.02f, 0.03f); // 하단
    public Sprite bgSprite;
    public bool   isAdUnlock      = true;
    public bool   defaultUnlocked = false;
}
