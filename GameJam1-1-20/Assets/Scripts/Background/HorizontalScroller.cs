using UnityEngine;

public class HorizontalScroller : MonoBehaviour
{
    [SerializeField] private Camera m_Camera;
    [SerializeField] private Transform m_LeftTransform;
    [SerializeField] private Transform m_RightTransform;
    [SerializeField] private float m_ScrollSpeed = 0f;
    [SerializeField] private Transform m_WorldRoot;

    private Bounds m_LeftBounds;
    private Bounds m_RightBounds;
    private float m_HalfScreenWidth = 0f;
    private float m_DistanceTraveled = 0f;

    private void Awake()
    {
        if (m_Camera == null)
            m_Camera = Camera.main;

        m_LeftBounds = m_LeftTransform.GetComponent<BoxCollider2D>().bounds;
        m_RightBounds = m_RightTransform.GetComponent<BoxCollider2D>().bounds;

        float aspectRatio = (float)Screen.width / (float)Screen.height;
        m_HalfScreenWidth = m_Camera.orthographicSize * aspectRatio;
    }

    private void Update()
    {
        float moveStep = GetMoveStep();

        Vector3 moveVec = new Vector3(-moveStep, 0f, 0f);

        m_LeftTransform.localPosition += new Vector3(-moveStep, 0f, 0f);
        m_RightTransform.localPosition += new Vector3(-moveStep, 0f, 0f);
        m_WorldRoot.position += moveVec;

        //Since we swap pieces, always just check the left
        if (m_LeftTransform.localPosition.x + m_LeftBounds.extents.x < -m_HalfScreenWidth)
        {
            //This'll get the new top to reset its position to the top
            m_LeftTransform.localPosition = new Vector3(m_RightTransform.localPosition.x + m_LeftBounds.size.x, 
                                                         m_LeftTransform.localPosition.y, 
                                                         0f);

            Transform right = m_RightTransform;
            m_RightTransform = m_LeftTransform;
            m_LeftTransform = right;

            m_LeftBounds = m_LeftTransform.GetComponent<BoxCollider2D>().bounds;
            m_RightBounds = m_RightTransform.GetComponent<BoxCollider2D>().bounds;
        }
        if (m_RightTransform.localPosition.x - m_RightBounds.extents.x > m_HalfScreenWidth)
        {
            //This'll get the new top to reset its position to the top
            m_RightTransform.localPosition = new Vector3(m_LeftTransform.localPosition.x - m_RightBounds.size.x,
                                                         m_RightTransform.localPosition.y,
                                                         0f);

            Transform left = m_LeftTransform;
            m_LeftTransform = m_RightTransform;
            m_RightTransform = left;

            m_LeftBounds = m_LeftTransform.GetComponent<BoxCollider2D>().bounds;
            m_RightBounds = m_RightTransform.GetComponent<BoxCollider2D>().bounds;
        }
    }

    public float GetMoveStep()
    {
        return Time.deltaTime * m_ScrollSpeed;
    }

    public void IncrementYPositionByMoveStep(float moveStep)
    {
        transform.localPosition += new Vector3(0f, moveStep, 0f);
    }

    public void SetScrollSpeed(float scrollSpeed)
    {
        m_ScrollSpeed = scrollSpeed;
    }

    public float GetScrollSpeed()
    {
        return m_ScrollSpeed;
    }
}
