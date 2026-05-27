using UnityEngine;

public class SettingsManager : MonoBehaviour
{
    public static SettingsManager Instance { get; private set; }

    const string PREF_BGM = "vol_bgm";
    const string PREF_SFX = "vol_sfx";

    public float BgmVolume { get; private set; } = 1f;
    public float SfxVolume { get; private set; } = 1f;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        BgmVolume = PlayerPrefs.GetFloat(PREF_BGM, 1f);
        SfxVolume = PlayerPrefs.GetFloat(PREF_SFX, 1f);
        AudioListener.volume = BgmVolume;
    }

    public void SetBgmVolume(float v)
    {
        BgmVolume = Mathf.Clamp01(v);
        PlayerPrefs.SetFloat(PREF_BGM, BgmVolume);
        AudioListener.volume = BgmVolume;
    }

    public void SetSfxVolume(float v)
    {
        SfxVolume = Mathf.Clamp01(v);
        PlayerPrefs.SetFloat(PREF_SFX, SfxVolume);
    }

    public void ResetAllData()
    {
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();
        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
    }
}
