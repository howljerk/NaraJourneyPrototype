using DG.Tweening;
using UnityEngine;

public class ShooterBeam : MonoBehaviour
{
    private const float kBeamUnitsPerSec = 7f;

    private Vector2 m_ScreenMoveMin = Vector2.zero;
    private Vector2 m_ScreenMoveMax = Vector2.zero;
    private float m_ScreenUnitsWidth = 0f;
    private float m_ScreenUnitsHeight = 0f;
    private Bounds m_ScreenBounds;

    private void Awake()
    {
    }

    private void Start()
    {
        m_ScreenUnitsHeight = Camera.main.orthographicSize * 2f;
        float aspectRatio = (float)Screen.width / (float)Screen.height;
        m_ScreenUnitsWidth = m_ScreenUnitsHeight * aspectRatio;

        m_ScreenMoveMin = new Vector2(-m_ScreenUnitsWidth * .5f + 1f, -m_ScreenUnitsHeight * .5f + 1f);
        m_ScreenMoveMax = new Vector2(m_ScreenUnitsWidth * .5f - 1f, m_ScreenUnitsHeight * .5f - 1f);
        m_ScreenBounds = new Bounds { min = m_ScreenMoveMin, max = m_ScreenMoveMax };
        m_ScreenBounds.center = new Vector3(0f, 0f, transform.position.z);

        SpriteRenderer[] sprites = GetComponentsInChildren<SpriteRenderer>(true);
        foreach (SpriteRenderer s in sprites)
        {
            Color color = s.color;
            s.color = new Color(color.r, color.g, color.b, 0f);
            s.DOFade(1f, .5f);
        }
    }

    private void Update()
    {
        transform.position += transform.right * kBeamUnitsPerSec * Time.deltaTime;       

        if(!m_ScreenBounds.Contains(transform.position))
            Destroy(gameObject);
    }
}
