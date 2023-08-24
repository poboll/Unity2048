using System;
using UnityEngine;

public class SwipeInputManager : IInputManager
{
    private bool _isSwiping = false;//滑动状态
    private Vector3 _startPos;//开始滑动
    private const int MinSwipeDist = 100;//最小移动距离

    public InputResult GetInput()
    {
        InputResult result = new InputResult();
        if (!_isSwiping)//判断是否已经开始滑动
        {
            if (Input.GetMouseButton(0))//Unity自动模拟滑动：鼠标&触摸=>不用实现两次
            {
                _isSwiping = true;//从关键帧开始滑动
                _startPos = Input.mousePosition;
            }
        }
        else//已经开始滑动
        {
            //检测松开鼠标/离开屏幕
            if (!Input.GetMouseButton(0))
            {
                _isSwiping = false;
                //计算滑动距离
                Vector3 delta = Input.mousePosition - _startPos;
                if(delta.magnitude >= MinSwipeDist)//有效滑动
                {
                    //判断方向
                    if(Mathf.Abs(delta.x) > Mathf.Abs(delta.y))//水平输入
                    {
                        result.XInput = Math.Sign(delta.x);//x输入
                    }
                    else
                    {
                        result.YInput = Math.Sign(delta.y);//y输入
                    }
                }
            }
        }
        return result;
    }
}
