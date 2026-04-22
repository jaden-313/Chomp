using UnityEngine;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
public class MainMenuManager : MonoBehaviour
{
    private const string HighScoreKey = "HighScore";
    private const string ChompSoundMutedKey = "ChompSoundMuted";

    public SimpleShapeGraphic muteButtonIcon;
    public Texture2D speakerOnIconTexture;
    public Texture2D speakerOffIconTexture;

    private Texture2D cleanedSpeakerOnTexture;
    private Texture2D cleanedSpeakerOffTexture;

    void Start()
    {
        UpdateMuteButtonLabel();
    }

    void OnValidate()
    {
        UpdateMuteButtonLabel();
    }

    void OnDestroy()
    {
        DestroyGeneratedTexture(ref cleanedSpeakerOnTexture);
        DestroyGeneratedTexture(ref cleanedSpeakerOffTexture);
    }

    public void LoadScene(string sceneName)
    {
        // Starting a game from the main menu should always reset the stored high score.
        PlayerPrefs.SetInt(HighScoreKey, 0);
        PlayerPrefs.Save();

        SceneManager.LoadScene(sceneName);
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    public void ToggleChompSound()
    {
        bool muted = IsChompSoundMuted();
        PlayerPrefs.SetInt(ChompSoundMutedKey, muted ? 0 : 1);
        PlayerPrefs.Save();
        UpdateMuteButtonLabel();
    }

    void UpdateMuteButtonLabel()
    {
        if (muteButtonIcon == null)
        {
            return;
        }

        muteButtonIcon.shape = SimpleShapeGraphic.ShapeType.TexturedQuad;
        EnsureCleanIconTextures();
        muteButtonIcon.textureAsset = IsChompSoundMuted() ? cleanedSpeakerOffTexture : cleanedSpeakerOnTexture;
        muteButtonIcon.SetVerticesDirty();
        muteButtonIcon.SetMaterialDirty();
    }

    public static bool IsChompSoundMuted()
    {
        return PlayerPrefs.GetInt(ChompSoundMutedKey, 0) == 1;
    }

    void EnsureCleanIconTextures()
    {
        cleanedSpeakerOnTexture = RefreshCleanIconTexture(cleanedSpeakerOnTexture, speakerOnIconTexture, "SpeakerOnClean");
        cleanedSpeakerOffTexture = RefreshCleanIconTexture(cleanedSpeakerOffTexture, speakerOffIconTexture, "SpeakerOffClean");
    }

    Texture2D RefreshCleanIconTexture(Texture2D currentTexture, Texture2D sourceTexture, string textureName)
    {
        if (sourceTexture == null)
        {
            DestroyGeneratedTexture(ref currentTexture);
            return null;
        }

        if (currentTexture != null &&
            currentTexture.width == sourceTexture.width &&
            currentTexture.height == sourceTexture.height)
        {
            return currentTexture;
        }

        DestroyGeneratedTexture(ref currentTexture);

        Texture2D cleanedTexture = new Texture2D(sourceTexture.width, sourceTexture.height, TextureFormat.RGBA32, false);
        cleanedTexture.name = textureName;
        cleanedTexture.hideFlags = HideFlags.HideAndDontSave;
        cleanedTexture.filterMode = FilterMode.Bilinear;
        cleanedTexture.wrapMode = TextureWrapMode.Clamp;

        Color32[] sourcePixels = sourceTexture.GetPixels32();
        Color32[] cleanedPixels = new Color32[sourcePixels.Length];

        for (int i = 0; i < sourcePixels.Length; i++)
        {
            Color32 sourcePixel = sourcePixels[i];
            float luminance = (sourcePixel.r + sourcePixel.g + sourcePixel.b) / (255f * 3f);
            float alpha = Mathf.Clamp01(1f - luminance);

            if (alpha < 0.08f)
            {
                cleanedPixels[i] = new Color32(0, 0, 0, 0);
                continue;
            }

            byte alphaByte = (byte)Mathf.RoundToInt(alpha * 255f);
            cleanedPixels[i] = new Color32(0, 0, 0, alphaByte);
        }

        cleanedTexture.SetPixels32(cleanedPixels);
        cleanedTexture.Apply(false, false);
        return cleanedTexture;
    }

    void DestroyGeneratedTexture(ref Texture2D textureToDestroy)
    {
        if (textureToDestroy == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            Destroy(textureToDestroy);
        }
        else
        {
            DestroyImmediate(textureToDestroy);
        }

        textureToDestroy = null;
    }
}
