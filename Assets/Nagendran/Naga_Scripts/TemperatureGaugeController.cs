using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using TMPro;

public class TemperatureGaugeController : MonoBehaviour
{
    [System.Serializable]
    public class GaugePageConfig
    {
        [Tooltip("Page index (0-based) this gauge configuration applies to.")]
        public int pageIndex;

        [Tooltip("Starting temperature (e.g., 0 or initial value).")]
        public float startTemperature = 0f;

        [Tooltip("Target temperature to reach (e.g., 26, 39, 100).")]
        public float targetTemperature = 39f;

        [Tooltip("Time in seconds to complete the fill and heating animation.")]
        public float duration = 3.0f;

        [Tooltip("If TRUE: resets fill to 0 and start temp when re-entering the page. If FALSE: stays completed once done.")]
        public bool resetOnRevisit = false;

        [Tooltip("If TRUE: requests PageNavigationController to unlock when target is reached.")]
        public bool unlockPageOnComplete = true;
    }

    [Header("UI References")]
    [Tooltip("The circular Image with Image Type set to 'Filled'.")]
    [SerializeField] private Image fillImage;

    [Tooltip("The TMP Text displaying the temperature.")]
    [SerializeField] private TMP_Text temperatureText;

    [Header("Configurations")]
    [SerializeField] private List<GaugePageConfig> pageConfigs = new();

    // Internal State Tracking
    private readonly Dictionary<int, float> savedTemperatures = new();
    private readonly Dictionary<int, float> savedFillAmounts = new();
    private readonly HashSet<int> completedPages = new();
    private Coroutine gaugeCoroutine;
    private int currentPageIndex = 0;

    private void OnEnable()
    {
        PageNavigationController.OnPageChanged += HandlePageChanged;
    }

    private void OnDisable()
    {
        PageNavigationController.OnPageChanged -= HandlePageChanged;
    }

    private void Start()
    {
        HandlePageChanged(PageNavigationController.CurrentIndex);
    }

    private void HandlePageChanged(int pageIndex)
    {
        currentPageIndex = pageIndex;

        if (gaugeCoroutine != null)
        {
            StopCoroutine(gaugeCoroutine);
            gaugeCoroutine = null;
        }

        GaugePageConfig config = GetConfigForPage(currentPageIndex);
        if (config == null)
            return;

        // Reset state if marked to reset on revisit
        if (config.resetOnRevisit)
        {
            savedTemperatures[currentPageIndex] = config.startTemperature;
            savedFillAmounts[currentPageIndex] = 0f;
            completedPages.Remove(currentPageIndex);
        }

        // Apply completed/saved values or set initial baseline
        if (completedPages.Contains(currentPageIndex))
        {
            UpdateUI(config.targetTemperature, 1f);
        }
        else
        {
            float initTemp = savedTemperatures.TryGetValue(currentPageIndex, out float t) ? t : config.startTemperature;
            float initFill = savedFillAmounts.TryGetValue(currentPageIndex, out float f) ? f : 0f;
            UpdateUI(initTemp, initFill);
        }
    }

    /// <summary>
    /// Call this method from your burner/heating interaction event or trigger.
    /// </summary>
    public void StartHeating()
    {
        GaugePageConfig config = GetConfigForPage(currentPageIndex);
        if (config == null) return;

        // If already completed and locked, ignore duplicate triggers
        if (completedPages.Contains(currentPageIndex) && !config.resetOnRevisit)
            return;

        if (gaugeCoroutine != null)
            StopCoroutine(gaugeCoroutine);

        gaugeCoroutine = StartCoroutine(AnimateGaugeRoutine(config));
    }

    private IEnumerator AnimateGaugeRoutine(GaugePageConfig config)
    {
        float startTemp = savedTemperatures.TryGetValue(config.pageIndex, out float st) ? st : config.startTemperature;
        float startFill = savedFillAmounts.TryGetValue(config.pageIndex, out float sf) ? sf : 0f;

        float endTemp = config.targetTemperature;
        float endFill = 1f; // Fills completely from 0 to 1
        float elapsed = 0f;

        if (config.duration <= 0f)
        {
            UpdateUI(endTemp, endFill);
        }
        else
        {
            while (elapsed < config.duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / config.duration);

                float currentTemp = Mathf.Lerp(startTemp, endTemp, t);
                float currentFill = Mathf.Lerp(startFill, endFill, t);

                savedTemperatures[config.pageIndex] = currentTemp;
                savedFillAmounts[config.pageIndex] = currentFill;

                UpdateUI(currentTemp, currentFill);
                yield return null;
            }
        }

        // Lock values at final targets
        savedTemperatures[config.pageIndex] = endTemp;
        savedFillAmounts[config.pageIndex] = 1f;
        completedPages.Add(config.pageIndex);
        UpdateUI(endTemp, 1f);

        gaugeCoroutine = null;

        // Request unlock from PageNavigationController
        if (config.unlockPageOnComplete)
        {
            PageNavigationController.RequestNavigationUnlock();
        }
    }

    private void UpdateUI(float temperature, float fillAmount)
    {
        if (fillImage != null)
        {
            fillImage.fillAmount = Mathf.Clamp01(fillAmount);
        }

        if (temperatureText != null)
        {
            int roundedTemp = Mathf.RoundToInt(temperature);
            // Uses TextMeshPro Rich Text <sup> tag to position the degree symbol above
            temperatureText.text = $"{roundedTemp}<sup>o</sup>C";
        }
    }

    private GaugePageConfig GetConfigForPage(int pageIndex)
    {
        return pageConfigs.Find(p => p.pageIndex == pageIndex);
    }
}