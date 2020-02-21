
using UnityEngine;

[System.Serializable]
public class PlayerSpearThrowState : IPlayerState
{
    [SerializeField] private Animator m_Animations;
    [SerializeField] private Transform m_DisplayRoot;
    [SerializeField] private VerticalScroller m_BGScroller;
    [SerializeField] private Transform m_JumpBoostLeftArrow;
    [SerializeField] private Transform m_JumpBoostRightArrow;
    [SerializeField] private BackgroundTap m_BackgroundTap;
    [SerializeField] private FallingPlayerCam m_FallingCamera;

    private FallingPlayer m_Player;
    private float m_StateStartTime = 0f;
    private bool m_ActiveSpearThrow = false;

    public void Enter(FallingPlayer player)
    {
        m_Player = player;
        m_Animations.Play("JumpBoost");
        m_StateStartTime = Time.realtimeSinceStartup;
        m_ActiveSpearThrow = false;
    }

    private void HandleFallMovement()
    {
        Vector3 moveVel = Vector3.zero;

        //We fall in this state too. Let's do the movement first...
        moveVel += m_Player.GetFallMovementStep();
        m_Player.transform.localPosition += moveVel * Time.deltaTime;

        Vector3 pos = m_Player.transform.position;
        m_FallingCamera.ClampPositionToScreen(ref pos);
        m_Player.transform.position = pos;
    }

    public void Update()
    {
        HandleFallMovement();

        if (m_BGScroller != null)
            m_BGScroller.UpdateFromMoveStep();

        if (m_FallingCamera != null)
            m_FallingCamera.TryToFollow();

        if(m_ActiveSpearThrow)
        {
            if(m_BackgroundTap.GetHasInput() && m_Player.Spear.CanClamp)
                m_Player.ChangeToState(m_Player.AttachedToObjectState);

            return;
        }

        if (!m_ActiveSpearThrow && m_BackgroundTap.GetHasInputUp())
        {
            float timeInState = Time.realtimeSinceStartup - m_StateStartTime;

            if(timeInState > .25f)
            {
                //This means we held long enough in a direction to say: "yeah, player was throwing spear in direction"
                m_ActiveSpearThrow = true;
                m_Player.Spear.Clear();
                m_Player.Spear.FireIntoDirection(m_Player.transform.position,
                                                m_JumpBoostRightArrow.right,
                                                () => m_Player.ChangeToState(m_Player.IdleState));

                m_JumpBoostRightArrow.gameObject.SetActive(false);
            }
            else
            {
                m_Player.ChangeToState(m_Player.IdleState);
            }

            return;
        }

        //This means player is still holding down, so we update throw direction here:

        if (!m_JumpBoostRightArrow.gameObject.activeSelf)
            m_JumpBoostRightArrow.gameObject.SetActive(true);

        Vector2 playerScreenPos = Camera.main.WorldToScreenPoint(m_Player.transform.position);
        Vector2 arrowDirPos     = Input.mousePosition;
        Vector2 lookAt          = (arrowDirPos - playerScreenPos).normalized;
        float jumpBoostAngle    = Mathf.Atan2(lookAt.y, lookAt.x) * Mathf.Rad2Deg;

        m_JumpBoostRightArrow.rotation = Quaternion.Euler(new Vector3(0f, 0f, jumpBoostAngle));
        m_JumpBoostRightArrow.localPosition = m_JumpBoostRightArrow.transform.right * .3f;

        m_DisplayRoot.localScale = new Vector3(Mathf.Abs(m_DisplayRoot.localScale.x) * (lookAt.x < 0f ? -1f : 1f),
                                               m_DisplayRoot.localScale.y,
                                               m_DisplayRoot.localScale.z);
    }

    public void Exit()
    {

    }

    public PlayerState GetState()
    {
        return PlayerState.SpearThrow;
    }
}
