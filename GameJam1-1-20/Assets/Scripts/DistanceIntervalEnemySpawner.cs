using System.Collections.Generic;
using UnityEngine;

public class DistanceIntervalEnemySpawner : MonoBehaviour
{
    private static List<Enemy> s_CurrentEnemies = new List<Enemy>();
    public static List<Enemy> Enemies { get { return s_CurrentEnemies; } }

    public static void RemoveEnemy(Enemy e)
    {
        if (s_CurrentEnemies == null)
            return;

        int idx = s_CurrentEnemies.IndexOf(e);
        if (idx == -1)
            return;

        s_CurrentEnemies.RemoveAt(idx);
    }

    [SerializeField] private Transform m_WorldRoot;
    [SerializeField] private float m_DistanceUnitsInterval;
    [SerializeField] private HorizontalScroller m_BackgroundScroller;
    [SerializeField] private GameObject m_EnemyPrefab;
    [SerializeField] private SideviewPlayer m_Player;
    [SerializeField] private WorldGrid m_WorldGrid;
    [SerializeField] private GameObject m_SpawnPointerPrefab;
    [SerializeField] private Transform m_ScreenRoot;
    [SerializeField] private Canvas m_UIRoot;

    private int m_CurrentSpawnCount = 0;
    private float m_NextTimeToSpawn = 0f;
    private void Awake()
    {
        m_NextTimeToSpawn = Time.realtimeSinceStartup + Random.Range(10f, 20f);
    }

    private void Update()
    {
        //float totalDistanceTraveled = m_Player.DistTraveledFromStart.x;
        //int spawnCount = Mathf.RoundToInt(totalDistanceTraveled / m_DistanceUnitsInterval);

        //if(spawnCount > m_CurrentSpawnCount)
        //{
        //    m_CurrentSpawnCount = spawnCount;
        //    SpawnEnemy();
        //}

        if(Time.realtimeSinceStartup >= m_NextTimeToSpawn)
        {
            m_NextTimeToSpawn = Time.realtimeSinceStartup + Random.Range(10f, 20f);
            SpawnEnemy();
        }
    }

    private void SpawnEnemy()
    {
        float unitsHeight = Camera.main.orthographicSize * 2f;
        float aspectRatio = (float)Screen.width / (float)Screen.height;
        float unitsWidth = unitsHeight * aspectRatio;

        float spawnX = (unitsWidth * .5f + Random.Range(10f, 15f)) * m_Player.TravelDir;
        float spawnY = Random.Range(unitsHeight * .5f - 2f, -unitsHeight * .5f + 2f);

        GameObject enemyObj = Instantiate(m_EnemyPrefab, m_WorldRoot);
        enemyObj.transform.position = new Vector3(spawnX, spawnY, -2f);

        Enemy e = enemyObj.GetComponent<Enemy>();
        e.SetPlayer(m_Player);
        e.SetScreenRoot(m_ScreenRoot);
        e.SetScroller(m_BackgroundScroller);
        e.uiRoot = m_UIRoot;
        e.CreateSpawnMarker();

        if (m_Player.TravelDir == -1)
            e.TravelDir = -1;

        s_CurrentEnemies.Add(e);
    }
}
