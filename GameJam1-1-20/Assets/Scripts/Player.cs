using UnityEngine;
using DG.Tweening;

public class Player : MonoBehaviour
{
    private enum State
    {
        None,
        Intro,
        Idle,
        Flight,
        InvadeShot
    }

    [SerializeField] private Animator m_Animations;
    [SerializeField] private Transform m_DisplayRoot;
    [SerializeField] private BackgroundScroller m_BackgroundScroller;
    [SerializeField] private WorldGrid m_WorldGrid;
    [SerializeField] private GameObject m_PlayerSelectGridOutlinePrefab;

    private State m_PlayerState = State.Intro;
    private float m_DisplayLookXScale = 1f;
    private float m_DisplayLookYScale = 1f;
    private Vector2 m_PivotRange = Vector2.zero;
    private Vector3 m_IdlePosition;
    private float m_IdleScrollSpeed = 3f;
    private float m_FlyForwardScrollSpeed = 12f;
    private bool m_InFlightBurst = false;

    private int m_CurrentGridRow;
    private int m_CurrentGridCol;
    private GameObject m_PlayerSelectGridOutline;

    private void Awake()
    {
        m_IdlePosition = transform.localPosition;

        float unitsHeight = Camera.main.orthographicSize * 2f;
        float aspectRatio = (float)Screen.width / (float)Screen.height;
        float unitsWidth = unitsHeight * aspectRatio;

        m_PivotRange = new Vector2(unitsWidth * .5f - 1f, unitsHeight * .5f - 3.25f);

        m_CurrentGridRow = 0;
        m_CurrentGridCol = 1;
    }

    private void Start()
    {
        Vector3 gridCenter = m_WorldGrid.GetCenterForGrid(m_CurrentGridRow, m_CurrentGridCol);
        transform.position = new Vector3(gridCenter.x, gridCenter.y, transform.position.z);

        m_PlayerSelectGridOutline = Instantiate(m_PlayerSelectGridOutlinePrefab);
        m_PlayerSelectGridOutline.transform.localScale = new Vector3(WorldGrid.SingleGridWidth, WorldGrid.SingleGridHeight, 1f);
        m_PlayerSelectGridOutline.transform.position = transform.position;
    }

    // Update is called once per frame
    private void Update()
    {
        if (m_PlayerState == State.Intro)
            return;

        //Pressed down left/right/up arrows:
        //1) Change orientation
        //2) Move left/right/up 
        //3) Change state to flight
        //4) If going left/right offset scrolling background

        //float pivotXStep = 0f;
        //float pivotYStep = 0f;
        //float pivotYSpeed = 6f;
        //float pivotXSpeed = 10f;

        State nextState = State.None;
        //if (Input.GetKey(KeyCode.LeftArrow))
        //{
        //    m_DisplayLookXScale = -1f;
        //    nextState = State.Flight;
        //    pivotXStep = -pivotXSpeed * Time.deltaTime;
        //    pivotYStep = -pivotYSpeed * Time.deltaTime * .25f;
        //    m_BackgroundScroller.SetXMoveStep(-pivotXStep * .8f);
        //}
        //if (Input.GetKey(KeyCode.RightArrow))
        //{
        //    m_DisplayLookXScale = 1f;
        //    nextState = State.Flight;
        //    pivotXStep = pivotXSpeed * Time.deltaTime;
        //    pivotYStep = -pivotYSpeed * Time.deltaTime * .25f;
        //    //m_BackgroundScroller.SetXMoveStep(-pivotXStep * .8f);
        //}
        //if(Input.GetKeyDown(KeyCode.LeftShift))
        //{
        //    m_InFlightBurst = !m_InFlightBurst;
        //}
        //if(m_InFlightBurst)
        //{
        //    nextState = State.Flight;
        //    pivotYStep = pivotYSpeed * Time.deltaTime;
        //}
        //if(nextState == State.None)
        //{
        //    nextState = State.Idle;
        //    pivotYStep = -pivotYSpeed * Time.deltaTime * .25f;
        //}

        if(m_GridMoveSequence == null)
        {
            int gridCol = m_CurrentGridCol;
            int gridRow = m_CurrentGridRow;
            if (Input.GetKeyDown(KeyCode.LeftArrow))
            {
                gridCol = Mathf.Max(gridCol - 1, 0);
            }
            else if (Input.GetKeyDown(KeyCode.RightArrow))
            {
                gridCol = Mathf.Min(gridCol + 1, m_WorldGrid.GridColCount - 1);
            }
            if (Input.GetKeyDown(KeyCode.UpArrow))
            {
                gridRow = Mathf.Min(gridRow + 1, m_WorldGrid.GridRowCount - 1);
            }
            else if (Input.GetKeyDown(KeyCode.DownArrow))
            {
                gridRow = Mathf.Max(gridRow - 1, 0);
            }

            if (gridCol != m_CurrentGridCol || gridRow != m_CurrentGridRow)
            {
                m_CurrentGridRow = gridRow;
                m_CurrentGridCol = gridCol;
                HandleGridUpdate(m_CurrentGridRow, m_CurrentGridCol);

                float lookAngle = 0f;
                if ((m_CurrentGridRow == 0 && m_CurrentGridCol == 0) ||
                    m_CurrentGridRow == 1 && m_CurrentGridCol == m_WorldGrid.GridColCount - 1)
                    lookAngle = -45f;
                if ((m_CurrentGridRow == 0 && m_CurrentGridCol == m_WorldGrid.GridColCount - 1) ||
                    (m_CurrentGridRow == 1 && m_CurrentGridCol == 0))
                    lookAngle = 45f;

                transform.DORotate(new Vector3(0f, 0f, lookAngle), .5f);
            }
        }

        m_DisplayLookYScale = m_CurrentGridRow == 0 ? 1 : -1;

        //m_BackgroundScroller.SetScrollSpeed(m_IdleScrollSpeed + GetPivotYPercentFromIdle() * m_FlyForwardScrollSpeed);
        m_BackgroundScroller.SetScrollSpeed(m_IdleScrollSpeed);

        //HandlePivotXStep(pivotXStep);
        //HandlePivotYStep(pivotYStep);

        m_DisplayRoot.localScale = new Vector3(m_DisplayLookXScale, m_DisplayLookYScale, 1f);

        if (nextState == m_PlayerState)
            return;

        //An actual state change is needed
        m_PlayerState = nextState;

        //After state switching, check to see what kind of animation state change is needed
        switch(m_PlayerState)
        {
            case State.Idle:
                m_Animations.Play("Idle");
                break;
            case State.Flight:
                m_Animations.Play("Flight");
                break;
        }
    }

