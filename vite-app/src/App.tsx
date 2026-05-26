import { useState, useCallback, useRef } from 'react';
import UnityCanvas from './UnityCanvas';

type SendFn = (obj: string, method: string, param: string) => void;
type Expression = 'angry' | 'sad' | 'fear' | 'happy' | 'disgust' | 'contempt' | 'surprised' | 'blank';

// game-m 참고: 키워드 기반 감정 감지
const KEYWORD_WEIGHTS: Record<Expression, Array<[string, number]>> = {
  angry: [
    ['씨발', 4], ['씨바', 4], ['시발', 4], ['썅', 4], ['개새', 4],
    ['병신', 3], ['닥쳐', 3], ['꺼져', 3], ['존나', 3],
    ['빡', 2], ['열받', 2], ['짜증', 2], ['화나', 2], ['분노', 2], ['억울', 2],
    ['어이없', 2], ['황당', 2], ['미치겠', 2],
    ['최악', 1], ['싫어', 1], ['망했', 1],
  ],
  sad: [
    ['흑흑', 4], ['훌쩍', 4], ['엉엉', 4], ['으앙', 4],
    ['ㅠㅠ', 3], ['ㅜㅜ', 3], ['TT', 3],
    ['슬프', 2], ['눈물', 2], ['우울', 2], ['외로워', 2], ['무기력', 2],
    ['힘들', 2], ['보고싶', 2], ['그리워', 2], ['서러', 2],
    ['지쳐', 1], ['상처', 1], ['아파', 1], ['ㅠ', 1], ['ㅜ', 1],
  ],
  fear: [
    ['공황', 4], ['공포', 3], ['두려워', 3],
    ['무서워', 2], ['무섭다', 2], ['오싹', 2],
    ['겁나', 1], ['불안해', 1], ['떨려', 1], ['긴장돼', 1],
  ],
  disgust: [
    ['혐오', 4], ['역겨워', 3], ['구역질', 3], ['토나와', 3],
    ['징그러워', 2], ['더러워', 2],
    ['역해', 1], ['구리다', 1], ['냄새나', 1],
  ],
  contempt: [
    ['경멸', 4], ['하찮아', 3], ['무시해', 3], ['코웃음', 3],
    ['웃기지마', 2], ['찌질', 2],
    ['우습네', 1], ['웃기네', 1],
  ],
  surprised: [
    ['말도안돼', 3], ['믿을수없', 3], ['충격', 2], ['대박', 2], ['설마', 2],
    ['헐', 1], ['뭐야', 1], ['놀랐', 1], ['당황', 1], ['미쳤', 1],
  ],
  happy: [
    ['행복', 3], ['사랑', 3], ['기뻐', 3], ['즐거워', 3],
    ['감사', 2], ['고마워', 2], ['신남', 2], ['설레', 2], ['다행', 2],
    ['좋아', 1], ['신나', 1], ['최고', 1], ['기대', 1],
  ],
  blank: [],
};

const EXPRESSION_EMOJI: Record<Expression, string> = {
  angry: '😡', sad: '😢', fear: '😨', happy: '😊',
  disgust: '🤢', contempt: '😒', surprised: '😲', blank: '🫧',
};

const EXPRESSION_COLOR: Record<Expression, string> = {
  angry:    '#ff4444',
  sad:      '#4488ff',
  fear:     '#9966ff',
  happy:    '#ffaa00',
  disgust:  '#44cc66',
  contempt: '#99aabb',
  surprised:'#cc44ff',
  blank:    '#888899',
};

function detectExpression(text: string): Expression {
  const t = text.toLowerCase();
  let best: Expression = 'blank';
  let bestScore = 0;
  for (const [expr, pairs] of Object.entries(KEYWORD_WEIGHTS) as [Expression, Array<[string, number]>][]) {
    if (expr === 'blank') continue;
    const score = pairs.reduce((sum, [kw, w]) => sum + (t.includes(kw) ? w : 0), 0);
    if (score > bestScore) { bestScore = score; best = expr; }
  }
  return best;
}

interface Message {
  id: number;
  text: string;
  expression: Expression;
}

