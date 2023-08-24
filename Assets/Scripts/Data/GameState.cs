public class GameState
{
    //特定位置特定值的磁贴->二维数组
    public int[,] tailValues = new int[TileManager.GridSize, TileManager.GridSize];
    //修正撤销时的分数
    public int score;
    //移动计数字段
    public int moveCount;
}