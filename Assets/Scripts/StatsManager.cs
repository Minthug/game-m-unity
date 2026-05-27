using UnityEngine;

public class StatsManager : MonoBehaviour
{
    public static StatsManager Instance { get; private set; }

    const string PREF_TOTAL    = "stats_total";
    const string PREF_TODAY    = "stats_today";
    const string PREF_TODAY_DT = "stats_today_date";

    public int TotalCount { get; private set; }
    public int TodayCount { get; private set; }

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        Load();
    }

    void Load()
    {
        TotalCount = PlayerPrefs.GetInt(PREF_TOTAL, 0);

        string today = System.DateTime.Now.ToString("yyyy-MM-dd");
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
        TotalCount++;
        TodayCount++;
        PlayerPrefs.SetInt(PREF_TOTAL, TotalCount);
        PlayerPrefs.SetInt(PREF_TODAY, TodayCount);

        var key = $"stats_expr_{expression.ToString().ToLower()}";
        PlayerPrefs.SetInt(key, PlayerPrefs.GetInt(key, 0) + 1);
        PlayerPrefs.Save();
    }

    public int GetExpressionCount(Expression expression) =>
        PlayerPrefs.GetInt($"stats_expr_{expression.ToString().ToLower()}", 0);

    public void ResetStats()
    {
        PlayerPrefs.DeleteKey(PREF_TOTAL);
        PlayerPrefs.DeleteKey(PREF_TODAY);
        PlayerPrefs.DeleteKey(PREF_TODAY_DT);
        foreach (Expression e in System.Enum.GetValues(typeof(Expression)))
            PlayerPrefs.DeleteKey($"stats_expr_{e.ToString().ToLower()}");
        PlayerPrefs.Save();
        Load();
    }
}
