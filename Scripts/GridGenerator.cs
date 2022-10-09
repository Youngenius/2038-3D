using DG.Tweening;
using UnityEngine.Events;
using System;
using TMPro;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Collections;

public class GridGenerator : MonoBehaviour
{
    private ScoreManager _score;
    private CubeSpawner _cubeSpawner;
    private SavingSystem _savingSystem;

    [SerializeField] private int _horizontal = 4;
    [SerializeField] private int _vertical = 4;

    [SerializeField] private Grid _gridPref;

    [SerializeField] private Vector3 _camPos;
    [SerializeField] private float _travelTime = 1;
    [SerializeField] private int _winningBlockValue;

    [Header("WinLoseUI")]
    [Range(0.01f, 0.1f)]
    [SerializeField]private float _alfaKoef;

    private CanvasGroup _winLoseUI; //asigned in state machine, used in appear/disappear coroutines
    [SerializeField] private CanvasGroup _winUI;
    [SerializeField] private CanvasGroup _loseUI;

    private List<Grid> _grids = new List<Grid>();
    private List<Block> _spawnedBlocks = new List<Block>();

    public event Action<OnMergeEventArgs> OnMerge;

    [Header("Unity Events")]
    public UnityEvent OnLost;
    public UnityEvent OnWon;
    public UnityEvent OnNewGameStarted;

    public class OnMergeEventArgs : EventArgs
    {
        public int Score;
    }

    private State _currState;
    public enum State
    {
        WaitingForInput,
        Shifting,
        SpawningBlock,
        Merging,
        Win,
        Lose
    }

    private void Awake()
    {
        _cubeSpawner = FindObjectOfType<CubeSpawner>().GetComponent<CubeSpawner>();
        //Changes applied to list in Score change _spawnedBlocks
        _cubeSpawner.SpawnedBlocks.ListChanged += (sender, eventArgs) =>
        {
            if (eventArgs.ListChangedType == ListChangedType.ItemAdded)
            {
                _spawnedBlocks.Add(((BindingList<Block>)sender)[eventArgs.NewIndex]);
            }
        };
        SwipeDetector.OnSwipe += SwipeDetector_OnSwipe;
        GenerateGrid();

        _currState = State.WaitingForInput;
    }

    private void Start()
    {
        _score = FindObjectOfType<ScoreManager>().GetComponent<ScoreManager>();
        _savingSystem = FindObjectOfType<SavingSystem>().GetComponent<SavingSystem>();
        if (_savingSystem.WasSaved())
        {
            _savingSystem.LoadFromFile();
            _score.LoadScore(_score.ScoreLabel, _score.CurrentScore, ScoreManager.CurrentScoreName);
        }
        else
        {
            _cubeSpawner.SpawnBlocks(_grids, 2);
        }
    }

    public void ChangeState(State newState)
    {
        _currState = newState;
        switch (_currState)
        {
            case State.WaitingForInput:
                break;
            case State.SpawningBlock:
                var freeGrids = _cubeSpawner.GetFreeGrids(_grids).Count();

                //if 16 grids are empty spawn 2 cubes
                int blocksNum = freeGrids == 16 ? 2 : 1;
                _cubeSpawner.SpawnBlocks(_grids, blocksNum);
                                
                freeGrids -= blocksNum;
                if (freeGrids > 0 || CanMerge())
                {
                    ChangeState(State.WaitingForInput);
                }
                else if (freeGrids == 0)
                    if (!CanMerge())
                        ChangeState(State.Lose);
                break;
            case State.Shifting:
                break;
            case State.Merging:
                break;
            case State.Win:
                _winLoseUI = _winUI;
                EndGame(OnWon);
                break;
            case State.Lose:
                _winLoseUI = _loseUI;
                Debug.Log("lOSS");
                EndGame(OnLost);
                break;
        }
    }

    private void Update()
    {
        if (_currState != State.WaitingForInput)
            return;

        if (Input.GetKeyDown(KeyCode.LeftArrow)) Shift(Vector3.left);
        if (Input.GetKeyDown(KeyCode.RightArrow)) Shift(Vector3.right);
        if (Input.GetKeyDown(KeyCode.UpArrow)) Shift(Vector3.up);
        if (Input.GetKeyDown(KeyCode.DownArrow)) Shift(Vector3.down);
    }