function App() {
  const [text, setText] = useState('');
  const [messages, setMessages] = useState<Message[]>([]);
  const [sendToUnity, setSendToUnity] = useState<SendFn | null>(null);
  const inputRef = useRef<HTMLTextAreaElement>(null);
  const messagesEndRef = useRef<HTMLDivElement>(null);

  const handleReady = useCallback((send: SendFn) => {
    setSendToUnity(() => send);
  }, []);

  const handleSubmit = useCallback(() => {
    const trimmed = text.trim();
    if (!trimmed) return;

    const expression = detectExpression(trimmed);
    const payload = JSON.stringify({
      id: `slime-${Date.now()}`,
      text: trimmed,
      expression,
      stage: 1,
    });

    if (sendToUnity) {
      sendToUnity('SlimeManager', 'CreateSlimeFromWeb', payload);
    } else {
      console.log('[Chat] Unity 미연결 — payload:', payload);
    }

    setMessages(prev => [...prev, { id: Date.now(), text: trimmed, expression }]);
    setText('');
    inputRef.current?.focus();
    setTimeout(() => messagesEndRef.current?.scrollIntoView({ behavior: 'smooth' }), 50);
  }, [text, sendToUnity]);

  const handleKeyDown = useCallback((e: React.KeyboardEvent<HTMLTextAreaElement>) => {
    if (e.key === 'Enter' && !e.shiftKey) {
      e.preventDefault();
      handleSubmit();
    }
  }, [handleSubmit]);

  return (
    <div style={{ width: '100vw', height: '100vh', position: 'relative' }}>
      <UnityCanvas
        onProgress={(p) => console.log(`Loading: ${Math.round(p * 100)}%`)}
        onLoaded={() => console.log('Unity ready')}
        onError={(e) => console.error('Unity error:', e)}
        onReady={handleReady}
      />

      {/* 말풍선 목록 */}
      <div style={{
        position: 'fixed', bottom: 110, left: 0, right: 0,
        maxHeight: '55vh', overflowY: 'auto',
        padding: '0 16px 8px',
        display: 'flex', flexDirection: 'column', gap: 8,
        pointerEvents: 'none',
      }}>
        {messages.map(msg => (
          <div key={msg.id} style={{
            alignSelf: 'flex-end',
            maxWidth: '78%',
            background: 'rgba(30,24,50,0.92)',
            border: `1.5px solid ${EXPRESSION_COLOR[msg.expression]}44`,
            borderRadius: '18px 18px 4px 18px',
            padding: '10px 14px',
            display: 'flex', flexDirection: 'column', gap: 4,
          }}>
            <div style={{ display: 'flex', alignItems: 'center', gap: 6 }}>
              <span style={{ fontSize: 16 }}>{EXPRESSION_EMOJI[msg.expression]}</span>
              <span style={{
                fontSize: 11, fontWeight: 600,
                color: EXPRESSION_COLOR[msg.expression],
                textTransform: 'uppercase', letterSpacing: 0.5,
              }}>
                {msg.expression}
              </span>
            </div>
            <span style={{ fontSize: 14, color: '#ddd8f0', lineHeight: '20px', wordBreak: 'break-word' }}>
              {msg.text}
            </span>
          </div>
        ))}
        <div ref={messagesEndRef} />
      </div>

      {/* 채팅 입력 오버레이 */}
      <div style={{
        position: 'fixed', bottom: 0, left: 0, right: 0,
        padding: '12px 16px 28px',
        background: 'linear-gradient(to top, rgba(10,10,16,0.97) 70%, transparent)',
        display: 'flex', gap: 10, alignItems: 'flex-end',
      }}>
        <textarea
          ref={inputRef}
          value={text}
          onChange={e => setText(e.target.value)}
          onKeyDown={handleKeyDown}
          placeholder="지금 어떤 기분이에요? 다 털어놔요"
          maxLength={200}
          rows={1}
          style={{
            flex: 1,
            background: 'rgba(255,255,255,0.07)',
            border: '1.5px solid rgba(255,255,255,0.12)',
            borderRadius: 16,
            padding: '13px 16px',
            color: '#e8e8f0',
            fontSize: 15,
            lineHeight: '22px',
            resize: 'none',
            outline: 'none',
            fontFamily: 'inherit',
            maxHeight: 110,
            overflowY: 'auto',
          }}
          onInput={e => {
            const el = e.currentTarget;
            el.style.height = 'auto';
            el.style.height = Math.min(el.scrollHeight, 110) + 'px';
          }}
        />
        <button
          onClick={handleSubmit}
          disabled={!text.trim()}
          style={{
            background: text.trim() ? 'rgba(124,58,237,0.9)' : 'rgba(124,58,237,0.3)',
            border: 'none',
            borderRadius: 16,
            padding: '13px 20px',
            color: text.trim() ? '#fff' : 'rgba(255,255,255,0.35)',
            fontSize: 15,
            fontWeight: 700,
            cursor: text.trim() ? 'pointer' : 'default',
            whiteSpace: 'nowrap',
            transition: 'background 0.2s, color 0.2s',
          }}
        >
          털어내기 🫠
        </button>
      </div>
    </div>
  );
}

export default App;
