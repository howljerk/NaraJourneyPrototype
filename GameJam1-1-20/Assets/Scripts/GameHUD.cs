using UnityEngine;
using UnityEngine.UI;

public class GameHUD : MonoBehaviour
{
    public static event System.Action OnLeaveOrEnterShipButtonTapped; 

    private static GameHUD s_Instance = null;
    public static GameHUD Instance { get { return s_Instance; } }

    [SerializeField] private Button m_LeaveOrEnterShipButton;
    [SerializeField] private Text m_LeaveOrEnterShipButtonLabel;

    public Button LeaveOrEnterShipButton { get { return m_LeaveOrEnterShipButton; } }

    private void Awake()
    {
        s_Instance = this;

        m_LeaveOrEnterShipButton.onClick.AddListener(() => 
        {
            string labelStr = m_LeaveOrEnterShipButtonLabel.text;
            m_LeaveOrEnterShipButtonLabel.text = labelStr == "Leave Ship" ? "Enter Ship" : "Leave Ship";
            OnLeaveOrEnterShipButtonTapped?.Invoke();   
        });
        m_LeaveOrEnterShipButton.gameObject.SetActive(false);
    }
}
