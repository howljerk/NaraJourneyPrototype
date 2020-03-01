using System.Collections.Generic;
using UnityEngine;

public class FallingPlayer : MonoBehaviour
{
    public const float kMaxFallYVel = 3f;
    public const float kMaxSteerXVel = 5f;

    [SerializeField] private GameObject m_PlayerSpearPrefab;
    [SerializeField] private PlayerIdleState m_IdleState = new PlayerIdleState();
    [SerializeField] private PlayerSpearThrowState m_SpearThrowState = new PlayerSpearThrowState();
    [SerializeField] private PlayerAttachedToObjectState m_AttachedToObjectState = new PlayerAttachedToObjectState();
    [SerializeField] private Transform m_DisplayRoot;
    [SerializeField] private FallingPlayerCam m_FallingPlayerCam;
    [SerializeField] private WorldChunkManager m_ChunkManager;

    public PlayerIdleState IdleState { get { return m_IdleState; } }
    public PlayerSpearThrowState SpearThrowState { get { return m_SpearThrowState; } }
    public PlayerAttachedToObjectState AttachedToObjectState {  get { return m_AttachedToObjectState; } }

    private IPlayerState m_CurrentState = null;
    private PlayerSpear m_PlayerSpear;
    private Vector3 m_FallVel;
    private Vector3 m_FallAccel;
    private Vector3 m_SteerVel;
    private Vector3 m_SteerAccel;
    private bool m_PrevSteeredLeft;
    List<WorldChunk> chunks = new List<WorldChunk>();

    public PlayerSpear Spear {  get { return m_PlayerSpear; } }

    private void Awake()
    {
        GameObject spearObj = Instantiate(m_PlayerSpearPrefab);
        m_PlayerSpear = spearObj.GetComponent<PlayerSpear>();
        m_PlayerSpear.transform.SetParent(transform, false);
        m_PlayerSpear.Player = this;
        m_PlayerSpear.FallingPlayerCam = m_FallingPlayerCam;

        ChangeToState(m_IdleState);
    }

    private void Update()
    {
        if (m_CurrentState != null)
            m_CurrentState.Update();
    }

    public void ChangeToState(IPlayerState state)
    {
        if (m_CurrentState != null)
            m_CurrentState.Exit();

        m_CurrentState = state;
        if (m_CurrentState == null)
            return;

        m_CurrentState.Enter(this);
    }

    public void FaceRightDirection()
    {
        Vector3 localScale = m_DisplayRoot.transform.localScale;
        m_DisplayRoot.transform.localScale = new Vector3(Mathf.Abs(localScale.x),
            localScale.y,
            localScale.z);
    }

    public void FaceLeftDirection()
    {
        Vector3 localScale = m_DisplayRoot.transform.localScale;
        m_DisplayRoot.transform.localScale = new Vector3(-Mathf.Abs(localScale.x),
            localScale.y,
            localScale.z);
    }

    public void ResolveCollisions()
    {
        chunks.Clear();
        chunks.Add(m_ChunkManager.CurrentChunk.m_Prev);
        chunks.Add(m_ChunkManager.CurrentChunk);
        chunks.Add(m_ChunkManager.CurrentChunk.m_Next);

        Bounds playerBounds = GetComponent<Collider2D>().bounds;

        foreach (WorldChunk c in chunks)
        {
            PushOutCollision[] collision = c.m_Collisions;
            foreach(PushOutCollision col in collision)
            {
                BoxCollider2D collider = col.GetComponent<BoxCollider2D>();
                Bounds pushOutBounds = collider.bounds;

                if (!CollisionUtils.GetDoesCollide(transform.position,
                    playerBounds,
                    col.transform.position,
                    pushOutBounds,
                    out var results))
                    continue;

                Vector3 pos = transform.position;

                if (results.m_PushOutAxis == RectTransform.Axis.Horizontal)
                    transform.position = new Vector3(pos.x + results.m_PushOut, pos.y, pos.z);
                else
                    transform.position = new Vector3(pos.x, pos.y + results.m_PushOut, pos.z);
            }
        }
    }

    #region steer movement

    public void InitSteerMovement()
    {
        m_SteerVel = m_SteerAccel = Vector3.zero;
    }

    public Vector3 GetSteerRightMovementStep()
    {
        if (m_PrevSteeredLeft)
            m_SteerVel = Vector3.zero;

        m_PrevSteeredLeft = false;
        m_SteerAccel = new Vector3(FallingPlayer.kMaxSteerXVel * 3, 0f, 0f);
        m_SteerVel += m_SteerAccel * Time.deltaTime;

        if (m_SteerVel.x >= FallingPlayer.kMaxSteerXVel)
            m_SteerVel = new Vector3(FallingPlayer.kMaxSteerXVel, 0f, 0f);

        return m_SteerVel;
    }

    public Vector3 GetSteerLeftMovementStep()
    {
        if (!m_PrevSteeredLeft)
            m_SteerVel = Vector3.zero;

        m_PrevSteeredLeft = true;
        m_SteerAccel = new Vector3(-FallingPlayer.kMaxSteerXVel * 3, 0f, 0f);
        m_SteerVel += m_SteerAccel * Time.deltaTime;

        if (Mathf.Abs(m_SteerVel.x) >= FallingPlayer.kMaxSteerXVel)
            m_SteerVel = new Vector3(-FallingPlayer.kMaxSteerXVel, 0f, 0f);

        return m_SteerVel;
    }

    #endregion

    #region fall movement

    public void InitFallMovement()
    {
        m_FallVel = Vector3.zero;
        m_FallAccel = new Vector3(0f, FallingPlayer.kMaxFallYVel, 0f);
    }

    public Vector3 GetFallMovementStep()
    {
        Vector3 fallMovement = m_FallVel;
        m_FallVel -= m_FallAccel * Time.deltaTime;

        if (Mathf.Abs(m_FallVel.y) >= kMaxFallYVel)
            m_FallVel = new Vector3(0f, -kMaxFallYVel, 0f);

        return fallMovement;
    }

    #endregion
}
