using UnityEngine;

[System.Serializable]
public class PlayerIdleState : IPlayerState
{
    [SerializeField] private Animator m_Animations;
    [SerializeField] private FallingPlayerCam m_FallingCamera;
    [SerializeField] private VerticalScroller m_BGScroller;
    [SerializeField] private BackgroundTap m_BackgroundTap;
    [SerializeField] private Transform m_JumpBoostLeftArrow;
    [SerializeField] private Transform m_JumpBoostRightArrow;

    private IPlayerState m_TransitionState;
    private FallingPlayer m_Player;
    private float m_InputDownStartTime;
    private bool m_InputDown;

    public void Enter(FallingPlayer player)
    {
        m_InputDown = false;
        m_Player = player;

        m_Animations.Play("Idle");

        m_JumpBoostLeftArrow.gameObject.SetActive(false);
        m_JumpBoostRightArrow.gameObject.SetActive(false);

        m_Player.InitFallMovement();
        m_Player.InitSteerMovement();

        VerticalScrollerUtils.StartScrolling(m_BGScroller, VerticalScrollerUtils.kFallingScrollSpeed);
    }

    public void Update()
    {
        m_TransitionState = null;

        HandleSteerAndFalling();

        HandleIfShouldSpearThrow();

        if (m_BGScroller != null)
            m_BGScroller.UpdateFromMoveStep();

        if (m_FallingCamera != null)
            m_FallingCamera.TryToFollow();

        if(m_TransitionState != null)
            m_Player.ChangeToState(m_TransitionState);
    }

    private void HandleIfShouldSpearThrow()
    {
        bool bgInputDown = m_BackgroundTap.GetHasInput();

        if(m_InputDown != bgInputDown && bgInputDown)
            m_InputDownStartTime = Time.realtimeSinceStartup;

        m_InputDown = bgInputDown;

        if (m_InputDown && Time.realtimeSinceStartup - m_InputDownStartTime >= .25f)
            m_TransitionState = m_Player.SpearThrowState;
    }

    private void HandleSteerAndFalling()
    {
        Vector3 moveVel = Vector3.zero;

        if (Input.GetKey(KeyCode.RightArrow))
        {
            moveVel += m_Player.GetSteerRightMovementStep();
            m_Player.FaceRightDirection();
        }
        else if (Input.GetKey(KeyCode.LeftArrow))
        {
            moveVel += m_Player.GetSteerLeftMovementStep();
            m_Player.FaceLeftDirection();
        }
        else
        {
            m_Player.InitSteerMovement();
        }

        //Add fall movement to the steer movement
        moveVel += m_Player.GetFallMovementStep();
        m_Player.transform.localPosition += moveVel * Time.deltaTime;

        Vector3 pos = m_Player.transform.position;
        m_FallingCamera.ClampPositionToScreen(ref pos);
        m_Player.transform.position = pos;
    }

    public void Exit()
    {
    }

    public PlayerState GetState()
    {
        return PlayerState.Idle;
    }
}
