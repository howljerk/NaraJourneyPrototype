public enum PlayerState
{
    None,
    Idle,
    SpearThrow,
    AttachedToObject
}

public interface IPlayerState
{
    void Enter(FallingPlayer player);
    void Update();
    void Exit();
    PlayerState GetState();
}
