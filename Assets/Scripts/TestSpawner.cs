using UnityEngine;
using UnityEngine.InputSystem;

public class TestSpawner : MonoBehaviour
{
    readonly string[] expressions = { "angry", "sad", "fear", "happy", "disgust", "surprised", "contempt", "blank" };
    readonly string[] colors      = { "#DC2626", "#2563EB", "#4F46E5", "#7C3AED", "#16A34A", "#9333EA", "#64748B", "#6B7280" };

    void Awake()
    {
#if !UNITY_EDITOR && !DEVELOPMENT_BUILD
        Destroy(this);
#endif
    }

    void Update()
    {
        var kb = Keyboard.current;
        if (kb == null) return;

        if (kb.spaceKey.wasPressedThisFrame)
        {
            if (SlimeManager.Instance == null)
            {
                Debug.LogError("[TestSpawner] SlimeManager.Instance가 null — Setup Scene을 다시 실행하세요");
                return;
            }
            if (SlimeManager.Instance.slimePrefab == null)
            {
                Debug.LogError("[TestSpawner] slimePrefab이 null — Setup Scene을 다시 실행하세요");
                return;
            }
            int i = Random.Range(0, expressions.Length);
            var req = new SlimeCreateRequest
            {
                id         = $"test-{System.DateTime.Now.Ticks}",
                text       = "테스트",
                expression = expressions[i],
                color      = colors[i],
                size       = Random.Range(48f, 95f),
            };
            SlimeManager.Instance.CreateSlimeFromWeb(JsonUtility.ToJson(req));
            Debug.Log($"[TestSpawner] 슬라임 소환: {expressions[i]}");
        }

        if (kb.sKey.wasPressedThisFrame)
            SlimeManager.Instance?.TriggerShake("");
    }
}
