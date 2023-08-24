using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ScoreDisplay : MonoBehaviour
{
    //添加对TextMessage(pro)引用
    private TMP_Text _text;
    private Animator _animator;

    void Awake()
    {
        //获取该文本对象引用
        _text = GetComponent<TMP_Text>();
        _animator = GetComponent<Animator>();
    }

    //更新：设置文本
    public void UpdateScore(int score)
    {
        //本质上文本对象null无论何时调用。点击新游戏按钮时，实际是在卸载和重新加载场景
        //重新加载场景中所有游戏对象都会触发其不同的生命周期事件
        //第一个运行的是Start方法：解决方案如下
        //1、重构脚本执行顺序Unity-Edit-ProjectSettings-ScriptExecutionOrder
        //2、唤醒生命周期事件（将在启动前触发并应用于初始化）：获取对对象的引用，以便类的其余部分可以正常工作
        _text.text = score.ToString();
        //解决报错“MissingComponentException”
        //添加动画检查
        if (_animator != null)
            _animator.SetTrigger("ScoreUpdated");//设置触发器
        //需要调用方法：Unity事件进行更新
        //设置动画触发器
        //_animator.SetTrigger("ScoreUpdated");
    }
}
