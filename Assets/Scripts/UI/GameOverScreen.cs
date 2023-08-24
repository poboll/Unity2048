using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameOverScreen : MonoBehaviour
{
    private Animator _animator;//获取对Animator组件的引用
    void Awake()
    {
        _animator = GetComponent<Animator>();
    }

    //公共集合->bool
    public void SetGameOver(bool isGameOver)
    {
        _animator.SetBool("IsGameOver", isGameOver);//使用动画设置参数（"名称"，传入）
    }
}