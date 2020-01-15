﻿using DG.Tweening;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    public static event System.Action OnSuccessfullyHijacked; 

    public enum State
    {
        Patrol,
        GettingHijacked,
        PlayerControlled,
        PlayerControlledIdle
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

    private SpawnMarker m_SpawnMarker;
    private BoxCollider2D m_HatchBoxCollider;
    private SpriteRenderer m_HatchSprite;
    private HijackUI m_HijackedMeterUI;
    private float m_IsOpeningTimeScale;
    private float m_IsOpeningTick;
    private float m_IsOpeningTotalTime;
    private Tweener m_AnimateHatchTween;
    private Tweener m_AnchorShipToScreenEdgeTween;
    private Tweener m_FlightTween;
    private Vector2 m_ScreenMoveMin = Vector2.zero;
    private Vector2 m_ScreenMoveMax = Vector2.zero;
    private float m_ScreenUnitsWidth = 0f;
    private float m_ScreenUnitsHeight = 0f;
    private const float kScreenHEdgeMargin = 3f;
    private const float kScreenVEdgeMargin = 1f;

    //Crashed state vars
    private bool m_DidCrash = false;
    private Vector2 m_CrashedAccel;
    private Vector2 m_CrashedVel;
    private Vector2 m_CrashedDir;
    private Tweener m_CrashPunchScaleTween;

    //Player controlled state vars
    private bool m_IsMovingForward;

    public State CurrState { get; set; } = State.Patrol;

    private void Awake()
    {
        m_ScreenUnitsHeight = Camera.main.orthographicSize * 2f;
        float aspectRatio = (float)Screen.width / (float)Screen.height;
        m_ScreenUnitsWidth = m_ScreenUnitsHeight * aspectRatio;

        m_ScreenMoveMin = new Vector2(-m_ScreenUnitsWidth * .5f + kScreenHEdgeMargin, -m_ScreenUnitsHeight * .5f + kScreenVEdgeMargin);
        m_ScreenMoveMax = new Vector2(m_ScreenUnitsWidth * .5f - kScreenHEdgeMargin, m_ScreenUnitsHeight * .5f - kScreenVEdgeMargin);
    }

    private void Start()
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

        GameObject pointerObj = Instantiate(m_SpawnPointerPrefab, m_ScreenRoot);
        pointerObj.transform.localPosition = pointerPos;
        m_SpawnMarker = pointerObj.GetComponent<SpawnMarker>();
        m_SpawnMarker.TrackedEntity = gameObject;
    }

    private void Remove()
    {
        DistanceIntervalEnemySpawner.RemoveEnemy(this);

        Destroy(gameObject);
        Destroy(m_SpawnMarker.gameObject);

        m_SpawnMarker = null;
    }

    private void ResetState(State state)
    {
        switch(state)
        {
            case State.PlayerControlled:
                {
                    if (m_FlightTween != null)
                        m_FlightTween.Kill();

                    m_FlightTween = null;
                    m_IsMovingForward = false;
                }
                break;
        }
    }

    public void SetState(State state)
    {
        ResetState(CurrState);
        CurrState = state;

        switch(CurrState)
        {
            case State.Patrol:
                break;
            case State.GettingHijacked:
                InitGettingHijackedState();
                break;
            case State.PlayerControlled:
                m_SpawnMarker.DoTracking = false;
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

    private bool GetIsEnemyTooFarAway()
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
        float unitsHeight = Camera.main.orthographicSize * 2f;
        float aspectRatio = (float)Screen.width / (float)Screen.height;
        float unitsWidth = unitsHeight * aspectRatio;

        m_DisplayRoot.transform.localScale = new Vector3(TravelDir, 1f, 1f);

        if(CurrState == State.Patrol)
        {
            bool playerTravelingRight = m_Player.TravelDir == 1;
            Vector3 vel = Vector3.zero;
            //To make sure enemy stays at screen position when player gets in contact
            //with the hatch
            if (CurrState != State.GettingHijacked)
                vel = new Vector3(-kEnemySpeed * TravelDir, 0f, 0f);

            transform.position += vel * Time.deltaTime;

            if (GetIsEnemyTooFarAway())
            {
                Remove();
                return;
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

                if (m_AnchorShipToScreenEdgeTween != null)
                    m_AnchorShipToScreenEdgeTween.Kill();
                if (m_AnimateHatchTween != null)
                    m_AnimateHatchTween.Kill();

                m_HatchSprite.gameObject.SetActive(false);

                m_AnchorShipToScreenEdgeTween = transform.DOMoveX(-unitsWidth * .5f + kScreenHEdgeMargin, 1f);
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

                    if (m_FlightTween != null)
                        m_FlightTween.Kill();
                    
                    m_FlightTween = DOTween.To(x => m_BackgroundScroller.SetScrollSpeed(x), 
                                               0, 
                                               SideviewPlayer.kFlightMaxScrollSpeed, 
                                               2f);
                }
            }
            else if(Input.GetKeyUp(KeyCode.LeftCommand))
            {
                if (m_IsMovingForward)
                {
                    m_IsMovingForward = false;

                    if (m_FlightTween != null)
                        m_FlightTween.Kill();
                    
                    m_FlightTween = DOTween.To(x => m_BackgroundScroller.SetScrollSpeed(x), 
                                               m_BackgroundScroller.GetScrollSpeed(), 
                                               0f, 
                                               2f);
                }
            }

            Vector3 vel = new Vector3(0f, 5f, 0f) * Time.deltaTime; 
            if(Input.GetKey(KeyCode.UpArrow))
                transform.position = GetClampedToScreenAreaPos(transform.position + vel);
            if(Input.GetKey(KeyCode.DownArrow))
                transform.position = GetClampedToScreenAreaPos(transform.position - vel);
        }

        if(m_DidCrash)
        {
            Vector2 nextPos = transform.position + (Vector3)m_CrashedVel * Time.deltaTime;
            float velDot = Vector2.Dot(m_CrashedDir, (nextPos - (Vector2)transform.position).normalized);

            bool nextPosWillBeOutsideScreenArea = nextPos.x > m_ScreenMoveMax.x ||
                                                 nextPos.x < m_ScreenMoveMin.x ||
                                                 nextPos.y > m_ScreenMoveMax.y ||
                                                 nextPos.y < m_ScreenMoveMin.y;
            
            bool clampPosToScreenArea = CurrState != State.Patrol && nextPosWillBeOutsideScreenArea;

            if (velDot > 0f)
            {
                transform.position = nextPos;
                m_CrashedVel -= m_CrashedAccel * Time.deltaTime;

                if (clampPosToScreenArea)
                {
                    m_DidCrash = false;

                    //We need to clamp position to screen boundaries...so do that here.
                    transform.position = GetClampedToScreenAreaPos(transform.position);
                }
            }
            else
            {
                m_DidCrash = false;
            }
        }
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

    private void OnTriggerEnter2D(Collider2D otherCollider)
    {
        if (otherCollider.tag != "Enemy")
            return;

        Enemy otherEnemy = otherCollider.GetComponent<Enemy>();

        if(m_CrashPunchScaleTween == null)
        {
            m_CrashPunchScaleTween = transform.DOPunchScale(new Vector3(.4f, .4f, 1f), .3f, 20);
            m_CrashPunchScaleTween.onComplete = () => m_CrashPunchScaleTween = null;
        }
        if (otherEnemy.m_CrashPunchScaleTween == null)
        {
            otherEnemy.m_CrashPunchScaleTween = otherEnemy.transform.DOPunchScale(new Vector3(.4f, .4f, 1f), .3f, 20);
            otherEnemy.m_CrashPunchScaleTween.onComplete = () => otherEnemy.m_CrashPunchScaleTween = null;
        }

        OnCrashedIntoOtherEnemyShip(otherEnemy);
    }

    private void OnTriggerStay2D(Collider2D otherCollider)
    {
        if (otherCollider.tag != "Enemy")
            return;

        Enemy otherEnemy = otherCollider.GetComponent<Enemy>();
        OnCrashedIntoOtherEnemyShip(otherEnemy);
    }

    private void OnCrashedIntoOtherEnemyShip(Enemy otherEnemy)
    {
        otherEnemy.m_DidCrash = true;
        otherEnemy.m_CrashedDir = (otherEnemy.transform.position - transform.position).normalized;
        otherEnemy.m_CrashedVel = otherEnemy.m_CrashedDir * 20f;
        otherEnemy.m_CrashedAccel = otherEnemy.m_CrashedVel * 4f;

        m_DidCrash = true;
        m_CrashedDir = (transform.position - otherEnemy.transform.position).normalized;
        m_CrashedVel = m_CrashedDir * 20f;
        m_CrashedAccel = m_CrashedVel * 4f;
    }
}