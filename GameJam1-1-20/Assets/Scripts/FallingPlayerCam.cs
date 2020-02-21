using UnityEngine;

public class FallingPlayerCam : MonoBehaviour
{
    [SerializeField] private BoxCollider2D m_FollowBox;
    [SerializeField] private FallingPlayer m_Player;
    [SerializeField] private BoxCollider2D m_PlayerCollider;

    private Camera m_Camera;
    private bool m_InPursuit;
    private bool m_LockedOn;
    private Vector3 m_PursuitVel;

    private void Awake()
    {
        m_Camera = GetComponent<Camera>();

        float unitsHeight = m_Camera.orthographicSize * 2f;
        float aspectRatio = (float)Screen.width / (float)Screen.height;
        float unitsWidth = unitsHeight * aspectRatio;

        m_FollowBox.size = new Vector2(unitsWidth, unitsHeight * .5f);
    }

    private void Update()
    {
        if (!m_InPursuit && !m_LockedOn)
            return;

        if(m_InPursuit)
        {
            transform.position += m_PursuitVel * Time.deltaTime;

            if (transform.position.y >= m_Player.transform.position.y && m_PursuitVel.y > 0f)
                m_LockedOn = true;
            else if (transform.position.y <= m_Player.transform.position.y && m_PursuitVel.y < 0f)
                m_LockedOn = true;

            if (m_LockedOn)
                m_InPursuit = false;
        }

        if(m_LockedOn)
            transform.position = new Vector3(transform.position.x, m_Player.transform.position.y, transform.position.z);
    }

    public void TryToFollow()
    {
        if (m_InPursuit || m_LockedOn)
            return;

        if(m_Player.transform.position.y > m_FollowBox.bounds.max.y)
        {
            m_InPursuit = true;
            m_PursuitVel = new Vector3(0f, 4f, 0f);
        }
        else if(m_Player.transform.position.y < m_FollowBox.bounds.min.y)
        {
            m_InPursuit = true;
            m_PursuitVel = new Vector3(0f, -4f, 0f);
        }
    }

    public void StopFollow()
    {
        m_InPursuit = m_LockedOn = false;
    }

    public void ClampPositionToScreen(ref Vector3 pos)
    {
        float screenWorldHeight = m_Camera.orthographicSize * 2f;
        float aspectRatio = (float)Screen.width / (float)Screen.height;
        float screenWorldWidth = screenWorldHeight * aspectRatio;

        float leftEdge = transform.position.x - screenWorldWidth * .5f + m_PlayerCollider.size.x * .5f;
        float rightEdge = transform.position.x + screenWorldWidth * .5f - m_PlayerCollider.size.x * .5f;

        if (pos.x > rightEdge)
            pos = new Vector3(rightEdge, pos.y,pos.z);
        else if (pos.x < leftEdge)
            pos = new Vector3(leftEdge, pos.y, pos.z);
    }
}
