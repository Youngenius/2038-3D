using System.IO;
using UnityEngine;
using System.Linq;
using System;

public class SavingSystem : MonoBehaviour
{
    #region Variables
    private CubeSpawner _cubeSpawner;
    private string _savePath;
    [SerializeField] private string _saveFileName = "BlocksPositionData.json";

    private Block[] _blocks;
    private GameObject[] _blocksObj;
    #endregion

    private void Awake()
    {
        _savePath = Path.Combine(Application.persistentDataPath, _saveFileName);
        if (!File.Exists(_savePath))
        {
            File.Create(_savePath);
            Debug.Log("Not created");
        }
        Debug.Log(_savePath);
        Debug.Log("UGA-UGA");
        _cubeSpawner = FindObjectOfType<CubeSpawner>().GetComponent<CubeSpawner>();
    }

    #region PublicMethod
    public bool WasSaved()
    {
        string file = File.ReadAllText(_savePath);
        using (var sr = new StreamReader(_savePath))
        {
            var text = sr.ReadToEnd();
            Debug.Log(text);
        }
        Debug.Log(file);
        if (string.IsNullOrEmpty(file))
        {
            return false;
        }
        return true;
    }
    void SaveToFile()
    {
        //�������� ��� ����� �� ����� � ������ ����������
        _blocks = FindObjectsOfType<Block>();
        _blocksObj = new GameObject[_blocks.Length]; //�������������� ��� ��� ������
        for (int i = 0; i < _blocks.Length; i++)
        {
            _blocksObj[i] = _blocks[i].gameObject;
        }
        //����� ��� ������ ������
        string[] jsons = new string[_blocks.Length];
        for (int i = 0; i < _blocks.Length; i++)
        {
            DataSaveCube dataSave = new DataSaveCube //������ ��������� ��� ������� ����
            {
                Value = _blocks[i].Value,
                Order = _blocks[i].Grid.Order
            };
            string json = JsonUtility.ToJson(dataSave); // ����� ������ � ��������� ����������
            jsons[i] = json; //����� �� � ����� �����
        }
        File.WriteAllLines(_savePath, jsons); //����� �������� ���� ����� ���������� ����� � ������� � ����
    }

    public void LoadFromFile()
    {
        Debug.Log("bla-bla");
        DataSaveCube dataSave;
        Grid[] grids = FindObjectsOfType<Grid>().OrderBy(grid => grid.Order).ToArray();
        string[] dataCube = File.ReadAllLines(_savePath); //��������� ��� ������
        for (int i = 0; i < dataCube.Length; i++)
        {
            dataSave = JsonUtility.FromJson<DataSaveCube>(dataCube[i]);//��������� ���� ���������, ��� ��� �������� � ����������
            _cubeSpawner.SpawnBlock(grids[dataSave.Order - 1], dataSave.Value);
        }
    }
    #endregion

    #region SaveAuto
    private void OnApplicationFocus(bool focus)
    {
        if (!focus)
        {
            SaveToFile();

            Debug.Log("Saved");
        }
    }
    #endregion

    [Serializable]
    public struct DataSaveCube
    {
        public int Value;
        public int Order;
    }
}