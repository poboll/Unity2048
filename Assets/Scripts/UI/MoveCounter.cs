using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class MoveCounter : MonoBehaviour
{
    //添加对文本对象的引用
    private TMP_Text _text;

    private void Awake()
    {
        _text = GetComponent<TMP_Text>();
        
    }

    //加载前一个游戏状态时更新逻辑
    public void UpdateCount(int moveCount)
    {
        //不唯一需要叠加->创建变量存储
        bool shouldDisPlayPlural = moveCount != 1;
        _text.text = $"{moveCount} {(shouldDisPlayPlural ? "moves" : "move")}";//使用一些角色串插值，使其更容易引入（括号内任何内容都是实际代码，括号外任何内容都是角色串
    }
}
