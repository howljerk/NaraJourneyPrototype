using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WorldChunkManager : MonoBehaviour
{
    //TODO: Will use pool when I get the chunk management working
    [SerializeField] private GameObject m_WorldChunkPrefab;
    [SerializeField] private FallingPlayer m_Player;

    private WorldChunk m_CurrentChunk;

    private void Awake()
    {
        m_CurrentChunk = CreateChunk(transform.position);
        m_CurrentChunk.m_Prev = CreateChunk(transform.position + new Vector3(0f, 10f, 0f));
        m_CurrentChunk.m_Next = CreateChunk(transform.position + new Vector3(0f, -10f, 0f));
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
            newNextChunk = m_CurrentChunk;
            newCurrChunk = m_CurrentChunk.m_Prev;
            newPrevChunk = CreateChunk(newCurrChunk.transform.position + new Vector3(0f, 10f, 0f));

            m_CurrentChunk = newCurrChunk;
            m_CurrentChunk.m_Prev = newPrevChunk;
            m_CurrentChunk.m_Next = newNextChunk;
            
            return;
        }

        newPrevChunk = m_CurrentChunk;
        newCurrChunk = m_CurrentChunk.m_Next;
        newNextChunk = CreateChunk(newCurrChunk.transform.position + new Vector3(0f, -10f, 0f));

        m_CurrentChunk = newCurrChunk;
        m_CurrentChunk.m_Prev = newPrevChunk;
        m_CurrentChunk.m_Next = newNextChunk;
    }

    private WorldChunk CreateChunk(Vector3 chunkPos)
    {
        GameObject chunkObj = Instantiate(m_WorldChunkPrefab, transform);
        chunkObj.transform.position = chunkPos;
        return chunkObj.GetComponent<WorldChunk>();
    }
}
