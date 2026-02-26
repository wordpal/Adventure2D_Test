using UnityEngine.UIElements.Experimental;

//×´Ì¬»ú³éÏóÀà
public abstract class BaseState
{
    protected Enemy currentEnemy;
    public abstract void OnEnter(Enemy emeny);
    public abstract void LogicUpdate();
    public abstract void PhysicsUpdate();
    public abstract void OnExit();
}
