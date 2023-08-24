//移动端滑动输入
using UnityEngine;

public class KeyboardInputManager : IInputManager
{
    //读入用户输入
    private int _lastXInput;
    private int _lastYInput;

    //使用函数获取此帧的输入
    public InputResult GetInput()
    {
        InputResult result = new InputResult();

        var xInput = Mathf.RoundToInt(Input.GetAxisRaw("Horizontal"));//获取原始输入值Unity方法：输入平滑
        var yInput = Mathf.RoundToInt(Input.GetAxisRaw("Vertical"));


        //添加检查：仅当最后的输入为零时才允许移动
        if (_lastXInput == 0 && _lastYInput == 0)
        {
            //Debug.Log($"Input {xInput}, {yInput}");调试输出
            result.XInput = xInput;
            result.YInput = yInput;

        }
        //最后的输入设置为当前值
        _lastYInput = xInput;
        _lastYInput = yInput;

        return result;
    }
}