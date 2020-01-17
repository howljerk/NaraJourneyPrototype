using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class EnemyMechbot : MonoBehaviour
{
    public enum State
    {
        Idle,
        GotoAnchor,
        DestroyTarget,
        Destroyed
    }

    public Enemy LinkedEnemy { get; set; }
    public Transform WorldRoot { get; set; }
    public int ClaimedGridIndex { get; set; } = -1;

    private State m_CurrentState = State.Idle;
    public State CurrentState
    {
        get { return m_CurrentState; }
        set
        {

            ResetState(m_CurrentState);
            m_CurrentState = value;
            InitState(m_CurrentState);
        }
    }

    [SerializeField] private GameObject m_GridAnchorDebug;
    [SerializeField] private bool m_EnableGridAnchorDebug = false;

    private void Awake()
    {
    }

    private void Start()
    {
        CurrentState = State.GotoAnchor;
    }

    private void ResetState(State state)
    {

    }

    private void InitState(State state)
    {
        switch (state)
        {
            case State.GotoAnchor:
                InitGotoAnchor();
                break;
            case State.DestroyTarget:
                break;
            case State.Destroyed:
                InitDestroy();
                break;
        }
    }

    #region InitGotoAnchor state

    private void InitGotoAnchor()
    {
        m_CurrentState = State.GotoAnchor;

        if (m_EnableGridAnchorDebug)
        {
            int idx = 0;
            foreach (Bounds grid in LinkedEnemy.MechbotScreenGrid)
            {
                GameObject debugObj = Instantiate(m_GridAnchorDebug);
                debugObj.transform.position = grid.center;
                debugObj.name = "grid_anchor_" + (idx++).ToString();

                Sequence destroySeq = DOTween.Sequence();
                destroySeq.AppendInterval(10f);
                destroySeq.AppendCallback(() => Destroy(debugObj));
            }
        }

        Bounds anchor = LinkedEnemy.MechbotScreenGrid[ClaimedGridIndex];
        float anchorDist = (anchor.center - LinkedEnemy.transform.position).magnitude;

        Vector3 moveDir = (anchor.center - LinkedEnemy.transform.position).normalized;
        Vector3 ls = transform.localScale;
        transform.localScale = new Vector3(ls.x * (moveDir.x < 0f ? -1 : 1), ls.y, ls.z);

        transform.position = WorldRoot.InverseTransformPoint(LinkedEnemy.transform.position);

        SpriteRenderer[] sprites = GetComponentsInChildren<SpriteRenderer>(true);
        foreach (SpriteRenderer s in sprites)
        {
            s.color = new Color(s.color.r, s.color.g, s.color.b, 0f);
            s.DOFade(1f, .5f);
        }

        float unitsPerSec = 7f;
        float moveTime = anchorDist / unitsPerSec;
        transform.DOMove(WorldRoot.InverseTransformPoint(anchor.center), moveTime).onComplete = () =>
        {
            //When done moving, face back towards the enemy ship
            transform.localScale = new Vector3(-transform.localScale.x,
                                               transform.localScale.y,
                                               transform.localScale.z);

            CurrentState = State.DestroyTarget;
        };
    }

    #endregion

    #region Destroy Target state

    //TODO: Do shooting at player
    private void InitDestroyTarget()
    {
        m_CurrentState = State.DestroyTarget;
    }

    #endregion

    #region Destroyed state

    private void InitDestroy()
    {
        //TODO: Do explosion anim
        Destroy(gameObject);
    }

    #endregion
}
