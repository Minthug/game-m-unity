using System.Collections.Generic;

public enum Expression
{
    Angry, Sad, Fear, Happy, Disgust, Surprised, Contempt, Blank
}

public static class EmotionDetector
{
    static readonly Dictionary<Expression, string[]> Keywords = new()
    {
        [Expression.Angry]    = new[] { "화나", "짜증", "열받", "빡", "분노", "화남", "미치", "싫다", "개열", "욱" },
        [Expression.Sad]      = new[] { "슬프", "울고", "눈물", "우울", "외로", "허무", "공허", "힘들", "지쳐", "아파", "그리워", "보고싶" },
        [Expression.Fear]     = new[] { "무서", "두려", "공포", "불안", "걱정", "떨려", "무섭", "겁나", "긴장", "초조" },
        [Expression.Happy]    = new[] { "행복", "기뻐", "신나", "즐거", "좋아", "설레", "기대", "웃겨", "재밌", "좋다", "신남", "기쁘" },
        [Expression.Disgust]  = new[] { "역겨", "구역질", "더러", "역하", "싫어", "토나", "징그", "메스꺼" },
        [Expression.Surprised]= new[] { "놀라", "깜짝", "헐", "대박", "어머", "설마", "진짜", "믿기지", "충격", "와" },
        [Expression.Contempt] = new[] { "한심", "무시", "쓸모", "별로", "시시", "하찮", "그게", "됩니다", "뭐가", "ㅋㅋ" },
    };

    public static Expression Detect(string text)
    {
        if (string.IsNullOrEmpty(text)) return Expression.Blank;

        var scores = new Dictionary<Expression, int>();
        foreach (var kv in Keywords)
        {
            scores[kv.Key] = 0;
            foreach (var word in kv.Value)
                if (text.Contains(word)) scores[kv.Key]++;
        }

        Expression best = Expression.Blank;
        int bestScore = 0;
        foreach (var kv in scores)
        {
            if (kv.Value > bestScore) { bestScore = kv.Value; best = kv.Key; }
        }
        return best;
    }
}