    private Sequence m_GridMoveSequence = null;
    private void HandleGridUpdate(int row, int col)
    {
        Vector3 gridCenter = m_WorldGrid.GetCenterForGrid(row, col);
        float moveDistance = new Vector2(gridCenter.x - transform.position.x, gridCenter.y - transform.position.y).magnitude;
        float moveUnitsPerSecond = 12f;
        float moveTime = moveDistance / moveUnitsPerSecond;

        transform.DOMove(new Vector3(gridCenter.x, gridCenter.y, transform.position.z), moveTime);

        m_GridMoveSequence = DOTween.Sequence();
        //Let player do another move roughly 70% into the current grid move's update
        m_GridMoveSequence.AppendInterval(moveTime * .3f);
        m_GridMoveSequence.AppendCallback(() => m_GridMoveSequence = null);

        m_PlayerSelectGridOutline.transform.position = new Vector3(gridCenter.x, gridCenter.y, m_PlayerSelectGridOutline.transform.position.z);
    }

    private void HandlePivotXStep(float pivotXStep)
    {
        Vector3 localPos = transform.localPosition;
        float xStep = localPos.x + pivotXStep;
        float xStepSign = Mathf.Sign(xStep);
        xStep = Mathf.Min(Mathf.Abs(xStep), m_IdlePosition.x + m_PivotRange.x) * xStepSign;

        transform.localPosition = new Vector3(xStep, localPos.y, localPos.z);

        m_BackgroundScroller.SetXMoveStep(-xStep * .8f);
    }

    private float GetPivotYPercentFromIdle()
    {
        float currYRange = transform.localPosition.y - m_IdlePosition.y;
        return currYRange / m_PivotRange.y;
    }

    private void HandlePivotYStep(float pivotYStep)
    {
        Vector3 localPos = transform.localPosition;
        float yStep = localPos.y + pivotYStep;
        float yStepSign = Mathf.Sign(yStep);
        yStep = Mathf.Min(yStep, m_IdlePosition.y + m_PivotRange.y);
        yStep = Mathf.Max(yStep, m_IdlePosition.y);

        transform.localPosition = new Vector3(localPos.x, yStep, localPos.z);
    }

    public void OnIntroDone()
    {
        m_PlayerState = State.Idle;
        m_BackgroundScroller.SetScrollSpeed(m_IdleScrollSpeed);
    }
}
