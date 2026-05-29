using System;
using System.Collections.Generic;
using UnityEngine;

public class StatsManager : MonoBehaviour
{
    public static StatsManager Instance { get; private set; }

    [Serializable]
    public class DiaryEntry
    {
        public string text;
        public string expression;
        public long   timestamp;
    }

    [Serializable]
    class DiaryList { public List<DiaryEntry> entries = new(); }

    const string PREF_TOTAL     = "stats_total";
    const string PREF_TODAY     = "stats_today";
    const string PREF_TODAY_DT  = "stats_today_date";
    const string PREF_STREAK    = "stats_streak";
    const string PREF_LAST_DATE = "stats_last_date";
    const string PREF_DIARY     = "diary_entries_v1";
    const int    MAX_DIARY      = 50;

    public int TotalCount { get; private set; }
    public int TodayCount { get; private set; }
    public int Streak     { get; private set; }

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        Load();
    }

    void Load()
    {
        TotalCount = PlayerPrefs.GetInt(PREF_TOTAL, 0);
        Streak     = PlayerPrefs.GetInt(PREF_STREAK, 0);

        string today     = DateTime.Now.ToString("yyyy-MM-dd");
        string yesterday = DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd");
        string lastDate  = PlayerPrefs.GetString(PREF_LAST_DATE, "");

        // 어제 이전에 마지막으로 플레이 → 연속 기록 깨짐
        if (!string.IsNullOrEmpty(lastDate) && lastDate != today && lastDate != yesterday)
        {
            Streak = 0;
            PlayerPrefs.SetInt(PREF_STREAK, 0);
        }

        if (PlayerPrefs.GetString(PREF_TODAY_DT, "") != today)
        {
            PlayerPrefs.SetInt(PREF_TODAY, 0);
            PlayerPrefs.SetString(PREF_TODAY_DT, today);
            TodayCount = 0;
        }
        else
        {
            TodayCount = PlayerPrefs.GetInt(PREF_TODAY, 0);
        }
    }

    public void RecordSlime(Expression expression)
    {
        bool firstOfDay = (TodayCount == 0);
        TotalCount++;
        TodayCount++;
        PlayerPrefs.SetInt(PREF_TOTAL, TotalCount);
        PlayerPrefs.SetInt(PREF_TODAY, TodayCount);

        var key = $"stats_expr_{expression.ToString().ToLower()}";
        PlayerPrefs.SetInt(key, PlayerPrefs.GetInt(key, 0) + 1);

        int newStreak = firstOfDay ? UpdateStreak() : 0;
        PlayerPrefs.Save();

        MilestoneManager.Instance?.CheckMilestones(expression, TotalCount, newStreak);
    }

    int UpdateStreak()
    {
        string today     = DateTime.Now.ToString("yyyy-MM-dd");
        string yesterday = DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd");
        string lastDate  = PlayerPrefs.GetString(PREF_LAST_DATE, "");

        if (lastDate == yesterday)
            Streak++;
        else if (lastDate != today)
            Streak = 1;

        PlayerPrefs.SetString(PREF_LAST_DATE, today);
        PlayerPrefs.SetInt(PREF_STREAK, Streak);
        return Streak;
    }

    public int GetExpressionCount(Expression expression) =>
        PlayerPrefs.GetInt($"stats_expr_{expression.ToString().ToLower()}", 0);

    public void RecordDiary(string text, Expression expression)
    {
        if (string.IsNullOrWhiteSpace(text)) return;
        var list = LoadDiary();
        list.entries.Insert(0, new DiaryEntry
        {
            text       = text.Trim(),
            expression = expression.ToString().ToLower(),
            timestamp  = ColorUtil.NowMs(),
        });
        if (list.entries.Count > MAX_DIARY)
            list.entries.RemoveRange(MAX_DIARY, list.entries.Count - MAX_DIARY);
        PlayerPrefs.SetString(PREF_DIARY, JsonUtility.ToJson(list));
        PlayerPrefs.Save();
    }

    public DiaryEntry[] GetDiaryEntries()
    {
        return LoadDiary().entries.ToArray();
    }

    public class WeeklyReportData
    {
        public int totalCount;
        public List<(string expression, int count)> breakdown = new();
    }

    public WeeklyReportData GetWeeklyReport()
    {
        long weekAgo = ColorUtil.NowMs() - 7L * 24 * 60 * 60 * 1000;
        var entries  = GetDiaryEntries();
        var counts   = new Dictionary<string, int>();
        int total    = 0;

        foreach (var e in entries)
        {
            if (e.timestamp < weekAgo) continue;
            total++;
            counts.TryGetValue(e.expression, out int c);
            counts[e.expression] = c + 1;
        }

        var report = new WeeklyReportData { totalCount = total };
        foreach (var kv in counts)
            report.breakdown.Add((kv.Key, kv.Value));
        report.breakdown.Sort((a, b) => b.count.CompareTo(a.count));
        return report;
    }

    DiaryList LoadDiary()
    {
        var json = PlayerPrefs.GetString(PREF_DIARY, "");
        if (string.IsNullOrEmpty(json)) return new DiaryList();
        try { return JsonUtility.FromJson<DiaryList>(json) ?? new DiaryList(); }
        catch { return new DiaryList(); }
    }

    public void ResetStats()
    {
        PlayerPrefs.DeleteKey(PREF_TOTAL);
        PlayerPrefs.DeleteKey(PREF_TODAY);
        PlayerPrefs.DeleteKey(PREF_TODAY_DT);
        PlayerPrefs.DeleteKey(PREF_STREAK);
        PlayerPrefs.DeleteKey(PREF_LAST_DATE);
        PlayerPrefs.DeleteKey(PREF_DIARY);
        foreach (Expression e in Enum.GetValues(typeof(Expression)))
            PlayerPrefs.DeleteKey($"stats_expr_{e.ToString().ToLower()}");
        PlayerPrefs.Save();
        Load();
    }
}
