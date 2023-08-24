using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
//使用完整名称空间或别名：using Debug = UnityEngine.Debug;

public class TileManager : MonoBehaviour
{
    public static int GridSize = 4;//4*4
    private readonly Transform[,] _tilePositions = new Transform[GridSize, GridSize];//二维数组存位置
    private readonly Tile[,] _tiles = new Tile[GridSize, GridSize];//游戏开始时新建格子
    [SerializeField] private Tile tilePrefab;
    [SerializeField] private TileSettings tileSettings;
    [SerializeField] private UnityEvent<int> scoreUpdated;//传递分数的事件（整数）强转,每当分数更新则调用事件
    [SerializeField] private UnityEvent<int> bestScoreUpdated;//Unity事件告诉UI最佳分数已改变
    [SerializeField] private UnityEvent<int> moveCountUpdated;//记录移动次数的事件
    [SerializeField] private UnityEvent<System.TimeSpan> gameTimeUpdated;//游戏中计时器事件。时间跨度：当前比赛时间内（使用类解决）
    [SerializeField] private GameOverScreen gameOverScreen;
    private Stack<GameState> _gameStates = new Stack<GameState>();//使用堆记录每次移动后的状态
    private System.Diagnostics.Stopwatch _gameStopwatch = new System.Diagnostics.Stopwatch();//记录从启动游戏开始的游戏时间（导入诊断库出现问题：类与类共享相同的名称->使用完整名称或别名解决）
    private IInputManager _inputManager = new MultipleInputManager(new KeyboardInputManager(), new SwipeInputManager());//接口的好处

    private bool _isAnimating;//bug修补：进行动画禁用移动。跟踪动画->设置在没有进行动画的情况下才尝试移动。带来新bug：无法移动时还需等待。添加一个字段跟踪格子是否真实移动并更新。
    private int _score;
    private int _bestScore;//最佳分数
    private int _moveCount;//移动次数

    // Start is called before the first frame update
    void Start()
    {
        GetTilePositions();
        TrySpawnTile();
        TrySpawnTile();
        UpdateTilePositions(true);//初始化预制格子不想出现动画

        _gameStopwatch.Start();//启动秒表
        _bestScore = PlayerPrefs.GetInt("BestScore", 0);//初始化；将在第一次玩游戏时返回
        bestScoreUpdated.Invoke(_bestScore);//更新最佳分数
    }

    //避免一直按键：确保用户在最后一帧没有按任何方向
    private int _lastXInput;
    private int _lastYInput;
        
    // Update is called once per frame
    void Update()
    {
        gameTimeUpdated.Invoke(_gameStopwatch.Elapsed);
        //立即对输入进行取整避免错误读入

        InputResult input = _inputManager.GetInput();
        //完成x和y的输入调用trymove函数实现移动
        if (!_isAnimating)//更新的动画是添加此内容的最佳（简）位置
            TryMove(input.XInput, input.YInput);//浮点值并不想要：四舍五入判断是否输入 问题：抓取并尝试移动无论当前是否正在设置动画->添加一个字段更新这个点
    }

    //分数
    public void AddScore(int value)
    {
        //获取值加入字段—>Tile.cs合并平铺立即更新
        _score += value;
        scoreUpdated.Invoke(_score);
        //检查现有分数是否优于最佳分数（只需覆盖最高分）
        if(_score > _bestScore)
        {
            _bestScore = _score;
            bestScoreUpdated.Invoke(_bestScore);//调用UI事件更新
            PlayerPrefs.SetInt("BestScore", _bestScore);//首选项
        }
    }

    //重启游戏
    public void RestartGame()
    {
        //卸载和重新加载场景
        var activeScene = SceneManager.GetActiveScene();//场景管理类
        SceneManager.LoadScene(activeScene.name);//传递交互式场景名称
    }

