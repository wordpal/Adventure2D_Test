using UnityEngine.Events;
using UnityEngine;

[CreateAssetMenu(menuName = "Event/VoidEventSO")]
public class VoidEventSO : ScriptableObject
{
    public UnityAction OnEventEaised;

    public void RaiseEvent()
    {
        OnEventEaised?.Invoke();
    }
}
