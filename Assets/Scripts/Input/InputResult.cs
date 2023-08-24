//移动端滑动输入
public class InputResult
{
    public int XInput = 0;
    public int YInput = 0;

    //验证是否有实际输入
    public bool HasValue => XInput != 0 || YInput != 0;
}