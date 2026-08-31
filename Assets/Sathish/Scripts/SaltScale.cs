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
    }

    private IEnumerator Start()
    {
        // Wait until end of frame to ensure PageNavigationController.CurrentIndex is fully initialized
        yield return new WaitForEndOfFrame();
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
        if (activeCoroutines.TryGetValue(config.targetObject, out Coroutine existingCoroutine) && existingCoroutine != null)
        {
            StopCoroutine(existingCoroutine);
        }

        // Force scale to initial scale IMMEDIATELY when entering the page
        config.targetObject.transform.localScale = config.initialScale;

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
            yield break;
        }

        while (elapsedTime < config.duration)
        {
            elapsedTime += Time.deltaTime;
            float normalizedTime = Mathf.Clamp01(elapsedTime / config.duration);
            
            float curveValue = config.easeCurve.Evaluate(normalizedTime);
            targetTransform.localScale = Vector3.LerpUnclamped(startScale, config.targetScale, curveValue);

            yield return null;
        }

        targetTransform.localScale = config.targetScale;
        activeCoroutines.Remove(config.targetObject);
    }
}