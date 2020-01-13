using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WorldGrid : MonoBehaviour
{
    [SerializeField] private bool m_DebugShowGrid;
    [SerializeField] private int m_GridRowCount;
    public int GridRowCount { get { return m_GridRowCount; } }

    [SerializeField] private int m_GridColCount;
    public int GridColCount { get { return m_GridColCount; } }

    [SerializeField] private GameObject m_EnemySelectGridOutlinePrefab;

    private Vector2 m_GridMin;
    private Vector2 m_GridMax;

    private static float s_SingleGridWidth;
    public static float SingleGridWidth { get { return s_SingleGridWidth; } }

    private static float s_SingleGridHeight;
    public static float SingleGridHeight { get { return s_SingleGridHeight; } }

    private Bounds[,] m_Grid;
    private List<GameObject> m_GridDisplay = new List<GameObject>();

    private void Awake()
    {
        float unitsHeight = Camera.main.orthographicSize * 2f;
        float aspectRatio = (float)Screen.width / (float)Screen.height;
        float unitsWidth = unitsHeight * aspectRatio;

        m_GridMin = new Vector2(-unitsWidth * .5f, -unitsHeight * .5f);
        m_GridMax = new Vector2(unitsWidth * .5f, unitsHeight * .5f);

        float heightLength = m_GridMax.y - m_GridMin.y;
        float widthLength = m_GridMax.x - m_GridMin.x;

        s_SingleGridWidth = widthLength / (float)m_GridColCount;
        s_SingleGridHeight = heightLength / (float)m_GridRowCount;

        BuildGrid();
    }

    public void GetColumnRowForPosition(Vector2 position, out int row, out int col)
    {
        row = col = -1;

        if (position.x < m_GridMin.x || 
            position.x > m_GridMax.x || 
            position.y < m_GridMin.y || 
            position.y > m_GridMax.y)
            return;
        
        for (int c = 0; c < m_Grid.GetLength(0); c++)
        {
            for (int r = 0; r < m_Grid.GetLength(1); r++)
            {
                Vector3 pos = new Vector3(position.x, position.y, m_Grid[c, r].center.z);
                if(m_Grid[c, r].Contains(pos))
                {
                    row = r;
                    col = c;
                    return;
                }
            }
        }
    }

    public Vector3 GetCenterForGrid(int row, int col)
    {
        return m_Grid[col, row].center;
    }

    private void OnValidate()
    {
        BuildGrid();
    }

    private void BuildGrid()
    {
        if (!Application.isPlaying)
            return;

        foreach (GameObject grid in m_GridDisplay)
            Destroy(grid);
        m_GridDisplay.Clear();

        m_Grid = new Bounds[m_GridColCount, m_GridRowCount];

        for (int c = 0; c < m_GridColCount; c++)
        {
            for (int r = 0; r < m_GridRowCount; r++)
            {
                Vector3 center = new Vector3(m_GridMin.x + s_SingleGridWidth * c + s_SingleGridWidth * .5f,
                                             m_GridMin.y + s_SingleGridHeight * r + s_SingleGridHeight * .5f,
                                            -1f);

                m_Grid[c, r] = new Bounds(center, new Vector3(s_SingleGridWidth, s_SingleGridHeight, 0));

                if (m_DebugShowGrid)
                {
                    GameObject gridObj = GameObject.CreatePrimitive(PrimitiveType.Quad);
                    gridObj.name = "grid_" + c + "_" + r;
                    gridObj.transform.position = m_Grid[c, r].center;
                    gridObj.GetComponent<MeshRenderer>().material = new Material(Shader.Find("Sprites/Default"));
                    gridObj.GetComponent<MeshRenderer>().material.color = new Color(Random.Range(0f, 1f), Random.Range(0f, 1f), Random.Range(0f, 1f), .4f);
                    gridObj.transform.localScale = new Vector3(s_SingleGridWidth, s_SingleGridHeight, 1f);

                    m_GridDisplay.Add(gridObj);
                }
            }
        }
    }
}
