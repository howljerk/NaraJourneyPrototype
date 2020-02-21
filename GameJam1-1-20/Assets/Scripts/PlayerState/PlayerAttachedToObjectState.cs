using UnityEngine;

[System.Serializable]
public class PlayerAttachedToObjectState : IPlayerState
{
    [SerializeField] private Animator m_Animations;

    private FallingPlayer m_Player;

    public void Enter(FallingPlayer player)
    {
        m_Player = player;
        m_Player.Spear.CancelRopeMovement();

        GameObject clampedObject = m_Player.Spear.ClampedObject;

        //Set player to coordinate space of hatchee
        m_Player.transform.SetParent(clampedObject.transform);
        m_Player.transform.position = new Vector3(m_Player.transform.position.x, m_Player.transform.position.y, -2f);

        //Reeling in should be last, since clamping is going to likely put player
        //into the transform space of the clampee
        m_Player.Spear.ReelIn(() =>
        {
            m_Animations.Play("Idle");
        });
    }

    public void Update()
    {

    }

    public void Exit()
    {

    }

    public PlayerState GetState()
    {
        return PlayerState.AttachedToObject;
    }

}