    //初始化全局位置
    private void GetTilePositions()
    {
        LayoutRebuilder.ForceRebuildLayoutImmediate(GetComponent<RectTransform>());//迫使网络布局立即计算位置和大小在初始化前进行更新
        int x = 0;
        int y = 0;
        foreach( Transform transform in this.transform)
        {
            _tilePositions[x,y] = transform;
            x++;
            if(x >= GridSize)
            {
                x = 0;
                y++;
            }
        }
    }
    private bool TrySpawnTile()
    {
        List<Vector2Int> availableSpots = new List<Vector2Int>();//指向int向量的空列表储存可用点
        //列出可用点
        for(int x = 0; x < GridSize; x++)
        for(int y = 0; y < GridSize; y++)
        {
                if (_tiles[x, y] == null)
                    availableSpots.Add(new Vector2Int(x, y));
        }
        //检查
        if (!availableSpots.Any())
            return false;
        //获得一个随机索引
        int randomIndex = Random.Range(0, availableSpots.Count);
        Vector2Int spot = availableSpots[randomIndex];
        //manager类拓展了模型行为，可用访问实例化函数预制件做平铺
        var tile = Instantiate(tilePrefab, transform.parent);
        //集值函数
        tile.SetValue(GetRandomValue());
        _tiles[spot.x, spot.y] = tile;//初始化
        return true;
    }
    //随机数
    private int GetRandomValue()
    {
        var rand = Random.Range(0f, 1f);//0-1
        if (rand < .8f)//80%为2
            return 2;
        else
            return 4;
    }
    //摆放
    private void UpdateTilePositions(bool instant)//告诉平铺设置动画到新位置的函数
    {
        //不是一个即时的位置更新：判断
        if(!instant)
        {
            _isAnimating = true;
            //需要一定时间完成动画 解决：不使用太多关于细节处理->例程功能
            StartCoroutine(WaitForTileAnimation());//重载接受一个返回枚举数的函数：等待平铺动画
             
        }
        for (int x = 0; x < GridSize; x++)
            for (int y = 0; y < GridSize; y++)
                //阵列中循环是否有格子
                if (_tiles[x, y] != null)
                    //生成平铺后调用
                    _tiles[x, y].SetPosition(_tilePositions[x, y].position, instant);
    }

    private IEnumerator WaitForTileAnimation()//返回一个等待数秒的函数
    {
        yield return new WaitForSeconds(tileSettings.AnimationTime);//获取动画时间
        //等待结束后添加一些生成逻辑
        if(!TrySpawnTile())//代表是否成功
        {
            //理论上永远不会发生，只有在成功移动后才能生成一个互动程序：没有剩余空格
            Debug.LogError("移动失败"); 
        }
        //新格子没有初始化位置：更新平铺位置
        UpdateTilePositions(true);
        //检查是否还有路->结束页面
        if (!AnyMovesLeft())
        {
            gameOverScreen.SetGameOver(true);
        }
        _isAnimating = false;//正在设置动画 = false
    }

    private bool AnyMovesLeft()//任何剩余移动
    {
        return CanMoveLeft() || CanMoveUp() || CanMoveRight() || CanMoveDown();
    }

    private bool _tilesUpdated;//若没有在任何函数中运行则意味着没有可用的移动

    //移动
    private void TryMove(int x, int y)
    {
        if (x == 0 && y == 0)
            return;

        //通过获取输入的绝对值确保输入一个方向
        if(Mathf.Abs(x) == 1 && Mathf.Abs(y) == 1)
        {
            return;
        }

        _tilesUpdated = false;//初始化
        int[,] preMoveTileValues = GetCurrentTileValues();//预移动的平铺值，若移动则将一个新的游戏状态推到堆栈

        //正确输入
        if(x==0)//垂直移动
        {
            if (y > 0)
                TryMoveUp();
            else
                TryMoveDown();
        }
        else//水平移动
        {
            if (x < 0)
                TryMoveLeft();
            else
                TryMoveRight();
        }

        //只有实际进行平铺更新时才会调用更新平铺位置函数。若不调用则没有动画可以等待
        if(_tilesUpdated)//成功移动后调用位置更新函数
        {
            _gameStates.Push(new GameState() { tailValues = preMoveTileValues, score = _score ,moveCount = _moveCount });//更新将游戏状态推送到堆栈：创建游戏设置分数字段在当前储存,指定移动增加计数
            //使用用户首选类储存游戏最佳分数->简单的事务：存储角色串和数字并检索
            _moveCount++;//增加移动技记数
            moveCountUpdated.Invoke(_moveCount);//调用移动计数更新事件添加到游戏状态中，以便撤销功能正常工作
            UpdateTilePositions(false);//平衡处理
        }

    }

