using System.Collections;
using UnityEngine;

public class PopEffect : MonoBehaviour
{
    public static void Spawn(Vector3 pos, Color color, Sprite sprite, float slimeSize)
    {
        var go = new GameObject("PopEffect");
        go.transform.position = pos;
        go.AddComponent<PopEffect>().Play(color, sprite, slimeSize);
    }

    void Play(Color color, Sprite sprite, float slimeSize)
    {
        StartCoroutine(Run(color, sprite, slimeSize));
    }

    IEnumerator Run(Color color, Sprite sprite, float slimeSize)
    {
        const int COUNT = 16;
        for (int i = 0; i < COUNT; i++)
        {
            float angle = (i / (float)COUNT) * Mathf.PI * 2f + (Random.value - 0.5f) * 0.8f;
            float dist  = slimeSize * (2f + Random.value * 2.5f);
            StartCoroutine(Particle(
                transform.position,
                new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * dist,
                color, sprite,
                0.04f + Random.value * 0.07f,
                0.38f + Random.value * 0.28f,
                Random.value * 0.08f
            ));
        }

        yield return new WaitForSeconds(1f);
        Destroy(gameObject);
    }

    IEnumerator Particle(Vector3 origin, Vector2 delta, Color col, Sprite sprite, float size, float dur, float delay)
    {
        if (delay > 0f) yield return new WaitForSeconds(delay);

        var go = new GameObject("P");
        go.transform.position   = origin;
        go.transform.parent     = transform;
        go.transform.localScale = Vector3.one * size;

        var sr    = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.color  = col;

        for (float t = 0f; t < 1f; t += Time.deltaTime / dur)
        {
            go.transform.position   = Vector3.Lerp(origin, origin + (Vector3)(Vector2)delta, t);
            sr.color                = new Color(col.r, col.g, col.b, 1f - t);
            go.transform.localScale = Vector3.one * size * Mathf.Lerp(1f, 0.2f, t);
            yield return null;
        }

        Destroy(go);
    }
}
