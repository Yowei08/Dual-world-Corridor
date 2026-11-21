
public enum E_Type
{
    Move,
    Jump
}

/// <summary>
/// 所有状态类父类接口
/// </summary>
public interface I_State
{
    // 进入状态执行方法
    public void OnEnter();
    // 状态持续执行方法
    public void OnUpdate();
    // 离开状态执行方法
    public void OnExit();
}

/// <summary>
/// 状态机
/// </summary>
public class FSM
{

}
