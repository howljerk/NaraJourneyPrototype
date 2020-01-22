using UnityEngine.UI;
using UnityEngine.EventSystems;

public class HijackButton : Button
{
    public bool IsHeldDown { get; private set; }

    protected override void OnEnable()
    {
        base.OnEnable();
        IsHeldDown = false;
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        IsHeldDown = false;
    }

    public override void OnPointerDown(PointerEventData eventData)
    {
        base.OnPointerDown(eventData);
        IsHeldDown = true;
    }

    public override void OnPointerUp(PointerEventData eventData)
    {
        base.OnPointerUp(eventData);
        IsHeldDown = false;
    }

    public override void OnPointerExit(PointerEventData eventData)
    {
        base.OnPointerExit(eventData);
        IsHeldDown = false;
    }
}
