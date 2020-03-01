using UnityEngine;

public class WorldChunk : MonoBehaviour
{
    public WorldChunk m_Prev;
    public WorldChunk m_Next;
    public PushOutCollision[] m_Collisions;
}
