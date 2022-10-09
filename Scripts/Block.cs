using DG.Tweening;
using TMPro;
using UnityEngine;

public class Block : MonoBehaviour
{
    public ParticleSystem BurstParticle { get; set; }
    public int Value;
    public int Order;
    public Grid Grid;
    public Block MergingBlock;
    public bool Merging;
    [SerializeField] private TextMeshPro _text;

    public Vector3 Pos => transform.position;
    public Vector3 GetPosFromGrid(Grid grid) => new Vector3
               (grid.Pos.x, grid.Pos.y, grid.Pos.z - 0.25f);

    public void Init(BlockType type)
    {
        Value = type.Value;
        type.Material.color = type.Color;
        gameObject.GetComponent<MeshRenderer>().material = type.Material;
        _text.text = Value.ToString();
    }

    public void SetBlock(Grid grid)
    {
        if (Grid != null) Grid.OccupiedCube = null;
        Grid = grid;
        Grid.OccupiedCube = this;
        SetOrder(grid);
    }

    public void SetOrder(Grid grid) => Order = grid.Order;

    public void ResizeBlock()
    {
        transform.DOScale(transform.localScale * 0.75f, 0.2f).OnComplete(() =>
            transform.DOScale(transform.localScale / 0.75f, 0.2f));
    }

    public void Merge(Block blockToMergeWith)
    {
        MergingBlock = blockToMergeWith;
        Grid.OccupiedCube = null;
        blockToMergeWith.Merging = true;
    }
    public bool CanMerge(int value) => Value == value && MergingBlock == null && !Merging;
}