    #region Methods
    void GenerateGrid()
    {
        int i = 1;
        for (int h = 0; h < _horizontal; h++)
        {
            for (int v = 0; v < _vertical; v++)
            {
                var grid = Instantiate(_gridPref, new Vector3(h, v, 0), Quaternion.identity);
                grid.SetOrder(i);
                _grids.Add(grid);

                i++;
            }
        }

        var center = new Vector3
            (_horizontal / 2 - 0.5f, _vertical / 2 - 0.5f, 0);

        Camera.main.transform.position = _camPos;
    }
    private void SwipeDetector_OnSwipe(SwipeData swipe)
    {
        if (_currState != State.WaitingForInput) return;
        Debug.Log("Detected" + swipe.Direction);

        if (swipe.Direction == SwipeDirection.Left) Shift(Vector3.left);
        if (swipe.Direction == SwipeDirection.Right) Shift(Vector3.right);
        if (swipe.Direction == SwipeDirection.Up) Shift(Vector3.up);
        if (swipe.Direction == SwipeDirection.Down) Shift(Vector3.down);
    }

    public void Replay()
    {
        //Spawning first two blocks
        DestroyAllBlocks(() => ChangeState(State.SpawningBlock));

        //Reseting current score to 0
        _score.ResetScore();
    }

    public void StartNewGame()
    {
        var defaultSize = _gridPref.transform.localScale;
        var setActiveSize = new Vector3(0.3f, 0.3f, 1); //scale just after grid activated

        StartCoroutine(DisappearCoroutine());
        StopCoroutine(DisappearCoroutine());

        SpawnBlocks();

        async Task SpawnBlocks()
        {
            var tasks = new Task[_grids.Count()];
            Debug.Log(_grids.Count());
            foreach (var grid in _grids)
            {
                grid.gameObject.SetActive(true);
                grid.transform.localScale = setActiveSize;
            }
            
            Debug.Log(tasks.Count());
            await ActivateGrids();

            ChangeState(State.SpawningBlock);

            async Task ActivateGrids()
            {
                
                for (int i = 0; i < _grids.Count(); i++)
                {
                    tasks[i] = _grids[i].transform.DOScale(defaultSize, 1).AsyncWaitForCompletion();
                }
                await Task.WhenAll(tasks);
            }
        }
    }

    public async Task EndGame(UnityEvent WinOrLose)
    {
        await DestroyAllBlocks(() => WinOrLose.Invoke());
        //Task.WaitAll(func.Invoke());
        //await DestroyAllBlocks(() => OnLost.Invoke());
        //OnLost.Invoke();

        foreach (var grid in _grids)
        {
            grid.gameObject.SetActive(false);
        }

        StartCoroutine(AppearCoroutine());
        StopCoroutine(AppearCoroutine());
    }

    void BurstAllBlocks()
        {
            for (int i = 0; i < _spawnedBlocks.Count;)
            {   
                BurstBlock(_spawnedBlocks[i]);
            }
        }

    async Task DestroyAllBlocks(Action OnBlocksDestroyed)
    {
        var tasks = new List<Task>();
        var scaleKoef = 0.5f;
        //Cleaning grid web
        foreach (var block in _spawnedBlocks)
        {
            tasks.Add(block.transform.DOScale
                (block.transform.localScale * scaleKoef, 0.4f).AsyncWaitForCompletion());
        }

        await Task.WhenAll(tasks);

        for (int i = 0; i < _spawnedBlocks.Count; i++)
        {
            tasks[i] = _spawnedBlocks[i].transform.DOScale
               (_spawnedBlocks[i].transform.localScale / scaleKoef, 0.4f).AsyncWaitForCompletion();
        }

        await Task.WhenAll(tasks);

        BurstAllBlocks();
        OnBlocksDestroyed();
    }

    async Task BurstBlock(Block block)
    {
        block.BurstParticle.Play();
        block.Grid.OccupiedCube = null;
        RemoveBlock(block, false);
    }
    
    void RemoveBlock(Block block, bool releaseParticle)
    {
        _spawnedBlocks.Remove(block);
        Destroy(block.gameObject);
        if (releaseParticle)
            _cubeSpawner.ReleaseParticle(block.BurstParticle);
    }
    
