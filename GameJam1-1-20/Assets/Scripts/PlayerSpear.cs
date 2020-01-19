using UnityEngine;
using DG.Tweening;
using System.Collections.Generic;

public class PlayerSpear : MonoBehaviour
{
    [SerializeField] private Sprite m_RopeSprite;
    [SerializeField] private Sprite m_SpearTipSprite;
    [SerializeField] private GameObject m_RopeNode;

    private SpriteRenderer m_SpearTip;
    private List<SpriteRenderer> m_Ropes = new List<SpriteRenderer>();

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
        foreach (SpriteRenderer r in m_Ropes)
            Destroy(r.gameObject);
        m_Ropes.Clear();

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

        Vector3 screenStartPos = Camera.main.WorldToScreenPoint(startPos);
        Vector3 screenEndPos = Camera.main.WorldToScreenPoint(endPos);
        Vector2 lookAt = (screenEndPos - screenStartPos).normalized;
        float angle = Mathf.Atan2(lookAt.y, lookAt.x) * Mathf.Rad2Deg;
        float dist = (endPos - transform.position).magnitude;
        float pos = 0f;
        float segmentScale = 1f;
        float segmentWidthPlusHalf = 1.5f * segmentScale;
        float segmentWidth = 1.0f * segmentScale;

        int idx = 0;
        for (float d = 0f; !Mathf.Approximately(d, dist);)
        {
            float totalDistDiff = dist - d;
            float xScale = 1f * segmentScale;

            if (totalDistDiff >= segmentWidthPlusHalf)
            {
                pos += segmentWidth;
                d += segmentWidth;
            }
            else
            {
                xScale = (totalDistDiff - segmentWidth * .5f) / segmentWidth;
                pos += segmentWidth * .5f + (totalDistDiff - segmentWidth * .5f) * .5f;
                d += totalDistDiff;
            }

            Vector3 ropePos = startPos + (Vector3)dir * pos;

            GameObject rope = new GameObject("rope");
            rope.transform.SetParent(transform);
            rope.transform.position = ropePos;
            rope.transform.rotation = Quaternion.Euler(new Vector3(0f, 0f, angle));
            rope.transform.localScale = new Vector3(xScale, .1f, 1f);

            Sequence appearSeq = DOTween.Sequence();
            appearSeq.AppendInterval(idx++ * .05f);
            appearSeq.AppendCallback(() =>
            {
                SpriteRenderer ropeSprite = rope.AddComponent<SpriteRenderer>();
                ropeSprite.sprite = m_RopeSprite;
                m_Ropes.Add(ropeSprite);
            });
        }

        GameObject spearTip = new GameObject("spear_tip");
        spearTip.transform.SetParent(transform);
        spearTip.transform.position = new Vector3(endPos.x, endPos.y, endPos.z - 1f);
        spearTip.transform.localScale = new Vector3(.2f, .2f, 1f);

        m_SpearTip = spearTip.AddComponent<SpriteRenderer>();
        m_SpearTip.sprite = m_SpearTipSprite;
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
