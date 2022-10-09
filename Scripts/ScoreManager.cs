using System;
using TMPro;
using UnityEngine;

public class ScoreManager : MonoBehaviour
{
    private GridGenerator _gridGenerator;
    public GameObject canvas;

    public int CurrentScore;
    private int _highestScore = 0;
    public TextMeshProUGUI ScoreLabel;
    public TextMeshProUGUI HighestScoreLabel;

    public const string CurrentScoreName = "CurrentScore";
    public const string HighestScoreName = "HighestScore";

    public static event Action OnScoreChanged;

    private void Start()
    {
        Debug.Log(HighestScoreLabel.gameObject == null);
        Debug.Log(HighestScoreLabel.text);
        _gridGenerator = FindObjectOfType<GridGenerator>().GetComponent<GridGenerator>();
        _gridGenerator.OnMerge += GridGenerator_OnMerge;

        SetScore(out CurrentScore, GetScore(CurrentScoreName), CurrentScoreName);
        SetScore(out _highestScore, GetScore(HighestScoreName), HighestScoreName);
        LoadScore(HighestScoreLabel, _highestScore, HighestScoreName);
    }
    private void OnApplicationFocus(bool focus)
    {
        if(!focus)
            PlayerPrefs.SetInt(CurrentScoreName, CurrentScore);
    }   

    private void GridGenerator_OnMerge(GridGenerator.OnMergeEventArgs obj)
    {
        CurrentScore += obj.Score;
        ScoreLabel.text = CurrentScore.ToString();
        if (CurrentScore > _highestScore)
        {
            SetScore(out _highestScore, CurrentScore, HighestScoreName);
            HighestScoreLabel.text = _highestScore.ToString();
        }
    }

    public void LoadScore(TextMeshProUGUI scoreLabel, int score, string varName)
    {
        score = GetScore(varName);
        scoreLabel.text = score.ToString();
    }

    public void ResetScore()
    {
        SetScore(out CurrentScore, 0, CurrentScoreName);
        ScoreLabel.text = CurrentScore.ToString();
    }
    public int GetScore(string varName) => PlayerPrefs.GetInt(varName);

    private void SetScore(out int previousScore, int newScore, string varName)
    {
        previousScore = newScore;
        PlayerPrefs.SetInt(varName, previousScore);
    }
}
