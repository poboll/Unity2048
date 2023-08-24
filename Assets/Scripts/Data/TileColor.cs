using System;//允许在编译器中对其进行编辑，无论何时将它附加到游戏状态
using UnityEngine;

[Serializable]
public class TileColor
{
    public int value;
    public Color fgColor = Color.black;
    public Color bgColor = Color.white;

}
