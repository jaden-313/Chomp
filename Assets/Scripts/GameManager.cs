using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    private const string HighScoreKey = "HighScore";
    private const string HighScoreInitializedKey = "HighScoreInitialized";
    private const string MainMenuSceneName = "MainMenuScene";

    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI highScoreText;
    public TextMeshProUGUI gameOverText;
    public Button restartButton;
    public Button mainMenuButton;
    public Slider hungerBar;
    public Image hungerBarFill;
    public Color hungerFullColor = new Color(0.2f, 0.95f, 0.3f, 1f);
    public Color hungerMidColor = new Color(1f, 0.85f, 0.15f, 1f);
    public Color hungerLowColor = new Color(1f, 0.2f, 0.2f, 1f);
    public float hungerBarLerpSpeed = 8f;

    private int highScore = 0;
    private bool gameEnded = false;
    private float targetHungerNormalized = 1f;
    private float displayedHungerNormalized = 1f;

    void Start()
    {
        Time.timeScale = 1f;
        ResolveHungerBarReferences();

        if (!PlayerPrefs.HasKey(HighScoreInitializedKey))
        {
            PlayerPrefs.SetInt(HighScoreKey, 0);
            PlayerPrefs.SetInt(HighScoreInitializedKey, 1);
            PlayerPrefs.Save();
        }

        highScore = PlayerPrefs.GetInt(HighScoreKey, 0);

        UpdateScoreDisplay(0);
        UpdateHighScoreDisplay();

        gameOverText.gameObject.SetActive(false);
        restartButton.gameObject.SetActive(false);
        if (mainMenuButton != null)
        {
            mainMenuButton.gameObject.SetActive(false);
        }
        ApplyHungerBarVisual(force: true);
    }

    void Update()
    {
        if (Mathf.Abs(displayedHungerNormalized - targetHungerNormalized) <= 0.001f)
        {
            return;
        }

        displayedHungerNormalized = Mathf.MoveTowards(
            displayedHungerNormalized,
            targetHungerNormalized,
            hungerBarLerpSpeed * Time.unscaledDeltaTime);

        ApplyHungerBarVisual();
    }

    public void UpdateScore(int score)
    {
        UpdateScoreDisplay(score);

        if (score > highScore)
        {
            highScore = score;
            PlayerPrefs.SetInt(HighScoreKey, highScore);
            PlayerPrefs.Save();
            UpdateHighScoreDisplay();
        }
    }

    public void UpdateHunger(float hunger)
    {
        targetHungerNormalized = Mathf.Clamp01(hunger / 100f);

        if (hungerBarFill == null)
        {
            ResolveHungerBarReferences();
        }

        if (hungerBarFill != null && !Application.isPlaying)
        {
            displayedHungerNormalized = targetHungerNormalized;
            ApplyHungerBarVisual(force: true);
        }
    }

    public void GameOver()
    {
        if (gameEnded) return;

        gameEnded = true;

        gameOverText.gameObject.SetActive(true);
        restartButton.gameObject.SetActive(true);
        if (mainMenuButton != null)
        {
            mainMenuButton.gameObject.SetActive(true);
        }

        Time.timeScale = 0f;
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void GoToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(MainMenuSceneName);
    }

    void UpdateScoreDisplay(int score)
    {
        if (scoreText != null)
        {
            scoreText.text = "Score: " + score;
        }
    }

    void UpdateHighScoreDisplay()
    {
        if (highScoreText != null)
        {
            highScoreText.text = "High Score: " + highScore;
        }
    }

    void ResolveHungerBarReferences()
    {
        if (hungerBar != null)
        {
            if (hungerBarFill == null && hungerBar.fillRect != null)
            {
                hungerBarFill = hungerBar.fillRect.GetComponent<Image>();
            }

            if (hungerBar.handleRect != null)
            {
                hungerBar.handleRect.gameObject.SetActive(false);
            }
        }
    }

    void ApplyHungerBarVisual(bool force = false)
    {
        if (hungerBar == null || hungerBarFill == null)
        {
            return;
        }

        if (force)
        {
            displayedHungerNormalized = targetHungerNormalized;
        }

        float sliderValue = Mathf.Lerp(hungerBar.minValue, hungerBar.maxValue, displayedHungerNormalized);
        hungerBar.SetValueWithoutNotify(sliderValue);
        hungerBarFill.color = EvaluateHungerColor(displayedHungerNormalized);
    }

    Color EvaluateHungerColor(float hungerNormalized)
    {
        if (hungerNormalized >= 0.5f)
        {
            return Color.Lerp(hungerMidColor, hungerFullColor, (hungerNormalized - 0.5f) / 0.5f);
        }

        return Color.Lerp(hungerLowColor, hungerMidColor, hungerNormalized / 0.5f);
    }
}
