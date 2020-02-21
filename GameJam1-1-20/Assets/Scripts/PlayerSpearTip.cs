using DG.Tweening;
using UnityEngine;

public class PlayerSpearTip : MonoBehaviour
{
    public event System.Action<Collider2D> OnCanClamp;
    public event System.Action<Collider2D> OnCantClamp;
    public event System.Action<Collider2D> OnRicochet;

    private int m_ClampCount;
    private Sequence m_RicochetDelaySeq;

    public void Reset()
    {
        if (m_RicochetDelaySeq != null)
            m_RicochetDelaySeq.Kill();
        m_RicochetDelaySeq = null;
    }

    private void OnTriggerEnter2D(Collider2D otherCollider)
    {
        if (otherCollider.tag == "ShipHatch")
        {
            if(++m_ClampCount == 1)
                OnCanClamp?.Invoke(otherCollider);   
        }

        if(otherCollider.tag == "AttackableEnemy")
        {
            if (otherCollider.tag == "AttackableEnemy")
            {
                IAttackableEnemy attackableEnemy = otherCollider.GetComponent<IAttackableEnemy>();
                if (attackableEnemy == null)
                    attackableEnemy = otherCollider.GetComponentInChildren<IAttackableEnemy>();

                attackableEnemy.OnAttacked();
                OnRicochet?.Invoke(otherCollider);
            }
        }

        if (otherCollider.tag == "Enemy")
        {
            if (++m_ClampCount == 1)
                OnCanClamp?.Invoke(otherCollider);

            m_RicochetDelaySeq = DOTween.Sequence();
            m_RicochetDelaySeq.AppendInterval(.1f * DOTween.timeScale);
            m_RicochetDelaySeq.AppendCallback(
            () => 
            {
                OnRicochet?.Invoke(otherCollider);
            });
        }
    }

    private void OnTriggerExit2D(Collider2D otherCollider)
    {
        if (otherCollider.tag == "ShipHatch" || otherCollider.tag == "Enemy")
        {
            if (--m_ClampCount == 0)
                OnCantClamp?.Invoke(otherCollider);
        }
    }
}
