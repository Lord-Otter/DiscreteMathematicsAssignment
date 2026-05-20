using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

public class SpeedController : MonoBehaviour, IPointerClickHandler
{
    private readonly float[] speedTiers = { 1f, 2f, 3f };
    private int currentTierIndex = 0;

    [Header("UI Reference")]
    [SerializeField] private TextMeshProUGUI speedText;

    public static float SpeedMultiplier { get; private set; } = 1f;

    private void Start()
    {
        UpdateSpeedUI();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            ChangeSpeed(1);
        }
        else if (eventData.button == PointerEventData.InputButton.Right)
        {
            ChangeSpeed(-1);
        }
    }

    private void ChangeSpeed(int direction)
    {
        currentTierIndex += direction;
        currentTierIndex = Mathf.Clamp(currentTierIndex, 0, speedTiers.Length - 1);

        SpeedMultiplier = speedTiers[currentTierIndex];
        UpdateSpeedUI();
    }

    private void UpdateSpeedUI()
    {
        if (speedText != null)
        {
            speedText.text = $"{SpeedMultiplier}x";
        }
    }
}