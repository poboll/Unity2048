using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class GameTimer : MonoBehaviour
{
    private TMP_Text _text;//添加对文本对象的引用

    void Awake()
    {
        _text = GetComponent<TMP_Text>();
    }

    //更新计时器：任何时间跨度
    public void UpdateTimer(TimeSpan gameTime)
    {
        //测试极端情况
        //gameTime = gameTime.Add(TimeSpan.FromHours(1));
        //把分钟放在这里，并且始终使用两位数。若小于10则会有一个前导0
        //想要显示":"->\\转译
        string format = "";
        //超过一个小时时间重置->添加检查
        if (gameTime.Hours > 0)
            format = "h\\:";//从小时开始格式化，不规范使用两位数
        format = "mm\\:ss";
        _text.text = gameTime.ToString(format);//将文本设置为游戏时间的结果
        //想要显示分和秒，都是前导零：通过向ToString函数传递一个格式解决
    }
}