    private int[,] GetCurrentTileValues()
    {
        //数组记录操作记录
        int[,] result = new int[GridSize, GridSize];
        for (int x = 0; x < GridSize; x++)
            for (int y = 0; y < GridSize; y++)
                if (_tiles[x, y] != null)
                    result[x, y] = _tiles[x, y].GetValue();//必须公开来获取值->Tile.cs
        return result;//现已将游戏状态推到堆栈上->恢复函数
    }

    //加载最后游戏状态：将游戏状态推到堆栈上,将状态从堆栈中移除，恢复游戏
    public void LoadLastStates()
    {
        //检查措施-用户可以随时按下撤销按钮
        //确保未在移动中
        if (_isAnimating)
            return;
        //堆栈无任何内容
        if (!_gameStates.Any())
            return;
        //获取上一个游戏状态
        GameState previousGameState = _gameStates.Pop();
        //撤销回溯
        gameOverScreen.SetGameOver(false);
        //恢复比分
        _score = previousGameState.score;
        //确保调用或记录更新的事件
        scoreUpdated.Invoke(_score);
        //加载前一个游戏状态时更新逻辑
        _moveCount = previousGameState.moveCount;
        moveCountUpdated.Invoke(_moveCount);
        //清除当前场景在所有磁贴中的循环
        foreach (Tile t in _tiles)
            if (t != null)
                Destroy(t.gameObject);//摧毁游戏对象->下一帧时完全删除
        //销毁后则循环
        for(int x = 0; x < GridSize; x ++)
            for(int y = 0; y < GridSize; y++)
            {
                _tiles[x, y] = null;//先放置磁贴观察情况
                //检查前一个状态是否有值
                if(previousGameState.tailValues[x, y] == 0)
                    continue;//略过

                //无值则创建
                Tile tile = Instantiate(tilePrefab, transform.parent);
                tile.SetValue(previousGameState.tailValues[x, y]);//设置为当前状态
                _tiles[x, y] = tile;//确保放在正确位置
            }
        //调用更新执行一系列操作
        UpdateTilePositions(true);
    }

    private bool TileExistsBetween(int x, int y, int x2, int y2)
    {
        //水平｜垂直
        if (x == x2)
            return TileExistsBetweenVertical(x, y, y2);//纵向检查
        else if (y == y2)
            //横向检查
            return TileExistsBetweenHorizontal(x, x2, y);
        //错误-备用措施
        Debug.LogError($"BETWEEN CHECK - INVALID PARAMETERS ({x},{y}) ({x2},{y2})");
        return true;
    }

    private bool TileExistsBetweenHorizontal(int x, int x2, int y)//水平
    {
        int minX = Mathf.Min(x, x2);
        int maxX = Mathf.Max(x, x2);
        //从最小值右侧的一个平铺开始
        for (int xIndex = minX + 1; xIndex < maxX; xIndex++)
            if (_tiles[xIndex, y] != null)//一块磁贴挡住去路，原路返回
                return true;
        return false;
    }

    private bool TileExistsBetweenVertical(int x, int y, int y2)//垂直
    {
        int minY = Mathf.Min(y, y2);
        int maxY = Mathf.Max(y, y2);
        //从最小值右侧的一个平铺开始
        for (int yIndex = minY + 1; yIndex < maxY; yIndex++)
            if (_tiles[x, yIndex] != null)//一块磁贴挡住去路，原路返回
                return true;
        return false;
    }

