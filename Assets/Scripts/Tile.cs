using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Tile : MonoBehaviour
{
    private int _value = 2;//私有变量，数字
    [SerializeField] private TMP_Text text;//添加序列化字段属性，确保显示正确性

    private Vector3 _startPos;//跟踪动画的开始位置
    private Vector3 _endPos;//跟踪动画的结束位置
    private bool _isAnimating;//跟踪动画是否在播放
    private float _count;//跟踪动画的播放时间
    private Tile _mergeTile;//所有合并样式
    private Animator _animator;//合并时播放动画
    private TileManager _tileManager;//静态访问每当创建
    private Image _tileImage;//图像脚本引用

    //仅在Update调用之前检查Start
    private void Awake()
    {
        //获取游戏对象上存在的动画
        _animator = GetComponent<Animator>();
        _tileManager = FindObjectOfType<TileManager>();//函数类型查找对象：搜索当前场景并返回第一个包含脚本的对象
        _tileImage = GetComponent<Image>();//开始时获取对实际图像引用
    }

    /*
    [SerializeField] private float _animationTime = .3f;//确定动画需要多长时间
    [SerializeField] private AnimationCurve animationCure;//控制线性曲线
    */
    [SerializeField] private TileSettings tileSettings;

    public void SetValue(int value)
    {
        _value = value;
        text.text = value.ToString();
        TileColor newColor = tileSettings.TileColors.FirstOrDefault(color => color.value == _value) ?? new TileColor();//为新值查找适当的颜色：使用系统库使查找对象更容易（第一个默认函数）
        //将会遍历一个集合并基于给出的条件返回通过该条件的第一个绑定项。否则null（复杂类型）
        text.color = newColor.fgColor;//将文本颜色设置为前景色
        //期望磁贴颜色与背景颜色一致但目前未对图像脚本引用
        _tileImage.color = newColor.bgColor;
    }

    //更新
    private void Update()
    {
        if (!_isAnimating)
            return;
        //更新已经设置过的秒数：data time获取上次更新以来经过的秒数
        _count += Time.deltaTime;
        //计算并设定位置:0动画，1结束
        float t = _count / tileSettings.AnimationTime;//按动画因设置的秒数
        t = tileSettings.AnimationCurve.Evaluate(t);//运行动画曲线评估功能0-1
        //线性插值：计算当前位置（介于开始位置和结束位置之间）。
        Vector3 newPos = Vector3.Lerp(_startPos, _endPos, t);
        //更新对象位置：
        transform.position = newPos;
        //更新最后一个检查：设置布尔值的动画(大于等于动画所需秒数则完成）同t值
        if (_count >= tileSettings.AnimationTime)
        {
            _isAnimating = false;
            //动画完成后检查是否已经形成形式：移动中有一个要合并的磁贴
            if(_mergeTile != null)
            {
                int newValue = _value + _mergeTile._value;
                _tileManager.AddScore(newValue);
                SetValue(newValue);//值相加//_value + _mergeTile._value重构转化为自己的变量
                Destroy(_mergeTile.gameObject);//销毁另一块磁贴
                _animator.SetTrigger("Merge");//设置触发器函数，只用传递动画名称
                _mergeTile = null;//清除场景中引用
            }
        }
            
    }

    //集合位置函数控制动画逻辑
    public void SetPosition(Vector3 newPos,bool instant)
    {
        //即时设置位置（初始化、新建格子直接设置位置）
        if(instant)
        {
            transform.position = newPos;
            return;
        }
        _startPos = transform.position;//起始位置：当前位置
        _endPos = newPos;//新建结束位置
        _count = 0;//重置帐户
        _isAnimating = true;//动画=真
        //合并磁贴
        if(_mergeTile != null)
        {
            _mergeTile.SetPosition(newPos, false);//设置即时位置和更新动画->改进：向磁贴添加合并动画，在合并时触发
        }
    }

    //合并
    public bool Merge(Tile otherTile)//接收另一个格子来合并
    {
        if (!CanMerge(otherTile))
            return false;

        _mergeTile = otherTile;
        return true;
        
    }

    //检查
    public bool CanMerge(Tile otherTile)//接收另一个格子来合并
    {
        //添加检查确保值相等
        if (this._value != otherTile._value)
            return false;//无法与样式合并
        if (_mergeTile != null || otherTile._mergeTile != null)//检查是否已经合并
            return false;

        return true;

    }

    public int GetValue()
    {
        return _value;//返回值构建，并将其保存回TilManager
    }
}