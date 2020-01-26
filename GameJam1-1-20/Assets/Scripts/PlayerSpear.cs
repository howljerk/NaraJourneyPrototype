using UnityEngine;
using DG.Tweening;
using System.Collections.Generic;

public class PlayerSpear : MonoBehaviour
{
    public enum State
    {
        Idle,
        Extending,
        Pullback,
        ReelingIn,
        ClosedToClamp,
        SwingingOutFromClamp
    }

    [SerializeField] private Sprite m_RopeSprite;

    [SerializeField] private GameObject m_RopeNode;
    [SerializeField] private PlayerSpearTip m_SpearTip;
    [SerializeField] private GameObject m_RopeShowMask;

    private const float kFireDist = 10f;
    private const float kFireUnitsPerSec = 12f;

    private State m_State = State.Idle;
    public State CurrentState { get { return m_State; } }

    private bool m_CanClamp = false;
    public bool CanClamp { get { return m_CanClamp; } }

    public SideviewPlayer Player { get; set; }

    public GameObject ClampedObject { get; private set; }

    private List<SpriteRenderer> m_RopeSegments = new List<SpriteRenderer>();
    private Vector2 m_ScreenMoveMin = Vector2.zero;
    private Vector2 m_ScreenMoveMax = Vector2.zero;
    private float m_ScreenUnitsWidth = 0f;
    private float m_ScreenUnitsHeight = 0f;
    private Tweener m_RopeMaskScaleTween;
    private Tweener m_RopeMaskMoveTween;
    private Tweener m_SpearMoveTween;
    private Tweener m_PlayerReelInMoveTween;
    private Vector3 m_CurrentStartPos;
    private Vector3 m_CurrentEndPos;
    private float m_CurrentDist;
    private System.Action m_PulledBackInCallback;
    private System.Action m_ReeledInCallback;

    private void Awake()
    {
        m_ScreenUnitsHeight = Camera.main.orthographicSize * 2f;
        float aspectRatio = (float)Screen.width / (float)Screen.height;
        m_ScreenUnitsWidth = m_ScreenUnitsHeight * aspectRatio;

        m_ScreenMoveMin = new Vector2(-m_ScreenUnitsWidth * .5f + 1f, -m_ScreenUnitsHeight * .5f + 1f);
        m_ScreenMoveMax = new Vector2(m_ScreenUnitsWidth * .5f - 1f, m_ScreenUnitsHeight * .5f - 1f);

        m_SpearTip.OnCanClamp += OnSpearCanClamp;
        m_SpearTip.OnCantClamp += OnSpearCantClamp;
        m_SpearTip.OnRicochet += OnSpearRicochet;
    }

    private void Update()
    {
    }

    public void Clear()
    {
        m_CanClamp = false;
        m_PulledBackInCallback = null;

        m_SpearTip.gameObject.SetActive(false);
        m_SpearTip.transform.localPosition = Vector3.zero;

        foreach (SpriteRenderer r in m_RopeSegments)
            Destroy(r.gameObject);
        m_RopeSegments.Clear();

        CancelRopeMovement();
    }

    public void ResetToIdle()
    {
        m_State = State.Idle;
        Clear();
    }

    public void CancelRopeMovement()
    {
        if (m_RopeMaskScaleTween != null)
            m_RopeMaskScaleTween.Kill();
        if (m_RopeMaskMoveTween != null)
            m_RopeMaskMoveTween.Kill();
        if (m_SpearMoveTween != null)
            m_SpearMoveTween.Kill();        

        DOTween.timeScale = Time.timeScale = 1f;

        foreach (SpriteRenderer r in m_RopeSegments)
            r.color = Color.white;
    }

    public void FireIntoDirection(Vector3 startPos, Vector2 dir, System.Action pulledBackInCallback)
    {
        m_PulledBackInCallback = pulledBackInCallback;

        transform.position = new Vector3(startPos.x, startPos.y, transform.position.z);

        Vector3 endPos = GetClampedToScreenAreaPos(transform.position + (Vector3)(dir * kFireDist));
        dir = (endPos - transform.position).normalized;

        m_CurrentDist = (endPos - transform.position).magnitude;

        Vector3 screenStartPos = Camera.main.WorldToScreenPoint(startPos);
        Vector3 screenEndPos = Camera.main.WorldToScreenPoint(endPos);
        Vector2 lookAt = (screenEndPos - screenStartPos).normalized;
        float angle = Mathf.Atan2(lookAt.y, lookAt.x) * Mathf.Rad2Deg;
        float pos = 0f;
        float segmentScale = 1f;
        float segmentWidthPlusHalf = 1.5f * segmentScale;
        float segmentWidth = 1.0f * segmentScale;

        for (float d = 0f; !Mathf.Approximately(d, m_CurrentDist);)
        {
            float totalDistDiff = m_CurrentDist - d;
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
            rope.transform.SetParent(m_RopeNode.transform);
            rope.transform.position = ropePos;
            rope.transform.rotation = Quaternion.Euler(new Vector3(0f, 0f, angle));
            rope.transform.localScale = new Vector3(xScale, .1f, 1f);

            SpriteRenderer ropeSprite = rope.AddComponent<SpriteRenderer>();
            ropeSprite.sprite = m_RopeSprite;
            ropeSprite.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;

            m_RopeSegments.Add(ropeSprite);
        }

        float tweenTime = m_CurrentDist / kFireUnitsPerSec;

        m_RopeShowMask.transform.localRotation = Quaternion.Euler(new Vector3(0f, 0f, angle));

        if (m_RopeMaskScaleTween != null)
            m_RopeMaskScaleTween.Kill();

        m_RopeShowMask.transform.localScale = new Vector3(0f, 1f, 1f);
        m_RopeMaskScaleTween = m_RopeShowMask.transform.DOScaleX(pos, tweenTime);

        if (m_RopeMaskMoveTween != null)
            m_RopeMaskMoveTween.Kill();

        m_CurrentStartPos = m_RopeShowMask.transform.parent.InverseTransformPoint(startPos);
        m_CurrentEndPos = m_RopeShowMask.transform.parent.InverseTransformPoint(endPos);

        m_RopeShowMask.transform.localPosition = m_CurrentStartPos;
        m_RopeMaskMoveTween = m_RopeShowMask.transform.DOLocalMove(m_CurrentStartPos + (Vector3)dir * m_CurrentDist * .5f, tweenTime);

        if (m_SpearMoveTween != null)
            m_SpearMoveTween.Kill();

        m_SpearTip.gameObject.SetActive(true);
        m_SpearTip.transform.localPosition = new Vector3(m_CurrentStartPos.x, m_CurrentStartPos.y, startPos.z - 1f);
        m_SpearMoveTween = m_SpearTip.transform.DOLocalMove(m_CurrentEndPos + new Vector3(0, 0, -1f), tweenTime);
        m_SpearMoveTween.onComplete = OnRopeExtended;

        m_State = State.Extending;
    }

