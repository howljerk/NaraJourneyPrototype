using UnityEngine;
using UnityEngine.EventSystems;

public class BackgroundTap : MonoBehaviour
{
    [SerializeField] private Camera m_TapCamera;
    [SerializeField] private BoxCollider2D m_TapCollider;

    RaycastHit2D[] m_HitsThisFrame;

    private void Awake()
    {
        float unitsHeight = m_TapCamera.orthographicSize * 2f;
        float aspectRatio = (float)Screen.width / (float)Screen.height;
        float unitsWidth = unitsHeight * aspectRatio;
        m_TapCollider.size = new Vector2(unitsWidth, unitsHeight);
    }



    private void Update()
    {
        if (EventSystem.current.IsPointerOverGameObject(-1))
            return;
        
        m_HitsThisFrame = Physics2D.RaycastAll(m_TapCamera.transform.position, new Vector2(0, -1f));
    }

    public bool GetHasInput()
    {
        if (!Input.GetMouseButton(0) ||
            EventSystem.current.IsPointerOverGameObject(-1))
            return false;

        foreach (RaycastHit2D h in m_HitsThisFrame)
        {
            if (h.collider != m_TapCollider)
                continue;
            
            return true;
        }
        return false;
    }

    public bool GetHasInputUp()
    {
        if (!Input.GetMouseButtonUp(0) ||
            EventSystem.current.IsPointerOverGameObject(-1))
            return false;

        foreach (RaycastHit2D h in m_HitsThisFrame)
        {
            if (h.collider != m_TapCollider)
                continue;
            
            return true;
        }
        return false;
    }
}
