using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI highScoreText;
    public TextMeshProUGUI gameOverText;
    public Button restartButton;
    public Slider hungerBar;

    private int highScore = 0;
    private bool gameEnded = false;

    void Start()
    {
        Time.timeScale = 1f;

        highScore = PlayerPrefs.GetInt("HighScore", 0);

        scoreText.text = "Score: 0";
        highScoreText.text = "High Score: " + highScore;

        gameOverText.gameObject.SetActive(false);
        restartButton.gameObject.SetActive(false);
    }

    public void UpdateScore(int score)
    {
        scoreText.text = "Score: " + score;

        if (score > highScore)
        {
            highScore = score;
            PlayerPrefs.SetInt("HighScore", highScore);
            highScoreText.text = "High Score: " + highScore;
        }
    }

    public void UpdateHunger(float hunger)
    {
        hungerBar.value = hunger;
    }

    public void GameOver()
    {
        if (gameEnded) return;

        gameEnded = true;

        gameOverText.gameObject.SetActive(true);
        restartButton.gameObject.SetActive(true);

        Time.timeScale = 0f;
    }
        public void RestartGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}