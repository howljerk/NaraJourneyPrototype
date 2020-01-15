using DG.Tweening;
using UnityEngine;

public class SpawnMarker : MonoBehaviour
{
    [SerializeField] private SpriteRenderer m_Icon;
    [SerializeField] private SpriteRenderer m_LeftArrow;
    [SerializeField] private SpriteRenderer m_RightArrow;

    public GameObject TrackedEntity { get; set; }
    public bool DoTracking { get; set; } = true;

    private Sequence m_SpawnPointerScaleSequence;

    private void Awake()
    {
        m_LeftArrow.enabled = m_RightArrow.enabled = false;

        System.Action doSequence = null;
        doSequence = () =>
        {
            m_SpawnPointerScaleSequence = DOTween.Sequence();
            m_SpawnPointerScaleSequence.Append(transform.DOPunchScale(new Vector3(2f, 2f, 1f), .2f));
            m_SpawnPointerScaleSequence.AppendCallback(() => doSequence());
        };
        doSequence();
    }

    private void Update()
    {
        if(!DoTracking)
        {
            m_Icon.enabled = m_LeftArrow.enabled = m_RightArrow.enabled = false;
            return;
        }

        float unitsHeight = Camera.main.orthographicSize * 2f;
        float aspectRatio = (float)Screen.width / (float)Screen.height;
        float unitsWidth = unitsHeight * aspectRatio;

        bool outOfBounds = !(TrackedEntity.transform.position.x > -unitsWidth * .5f && TrackedEntity.transform.position.x < unitsWidth * .5f);

        m_Icon.enabled = outOfBounds;

        //Anchor marker based on where entity is offscreen
        if (m_Icon.enabled)
        {
            float screenPosX = unitsWidth * .5f - 1f;
            bool anchoredToRight = true;

            if (TrackedEntity.transform.position.x < unitsWidth * .5f)
            {
                screenPosX = -unitsWidth * .5f + 1f;
                anchoredToRight = false;
            }

            transform.position = new Vector3(screenPosX,
                                           transform.position.y,
                                           transform.position.z);

            m_LeftArrow.enabled = !anchoredToRight;
            m_RightArrow.enabled = anchoredToRight;
        }
        else
        {
            m_LeftArrow.enabled = m_RightArrow.enabled = false;
        }
    }

    private void OnDestroy()
    {
        if (m_SpawnPointerScaleSequence != null)
            m_SpawnPointerScaleSequence.Kill();
        m_SpawnPointerScaleSequence = null;
    }
}
