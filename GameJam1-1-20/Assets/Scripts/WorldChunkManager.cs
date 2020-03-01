using UnityEngine;

public class WorldChunkManager : MonoBehaviour
{
    //TODO: Will use pool when I get the chunk management working
    [SerializeField] private GameObject m_WorldChunkPrefab;
    [SerializeField] private FallingPlayer m_Player;

    private WorldChunk m_CurrentChunk;
    public WorldChunk CurrentChunk {  get { return m_CurrentChunk; } }

    private void Awake()
    {
        m_CurrentChunk = CreateChunk(transform.position, "current_chunk");
        m_CurrentChunk.m_Prev = CreateChunk(transform.position + new Vector3(0f, 10f, 0f), "prev_chunk");
        m_CurrentChunk.m_Next = CreateChunk(transform.position + new Vector3(0f, -10f, 0f), "next_chunk");
    }

    private void Update()
    {
        UpdateChunksFromPlayerPosition();
    }

    public void UpdateChunksFromPlayerPosition()
    {
        if (m_CurrentChunk == null)
            return;

        Vector3 playerPos = m_Player.transform.position;
        Bounds currChunkBounds = m_CurrentChunk.GetComponent<BoxCollider2D>().bounds;

        //Nothing to do if we're still in the current chunk
        if(currChunkBounds.Contains(new Vector3(playerPos.x,
            playerPos.y,
            m_CurrentChunk.transform.position.z)))
        {
            return;
        }

        Bounds prevChunkBounds = m_CurrentChunk.m_Prev.GetComponent<BoxCollider2D>().bounds;
        WorldChunk newNextChunk, newCurrChunk, newPrevChunk = null;

        if (prevChunkBounds.Contains(new Vector3(playerPos.x,
                playerPos.y,
                m_CurrentChunk.transform.position.z)))
        {
            WorldChunk oldNext = m_CurrentChunk.m_Next;
            Destroy(oldNext.gameObject);

            newNextChunk = m_CurrentChunk;
            newNextChunk.name = "next_chunk";

            newCurrChunk = m_CurrentChunk.m_Prev;
            newCurrChunk.name = "current_chunk";

            newPrevChunk = CreateChunk(newCurrChunk.transform.position + new Vector3(0f, 10f, 0f), "prev_chunk");

            m_CurrentChunk = newCurrChunk;
            m_CurrentChunk.m_Prev = newPrevChunk;
            m_CurrentChunk.m_Next = newNextChunk;
            
            return;
        }

        WorldChunk oldPrev = m_CurrentChunk.m_Prev;
        Destroy(oldPrev.gameObject);

        newPrevChunk = m_CurrentChunk;
        newPrevChunk.name = "prev_chunk";

        newCurrChunk = m_CurrentChunk.m_Next;
        newCurrChunk.name = "current_chunk";

        newNextChunk = CreateChunk(newCurrChunk.transform.position + new Vector3(0f, -10f, 0f), "next_chunk");

        m_CurrentChunk = newCurrChunk;
        m_CurrentChunk.m_Prev = newPrevChunk;
        m_CurrentChunk.m_Next = newNextChunk;
    }

    private WorldChunk CreateChunk(Vector3 chunkPos, string name)
    {
        GameObject chunkObj = Instantiate(m_WorldChunkPrefab, transform);
        chunkObj.transform.position = chunkPos;
        chunkObj.name = name;
        WorldChunk chunk = chunkObj.GetComponent<WorldChunk>();
        chunk.m_Collisions = chunk.GetComponentsInChildren<PushOutCollision>();
        return chunk;
    }
}
