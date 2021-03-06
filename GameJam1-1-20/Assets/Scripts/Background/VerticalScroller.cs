﻿using UnityEngine;

public class VerticalScroller : MonoBehaviour
{
    [SerializeField] private Camera m_Camera;
    [SerializeField] private Transform m_TopTransform;
    [SerializeField] private Transform m_BottomTransform;

    private Bounds m_BottomBounds;
    private Bounds m_TopBounds;
    private float m_ScrollSpeed = 0f;

    private void Awake()
    {
        if (m_Camera == null)
            m_Camera = Camera.main;

        m_BottomBounds = m_BottomTransform.GetComponentInChildren<SpriteRenderer>().bounds;
        m_TopBounds = m_TopTransform.GetComponentInChildren<SpriteRenderer>().bounds;
    }

    public void UpdateFromMoveStep()
    {
        float moveStep = GetMoveStep();

        m_TopTransform.localPosition += new Vector3(0f, moveStep, 0f);
        m_BottomTransform.localPosition += new Vector3(0f, moveStep, 0f);

        //Since we swap pieces, always just check the bottom
        if (m_ScrollSpeed < 0f &&
            m_BottomTransform.position.y + m_BottomBounds.extents.y < m_Camera.transform.position.y - m_Camera.orthographicSize)
        {
            //This'll get the new top to reset its position to the top
            m_BottomTransform.localPosition = new Vector3(0f, m_TopTransform.localPosition.y + m_TopBounds.size.y, 0f);

            Transform top = m_TopTransform;
            m_TopTransform = m_BottomTransform;
            m_BottomTransform = top;

            m_BottomBounds = m_BottomTransform.GetComponentInChildren<SpriteRenderer>().bounds;
        }
        if (m_ScrollSpeed > 0f &&
            m_TopTransform.position.y - m_TopBounds.extents.y > m_Camera.transform.position.y + m_Camera.orthographicSize)
        {
            //This'll get the new bottom to reset its position to the bottom
            m_TopTransform.localPosition = new Vector3(0f, m_BottomTransform.localPosition.y - m_BottomBounds.size.y, 0f);

            Transform bottom = m_BottomTransform;
            m_BottomTransform = m_TopTransform;
            m_TopTransform = bottom;
        }
    }

    public float GetMoveStep()
    {
        return Time.deltaTime * m_ScrollSpeed;
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
