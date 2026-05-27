using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MilestoneManager : MonoBehaviour
{
    public static MilestoneManager Instance { get; private set; }

    CanvasGroup     _toastCG;
    TextMeshProUGUI _toastText;

    readonly Queue<string> _queue = new();
    bool _showing;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        BuildCanvas();
    }

    void BuildCanvas()
    {
        var canvasGO = new GameObject("MilestoneCanvas");
        canvasGO.transform.SetParent(transform, false);
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 80; // SideMenu(30) 위, Ad(100) 아래
        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.matchWidthOrHeight  = 0.5f;
        canvasGO.AddComponent<GraphicRaycaster>();

        var toastGO = new GameObject("Toast");
        toastGO.transform.SetParent(canvasGO.transform, false);
        _toastCG = toastGO.AddComponent<CanvasGroup>();
        _toastCG.alpha          = 0f;
        _toastCG.interactable   = false;
        _toastCG.blocksRaycasts = false;

        var rt = toastGO.GetComponent<RectTransform>();
        rt.anchorMin        = new Vector2(0.5f, 1f);
        rt.anchorMax        = new Vector2(0.5f, 1f);
        rt.pivot            = new Vector2(0.5f, 1f);
        rt.sizeDelta        = new Vector2(560f, 90f);
        rt.anchoredPosition = new Vector2(0f, -150f);

        var img = toastGO.AddComponent<Image>();
        img.color         = new Color(0.08f, 0.04f, 0.18f, 0.96f);
        img.raycastTarget = false;

        var txtGO = new GameObject("Text");
        txtGO.transform.SetParent(toastGO.transform, false);
        _toastText = txtGO.AddComponent<TextMeshProUGUI>();
        _toastText.fontSize           = 21f;
        _toastText.alignment          = TextAlignmentOptions.Center;
        _toastText.color              = Color.white;
        _toastText.enableWordWrapping = false;
        var f = Resources.Load<TMP_FontAsset>("KoreanFont");
        if (f != null) _toastText.font = f;

        var tr = txtGO.GetComponent<RectTransform>();
        tr.anchorMin = Vector2.zero;
        tr.anchorMax = Vector2.one;
        tr.offsetMin = new Vector2(20f, 8f);
        tr.offsetMax = new Vector2(-20f, -8f);
    }

    public void Enqueue(string msg)
    {
        _queue.Enqueue(msg);
        if (!_showing) StartCoroutine(ShowNext());
    }

    IEnumerator ShowNext()
    {
        _showing = true;
        while (_queue.Count > 0)
        {
            _toastText.text = _queue.Dequeue();
            yield return StartCoroutine(Fade(0f, 1f, 0.28f));
            yield return new WaitForSeconds(2.5f);
            yield return StartCoroutine(Fade(1f, 0f, 0.4f));
        }
        _showing = false;
    }

    IEnumerator Fade(float from, float to, float dur)
    {
        for (float t = 0f; t < dur; t += Time.deltaTime)
        {
            _toastCG.alpha = Mathf.Lerp(from, to, t / dur);
            yield return null;
        }
        _toastCG.alpha = to;
    }

    // StatsManager.RecordSlime 이후 호출
    // newStreak: 오늘 첫 슬라임일 때만 실제 값, 아니면 0
    public void CheckMilestones(Expression expression, int totalCount, int newStreak)
    {
        if (totalCount == 1) { Enqueue("첫 감정을 털어냈어요 🌱"); return; }

        // 해당 감정 첫 표현
        if ((StatsManager.Instance?.GetExpressionCount(expression) ?? 0) == 1)
            Enqueue($"처음으로 '{ExprName(expression)}'을 털어냈어요 {ExprEmoji(expression)}");

        // 누적 횟수 마일스톤
        int[] countGoals = { 10, 30, 50, 100, 200, 500 };
        if (Array.IndexOf(countGoals, totalCount) >= 0)
            Enqueue($"감정 {totalCount}번 털어내기 달성! ✨");

        // 연속 기록 마일스톤 (오늘 첫 슬라임일 때만)
        int[] streakGoals = { 3, 7, 14, 30 };
        if (newStreak > 0 && Array.IndexOf(streakGoals, newStreak) >= 0)
            Enqueue($"{newStreak}일 연속 감정 털어내기! 🔥");
    }

    static string ExprName(Expression e) => e switch
    {
        Expression.Angry     => "분노",
        Expression.Sad       => "슬픔",
        Expression.Fear      => "두려움",
        Expression.Happy     => "기쁨",
        Expression.Disgust   => "혐오",
        Expression.Surprised => "놀람",
        Expression.Contempt  => "경멸",
        _                    => "무감정",
    };

    static string ExprEmoji(Expression e) => e switch
    {
        Expression.Angry     => "💢",
        Expression.Sad       => "💧",
        Expression.Fear      => "😨",
        Expression.Happy     => "✨",
        Expression.Disgust   => "🤢",
        Expression.Surprised => "😲",
        Expression.Contempt  => "😒",
        _                    => "🫥",
    };
}
