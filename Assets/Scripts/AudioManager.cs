using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    AudioSource _sfxSource;
    AudioSource _bgmSource;
    float       _sfxVolume = 1f;

    readonly Dictionary<string, AudioClip> _clips = new();

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;

        _sfxSource = gameObject.AddComponent<AudioSource>();
        _sfxSource.playOnAwake = false;

        _bgmSource = gameObject.AddComponent<AudioSource>();
        _bgmSource.playOnAwake = false;
        _bgmSource.loop = true;

        foreach (var clip in Resources.LoadAll<AudioClip>("Sounds"))
            _clips[clip.name] = clip;

        Debug.Log($"[AudioManager] {_clips.Count}개 클립 로드: {string.Join(", ", _clips.Keys)}");
    }

    void Start()
    {
        // SettingsManager는 Awake에서 초기화 → Start에서 볼륨 적용
        _sfxVolume        = SettingsManager.Instance?.SfxVolume ?? 1f;
        _bgmSource.volume = SettingsManager.Instance?.BgmVolume ?? 1f;
    }

    // ── 볼륨 제어 ──────────────────────────────────────────────

    public void SetBgmVolume(float v) => _bgmSource.volume = v;
    public void SetSfxVolume(float v) => _sfxVolume = v;

    // ── BGM ────────────────────────────────────────────────────

    public void PlayBgm(string clipName)
    {
        if (_clips.TryGetValue(clipName, out var clip))
        {
            _bgmSource.clip = clip;
            _bgmSource.Play();
        }
    }

    public void StopBgm() => _bgmSource.Stop();

    // ── SFX 액션 ───────────────────────────────────────────────
    // bubble_pop 하나를 피치 변조로 상황별로 다르게 활용

    public void PlaySpawn()     => PlaySfx("bubble_pop", 1.0f + Random.Range(-0.12f, 0.12f));
    public void PlayPop()       => PlaySfx("bubble_pop", 1.5f);
    public void PlayMerge()     => PlaySfx("bubble_pop", 0.72f);
    public void PlaySplit()     => PlaySfx("bubble_pop", 1.25f);
    public void PlayMilestone() => PlaySfx("bubble_pop", 1.8f);

    // ── 내부 ───────────────────────────────────────────────────

    void PlaySfx(string nameFragment, float pitch = 1f)
    {
        AudioClip clip = null;
        foreach (var kv in _clips)
            if (kv.Key.Contains(nameFragment)) { clip = kv.Value; break; }

        if (clip == null)
        {
            // fragment 없으면 첫 번째 클립 fallback
            foreach (var kv in _clips) { clip = kv.Value; break; }
        }
        if (clip == null) return;

        _sfxSource.pitch  = pitch;
        _sfxSource.volume = _sfxVolume;
        _sfxSource.PlayOneShot(clip);
    }
}
