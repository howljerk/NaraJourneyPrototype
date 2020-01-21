using DG.Tweening;
using UnityEngine;

public class PlayerSpearTip : MonoBehaviour
{
    public event System.Action<Collider2D> OnCanClamp;
    public event System.Action<Collider2D> OnCantClamp;

    private int m_ClampCount;

    private void OnTriggerEnter2D(Collider2D otherCollider)
    {
        if (otherCollider.tag == "ShipHatch")
        {
            if(++m_ClampCount == 1)
                OnCanClamp?.Invoke(otherCollider);   
        }
        if(otherCollider.tag == "Enemy")
        {
            //TODO: Damage enemy
        }            
    }

    private void OnTriggerExit2D(Collider2D otherCollider)
    {
        if (otherCollider.tag == "ShipHatch")
        {
            if (--m_ClampCount == 0)
                OnCantClamp?.Invoke(otherCollider);
        }
    }
}
