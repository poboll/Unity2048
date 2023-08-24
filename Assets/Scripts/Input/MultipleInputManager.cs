using System;
using System.Linq;

public class MultipleInputManager : IInputManager//多端输入管理：同样扩展接口
{
    private IInputManager[] _managers;//获取键盘输入
    public MultipleInputManager(params IInputManager[] managers)//使用params可以逐个传入输入
    {
        //输入管理器
        _managers = managers;//分配给字段
    }
    public InputResult GetInput()
    {
        //获取实际输入
        var inputResults = _managers.Select(manager => manager.GetInput());//获取输入管理器、调用getinput并作为课枚举项返回
        //只需要第一个输入
        InputResult result = inputResults.FirstOrDefault(input => input.HasValue);
        return result ?? new InputResult();//正则表达式进行null检查
    }
}
