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

        // Space / 1 / 2 / 3 : 단계별 소환
        int spawnStage = 0;
        if (kb.spaceKey.wasPressedThisFrame || kb.digit1Key.wasPressedThisFrame) spawnStage = 1;
        else if (kb.digit2Key.wasPressedThisFrame) spawnStage = 2;
        else if (kb.digit3Key.wasPressedThisFrame) spawnStage = 3;

        if (spawnStage > 0)
        {
            if (SlimeManager.Instance == null)
            {
                Debug.LogError("[TestSpawner] SlimeManager.Instance가 null — Setup Scene을 다시 실행하세요");
                return;
            }
            int i = Random.Range(0, expressions.Length);
            var req = new SlimeCreateRequest
            {
                id         = $"test-{System.DateTime.Now.Ticks}",
                text       = "테스트",
                expression = expressions[i],
                color      = colors[i],
                stage      = spawnStage,
            };
            SlimeManager.Instance.CreateSlimeFromWeb(JsonUtility.ToJson(req));
            Debug.Log($"[TestSpawner] {spawnStage}단계 슬라임 소환: {expressions[i]}");
        }

        if (kb.sKey.wasPressedThisFrame)
            SlimeManager.Instance?.TriggerShake("");

        // D: 2단계 이상 슬라임 첫 번째를 강제 분리
        if (kb.dKey.wasPressedThisFrame)
            SlimeManager.Instance?.SplitFirst();

        // P: 첫 번째 슬라임 팝 (꾹 누르기 효과 테스트)
        if (kb.pKey.wasPressedThisFrame)
            SlimeManager.Instance?.PopFirst();
    }
}
