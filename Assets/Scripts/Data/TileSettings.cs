using UnityEngine;

[CreateAssetMenu(fileName = "TileSettings", menuName = "2048/Assets/TileSettings", order = 0)]

public class TileSettings : ScriptableObject {
    public float AnimationTime = .3f;//平铺设置
    public AnimationCurve AnimationCurve;//动画曲线添加到类的尾部
    public TileColor[] TileColors;//添加瓷砖颜色值数组
}