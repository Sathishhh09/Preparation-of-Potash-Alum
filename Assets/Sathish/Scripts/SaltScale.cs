using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SaltScale : MonoBehaviour
{
    [System.Serializable]
    public class PageScaleConfig
    {
        [Tooltip("The page index (0-based) that triggers this scale animation.")]
        public int targetPageIndex;

        [Tooltip("The GameObject you want to scale.")]
        public GameObject targetObject;

        [Header("Scale Values")]
        [Tooltip("The initial scale set immediately upon entering the page.")]
        public Vector3 initialScale = Vector3.zero;

        [Tooltip("Target scale to reach after animating.")]
        public Vector3 targetScale = Vector3.one;

        [Header("Animation Settings")]
        [Tooltip("Time in seconds to complete the scaling.")]
        public float duration = 1.0f;

        [Tooltip("Animation curve to control the easing of the scale.")]
        public AnimationCurve easeCurve = AnimationCurve.Linear(0, 0, 1, 1);
    }

    [Header("Page Scale Configurations")]
    [SerializeField] private List<PageScaleConfig> scaleConfigs = new List<PageScaleConfig>();

    private readonly Dictionary<GameObject, Coroutine> activeCoroutines = new Dictionary<GameObject, Coroutine>();

    private void OnEnable()
    {
        PageNavigationController.OnPageChanged += HandlePageChanged;
    }

    private void OnDisable()
    {
        PageNavigationController.OnPageChanged -= HandlePageChanged;
        StopAllCoroutines();
        activeCoroutines.Clear();
    }

    private void Start()
    {
        // Evaluate initial page state directly without yield delay spikes
        HandlePageChanged(PageNavigationController.CurrentIndex);
    }

    private void HandlePageChanged(int pageIndex)
    {
        foreach (var config in scaleConfigs)
        {
            if (config.targetPageIndex == pageIndex && config.targetObject != null)
            {
                TriggerScale(config);
            }
        }
    }

    public void TriggerScale(PageScaleConfig config)
    {
        if (config.targetObject == null) return;

        // Stop existing animation on this object if running
        if (activeCoroutines.TryGetValue(config.targetObject, out Coroutine existingCoroutine) && existingCoroutine != null)
        {
            StopCoroutine(existingCoroutine);
        }

        // Apply initial scale immediately
        config.targetObject.transform.localScale = config.initialScale;

        // Start new scale routine
        Coroutine newCoroutine = StartCoroutine(AnimateScaleRoutine(config));
        activeCoroutines[config.targetObject] = newCoroutine;
    }

    private IEnumerator AnimateScaleRoutine(PageScaleConfig config)
    {
        Transform targetTransform = config.targetObject.transform;
        Vector3 startScale = config.initialScale;
        float elapsedTime = 0f;

        if (config.duration <= 0f)
        {
            targetTransform.localScale = config.targetScale;
            activeCoroutines.Remove(config.targetObject);
            yield break;
        }

        // Ensure object starts explicitly at startScale on frame 0
        targetTransform.localScale = startScale;

        while (elapsedTime < config.duration)
        {
            yield return null; // Yield FIRST so Time.deltaTime reflects actual frame elapsed time

            elapsedTime += Time.deltaTime;
            float normalizedTime = Mathf.Clamp01(elapsedTime / config.duration);
            
            float curveValue = config.easeCurve.Evaluate(normalizedTime);
            targetTransform.localScale = Vector3.LerpUnclamped(startScale, config.targetScale, curveValue);
        }

        targetTransform.localScale = config.targetScale;
        activeCoroutines.Remove(config.targetObject);
    }
}