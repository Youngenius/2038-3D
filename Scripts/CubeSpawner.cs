using System.Collections;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using System.ComponentModel;
using System;
using UnityEngine.Pool;

public class CubeSpawner : MonoBehaviour
{
    [SerializeField] private List<BlockType> _blockTypes;
    [SerializeField] private Block _blockPref;
    [SerializeField] private ParticleSystem _burstParticlePref;

    private ObjectPool<ParticleSystem> _particlePool;
    public BindingList<Block> SpawnedBlocks = new BindingList<Block>();

    private void Awake()
    {
        _particlePool = new ObjectPool<ParticleSystem>(() =>
        {
            return Instantiate(_burstParticlePref);
        }, particle =>
        {
            particle.gameObject.SetActive(true);
        }, partcle =>
        {
            partcle.gameObject.SetActive(false);
        }, particle => {
            Destroy(particle.gameObject);
        });
    }

    void AddBlock(Block block) => SpawnedBlocks.Add(block);

    private BlockType GetBlockTypeByValue(int value) =>
        _blockTypes.First(t => t.Value == value);
    public List<Grid> GetFreeGrids(IEnumerable<Grid> grids) =>
    grids.Where(g => g.OccupiedCube == null)
        .OrderBy(b => UnityEngine.Random.value).ToList();

    public void SpawnBlocks(IEnumerable<Grid> grids, int ammount)
    {
        var freeNodes = GetFreeGrids(grids);

        foreach (var grid in freeNodes.Take(ammount))
        {
            SpawnBlock(grid, UnityEngine.Random.value > 0.8 ? 4 : 2);
        }
    }

    public Block SpawnBlock(Grid grid, int value)
    {
        var block = Instantiate(_blockPref,
               grid.Pos, Quaternion.identity);
        block.transform.position = block.GetPosFromGrid(grid);
        block.Init(GetBlockTypeByValue(value));
        block.SetBlock(grid);
        AddBlock(block);

        SpawnParticle(block);

        return block;
    }

    private void SpawnParticle(Block block)
    {
        var particle = _particlePool.Get();
        particle.transform.position = block.transform.position;
        particle.GetComponent<Renderer>().material = GetBlockTypeByValue(block.Value).Material;
        block.BurstParticle = particle; //cashing particle to be able to reffer to it through block
    }

    public void ReleaseParticle(ParticleSystem particle) =>
        _particlePool.Release(particle);

}

[Serializable]
public struct BlockType
{
    public int Value;
    public Color Color;
    public Material Material;
    public ParticleSystem Particle;
}
