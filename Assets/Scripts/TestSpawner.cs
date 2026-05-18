using UnityEngine;
using UnityEngine.InputSystem;

public class TestSpawner : MonoBehaviour
{
    readonly string[] expressions = { "angry", "sad", "fear", "happy", "disgust", "surprised", "contempt", "blank" };
    readonly string[] colors = { "#DC2626", "#2563EB", "#4F46E5", "#7C3AED", "#16A34A", "#9333EA", "#64748B", "#6B7280" };

    void Update()
    {
        var kb = Keyboard.current;
        if (kb == null) return;

        if (kb.spaceKey.wasPressedThisFrame)
        {
            int i = Random.Range(0, expressions.Length);
            var req = new SlimeCreateRequest
            {
                id         = $"test-{System.DateTime.Now.Ticks}",
                text       = "테스트",
                expression = expressions[i],
                color      = colors[i],
                size       = Random.Range(48f, 95f),
            };
            SlimeManager.Instance?.CreateSlimeFromWeb(JsonUtility.ToJson(req));
        }

        if (kb.sKey.wasPressedThisFrame)
            SlimeManager.Instance?.TriggerShake("");
    }
}
