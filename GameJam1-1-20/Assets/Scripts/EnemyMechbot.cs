using DG.Tweening;
using UnityEngine;

public class EnemyMechbot : MonoBehaviour, IAttackableEnemy
{
    public enum State
    {
        Idle,
        GotoAnchor,
        DestroyTarget,
        Destroyed,
        Damaged
    }

    public Enemy LinkedEnemy { get; set; }
    public Transform WorldRoot { get; set; }
    public int ClaimedGridIndex { get; set; } = -1;
    public SideviewPlayer Player { get; set; }

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
    [SerializeField] private Animator m_AnimationState;
    [SerializeField] private GameObject m_ShooterBeamPrefab;

    //Destroy target state vars
    private const float kBeamShootMinTime = 2f;
    private const float kBeamShootMaxTime = 4f;
    private float m_TimeSinceBeamShoot = -1f;

    private void Awake()
    {
    }

    private void Start()
    {
        CurrentState = State.GotoAnchor;
    }

    private void Update()
    {
       if (CurrentState != State.DestroyTarget)
            return;

        if(Time.realtimeSinceStartup > m_TimeSinceBeamShoot)
        {
            ShootBeamAtPlayer();
            m_TimeSinceBeamShoot = Time.realtimeSinceStartup + Random.Range(kBeamShootMinTime, kBeamShootMaxTime);
        }
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
                InitDestroyTarget();
                break;
            case State.Destroyed:
                InitDestroy();
                break;
            case State.Damaged:
                InitDamagedState();
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

        transform.position = new Vector3(LinkedEnemy.transform.position.x, 
                                         LinkedEnemy.transform.position.y, 
                                         transform.position.z);

        SpriteRenderer[] sprites = GetComponentsInChildren<SpriteRenderer>(true);
        foreach (SpriteRenderer s in sprites)
        {
            s.color = new Color(s.color.r, s.color.g, s.color.b, 0f);
            s.DOFade(1f, .5f);
        }

        float unitsPerSec = 7f;
        float moveTime = anchorDist / unitsPerSec;
        transform.DOMove(new Vector3(anchor.center.x, anchor.center.y, transform.position.z), moveTime).onComplete = () =>
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

    private void InitDestroyTarget()
    {
        m_CurrentState = State.DestroyTarget;

        //If first time we hit this, write out beam shoot time
        if(Mathf.Approximately(m_TimeSinceBeamShoot, -1f))
            m_TimeSinceBeamShoot = Time.realtimeSinceStartup + Random.Range(kBeamShootMinTime, kBeamShootMaxTime);
    }

    private void ShootBeamAtPlayer()
    {
        GameObject shooterBeamObj = Instantiate(m_ShooterBeamPrefab, WorldRoot);
        shooterBeamObj.transform.position = transform.position;

        Vector3 right = (Player.transform.position - transform.position).normalized;
        Vector3 forward = new Vector3(0f, 0f, -1f);
        Vector3 up = Vector3.Cross(right, forward);

        shooterBeamObj.transform.right = right;
        shooterBeamObj.transform.forward = forward;
        shooterBeamObj.transform.up = up;
    }

    #endregion

    #region Destroyed state

    private void InitDestroy()
    {
        m_AnimationState.Play("EnemyMechbot_Explode");
    }

    public void OnExplosionAnimFinished()
    {
        Destroy(gameObject);        
    }

    #endregion

    #region Damaged state

    public void OnAttacked()
    {
        if (CurrentState == State.GotoAnchor || CurrentState == State.Damaged)
            return;

        //TODO: Decrement hp
        CurrentState = State.Damaged;
    }

    private void InitDamagedState()
    {
        m_CurrentState = State.Damaged;
        m_AnimationState.Play("EnemyMechbot_Damaged");
    }

    public void OnDamagedAnimDone()
    {
        CurrentState = State.DestroyTarget;
        m_AnimationState.Play("EnemyMechbot_Idle");
    }

    #endregion
}
