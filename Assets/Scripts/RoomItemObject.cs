using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(SpriteRenderer), typeof(BoxCollider2D))]
public class RoomItemObject : MonoBehaviour
{
    public string   InstanceId { get; private set; }
    public RoomItem Data       { get; private set; }

    SpriteRenderer sr;
    BoxCollider2D  col;
    Camera         mainCam;
    bool           isDragging;
    Vector2        dragOffset;

    static RoomItemObject pressedItem;

    public void Init(string instanceId, RoomItem data, float overrideScale = 0f)
    {
        InstanceId = instanceId;
        Data       = data;
        sr         = GetComponent<SpriteRenderer>();
        col        = GetComponent<BoxCollider2D>();
        mainCam    = Camera.main;

        float scl = overrideScale > 0f ? overrideScale : data.defaultScale;
        transform.localScale = Vector3.one * scl;

        if (data.Is3D)
        {
            // 3D 프리팹: 자식으로 인스턴스화
            sr.enabled = false;
            var child = Instantiate(data.prefab, transform);
            child.transform.localPosition = Vector3.zero;
            child.transform.localRotation = Quaternion.identity;
            col.size = Vector2.one; // 기본 콜라이더
        }
        else
        {
            // 2D 스프라이트
            sr.sprite       = data.sprite;
            sr.sortingOrder = data.sortingOrder;
            if (data.sprite != null)
                col.size = data.sprite.bounds.size;
        }
    }

    void Update()
    {
        var mouse = Mouse.current;
        if (mouse == null) return;

        Vector2 mouseWorld = GetMouseWorld();

        if (mouse.leftButton.wasPressedThisFrame && pressedItem == null)
        {
            var hit = Physics2D.OverlapPoint(mouseWorld);
            if (hit != null && hit.gameObject == gameObject)
            {
                pressedItem = this;
                isDragging  = true;
                dragOffset  = (Vector2)transform.position - mouseWorld;
            }
        }

        if (isDragging && mouse.leftButton.isPressed)
            transform.position = (Vector3)(mouseWorld + dragOffset);

        if (isDragging && mouse.leftButton.wasReleasedThisFrame)
        {
            isDragging  = false;
            pressedItem = null;
            RoomManager.Instance?.SaveRoom();
        }
    }

    // 감정 변화 시 반응 (확장 포인트)
    public void OnEmotionChange(Expression e)
    {
        // 현재는 색상 유지, 추후 감정별 연출 추가 가능
        sr.color = Color.white;
    }

    Vector2 GetMouseWorld()
    {
        var mp = Mouse.current.position.ReadValue();
        return mainCam.ScreenToWorldPoint(new Vector3(mp.x, mp.y, -mainCam.transform.position.z));
    }
}