    private void TryMoveRight()
    {
        for (int y = 0; y < GridSize; y++)//从顶层开始
            for (int x = GridSize - 1; x >= 0; x--)//每一排从右向左
            {
                if (_tiles[x, y] == null) continue;//检查：空则继续

                for (int x2 = GridSize - 1; x2 > x; x2--)//有：从这一行最右侧寻找一个空白点继续
                {
                    //修复bug：检查确保两者之间没有瓷砖

                    if (_tiles[x2, y] != null)
                    {
                        if (TileExistsBetween(x, y, x2, y))//如果有任何磁贴
                            continue;//防止合并
                        //将磁贴作为进一步的右侧：将x添加到y，并告诉它尝试与尾部合并->可能遇到不同的值无法合并
                        if(_tiles[x2, y].Merge(_tiles[x, y]))
                        {
                            _tiles[x, y] = null;//将它从平铺管理器中删除但仍保留在场景中
                            _tilesUpdated = true;//有效的移动
                            //打破x2循环，由于已经移动了x-y
                            break;
                        }
                        continue;//未能合并则继续：磁贴将尝试在左侧找到一个空白点
                    }

                    _tilesUpdated = true;
                    _tiles[x2, y] = _tiles[x, y];
                    _tiles[x, y] = null;
                    break;//成功移动，推出函数
                }
            }
    }

    private void TryMoveLeft()
    {
        for (int y = 0; y < GridSize; y++)//从顶层开始
            for (int x = 0; x < GridSize; x++)//每一排从左向右
            {
                if (_tiles[x, y] == null) continue;//检查：空则继续
                for (int x2 = 0; x2 < x; x2++)//有：从这一行最左侧寻找一个空白点继续
                {
                    if (_tiles[x2, y] != null)
                    {
                        if (TileExistsBetween(x, y, x2, y))//如果有任何磁贴
                            continue;//防止合并
                        //将磁贴作为进一步的右侧：将x添加到y，并告诉它尝试与尾部合并->可能遇到不同的值无法合并
                        if (_tiles[x2, y].Merge(_tiles[x, y]))
                        {
                            _tiles[x, y] = null;//将它从平铺管理器中删除但仍保留在场景中
                            _tilesUpdated = true;//有效的移动
                            //打破x2循环，由于已经移动了x-y
                            break;
                        }
                        continue;//未能合并则继续：磁贴将尝试在左侧找到一个空白点
                    }

                    _tilesUpdated = true;
                    _tiles[x2, y] = _tiles[x, y];
                    _tiles[x, y] = null;
                    break;//成功移动，推出函数
                }
            }
    }

    private void TryMoveDown()
    {
        for (int x = 0; x < GridSize; x++)//从第一列开始
            for (int y = GridSize - 1; y >= 0; y--)//每一层从下向上
            {
                if (_tiles[x, y] == null) continue;//检查：空则继续
                for (int y2 = GridSize - 1; y2 > y; y2--)//有：从这一列最下方寻找一个空白点继续
                {
                    if (_tiles[x, y2] != null)
                    {
                        if (TileExistsBetween(x, y, x, y2))//如果有任何磁贴
                            continue;//防止合并
                        //将磁贴作为进一步的右侧：将x添加到y，并告诉它尝试与尾部合并->可能遇到不同的值无法合并
                        if (_tiles[x, y2].Merge(_tiles[x, y]))
                        {
                            _tiles[x, y] = null;//将它从平铺管理器中删除但仍保留在场景中
                            _tilesUpdated = true;//有效的移动
                            //打破x2循环，由于已经移动了x-y
                            break;
                        }
                        continue;//未能合并则继续：磁贴将尝试在左侧找到一个空白点
                    }

                    _tilesUpdated = true;
                    _tiles[x, y2] = _tiles[x, y];
                    _tiles[x, y] = null;
                    break;//成功移动，推出函数
                }
            }
    }

    private void TryMoveUp()
    {
        for (int x = 0; x < GridSize; x++)//从第一列开始
            for (int y = 0; y < GridSize; y++)//每一层从上向下
            {
                if (_tiles[x, y] == null) continue;//检查：空则继续
                for (int y2 = 0; y2 < y; y2++)//有：从这一列最上方寻找一个空白点继续
                {
                    if (_tiles[x, y2] != null)
                    {
                        if (TileExistsBetween(x, y, x, y2))//如果有任何磁贴
                            continue;//防止合并
                        //将磁贴作为进一步的右侧：将x添加到y，并告诉它尝试与尾部合并->可能遇到不同的值无法合并
                        if (_tiles[x, y2].Merge(_tiles[x, y]))
                        {
                            _tiles[x, y] = null;//将它从平铺管理器中删除但仍保留在场景中
                            _tilesUpdated = true;//有效的移动
                            //打破x2循环，由于已经移动了x-y
                            break;
                        }
                        continue;//未能合并则继续：磁贴将尝试在左侧找到一个空白点
                    }

                    _tilesUpdated = true;
                    _tiles[x, y2] = _tiles[x, y];
                    _tiles[x, y] = null;
                    break;//成功移动，推出函数
                }
            }
    }

