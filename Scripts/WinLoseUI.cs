using TMPro;
using UnityEngine;

public class WinLoseUI : MonoBehaviour
{
    private ScoreManager _score;
    [SerializeField] private TextMeshProUGUI _scoreLabel;
    [SerializeField] private TextMeshProUGUI _highestScoreLabel;

    private void OnEnable()
    {
        _score = FindObjectOfType<ScoreManager>().GetComponent<ScoreManager>();
        _scoreLabel.text = _score.CurrentScore.ToString();
        _highestScoreLabel.text = _score.GetScore(ScoreManager.HighestScoreName).ToString();

        _score.ResetScore();
    }
}