    void Shift(Vector3 direction)
    {
        ChangeState(State.Shifting);
    
        var canSpawn = false;
        var orderedBlocks = _spawnedBlocks.OrderBy(b => b.Pos.x).ThenBy(b => b.Pos.y).ToList();
        if (direction == Vector3.right || direction == Vector3.up) orderedBlocks.Reverse();
        foreach (var block in orderedBlocks)
        {
            var next = block.Grid;
            do
            {
                block.SetBlock(next);
                var possibleGrid = FindGridAtPosition(next.Pos + direction);
    
                if (possibleGrid != null)
                {
                    var occupiedCube = possibleGrid.OccupiedCube;
    
                    if (occupiedCube != null && occupiedCube.CanMerge(block.Value))
                    {
                        block.Merge(occupiedCube);
                        canSpawn = true;
                    }
    
                    else if (possibleGrid.OccupiedCube == null) 
                    { 
                        next = possibleGrid;
                        canSpawn = true;
                    }
                }
                
            } while (next != block.Grid);
        }
    
        var sequence = DOTween.Sequence();
        
        //moving part
        foreach (var block in orderedBlocks)
        {
            var movePoint = block.MergingBlock != null ? 
                block.MergingBlock.GetPosFromGrid(block.MergingBlock.Grid) 
                    : block.GetPosFromGrid(block.Grid);
            
            sequence.Insert(0, block.transform.DOMove(movePoint, _travelTime));
            block.BurstParticle.transform.position = movePoint;
        }
        //merging part
        sequence.OnComplete(() =>
        {
            var mergingBlocksList = orderedBlocks.Where(b => b.MergingBlock != null).
                OrderByDescending(block => block.Value);
            var mergeTasks = new List<Task>();

            if (mergingBlocksList.Count() > 0)
            {
                _cubeSpawner.SpawnBlocks(_grids, 1);
                foreach (var block in mergingBlocksList)
                {
                    Merge(block.MergingBlock, block);
                    //this is value before merging, so we need to double it
                    if (block.Value * 2 == _winningBlockValue) return;
                }
            }
            else if (mergingBlocksList.Count() == 0)
            {
                if (canSpawn) ChangeState(State.SpawningBlock);
                else if (!canSpawn) ChangeState(State.WaitingForInput);
            }
        });
    }
    
    void Merge(Block blockToMergeWith, Block originBlock)
    {
        var newValue = originBlock.Value * 2;
        var freeGridsNum = _cubeSpawner.GetFreeGrids(_grids).Count();
        Block newBlock = _cubeSpawner.SpawnBlock(blockToMergeWith.Grid, newValue);
                
        OnMerge?.Invoke(new OnMergeEventArgs { Score = newValue });
    
        RemoveBlock(blockToMergeWith, true);
        RemoveBlock(originBlock, true);

        freeGridsNum = _cubeSpawner.GetFreeGrids(_grids).Count();
        if ((freeGridsNum > 0 || CanMerge()) &&
            newValue != _winningBlockValue) 
        { 
            newBlock.ResizeBlock();
            ChangeState(State.WaitingForInput);
        }
        else if (freeGridsNum == 0)
            ChangeState(State.Lose);

        if (newValue == _winningBlockValue)
            ChangeState(State.Win);

    }
    
    //checks if any cubes can be merged
    public bool CanMerge()
    {
        var gridsWithCubes = _grids.Where(grid => grid.OccupiedCube != null).ToArray();

        for (int i = 0; i < gridsWithCubes.Count(); i++)
        {
            Debug.Log(gridsWithCubes[i].OccupiedCube.Order);
            //checks cube above(i++) or 
            if (BlockExistsAtPosition(gridsWithCubes[i], Vector3.up))
            {
                if (gridsWithCubes[i].OccupiedCube.CanMerge(gridsWithCubes[i + 1].OccupiedCube.Value))
                {
                    Debug.Log("mergin");
                    return true;
                }
            }
    
            if (BlockExistsAtPosition(gridsWithCubes[i], Vector3.right))
            {
                if (gridsWithCubes[i].OccupiedCube.CanMerge(gridsWithCubes[i + 4].OccupiedCube.Value))
                {
                    Debug.Log(gridsWithCubes[i + 4].OccupiedCube.Value);
                    return true;
                }
            }
        }
    
        return false;
    }
    
    Grid FindGridAtPosition(Vector3 pos)
    {
        return _grids.FirstOrDefault(g => g.Pos == pos);
    }

    bool BlockExistsAtPosition(Grid grid, Vector3 direction) =>
        FindGridAtPosition(grid.Pos + direction) != null ? true : false;

    IEnumerator AppearCoroutine()
    {

        for (int i = 0; i < 1 / _alfaKoef; i++)
        {
            _winLoseUI.alpha += Mathf.Lerp(0, 1, _alfaKoef);
            yield return null;
        }
    }

    IEnumerator DisappearCoroutine()
    {
        for (int i = 0; i < 1 / _alfaKoef; i++)
        {
            _winLoseUI.alpha -= Mathf.Lerp(0, 1, _alfaKoef);
            yield return null;
        }
        OnNewGameStarted.Invoke();
    }
    #endregion  
}