    public void ReelIn(System.Action reeledInCallback)
    {
        m_State = State.ReelingIn;

        float tweenTime = m_CurrentDist / kFireUnitsPerSec;

        m_RopeMaskScaleTween = m_RopeShowMask.transform.DOScaleX(0f, tweenTime);
        m_RopeMaskMoveTween = m_RopeShowMask.transform.DOLocalMove(Vector3.zero, tweenTime);

        Vector3 spearToPlayerLocalPos = Player.transform.parent.InverseTransformPoint(m_SpearTip.transform.position);
        m_PlayerReelInMoveTween = Player.transform.DOLocalMove(spearToPlayerLocalPos, tweenTime);

        m_SpearMoveTween = m_SpearTip.transform.DOLocalMove(Vector3.zero, tweenTime);
        m_SpearMoveTween.onComplete = OnRopeReeledIn;

        m_ReeledInCallback = reeledInCallback;
    }

    //TODO: WIP
    public void SwingAwayFromClamp(Vector3 startPos, Vector2 dir, float swingDistance)
    {
        m_State = State.SwingingOutFromClamp;
        m_CurrentDist = swingDistance;

        transform.position = new Vector3(startPos.x, startPos.y, transform.position.z);

        Vector3 endPos = GetClampedToScreenAreaPos(transform.position + (Vector3)(dir * kFireDist));
        dir = (endPos - transform.position).normalized;

        float tweenTime = m_CurrentDist / kFireUnitsPerSec;
    }

    /// <summary>
    /// Callback for when spear has finished being reeled, which always follows
    /// a successful clamp to an object.
    /// </summary>
    private void OnRopeReeledIn()
    {
        m_State = State.ClosedToClamp;
        m_ReeledInCallback?.Invoke();
    }

    /// <summary>
    /// Callback for when the spear has reached its full extension
    /// </summary>
    private void OnRopeExtended()
    {
        if (m_State != State.Extending)
            return;

        m_State = State.Pullback;

        float tweenTime = m_CurrentDist / (kFireUnitsPerSec * 2);

        m_RopeMaskScaleTween = m_RopeShowMask.transform.DOScaleX(0f, tweenTime);
        m_RopeMaskMoveTween = m_RopeShowMask.transform.DOLocalMove(m_CurrentStartPos, tweenTime);
        m_SpearMoveTween = m_SpearTip.transform.DOLocalMove(m_CurrentStartPos + new Vector3(0, 0, -1f), tweenTime);
        m_SpearMoveTween.onComplete = OnRopePulledIn;
    }

    /// <summary>
    /// Callback for the spear has finished being pulled back in
    /// </summary>
    private void OnRopePulledIn()
    {
        m_PulledBackInCallback?.Invoke();
        m_State = State.Idle;
        Clear();
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

    #region Spear tip callbacks

    private void OnSpearCanClamp(Collider2D otherCollider)
    {
        if (m_State != State.Extending)            
            return;

        m_CanClamp = true;
        ClampedObject = otherCollider.gameObject;

        DOTween.timeScale = Time.timeScale = .25f;

        foreach (SpriteRenderer r in m_RopeSegments)
            r.color = Color.red;
    }

    private void OnSpearCantClamp(Collider2D otherCollider)
    {
        if (m_State != State.Extending)
            return;

        m_CanClamp = false;
        ClampedObject = null;

        DOTween.timeScale = Time.timeScale = 1f;

        foreach (SpriteRenderer r in m_RopeSegments)
            r.color = Color.white;
    }

    private void OnSpearRicochet(Collider2D otherCollider)
    {
        CancelRopeMovement();

        m_State = State.Pullback;

        float tweenTime = m_CurrentDist / (kFireUnitsPerSec * 2);

        m_RopeMaskScaleTween = m_RopeShowMask.transform.DOScaleX(0f, tweenTime);
        m_RopeMaskMoveTween = m_RopeShowMask.transform.DOLocalMove(m_CurrentStartPos, tweenTime);
        m_SpearMoveTween = m_SpearTip.transform.DOLocalMove(m_CurrentStartPos + new Vector3(0, 0, -1f), tweenTime);
        m_SpearMoveTween.onComplete = OnRopePulledIn;
    }

    #endregion
}
