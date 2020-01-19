using UnityEngine;
using DG.Tweening;

public class PlayerSpear : MonoBehaviour
{
    [SerializeField] private Sprite m_RopeSprite;
    [SerializeField] private Sprite m_SpearTipSprite;
    [SerializeField] private GameObject m_RopeNode;

    private SpriteRenderer m_Rope;
    private SpriteRenderer m_SpearTip;

    private Vector2 m_ScreenMoveMin = Vector2.zero;
    private Vector2 m_ScreenMoveMax = Vector2.zero;
    private float m_ScreenUnitsWidth = 0f;
    private float m_ScreenUnitsHeight = 0f;

    private void Awake()
    {
        m_ScreenUnitsHeight = Camera.main.orthographicSize * 2f;
        float aspectRatio = (float)Screen.width / (float)Screen.height;
        m_ScreenUnitsWidth = m_ScreenUnitsHeight * aspectRatio;

        m_ScreenMoveMin = new Vector2(-m_ScreenUnitsWidth * .5f + 1f, -m_ScreenUnitsHeight * .5f + 1f);
        m_ScreenMoveMax = new Vector2(m_ScreenUnitsWidth * .5f - 1f, m_ScreenUnitsHeight * .5f - 1f);
    }

    private void Update()
    {
        
    }

    public void Clear()
    {
        if (m_Rope != null)
            Destroy(m_Rope.gameObject);
        m_Rope = null;

        if (m_SpearTip != null)
            Destroy(m_SpearTip.gameObject);
        m_SpearTip = null;
    }

    public void FireIntoDirection(Vector3 startPos, Vector2 dir)
    {
        transform.position = new Vector3(startPos.x, startPos.y, transform.position.z);

        const float fireDist = 10f;
        Vector3 endPos = GetClampedToScreenAreaPos(transform.position + (Vector3)(dir * fireDist));
        dir = (endPos - transform.position).normalized;

        GameObject rope = new GameObject("rope");
        rope.transform.SetParent(m_RopeNode.transform);

        Vector3 right = dir;
        Vector3 forward = new Vector3(0f, 0f, -1f);
        Vector3 up = Vector3.Cross(right, forward);

        rope.transform.right = right;
        rope.transform.forward = forward;
        rope.transform.up = up;

        m_Rope = rope.AddComponent<SpriteRenderer>();
        m_Rope.sprite = m_RopeSprite;

        Vector3 ropePos = (startPos + endPos) * .5f;
        m_RopeNode.transform.position = startPos;

        rope.transform.position = new Vector3(ropePos.x, ropePos.y, transform.position.z);

        float dist = (endPos - transform.position).magnitude;
        Vector3 ropeScale = rope.transform.localScale;
        rope.transform.localScale = new Vector3(dist, .1f, ropeScale.z);

        GameObject spearTip = new GameObject("spear_tip");
        spearTip.transform.SetParent(m_RopeNode.transform);
        spearTip.transform.position = new Vector3(endPos.x, endPos.y, endPos.z - 1f);
        spearTip.transform.localScale = new Vector3(.2f, .2f, 1f);

        m_SpearTip = spearTip.AddComponent<SpriteRenderer>();
        m_SpearTip.sprite = m_SpearTipSprite;

        float ropeExpandUnitsPerSec = 10f;
        float expandTime = (endPos - transform.position).magnitude / ropeExpandUnitsPerSec;

        m_RopeNode.transform.localScale = new Vector3(0f, 0f, 1f);
        m_RopeNode.transform.DOScale(Vector3.one, expandTime).SetEase(Ease.InSine);
    }

    private Vector3 GetClampedToScreenAreaPos(Vector3 pos)
    {
        float clampedX = pos.x;
        if (clampedX < m_ScreenMoveMin.x)
            clampedX = m_ScreenMoveMin.x;
        if (clampedX > m_ScreenMoveMax.x)
            clampedX = m_ScreenMoveMax.x;

        float clampedY = pos.y;
        if (clampedY < m_ScreenMoveMin.y)
            clampedY = m_ScreenMoveMin.y;
        if (clampedY > m_ScreenMoveMax.y)
            clampedY = m_ScreenMoveMax.y;

        return new Vector3(clampedX, clampedY, pos.z);
    }
}
