using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FishRadarUI : MonoBehaviour
{
    public Transform player;
    public RectTransform radarPanel;
    public RectTransform playerMarker;
    public SimpleShapeGraphic fishMarkerTemplate;

    public float radarRange = 35f;
    public float dotSize = 9f;

    public Color borderColor = new Color(0.6f, 0.9f, 1f, 0.9f);
    public Color edibleColor = new Color(0.2f, 1f, 0.2f);
    public Color neutralColor = Color.gray;
    public Color dangerousColor = new Color(1f, 0.15f, 0.15f);

    private readonly List<SimpleShapeGraphic> fishDots = new List<SimpleShapeGraphic>();

    void Awake()
    {
        SetupRadarUI();
    }

    void Update()
    {
        if (player == null || radarPanel == null)
        {
            return;
        }

        FishAI[] fish = FindObjectsByType<FishAI>(FindObjectsSortMode.None);
        EnsureDotCount(fish.Length);

        for (int i = 0; i < fishDots.Count; i++)
        {
            if (i >= fish.Length)
            {
                fishDots[i].gameObject.SetActive(false);
                continue;
            }

            UpdateFishDot(fishDots[i], fish[i]);
        }

        UpdatePlayerMarkerRotation();
    }

    void SetupRadarUI()
    {
        if (player == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");

            if (playerObject != null)
            {
                player = playerObject.transform;
            }
        }

        if (radarPanel == null)
        {
            return;
        }

        Outline outline = radarPanel.GetComponent<Outline>();

        if (outline != null)
        {
            outline.effectColor = borderColor;
            outline.effectDistance = new Vector2(2f, -2f);
        }

        SetupPlayerMarker();
        SetupFishMarkerTemplate();

        radarPanel.SetAsLastSibling();
    }

    void SetupPlayerMarker()
    {
        if (playerMarker == null)
        {
            return;
        }

        playerMarker.anchoredPosition = Vector2.zero;
    }

    void SetupFishMarkerTemplate()
    {
        if (fishMarkerTemplate == null)
        {
            return;
        }

        fishMarkerTemplate.raycastTarget = false;
        fishMarkerTemplate.gameObject.SetActive(false);
    }

    void EnsureDotCount(int fishCount)
    {
        while (fishDots.Count < fishCount)
        {
            if (fishMarkerTemplate == null)
            {
                return;
            }

            SimpleShapeGraphic dotImage = Instantiate(fishMarkerTemplate, radarPanel);
            dotImage.gameObject.name = "Fish Marker";
            dotImage.gameObject.SetActive(false);
            fishDots.Add(dotImage);
        }
    }

    void UpdateFishDot(SimpleShapeGraphic dotImage, FishAI fish)
    {
        Vector3 offset = fish.transform.position - player.position;
        Vector2 radarOffset = new Vector2(offset.x, offset.z);

        if (radarOffset.magnitude > radarRange)
        {
            dotImage.gameObject.SetActive(false);
            return;
        }

        dotImage.gameObject.SetActive(true);

        float halfRadarSize = Mathf.Min(radarPanel.rect.width, radarPanel.rect.height) * 0.5f;
        Vector2 dotPosition = radarOffset / radarRange * halfRadarSize;

        RectTransform dotTransform = dotImage.GetComponent<RectTransform>();
        dotTransform.anchoredPosition = dotPosition;
        float markerSize = GetMarkerSize(fish);
        dotTransform.sizeDelta = new Vector2(markerSize, markerSize);

        dotImage.color = GetDotColor(fish);
    }

    Color GetDotColor(FishAI fish)
    {
        float playerSize = player.localScale.x;
        float fishSize = fish.transform.localScale.x;

        if (fishSize < playerSize)
        {
            return edibleColor;
        }

        if (fishSize >= playerSize * 1.2f)
        {
            return dangerousColor;
        }

        return neutralColor;
    }

    float GetMarkerSize(FishAI fish)
    {
        float playerSize = Mathf.Max(player.localScale.x, 0.01f);
        float fishSize = fish.transform.localScale.x;
        float relativeSize = fishSize / playerSize;

        return dotSize * Mathf.Clamp(relativeSize, 0.65f, 1.7f);
    }

    void UpdatePlayerMarkerRotation()
    {
        if (playerMarker == null || player == null)
        {
            return;
        }

        Vector3 forward = player.forward;
        forward.y = 0f;

        if (forward == Vector3.zero)
        {
            return;
        }

        // The triangle sprite points up in UI space, so rotate it around Z.
        float angle = Mathf.Atan2(forward.x, forward.z) * Mathf.Rad2Deg;
        playerMarker.localRotation = Quaternion.Euler(0f, 0f, -angle);
    }

}
