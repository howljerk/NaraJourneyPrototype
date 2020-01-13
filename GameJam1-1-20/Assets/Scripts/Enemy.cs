using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class Enemy : MonoBehaviour
{
    public static event System.Action OnSuccessfullyHijacked; 

    public enum State
    {
        Idle,
        GettingHijacked,
        PlayerControlled
    }

    [SerializeField] private GameObject m_SpawnPointerPrefab;
    [SerializeField] private GameObject m_Hatch;
    [SerializeField] private BoxCollider2D m_BoxCollider;
    [SerializeField] private GameObject m_DisplayRoot;
    [SerializeField] Transform m_HijackedUIAnchor;
    [SerializeField] GameObject m_HijackedPrefab;

    private Transform m_ScreenRoot;
    public void SetScreenRoot(Transform screenRoot)
    {
        m_ScreenRoot = screenRoot;
    }

    private SideviewPlayer m_Player;
    public void SetPlayer(SideviewPlayer player)
    {
        m_Player = player;
    }

    private HorizontalScroller m_BackgroundScroller;
    public void SetScroller(HorizontalScroller backgroundScroller)
    {
        m_BackgroundScroller = backgroundScroller;
    }

    public int TravelDir { get; set; } = 1;
    public Canvas uiRoot { get; set; }

    public BoxCollider2D BoxCollider { get { return m_BoxCollider; } }
    public BoxCollider2D HatchBoxCollider { get { return m_HatchBoxCollider; } }

    public const float kEnemySpeed = 4f;

    private GameObject m_SpawnPointer;
    private Sequence m_SpawnPointerScaleSequence;
    private BoxCollider2D m_HatchBoxCollider;
    private SpriteRenderer m_HatchSprite;
    private HijackUI m_HijackedMeterUI;
    private float m_IsOpeningTimeScale;
    private float m_IsOpeningTick;
    private float m_IsOpeningTotalTime;
    private Tweener m_AnimateHatchTween;
    private Tweener m_AnchorShipToScreenEdgeTween;

    //Player controlled state vars
    private bool m_IsMovingForward;

    public State CurrState { get; set; } = State.Idle;

    // Start is called before the first frame update
    void Start()
    {
        m_HatchBoxCollider = m_Hatch.GetComponent<BoxCollider2D>();
        m_HatchSprite = m_Hatch.GetComponentInChildren<SpriteRenderer>();
        AnimateHatch();

        SideviewPlayer.OnStartedOpeningInHijack += OnPlayerStartedOpening;
        SideviewPlayer.OnStoppedOpeningInHijack += OnPlayerStoppedOpening;
    }

    public void CreateSpawnMarker()
    {
        float unitsHeight = Camera.main.orthographicSize * 2f;
        float aspectRatio = (float)Screen.width / (float)Screen.height;
        float unitsWidth = unitsHeight * aspectRatio;

        Vector3 enemyScreenPos = m_ScreenRoot.InverseTransformPoint(transform.position);
        Vector3 pointerPos = new Vector3((unitsWidth * .5f - 1f) * m_Player.TravelDir, enemyScreenPos.y, enemyScreenPos.z);

        m_SpawnPointer = Instantiate(m_SpawnPointerPrefab, m_ScreenRoot);
        m_SpawnPointer.transform.localPosition = pointerPos;

        System.Action doSequence = null;
        doSequence = () =>
        {
            m_SpawnPointerScaleSequence = DOTween.Sequence();
            m_SpawnPointerScaleSequence.Append(m_SpawnPointer.transform.DOPunchScale(new Vector3(2f, 2f, 1f), .2f));
            m_SpawnPointerScaleSequence.AppendCallback(() => doSequence());
        };
        doSequence();
    }

    public void SetState(State state)
    {
        CurrState = state;

        switch(CurrState)
        {
            case State.Idle:
                break;
            case State.GettingHijacked:
                InitGettingHijackedState();
                break;
        }
    }

    #region Idle state

    #endregion

    #region Getting hijacked state

    private void InitGettingHijackedState()
    {
        GameObject ui = Instantiate(m_HijackedPrefab);
        m_HijackedMeterUI = ui.GetComponent<HijackUI>();
        m_HijackedMeterUI.transform.SetParent(uiRoot.transform, false);

        Vector3 meterScreenPos = uiRoot.transform.InverseTransformPoint(m_HijackedUIAnchor.transform.position);
        m_HijackedMeterUI.transform.localPosition = meterScreenPos;

        m_IsOpeningTimeScale = 0f;
        m_IsOpeningTick = 0f;
        m_IsOpeningTotalTime = 4f;
    }

    private void OnPlayerStartedOpening()
    {
        m_IsOpeningTimeScale = 1f;
    }

    private void OnPlayerStoppedOpening()
    {
        m_IsOpeningTimeScale = 0f;
    }

    #endregion

    private bool GetIsEnemyOffscreen()
    {
        bool playerTravelingRight = m_Player.TravelDir == 1;
        float unitsHeight = Camera.main.orthographicSize * 2f;
        float aspectRatio = (float)Screen.width / (float)Screen.height;
        float unitsWidth = unitsHeight * aspectRatio;
        float offscreenX = -unitsWidth * 2f * (playerTravelingRight ? 1 : -1);

        return ((playerTravelingRight && transform.position.x < offscreenX) || 
            (!playerTravelingRight && transform.position.x > offscreenX));
    }

    private void AnimateHatch()
    {
        System.Action doSequence = null;
        doSequence = () =>
        {
            m_AnimateHatchTween = m_HatchSprite.transform.DOPunchScale(new Vector3(.4f, .4f, 1f), .3f);
            m_AnimateHatchTween.onComplete = () => doSequence();
        };
        doSequence();
    }

    // Update is called once per frame
    void Update()
    {
        m_DisplayRoot.transform.localScale = new Vector3(TravelDir, 1f, 1f);

        if(CurrState != State.PlayerControlled)
        {
            bool playerTravelingRight = m_Player.TravelDir == 1;
            Vector3 vel = Vector3.zero;
            //To make sure enemy stays at screen position when player gets in contact
            //with the hatch
            if (CurrState != State.GettingHijacked)
                vel = new Vector3(-kEnemySpeed * m_Player.TravelDir, 0f, 0f);

            transform.position += vel * Time.deltaTime;

            float unitsHeight = Camera.main.orthographicSize * 2f;
            float aspectRatio = (float)Screen.width / (float)Screen.height;
            float unitsWidth = unitsHeight * aspectRatio;

            if (((playerTravelingRight && transform.position.x < unitsWidth * .5f) ||
                (!playerTravelingRight && transform.position.x > -unitsWidth * .5f)) &&
               m_SpawnPointer != null)
            {
                Destroy(m_SpawnPointer);
                m_SpawnPointer = null;

                m_SpawnPointerScaleSequence.Kill();
                m_SpawnPointerScaleSequence = null;
            }

            if (GetIsEnemyOffscreen())
            {
                DistanceIntervalEnemySpawner.RemoveEnemy(this);
                Destroy(gameObject);
            }
        }

        if(CurrState == State.GettingHijacked)
        {
            m_IsOpeningTick = Mathf.Min(m_IsOpeningTick + Time.deltaTime * m_IsOpeningTimeScale, m_IsOpeningTotalTime);
            m_HijackedMeterUI.SetFillPercent(m_IsOpeningTick / m_IsOpeningTotalTime);

            if(m_IsOpeningTick >= m_IsOpeningTotalTime)
            {
                //This is where the player has finished hijacking and is now controlling this enemy

                CurrState = State.PlayerControlled;
                TravelDir = -TravelDir;

                Destroy(m_HijackedMeterUI.gameObject);
                m_HijackedMeterUI = null;

                float unitsHeight = Camera.main.orthographicSize * 2f;
                float aspectRatio = (float)Screen.width / (float)Screen.height;
                float unitsWidth = unitsHeight * aspectRatio;

                if (m_AnchorShipToScreenEdgeTween != null)
                    m_AnchorShipToScreenEdgeTween.Kill();
                if (m_AnimateHatchTween != null)
                    m_AnimateHatchTween.Kill();

                m_HatchSprite.gameObject.SetActive(false);

                m_AnchorShipToScreenEdgeTween = transform.DOMoveX(-unitsWidth * .5f + 3f, 1f);
                m_IsMovingForward = false;

                OnSuccessfullyHijacked?.Invoke();
            }
        }

        if(CurrState == State.PlayerControlled && 
           m_AnchorShipToScreenEdgeTween != null)
        {
            if(Input.GetKey(KeyCode.LeftCommand))
            {
                if(!m_IsMovingForward)
                {
                    m_IsMovingForward = true;
                    DOTween.To(x => m_BackgroundScroller.SetScrollSpeed(x), 0, SideviewPlayer.kFlightMaxScrollSpeed, 5f);
                }
            }
            else if(Input.GetKeyUp(KeyCode.LeftCommand))
            {
                if (m_IsMovingForward)
                {
                    m_IsMovingForward = false;
                    DOTween.To(x => m_BackgroundScroller.SetScrollSpeed(x), 0, 0f, 5f);
                }
            }

            Vector3 vel = new Vector3(0f, 5f, 0f) * Time.deltaTime; 
            if(Input.GetKey(KeyCode.UpArrow))
                transform.position += vel;
            if(Input.GetKey(KeyCode.DownArrow))
                transform.position -= vel;
        }
    }
}
