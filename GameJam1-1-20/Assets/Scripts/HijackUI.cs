using UnityEngine;
using UnityEngine.UI;

public class HijackUI : MonoBehaviour
{
    [SerializeField] private Image m_Meter;

    public void SetFillPercent(float perc)
    {
        m_Meter.fillAmount = perc;
    }
}
