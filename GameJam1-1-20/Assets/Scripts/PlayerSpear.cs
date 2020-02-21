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
        SwingingOutFromClamp,
        TuggedFromClamp
    }

    [SerializeField] private Sprite m_RopeSprite;

    [SerializeField] private GameObject m_RopeNode;
    [SerializeField] private PlayerSpearTip m_SpearTip;
    [SerializeField] private GameObject m_RopeShowMask;

    private const float kFireDist = 5f;
    private const float kFireUnitsPerSec = 12f;

    private State m_State = State.Idle;
    public State CurrentState { get { return m_State; } }

    private bool m_CanClamp = false;
    public bool CanClamp { get { return m_CanClamp; } }

    public FallingPlayer Player { get; set; }

    public GameObject ClampedObject { get; private set; }

    private List<SpriteRenderer> m_RopeSegments = new List<SpriteRenderer>();
    private Tweener m_RopeMaskScaleTween;
    private Tweener m_RopeMaskMoveTween;
    private Tweener m_SpearMoveTween;
    private Tweener m_PlayerReelInMoveTween;
    private Vector3 m_CurrentStartPos;
    private Vector3 m_CurrentEndPos;
    private float m_CurrentFireDist;
    private System.Action m_PulledBackInCallback;
    private System.Action m_ReeledInCallback;
    private System.Action m_TugDoneCallback;
    private FallingPlayerCam m_FallingPlayerCam;

    public FallingPlayerCam FallingPlayerCam {  set { m_FallingPlayerCam = value; } }

    private void Awake()
    {
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
        m_SpearTip.Reset();

        if (m_RopeMaskScaleTween != null)
            m_RopeMaskScaleTween.Kill();
        if (m_RopeMaskMoveTween != null)
            m_RopeMaskMoveTween.Kill();
        if (m_SpearMoveTween != null)
            m_SpearMoveTween.Kill();
        if (m_PlayerReelInMoveTween != null)
            m_PlayerReelInMoveTween.Kill();

        DOTween.timeScale = Time.timeScale = 1f;

        foreach (SpriteRenderer r in m_RopeSegments)
            r.color = Color.white;
    }

    public void FireIntoDirection(Vector3 startPos, Vector2 dir, System.Action pulledBackInCallback)
    {
        m_PulledBackInCallback = pulledBackInCallback;

        transform.position = new Vector3(startPos.x, startPos.y, transform.position.z);

        Vector3 endPos = transform.position + (Vector3)(dir * kFireDist);
        m_FallingPlayerCam.ClampPositionToScreen(ref endPos);

        float ropeWorldUnitLength = 0f;
        float ropeAngle = 0f;
        Vector2 ropeWorldDir = Vector2.zero;
        BuildRopeBetweenTwoPoints(startPos, endPos, out ropeWorldUnitLength, out ropeAngle, out ropeWorldDir);

        m_CurrentFireDist = ropeWorldUnitLength;

        float tweenTime = ropeWorldUnitLength / kFireUnitsPerSec;

        m_RopeShowMask.transform.localRotation = Quaternion.Euler(new Vector3(0f, 0f, ropeAngle));

        if (m_RopeMaskScaleTween != null)
            m_RopeMaskScaleTween.Kill();

        m_RopeShowMask.transform.localScale = new Vector3(0f, 1f, 1f);
        m_RopeMaskScaleTween = m_RopeShowMask.transform.DOScaleX(ropeWorldUnitLength, tweenTime);

        if (m_RopeMaskMoveTween != null)
            m_RopeMaskMoveTween.Kill();

        m_CurrentStartPos = m_RopeShowMask.transform.parent.InverseTransformPoint(startPos);
        m_CurrentEndPos = m_RopeShowMask.transform.parent.InverseTransformPoint(endPos);

        m_RopeShowMask.transform.localPosition = m_CurrentStartPos;
        m_RopeMaskMoveTween = m_RopeShowMask.transform.DOLocalMove(m_CurrentStartPos + (Vector3)ropeWorldDir * ropeWorldUnitLength * .5f, tweenTime);

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

        float tweenTime = m_CurrentFireDist / kFireUnitsPerSec;

        m_RopeMaskScaleTween = m_RopeShowMask.transform.DOScaleX(0f, tweenTime);
        m_RopeMaskMoveTween = m_RopeShowMask.transform.DOLocalMove(Vector3.zero, tweenTime);

        Vector3 spearToPlayerLocalPos = Player.transform.parent.InverseTransformPoint(m_SpearTip.transform.position);
        m_PlayerReelInMoveTween = Player.transform.DOLocalMove(spearToPlayerLocalPos, tweenTime);

        m_SpearMoveTween = m_SpearTip.transform.DOLocalMove(Vector3.zero, tweenTime);
        m_SpearMoveTween.onComplete = OnRopeReeledIn;

        m_ReeledInCallback = reeledInCallback;
    }

    public void SetRopeLengthBetweenAnchors(Vector3 anchorWorldStart, Vector3 anchorWorldEnd)
    {
        //Make sure to have clean slate for rope segments before building between 2 anchor points
        foreach (SpriteRenderer r in m_RopeSegments)
            Destroy(r.gameObject);
        m_RopeSegments.Clear();

        Vector3 spearTipPos = m_SpearTip.transform.position;
        m_SpearTip.transform.position = new Vector3(anchorWorldEnd.x, anchorWorldEnd.y, spearTipPos.z);
        m_SpearTip.gameObject.SetActive(true);

        float ropeWorldUnitLength = 0f;
        float ropeAngle = 0f;
        Vector2 ropeWorldDir = Vector2.zero;
        BuildRopeBetweenTwoPoints(anchorWorldStart, anchorWorldEnd, out ropeWorldUnitLength, out ropeAngle, out ropeWorldDir);

        Vector3 anchorLocalStart = m_RopeShowMask.transform.parent.InverseTransformPoint(anchorWorldStart);

        m_RopeShowMask.transform.localRotation = Quaternion.Euler(new Vector3(0f, 0f, ropeAngle));
        m_RopeShowMask.transform.localScale = new Vector3(ropeWorldUnitLength,
                                                        m_RopeShowMask.transform.localScale.y,
                                                        m_RopeShowMask.transform.localScale.z);
        m_RopeShowMask.transform.localPosition = (Vector3)anchorLocalStart + (Vector3)ropeWorldDir * ropeWorldUnitLength * .5f; 
    }

    public void SetClosedToClampState()
    {
        m_State = State.ClosedToClamp;
    }

    public void TugBackClampedRope(System.Action tugDoneCallback)
    {
        m_TugDoneCallback = tugDoneCallback;
        m_State = State.TuggedFromClamp;

        float tweenTime = m_CurrentFireDist / (kFireUnitsPerSec * 2);

        m_RopeMaskScaleTween = m_RopeShowMask.transform.DOScaleX(0f, tweenTime);
        m_RopeMaskMoveTween = m_RopeShowMask.transform.DOLocalMove(m_CurrentStartPos, tweenTime);
        m_SpearMoveTween = m_SpearTip.transform.DOLocalMove(m_CurrentStartPos + new Vector3(0, 0, -1f), tweenTime);
        m_SpearMoveTween.onComplete = OnRopeTuggedFromClamp;
    }

    /// <summary>
    /// Callback for when spear has finished being tugged back to player from a clamp position
    /// </summary>
    private void OnRopeTuggedFromClamp()
    {
        ResetToIdle();
        m_TugDoneCallback?.Invoke();
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

        float tweenTime = m_CurrentFireDist / (kFireUnitsPerSec * 2);

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

    private void BuildRopeBetweenTwoPoints(Vector3 startPos,
                                          Vector3 endPos,
                                          out float ropeWorldUnitLength,
                                          out float ropeAngle,
                                          out Vector2 ropeWorldDir)
    {
        Vector3 screenStartPos = Camera.main.WorldToScreenPoint(startPos);
        Vector3 screenEndPos = Camera.main.WorldToScreenPoint(endPos);
        Vector2 lookAt = (screenEndPos - screenStartPos).normalized;
        Vector2 worldDir = (endPos - startPos).normalized;
        float currentDist = (endPos - startPos).magnitude;
        float angle = Mathf.Atan2(lookAt.y, lookAt.x) * Mathf.Rad2Deg;

        GameObject rope = new GameObject("rope");
        rope.transform.SetParent(m_RopeNode.transform);
        rope.transform.position = (endPos + startPos) * .5f;
        rope.transform.localPosition = new Vector3(rope.transform.localPosition.x, rope.transform.localPosition.y, 1f);
        rope.transform.rotation = Quaternion.Euler(new Vector3(0f, 0f, angle));
        rope.transform.localScale = new Vector3(currentDist, .1f, 1f);

        SpriteRenderer ropeSprite = rope.AddComponent<SpriteRenderer>();
        ropeSprite.sprite = m_RopeSprite;
        ropeSprite.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;

        m_RopeSegments.Add(ropeSprite);

        ropeWorldUnitLength = currentDist;
        ropeAngle = angle;
        ropeWorldDir = worldDir;
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
        if (m_State != State.Extending)
            return;

        CancelRopeMovement();

        m_State = State.Pullback;

        float tweenTime = m_CurrentFireDist / (kFireUnitsPerSec * 2);

        m_RopeMaskScaleTween = m_RopeShowMask.transform.DOScaleX(0f, tweenTime);
        m_RopeMaskMoveTween = m_RopeShowMask.transform.DOLocalMove(m_CurrentStartPos, tweenTime);
        m_SpearMoveTween = m_SpearTip.transform.DOLocalMove(m_CurrentStartPos + new Vector3(0, 0, -1f), tweenTime);
        m_SpearMoveTween.onComplete = OnRopePulledIn;
    }

    #endregion
}