    //--
    private bool CanMoveRight()
    {
        for (int y = 0; y < GridSize; y++)//从顶层开始
            for (int x = GridSize - 1; x >= 0; x--)//每一排从右向左
            {
                if (_tiles[x, y] == null) continue;//检查：空则继续

                for (int x2 = GridSize - 1; x2 > x; x2--)//有：从这一行最右侧寻找一个空白点继续
                {
                    //修复bug：检查确保两者之间没有瓷砖

                    if (_tiles[x2, y] != null)
                    {
                        if (TileExistsBetween(x, y, x2, y))//如果有任何磁贴
                            continue;//防止合并
                        //将磁贴作为进一步的右侧：将x添加到y，并告诉它尝试与尾部合并->可能遇到不同的值无法合并
                        if (_tiles[x2, y].CanMerge(_tiles[x, y]))
                        {
                            return true;//至少有一个移动
                        }
                        continue;//未能合并则继续：磁贴将尝试在左侧找到一个空白点
                    }

                    return true;
                }
            }
        return false;//无法移动
    }

    private bool CanMoveLeft()
    {
        for (int y = 0; y < GridSize; y++)//从顶层开始
            for (int x = 0; x < GridSize; x++)//每一排从左向右
            {
                if (_tiles[x, y] == null) continue;//检查：空则继续
                for (int x2 = 0; x2 < x; x2++)//有：从这一行最左侧寻找一个空白点继续
                {
                    if (_tiles[x2, y] != null)
                    {
                        if (TileExistsBetween(x, y, x2, y))//如果有任何磁贴
                            continue;//防止合并
                        //将磁贴作为进一步的右侧：将x添加到y，并告诉它尝试与尾部合并->可能遇到不同的值无法合并
                        if (_tiles[x2, y].CanMerge(_tiles[x, y]))
                        {
                            return true;
                        }
                        continue;//未能合并则继续：磁贴将尝试在左侧找到一个空白点
                    }

                    return true;
                }
            }
        return false;
    }

    private bool CanMoveDown()
    {
        for (int x = 0; x < GridSize; x++)//从第一列开始
            for (int y = GridSize - 1; y >= 0; y--)//每一层从下向上
            {
                if (_tiles[x, y] == null) continue;//检查：空则继续
                for (int y2 = GridSize - 1; y2 > y; y2--)//有：从这一列最下方寻找一个空白点继续
                {
                    if (_tiles[x, y2] != null)
                    {
                        if (TileExistsBetween(x, y, x, y2))//如果有任何磁贴
                            continue;//防止合并
                        //将磁贴作为进一步的右侧：将x添加到y，并告诉它尝试与尾部合并->可能遇到不同的值无法合并
                        if (_tiles[x, y2].CanMerge(_tiles[x, y]))
                        {
                            return true;
                        }
                        continue;//未能合并则继续：磁贴将尝试在左侧找到一个空白点
                    }

                    return true;
                }
            }
        return false;
    }

    private bool CanMoveUp()
    {
        for (int x = 0; x < GridSize; x++)//从第一列开始
            for (int y = 0; y < GridSize; y++)//每一层从上向下
            {
                if (_tiles[x, y] == null) continue;//检查：空则继续
                for (int y2 = 0; y2 < y; y2++)//有：从这一列最上方寻找一个空白点继续
                {
                    if (_tiles[x, y2] != null)
                    {
                        if (TileExistsBetween(x, y, x, y2))//如果有任何磁贴
                            continue;//防止合并
                        //将磁贴作为进一步的右侧：将x添加到y，并告诉它尝试与尾部合并->可能遇到不同的值无法合并
                        if (_tiles[x, y2].CanMerge(_tiles[x, y]))
                        {
                            return true;
                        }
                        continue;//未能合并则继续：磁贴将尝试在左侧找到一个空白点
                    }

                    return true;
                }
            }
        return false;
    }

}