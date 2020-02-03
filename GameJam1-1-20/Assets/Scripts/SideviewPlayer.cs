using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class SideviewPlayer : MonoBehaviour
{
    public static event System.Action OnStartedOpeningInHijack;
    public static event System.Action OnStoppedOpeningInHijack;

    public enum State
    {
        None,
        Intro,
        Idle,
        Flight,
        JumpBoost,
        GoTo,
        Damaged,
        OnHatch,
        ControllingEnemy,
        AttachedToObject
    }

    [SerializeField] private Animator m_Animations;
    [SerializeField] private Transform m_DisplayRoot;
    [SerializeField] private HorizontalScroller m_BackgroundScroller;
    [SerializeField] private Transform m_JumpBoostLeftArrow;
    [SerializeField] private Transform m_JumpBoostRightArrow;
    [SerializeField] private Transform m_WorldRoot;
    [SerializeField] private GameObject m_GoToPointerPrefab;
    [SerializeField] private Transform m_ScreenRoot;
    [SerializeField] private BoxCollider2D m_BoxCollider;
    [SerializeField] private BackgroundTap m_BackgroundTap;
    //Temporarily bypass projectiles while working on hatch state movements
    [SerializeField] private bool m_TempDebugGetHitByProjectiles = false;
    [SerializeField] private HijackButton m_HijackButton;
    [SerializeField] private GameObject m_PlayerSpearPrefab;
    [SerializeField] private Sprite m_DebugDamagedDirArrowSprite;

    //Idle state vars
    private float m_IdleScrollSpeed = 0f;
    public const float kFlightMaxScrollSpeed = 12f;

    //Jump boost state vars
    private float m_JumpBoostStartTime = 0f;
    private Vector2 m_BoostVelocity = Vector2.zero;
    private Vector2 m_BoostAcceleration = Vector2.zero;
    private Vector2 m_BoostDir = Vector2.zero;
    private bool m_StartedBoostPath = false;
    private bool m_PressedForBoost = false;
    private float m_PressedForBoostStartTime = 0f;

    //Go-to state vars
    private GameObject m_GoToPointer;
    private Vector2 m_GoToSrcPos;
    private Vector2 m_GoToDestPos;
    private Vector2 m_GoToTravelDir;
    private float m_GoToTravelTime = 0f;
    private Tweener m_GoToTween;
    private const float kGoToUnitsPerSec = 6f;

    //Flight state vars

    //Damaged state vars
    private Vector2 m_DamagedVelocity = Vector2.zero;
    private Vector2 m_DamagedAcceleration = Vector2.zero;
    private Vector2 m_DamagedDir = Vector2.zero;

    //On Hatch state vars
    private bool m_IsOpening = false;
    private bool m_PressedDownOnHatch = false;
    private Vector2 m_PressedPos = Vector2.zero;
    private Vector2 m_ReleasePos = Vector2.zero;

    //Controlling enemy state vars
    private List<Enemy> m_OwnedEnemies;
    private Enemy m_CurrentAttachedEnemy;
    private Enemy m_CurrentControlledEnemy;

    //Clamp swinging vars
    private const float kMaxClampSwingTimeWindow = 1.5f; //This time check should enforce the "quick swipe" motion that's needed.
    private bool m_InputDownForClampSwing;
    private Vector2 m_ClampSwingScreenDownPos;
    private float m_ClampSwingDownStartTime;
    private Vector2 m_ClampSwingVelocity;
    private Vector2 m_ClampSwingDir;
    private Vector2 m_ClampSwingAcceleration;
    private bool m_IsSwingingOnClamp;
    private System.Action m_SwingOutDoneCallback;
    private Sequence m_TopOfSwingDelay;
    private Transform m_RopeSwingOutParent;
    private Vector2 m_RopeSwingOutEndWorldLocalPos;

    private State m_PlayerState;
    public State PlayerState { get { return m_PlayerState; } }
    private Vector2 m_ScreenMoveMin = Vector2.zero;
    private Vector2 m_ScreenMoveMax = Vector2.zero;
    private float m_ScreenUnitsWidth = 0f;
    private float m_ScreenUnitsHeight = 0f;
    private Vector3 m_InitScale = Vector3.one;
    private Button m_EnterOrLeaveShipBtn = null;

    private Vector2 m_DistTraveledFromStart = Vector2.zero;
    public Vector2 DistTraveledFromStart { get { return m_DistTraveledFromStart; } }

    private PlayerSpear m_PlayerSpear;

    private void Awake()
    {
        m_OwnedEnemies = new List<Enemy>();

        m_InitScale = transform.localScale;

        m_ScreenUnitsHeight = Camera.main.orthographicSize * 2f;
        float aspectRatio = (float)Screen.width / (float)Screen.height;
        m_ScreenUnitsWidth = m_ScreenUnitsHeight * aspectRatio;

        m_ScreenMoveMin = new Vector2(-m_ScreenUnitsWidth * .5f + 1f, -m_ScreenUnitsHeight * .5f + 1f);
        m_ScreenMoveMax = new Vector2(m_ScreenUnitsWidth * .5f - 1f, m_ScreenUnitsHeight * .5f - 1f);

        m_GoToPointer = Instantiate(m_GoToPointerPrefab, m_ScreenRoot);
        m_GoToPointer.SetActive(false);

        m_JumpBoostLeftArrow.gameObject.SetActive(false);
        m_JumpBoostRightArrow.gameObject.SetActive(false);

        GameObject spearObj = Instantiate(m_PlayerSpearPrefab);
        m_PlayerSpear = spearObj.GetComponent<PlayerSpear>();
        m_PlayerSpear.transform.SetParent(transform, false);
        m_PlayerSpear.Player = this;

        Enemy.OnSuccessfullyHijacked += OnSuccessfullyHijackedEnemy;
        GameHUD.OnLeaveOrEnterShipButtonTapped += OnEnterOrLeaveShipHUDButtonClicked;
    }

    private void Start()
    {
        m_EnterOrLeaveShipBtn = GameHUD.Instance.LeaveOrEnterShipButton;
    }

    private void Update()
    {
        if (m_PlayerState == State.Intro || 
            m_PlayerState == State.ControllingEnemy)
            return;

        State nextState = State.None;

        Vector2 moveVecThisFrame = Vector2.zero;
        bool bgInputDown = m_BackgroundTap.GetHasInput();
        bool bgInputUp = m_BackgroundTap.GetHasInputUp();

        if (nextState == State.Flight)
        {
            if (Input.GetKey(KeyCode.UpArrow))
                moveVecThisFrame = new Vector2(0f, 6f * Time.deltaTime);
            if (Input.GetKey(KeyCode.DownArrow))
                moveVecThisFrame = new Vector2(0f, -6f * Time.deltaTime);
        }

        //Detect to go into jump boost state
        if (bgInputDown && 
            m_PlayerState != State.Damaged && 
            m_PlayerState != State.OnHatch &&
            m_PlayerState != State.AttachedToObject)
        {
            if(m_StartedBoostPath && m_PlayerSpear.CanClamp)
            {
                HandleClampedToObjectByRope();
            }
            else
            {
                float currentTime = Time.realtimeSinceStartup;
                if (!m_PressedForBoost)
                {
                    m_PressedForBoost = true;
                    m_PressedForBoostStartTime = currentTime;
                }
                if (currentTime - m_PressedForBoostStartTime > .3f)
                {
                    nextState = State.JumpBoost;
                    ResetStateVars(nextState);

                    if (m_PlayerState != nextState)
                        InitJumpBoostState();

                    Vector2 playerScreenPos = Camera.main.WorldToScreenPoint(transform.position);
                    Vector2 arrowDirPos = Input.mousePosition;
                    Vector2 lookAt = (arrowDirPos - playerScreenPos).normalized;

                    float jumpBoostAngle = Mathf.Atan2(lookAt.y, lookAt.x) * Mathf.Rad2Deg;

                    m_JumpBoostRightArrow.rotation = Quaternion.Euler(new Vector3(0f, 0f, jumpBoostAngle));
                    m_JumpBoostRightArrow.localPosition = m_JumpBoostRightArrow.transform.right * .3f;

                    float timeToMaxBoost = 1f;
                    float timeInJumpBoost = Mathf.Min(Time.realtimeSinceStartup - m_JumpBoostStartTime, timeToMaxBoost);
                    float maxBoostAdditiveArrowScale = 7f;

                    m_JumpBoostRightArrow.localScale = new Vector3(maxBoostAdditiveArrowScale, 4f, 1f);

                    m_DisplayRoot.localScale = new Vector3(Mathf.Abs(m_DisplayRoot.localScale.x) * (lookAt.x < 0f ? -1f : 1f),
                                                           m_DisplayRoot.localScale.y,
                                                           m_DisplayRoot.localScale.z);
                }
            }
        }

        //On mouse up, see if we've been jump boost state, if not, see if there was no other state set, that
        //would mean we can do our go-to.
        if (bgInputUp && 
            m_PlayerState != State.Damaged &&
            m_PlayerState != State.OnHatch &&
            m_PlayerState != State.AttachedToObject)
        {
            m_PressedForBoost = false;

            if (m_PlayerState == State.JumpBoost)
            {
                if(!m_StartedBoostPath)
                {
                    m_StartedBoostPath = true;
                    m_JumpBoostRightArrow.gameObject.SetActive(false);

                    m_PlayerSpear.Clear();
                    m_PlayerSpear.FireIntoDirection(transform.position, 
                                                    m_JumpBoostRightArrow.right, 
                                                    () => 
                    {
                        ResetStateVars(State.Idle);
                        InitIdleState();
                    });
                }
            }
            else if (nextState == State.None)
            {
                nextState = State.GoTo;
                ResetStateVars(nextState);
                InitGoToState();
            }
        }

        if(m_PlayerState == State.OnHatch || m_PlayerState == State.AttachedToObject)
        {
            if(m_PlayerState != State.AttachedToObject)
            {
                bool hijackBtnPressed = m_HijackButton.IsHeldDown;

                if (hijackBtnPressed)
                {
                    if (!m_IsOpening)
                    {
                        m_IsOpening = true;
                        m_Animations.Play("HijackOpen");
                        OnStartedOpeningInHijack?.Invoke();
                    }
                }
                else if (m_IsOpening)
                {
                    m_IsOpening = false;
                    m_Animations.Play("Idle");
                    OnStoppedOpeningInHijack?.Invoke();
                }
            }

            if (m_PlayerSpear.CurrentState == PlayerSpear.State.ClosedToClamp)
            {
                if(!m_IsSwingingOnClamp)
                {
                    //Spear being in this state means we can swipe on the screen to clamp swing

                    if (bgInputDown && !m_InputDownForClampSwing)
                    {
                        m_InputDownForClampSwing = true;
                        m_ClampSwingScreenDownPos = Input.mousePosition;
                        m_ClampSwingDownStartTime = Time.realtimeSinceStartup;
                    }
                    else if (bgInputUp && m_InputDownForClampSwing)
                    {
                        m_InputDownForClampSwing = false;

                        float clampSwipeTime = Time.realtimeSinceStartup - m_ClampSwingDownStartTime;
                        if (clampSwipeTime <= kMaxClampSwingTimeWindow)
                        {
                            //Reset spear to its idle state before doing any swing out behavior
                            m_PlayerSpear.ResetToIdle();
                            m_PlayerSpear.SetClosedToClampState();

                            //We'll say that this was quick enough to be considered a swipe

                            Vector3 worldSwipeStartPos = Camera.main.ScreenToWorldPoint(m_ClampSwingScreenDownPos);
                            Vector3 worldSwipeEndPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                            //Get swipe direction and distance
                            Vector2 swipeDir = (worldSwipeEndPos - worldSwipeStartPos).normalized;
                            float swipeDist = swipeDir.magnitude;

                            m_ClampSwingDir = swipeDir;
                            m_ClampSwingVelocity = swipeDir * 30f;
                            m_ClampSwingAcceleration = m_ClampSwingVelocity * 4f;

                            //We now use these data points to get the player spear to do its clamp swing behavior
                            m_IsSwingingOnClamp = true;
                            m_RopeSwingOutEndWorldLocalPos = transform.localPosition;
                            m_RopeSwingOutParent = transform.parent;
                            Vector3 swingInDestPos = transform.localPosition;

                            m_SwingOutDoneCallback = () =>
                            {
                                Vector3 localMoveDest = swingInDestPos;
                                m_TopOfSwingDelay = DOTween.Sequence();
                                m_TopOfSwingDelay.AppendInterval(.3f);
                                m_TopOfSwingDelay.Append(DOTween.To(GetCurrentPosFromRopeSwingIn, HandleClampedRopeSwingInVelocity, localMoveDest, 1f));
                                m_TopOfSwingDelay.AppendCallback(() =>
                                {
                                    m_TopOfSwingDelay = null;
                                    m_IsSwingingOnClamp = false;
                                });
                            };
                        }
                    }
                }
                else if(bgInputUp && m_TopOfSwingDelay != null)
                {
                    m_TopOfSwingDelay.Kill();
                    m_TopOfSwingDelay = null;

                    m_PlayerSpear.ResetToIdle();
                    m_InputDownForClampSwing = m_IsSwingingOnClamp = false;

                    transform.SetParent(m_ScreenRoot);
                    transform.position = new Vector3(transform.position.x, transform.position.y, -2f);

                    ResetStateVars(State.Idle);

                    if (m_PlayerState != State.Idle)
                        InitIdleState();
                }
            }
        }

        //Detect to go into idle state
        if (nextState == State.None && 
            m_PlayerState != State.GoTo && 
            m_PlayerState != State.JumpBoost &&
            m_PlayerState != State.Damaged &&
            m_PlayerState != State.OnHatch &&
            m_PlayerState != State.AttachedToObject)
        {
            nextState = State.Idle;
            ResetStateVars(nextState);

            if (m_PlayerState != nextState)
                InitIdleState();
        }

        //if(m_StartedBoostPath && m_PlayerState == State.JumpBoost)
            //HandleBoostVelocity(ref moveVecThisFrame);

        if (m_PlayerState == State.Damaged)
            HandleDamagedVelocity(ref moveVecThisFrame);

        //if(m_PlayerState != State.OnHatch && m_PlayerState != State.AttachedToObject)
            HandleMoveStep(moveVecThisFrame);

        if ((m_PlayerState == State.OnHatch || m_PlayerState == State.AttachedToObject) && m_IsSwingingOnClamp)
            HandleClampedRopeSwingOutVelocity();

        //Check to see if player is close to any enemies that are controlled. If yes,
        //we show the enter/leave button
        if(m_PlayerState != State.ControllingEnemy && 
           m_PlayerState != State.OnHatch)
        {
            bool inRangeOfAny = false;
            if (m_CurrentControlledEnemy == null)
            {
                foreach (Enemy e in m_OwnedEnemies)
                {
                    float dist = (transform.position - e.transform.position).magnitude;
                    inRangeOfAny = (dist <= 5f);
                    if (inRangeOfAny)
                        break;
                }
            }
            m_EnterOrLeaveShipBtn.gameObject.SetActive(inRangeOfAny);
        }
        else if(m_PlayerState == State.ControllingEnemy &&
                !m_EnterOrLeaveShipBtn.gameObject.activeSelf)
        {
            m_EnterOrLeaveShipBtn.gameObject.SetActive(true);
        }

        float bgWorldStep = m_BackgroundScroller.GetMoveStep() * .05f;

        //m_DistTraveledFromStart += new Vector2(bgWorldStep, 0f);
        m_WorldRoot.transform.position += new Vector3(-bgWorldStep, 0f, 0f);
    }

    public int TravelDir
    {
        get
        {
            float bgWorldStep = m_BackgroundScroller.GetMoveStep() * .05f;
            return Mathf.Approximately(bgWorldStep, 0) || bgWorldStep > 0f ? 1 : -1;
        }
    }

    #region go to state

    private void InitGoToState()
    {
        m_GoToSrcPos = transform.position;
        m_GoToDestPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);

        Vector2 gotoLength = m_GoToDestPos - m_GoToSrcPos;
        m_GoToTravelTime = gotoLength.magnitude / kGoToUnitsPerSec;
        m_GoToTravelDir = gotoLength.normalized;

        m_DisplayRoot.localScale = new Vector3(m_DisplayRoot.localScale.x * (m_GoToTravelDir.x > 0 ? 1 : -1),
                                               m_DisplayRoot.localScale.y,
                                               m_DisplayRoot.localScale.z);

        m_GoToPointer.transform.position = new Vector3(m_GoToDestPos.x, 
                                                       m_GoToDestPos.y, 
                                                       m_GoToPointer.transform.position.z);
        m_GoToPointer.SetActive(true);

        if (m_GoToTween != null)
            m_GoToTween.Kill();
        m_GoToTween = DOTween.To(UpdateGoToPos, 0f, 1f, m_GoToTravelTime).SetEase(Ease.OutCirc);
        m_GoToTween.onComplete = () =>
        {
            m_PlayerState = State.Idle;
            m_GoToPointer.SetActive(false);
            m_GoToTween = null;
        };

        m_Animations.Play("Idle");
        m_PlayerState = State.GoTo;

        m_PlayerSpear.Clear();
    }

    private void UpdateGoToPos(float t)
    {
        Vector2 currPos = m_GoToSrcPos + (m_GoToDestPos - m_GoToSrcPos) * t;
        transform.position = new Vector3(currPos.x, currPos.y, transform.position.z);
        HandleMoveStep(Vector3.zero);
    }

    #endregion

    #region idle state

    private void InitIdleState()
    {
        m_Animations.Play("Idle");
        m_BackgroundScroller.SetScrollSpeed(m_IdleScrollSpeed);
        m_PlayerState = State.Idle;
    }

    #endregion

    #region flight state

    private void InitFlightState()
    {
        m_Animations.Play("Flight");
        m_DisplayRoot.localScale = Vector3.one;

        //Vector2 originDestPos = new Vector2(m_ScreenMoveMin.x, transform.position.y);
        //m_FlightOriginTravelDir = (originDestPos - (Vector2)transform.localPosition).normalized;

        m_JumpBoostRightArrow.localScale = new Vector3(4f, 4f, 1f);
        m_PlayerState = State.Flight;
        DOTween.To(x => m_BackgroundScroller.SetScrollSpeed(x), 0, kFlightMaxScrollSpeed, 2f);
    }

    #endregion

    #region jump boost state

    private void InitJumpBoostState()
    {
        m_Animations.Play("JumpBoost");
        m_JumpBoostRightArrow.gameObject.SetActive(true);
        m_JumpBoostStartTime = Time.realtimeSinceStartup;
        m_BackgroundScroller.SetScrollSpeed(m_IdleScrollSpeed);
        m_PlayerState = State.JumpBoost;
    }

    private void HandleBoostVelocity(ref Vector2 moveVectorOut)
    {
        Vector2 nextPos = transform.position + (Vector3)m_BoostVelocity * Time.deltaTime;
        float velocityDot = Vector2.Dot(m_BoostDir, (nextPos - (Vector2)transform.position).normalized);
        bool nextPosWillBeOutsideScreenArea = nextPos.x > m_ScreenMoveMax.x || 
                                             nextPos.x < m_ScreenMoveMin.x || 
                                             nextPos.y > m_ScreenMoveMax.y || 
                                             nextPos.y < m_ScreenMoveMin.y;
        
        if (velocityDot > 0f && !nextPosWillBeOutsideScreenArea)
        {
            moveVectorOut += m_BoostVelocity * Time.deltaTime;
            m_BoostVelocity -= m_BoostAcceleration * Time.deltaTime;
        }
        else
        {
            m_BoostVelocity = m_BoostAcceleration = Vector2.zero;
            ResetStateVars(State.Idle);
            InitIdleState();
        }
    }

    private void HandleClampedToObjectByRope()
    {
        if (m_PlayerSpear.CurrentState != PlayerSpear.State.Extending)
            return;
        
        m_PlayerSpear.CancelRopeMovement();

        GameObject clampedObject = m_PlayerSpear.ClampedObject;
        State nextStateFromClamp = State.None;

        if(clampedObject.tag == "ShipHatch")
        {
            Enemy enemy = clampedObject.GetComponentInParent<Enemy>();
            if (m_OwnedEnemies.IndexOf(enemy) != -1)
                return;

            //Now that we're attached to enemy, keep it in screen space
            enemy.transform.SetParent(m_ScreenRoot);
            //Tell enemy it's getting fucked over hard
            enemy.SetState(Enemy.State.GettingHijacked);

            nextStateFromClamp = State.OnHatch;

            m_CurrentAttachedEnemy = enemy;               
        }
        else
        {
            nextStateFromClamp = State.AttachedToObject;
        }


        if(clampedObject.tag == "Enemy")
        {
            Enemy enemy = clampedObject.GetComponentInParent<Enemy>();
            enemy.OnOffscreen -= OnEnemyOffscreenWhileAttached;
            enemy.OnOffscreen += OnEnemyOffscreenWhileAttached;
        }

        //Set player to coordinate space of hatchee
        transform.SetParent(clampedObject.transform);

        ResetStateVars(State.AttachedToObject);
        InitAttachedToObjectState();

        //Reeling in should be last, since clamping is going to likely put player
        //into the transform space of the clampee
        m_PlayerSpear.ReelIn(() =>
        {
            if (nextStateFromClamp != State.None)
                ResetStateVars(nextStateFromClamp);

            if (m_PlayerState != nextStateFromClamp)
            {
                switch (nextStateFromClamp)
                {
                    case State.OnHatch:
                        InitOnHatchState();
                        break;
                }
            }
        });
    }

    private void OnEnemyOffscreenWhileAttached()
    {
        if (m_PlayerState != State.OnHatch && m_PlayerState != State.AttachedToObject)
            return;

        if(m_TopOfSwingDelay != null)
            m_TopOfSwingDelay.Kill();
        m_TopOfSwingDelay = null;

        //Reset rope slinging state stuff
        m_PlayerSpear.ResetToIdle();
        m_InputDownForClampSwing = m_IsSwingingOnClamp = false;

        transform.SetParent(m_ScreenRoot);
        transform.position = new Vector3(transform.position.x, transform.position.y, -2f);

        ResetStateVars(State.Idle);

        if (m_PlayerState != State.Idle)
            InitIdleState();
    }

    #endregion

    #region damaged state

    private void InitDamagedState()
    {
        m_Animations.Play("Damaged");
        m_PlayerState = State.Damaged;
    }

    private void HandleDamagedVelocity(ref Vector2 moveVectorOut)
    {
        Vector2 nextPos = transform.position + (Vector3)m_DamagedVelocity * Time.deltaTime;
        float velocityDot = Vector2.Dot(m_DamagedDir, (nextPos - (Vector2)transform.position).normalized);
        bool nextPosWillBeOutsideScreenArea = nextPos.x > m_ScreenMoveMax.x ||
                                             nextPos.x < m_ScreenMoveMin.x ||
                                             nextPos.y > m_ScreenMoveMax.y ||
                                             nextPos.y < m_ScreenMoveMin.y;

        if (velocityDot > 0f && !nextPosWillBeOutsideScreenArea)
        {
            moveVectorOut += m_DamagedVelocity * Time.deltaTime;
            m_DamagedVelocity -= m_DamagedAcceleration * Time.deltaTime * 4f;
        }
        else
        {
            m_DamagedVelocity = m_DamagedAcceleration = Vector2.zero;
            ResetStateVars(State.Idle);
            InitIdleState();
        }
    }


    #endregion

    #region on hatch state

    private void InitOnHatchState()
    {
        m_Animations.Play("Idle");
        m_PlayerState = State.OnHatch;
        m_BackgroundScroller.SetScrollSpeed(0f);
        m_IsOpening = false;
        m_HijackButton.gameObject.SetActive(true);
    }

    private void OnSuccessfullyHijackedEnemy()
    {
        m_Animations.Play("InEnemy");

        ResetStateVars(State.ControllingEnemy);
        m_PlayerState = State.ControllingEnemy;

        m_CurrentControlledEnemy = m_CurrentAttachedEnemy;
        m_CurrentAttachedEnemy = null;
        m_OwnedEnemies.Add(m_CurrentControlledEnemy);

        m_CurrentControlledEnemy.SetState(Enemy.State.PlayerControlled);

        m_EnterOrLeaveShipBtn.gameObject.SetActive(true);
    }

    #endregion

    #region attached to object state

    private void InitAttachedToObjectState()
    {
        m_PlayerState = State.AttachedToObject;
    }

    private void HandleClampedRopeSwingOutVelocity()
    {
        Vector3 moveStep = (Vector3)m_ClampSwingVelocity * Time.deltaTime;
        Vector2 nextPos = transform.position + moveStep;
        float velocityDot = Vector2.Dot(m_ClampSwingDir, (nextPos - (Vector2)transform.position).normalized);
        bool nextPosWillBeOutsideScreenArea = nextPos.x > m_ScreenMoveMax.x ||
                                             nextPos.x < m_ScreenMoveMin.x ||
                                             nextPos.y > m_ScreenMoveMax.y ||
                                             nextPos.y < m_ScreenMoveMin.y;

        if (velocityDot > 0f && !nextPosWillBeOutsideScreenArea)
        {
            transform.position += moveStep;
            m_ClampSwingVelocity -= m_ClampSwingAcceleration * Time.deltaTime;
        }
        else
        {
            m_ClampSwingVelocity = m_ClampSwingAcceleration = Vector2.zero;
            m_SwingOutDoneCallback?.Invoke();
            m_SwingOutDoneCallback = null;
        }

        HandleMoveStep(Vector2.zero);

        Vector3 anchorEndWorldPos = m_RopeSwingOutParent.TransformPoint(m_RopeSwingOutEndWorldLocalPos);
        m_PlayerSpear.SetRopeLengthBetweenAnchors(transform.position,
                new Vector3(anchorEndWorldPos.x, anchorEndWorldPos.y, transform.position.z));
    }

    private Vector3 GetCurrentPosFromRopeSwingIn()
    {
        return transform.localPosition;
    }

    private void HandleClampedRopeSwingInVelocity(Vector3 pos)
    {
        transform.localPosition = pos;
        HandleMoveStep(Vector3.zero);

        Vector3 anchorEndWorldPos = m_RopeSwingOutParent.TransformPoint(m_RopeSwingOutEndWorldLocalPos);
        m_PlayerSpear.SetRopeLengthBetweenAnchors(transform.position,
                new Vector3(anchorEndWorldPos.x, anchorEndWorldPos.y, transform.position.z));
    }

    #endregion

    private void HandleMoveStep(Vector2 moveStep)
    {
        Vector3 pos = transform.position + (Vector3)moveStep;
        float xPos = Mathf.Max(Mathf.Min(pos.x, m_ScreenMoveMax.x), m_ScreenMoveMin.x);
        float yPos = Mathf.Max(Mathf.Min(pos.y, m_ScreenMoveMax.y), m_ScreenMoveMin.y);

        //float xDist = xPos - transform.localPosition.x;
        //float yDist = yPos - transform.localPosition.y;

        //m_DistTraveledFromStart += new Vector2(xDist, yDist);
        transform.position = new Vector3(xPos, yPos, pos.z);
    }

    private void ResetStateVars(State nextState)
    {
        if (m_PlayerState == nextState)
            return;

        switch(m_PlayerState)
        {
            case State.AttachedToObject:
                //I think some resetting stuff from this state will be contextual and not single purpose...
                break;
            case State.GoTo:
                if (m_GoToTween != null)
                    m_GoToTween.Kill();
                m_GoToTween = null;
                m_GoToPointer.SetActive(false);
                break;
            case State.JumpBoost:
                m_JumpBoostRightArrow.gameObject.SetActive(false);
                m_JumpBoostLeftArrow.gameObject.SetActive(false);
                m_PressedForBoost = m_StartedBoostPath = false;
                m_PressedForBoostStartTime = 0f;
                m_JumpBoostRightArrow.localScale = Vector3.one;
                m_BoostVelocity = m_BoostAcceleration = Vector2.zero;
                break;
            case State.Flight:
                DOTween.To(x => m_BackgroundScroller.SetScrollSpeed(x), kFlightMaxScrollSpeed, 0, 2f);
                break;
            case State.OnHatch:
                {
                    if(m_TopOfSwingDelay != null)
                        m_TopOfSwingDelay.Kill();

                    m_TopOfSwingDelay = null;
                    m_IsSwingingOnClamp = false;
                    m_SwingOutDoneCallback = null;
                    m_InputDownForClampSwing = false;

                    m_PlayerSpear.ResetToIdle();

                    m_IsOpening = false;
                    m_HijackButton.gameObject.SetActive(false);
                    m_PlayerSpear.Clear();
                }
                break;
        }
    }

    private GameObject m_DebugDamageHitArrow;
    private Sequence m_DebugDamageHideSeq;

    private void OnTriggerEnter2D(Collider2D otherCollider)
    {
        if (m_PlayerState == State.ControllingEnemy)
            return;

        if(otherCollider.tag == "Enemy" && 
           m_PlayerState != State.OnHatch &&
           m_PlayerState != State.AttachedToObject)
        {
            //TODO: Put this in for enemy mechbots...I think staying attached to ships is cool.
            //==============================================================================
            //if (m_PlayerState == State.AttachedToObject)
            //{
            //    m_PlayerSpear.ResetToIdle();
            //    transform.SetParent(m_ScreenRoot);
            //    //Hack
            //    transform.position = new Vector3(transform.position.x, transform.position.y, -2f);
            //}
            //==============================================================================

            ResetStateVars(State.Damaged);
            m_DamagedDir = (transform.position - otherCollider.gameObject.transform.position).normalized;
            m_DamagedAcceleration = m_DamagedVelocity = m_DamagedDir * 20f;
            InitDamagedState();

            ShowDamageDirDebugArrow(otherCollider.gameObject.transform.parent,
                m_DamagedDir,
                new Vector3(otherCollider.gameObject.transform.position.x, otherCollider.gameObject.transform.position.y, -5f));
        }
        else if(otherCollider.tag == "ShipHatch" && 
                m_PlayerState == State.JumpBoost)
        {
            //DEPRECATED...should now be handled by rope clamping

            //Now that we're attached to enemy, keep it in screen space
            //Enemy enemy = otherCollider.GetComponentInParent<Enemy>();

            //if(m_OwnedEnemies.IndexOf(enemy) != -1)
            //    return; 
            
            //enemy.transform.SetParent(m_ScreenRoot);
            //enemy.SetState(Enemy.State.GettingHijacked);

            ////Set player to hatch coordinate space
            //transform.SetParent(otherCollider.transform, false);
            //transform.localPosition = new Vector3(0f, 0f, -1f);
            //transform.localScale = new Vector3(1.5f, 1.5f, 1f);

            //m_CurrentAttachedEnemy = enemy;

            //ResetStateVars(State.OnHatch);
            //if(m_PlayerState != State.OnHatch)
                //InitOnHatchState();
        }
        else if(otherCollider.tag == "Projectile" &&
                m_PlayerState != State.Damaged &&
                m_TempDebugGetHitByProjectiles)
        {
            transform.SetParent(m_ScreenRoot);
            transform.localScale = m_InitScale;

            ResetStateVars(State.Damaged);
            m_DamagedDir = (transform.position - otherCollider.gameObject.transform.position).normalized;
            m_DamagedAcceleration = m_DamagedVelocity = m_DamagedDir * 5f;
            InitDamagedState();

            //Destroy projectile that hits player
            Destroy(otherCollider.gameObject);

            m_CurrentAttachedEnemy.SetState(Enemy.State.Patrol);
            m_CurrentAttachedEnemy = null;
        }
    }

    private void OnTriggerStay2D(Collider2D otherCollider)
    {
        if (m_PlayerState == State.ControllingEnemy)
            return;

        if (otherCollider.tag == "Enemy" &&
           m_PlayerState != State.OnHatch &&
           m_PlayerState != State.AttachedToObject)
        {
            ResetStateVars(State.Damaged);
            m_DamagedDir = (transform.position - otherCollider.gameObject.transform.position).normalized;
            m_DamagedAcceleration = m_DamagedVelocity = m_DamagedDir * 20f;
            InitDamagedState();
        }
    }

    private void OnEnterOrLeaveShipHUDButtonClicked()
    {
        if(m_PlayerState == State.ControllingEnemy)
        {
            //If we get here, trying to leave ship

            Vector3 leaveEnemyPos = m_CurrentControlledEnemy.transform.localPosition + m_CurrentControlledEnemy.transform.right * 2f;
            transform.SetParent(m_ScreenRoot.transform, false);
            transform.localPosition = leaveEnemyPos;
            transform.localScale = m_InitScale;

            //Put controlled enemy back into world space
            m_CurrentControlledEnemy.transform.SetParent(m_WorldRoot, true);
            m_CurrentControlledEnemy.SetState(Enemy.State.PlayerControlledIdle);
            m_CurrentControlledEnemy = null;

            ResetStateVars(State.ControllingEnemy);
            InitIdleState();

            return;
        }

        //If we get here, try to enter ship. Since player could click on button, had to be
        //close to an enemy
        float lowestDist = float.MaxValue;
        Enemy closestEnemy = null;
        foreach (Enemy e in m_OwnedEnemies)
        {
            float dist = (transform.position - e.transform.position).magnitude;
            if(dist < lowestDist)
            {
                lowestDist = dist;
                closestEnemy = e;
            }
        }

        if (closestEnemy == null)
            return;

        m_CurrentControlledEnemy = closestEnemy;
        m_CurrentControlledEnemy.SetState(Enemy.State.PlayerControlled);
        m_CurrentControlledEnemy.transform.SetParent(m_ScreenRoot);
        m_CurrentAttachedEnemy = null;

        m_Animations.Play("InEnemy");
        m_PlayerState = State.ControllingEnemy;
        m_BackgroundScroller.SetScrollSpeed(0f);
    }

    private void ShowDamageDirDebugArrow(Transform arrowParent, Vector2 dir, Vector3 arrowPos)
    {
        if (m_DebugDamageHitArrow == null)
        {
            m_DebugDamageHitArrow = new GameObject("damage_dir_debug_arrow");
            SpriteRenderer spriteRenderer = m_DebugDamageHitArrow.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = m_DebugDamagedDirArrowSprite;
            spriteRenderer.color = Color.yellow;
        }

        m_DebugDamageHitArrow.SetActive(true);
        m_DebugDamageHitArrow.transform.position = arrowPos;
        m_DebugDamageHitArrow.transform.SetParent(arrowParent);
        m_DebugDamageHitArrow.transform.localScale = new Vector3(.2f, .2f, 1f);
        m_DebugDamageHitArrow.transform.localRotation = Quaternion.Euler(new Vector3(0f, 0f, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg));

        if (m_DebugDamageHideSeq != null)
            m_DebugDamageHideSeq.Kill();

        m_DebugDamageHideSeq = DOTween.Sequence();
        m_DebugDamageHideSeq.AppendInterval(1f);
        m_DebugDamageHideSeq.AppendCallback(() =>
        {
            m_DebugDamageHitArrow.SetActive(false);
            m_DebugDamageHideSeq = null;
        });
    }

    public void OnIntroDone()
    {
        m_PlayerState = State.Idle;
    }
}
