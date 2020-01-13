using UnityEngine;

public class BackgroundScroller : MonoBehaviour
{
    [SerializeField] private Camera m_Camera;

    [SerializeField] private float m_ScrollSpeed = 1f;
    [SerializeField] private VerticalScroller[] m_VerticalScrollers;
    [SerializeField] private BoxCollider2D m_LeftScrollerPane;
    [SerializeField] private BoxCollider2D m_RightScrollerPane;
    [SerializeField] private Transform m_WorldRoot;

    private float m_DistanceTraveled = 0f;
    public float DistanceTraveled { get { return m_DistanceTraveled; } }

    private void Awake()
    {
        if (m_Camera == null)
            m_Camera = Camera.main;
    }

    private void Update()
    {
        foreach (VerticalScroller scroller in m_VerticalScrollers)
            scroller.UpdateFromMoveStep();

        float distanceTraveledStep = m_VerticalScrollers[0].GetMoveStep() * .25f;
        m_DistanceTraveled += distanceTraveledStep;
        m_WorldRoot.transform.position -= new Vector3(0f, distanceTraveledStep, 0f);
    }

    public void SetScrollSpeed(float scrollSpeed)
    {
        foreach (VerticalScroller scroller in m_VerticalScrollers)
            scroller.SetScrollSpeed(scrollSpeed);
    }

    public void SetXMoveStep(float moveStep)
    {
        //m_LeftScrollerPane.transform.localPosition += new Vector3(moveStep, 0f, 0f);
        //m_RightScrollerPane.transform.localPosition += new Vector3(moveStep, 0f, 0f);

        //m_WorldRoot.transform.position += new Vector3(moveStep, 0f, 0f);
        //m_LeftScrollerPane.transform.localPosition = new Vector3(moveStep, m_LeftScrollerPane.transform.localPosition.y, m_LeftScrollerPane.transform.localPosition.z);
        //m_RightScrollerPane.transform.localPosition = new Vector3(moveStep, m_RightScrollerPane.transform.localPosition.y, m_RightScrollerPane.transform.localPosition.z);
        //m_WorldRoot.transform.position = new Vector3(moveStep, m_WorldRoot.transform.position.y, m_WorldRoot.transform.position.z);

        float unitsHeight = m_Camera.orthographicSize * 2f;
        float aspectRatio = (float)Screen.width / (float)Screen.height;
        float unitsWidth = unitsHeight * aspectRatio;

        if(m_LeftScrollerPane.transform.localPosition.x + m_LeftScrollerPane.bounds.extents.x < -unitsWidth * .5f)
        {
            Vector3 leftPaneLocalPos = m_LeftScrollerPane.transform.localPosition;
            m_LeftScrollerPane.transform.localPosition = new Vector3(m_RightScrollerPane.transform.localPosition.x + 
                                                                     m_RightScrollerPane.bounds.extents.x + 
                                                                     m_LeftScrollerPane.bounds.extents.x, 
                                                                     leftPaneLocalPos.y, 
                                                                     leftPaneLocalPos.z);

            BoxCollider2D leftPane = m_LeftScrollerPane;
            m_LeftScrollerPane = m_RightScrollerPane;
            m_RightScrollerPane = leftPane;
        }
        if (m_RightScrollerPane.transform.localPosition.x - m_RightScrollerPane.bounds.extents.x > unitsWidth * .5f)
        {
            Vector3 rightPaneLocalPos = m_RightScrollerPane.transform.localPosition;
            m_RightScrollerPane.transform.localPosition = new Vector3(m_LeftScrollerPane.transform.localPosition.x -
                                                                      m_LeftScrollerPane.bounds.extents.x -
                                                                      m_RightScrollerPane.bounds.extents.x,
                                                                      rightPaneLocalPos.y,
                                                                      rightPaneLocalPos.z);
            BoxCollider2D rightPane = m_RightScrollerPane;
            m_RightScrollerPane = m_LeftScrollerPane;
            m_LeftScrollerPane = rightPane;

        }
    }
}
