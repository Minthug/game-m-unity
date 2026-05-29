using System.Collections;
using UnityEngine;
using TMPro;

public class ReleaseTextEffect : MonoBehaviour
{
    public static void Spawn(Vector3 pos, Expression expr, Color col)
    {
        var go = new GameObject("ReleaseTextEffect");
        go.AddComponent<ReleaseTextEffect>().Play(pos, expr, col);
    }

    void Play(Vector3 pos, Expression expr, Color col)
    {
        StartCoroutine(Run(pos, expr, col));
    }

    IEnumerator Run(Vector3 pos, Expression expr, Color col)
    {
        var tmp  = gameObject.AddComponent<TextMeshPro>();
        var font = Resources.Load<TMP_FontAsset>("KoreanFont");
        if (font != null) tmp.font = font;
        tmp.text         = ExprKorean(expr) + " 털어냈어요";
        tmp.fontSize     = 1.6f;
        tmp.alignment    = TextAlignmentOptions.Center;
        tmp.sortingOrder = 20;

        float dur = 1.6f;
        for (float t = 0f; t < dur; t += Time.deltaTime)
        {
            float p = t / dur;
            transform.position = pos + Vector3.up * (0.5f + 1.8f * p);
            float alpha = p < 0.12f ? p / 0.12f : 1f - (p - 0.12f) / 0.88f;
            tmp.color = new Color(col.r, col.g, col.b, alpha);
            yield return null;
        }

        Destroy(gameObject);
    }

    static string ExprKorean(Expression e) => e switch
    {
        Expression.Angry     => "분노",
        Expression.Sad       => "슬픔",
        Expression.Happy     => "기쁨",
        Expression.Fear      => "두려움",
        Expression.Surprised => "놀람",
        Expression.Disgust   => "혐오",
        Expression.Contempt  => "경멸",
        _                    => "감정",
    };
}
