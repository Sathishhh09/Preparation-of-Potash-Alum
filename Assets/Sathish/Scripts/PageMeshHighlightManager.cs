using System.Collections.Generic;
using UnityEngine;

public class PageMeshHighlightManager : MonoBehaviour
{
    // ==========================================================
    // MESH HIGHLIGHT ENTRY
    // ==========================================================

    [System.Serializable]
    public class MeshHighlightEntry
    {
        [Header("Target GameObject")]

        [Tooltip(
            "The parent GameObject. " +
            "All child objects containing MeshRenderer or " +
            "SkinnedMeshRenderer will also be highlighted."
        )]
        [SerializeField] private GameObject targetObject;

        [Tooltip("Automatically highlight when this page opens.")]
        [SerializeField] private bool autoHighlightOnPageEnter = false;

        public GameObject TargetObject => targetObject;

        public bool AutoHighlightOnPageEnter =>
            autoHighlightOnPageEnter;
    }


    // ==========================================================
    // PAGE HIGHLIGHT CONFIG
    // ==========================================================

    [System.Serializable]
    public class PageHighlightConfig
    {
        [Header("Page Index")]

        public int pageIndex = 0;

        [Header("Target Objects")]

        public List<MeshHighlightEntry> meshEntries =
            new List<MeshHighlightEntry>();
    }


    // ==========================================================
    // INSPECTOR VARIABLES
    // ==========================================================

    [Header("Highlight Material")]

    [SerializeField] private Material highlightMaterial;


    [Header("Page Configurations")]

    [SerializeField] private List<PageHighlightConfig> pageConfigs =
        new List<PageHighlightConfig>();


    // ==========================================================
    // RUNTIME DATA
    // ==========================================================

    private readonly HashSet<Renderer> highlightedRenderers =
        new HashSet<Renderer>();


    // Tracks entries that were manually disabled.
    private readonly HashSet<string> disabledEntries =
        new HashSet<string>();


    // ==========================================================
    // UNITY EVENTS
    // ==========================================================

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
        HandlePageChanged(
            PageNavigationController.CurrentIndex
        );
    }


    // ==========================================================
    // PAGE CHANGED
    // ==========================================================

    private void HandlePageChanged(int currentPageIndex)
    {
        // Remove highlights from previous page.
        ClearAllHighlightsGlobal();


        PageHighlightConfig config =
            GetConfigByPageIndex(currentPageIndex);


        if (config == null)
            return;


        for (int i = 0; i < config.meshEntries.Count; i++)
        {
            MeshHighlightEntry entry =
                config.meshEntries[i];


            if (entry == null)
                continue;


            // Auto highlight only if this entry
            // was not manually disabled.
            if (
                entry.AutoHighlightOnPageEnter &&
                !IsEntryDisabled(currentPageIndex, i)
            )
            {
                ApplyHighlightToEntry(entry);
            }
        }
    }


    // ==========================================================
    // UNITY EVENT HELPERS
    // ==========================================================

    public void EnableElement0ByPageIndex(int pageIndex)
    {
        EnableElementHighlight(pageIndex, 0);
    }


    public void DisableElement0ByPageIndex(int pageIndex)
    {
        DisableElementHighlight(pageIndex, 0);
    }


    public void EnableElement1ByPageIndex(int pageIndex)
    {
        EnableElementHighlight(pageIndex, 1);
    }


    public void DisableElement1ByPageIndex(int pageIndex)
    {
        DisableElementHighlight(pageIndex, 1);
    }


    public void EnableElement2ByPageIndex(int pageIndex)
    {
        EnableElementHighlight(pageIndex, 2);
    }


    public void DisableElement2ByPageIndex(int pageIndex)
    {
        DisableElementHighlight(pageIndex, 2);
    }


    // ==========================================================
    // ENABLE SINGLE ELEMENT
    // ==========================================================

    public void EnableElementHighlight(
        int pageIndex,
        int elementIndex
    )
    {
        PageHighlightConfig config =
            GetConfigByPageIndex(pageIndex);


        if (config == null)
            return;


        if (
            elementIndex < 0 ||
            elementIndex >= config.meshEntries.Count
        )
            return;


        disabledEntries.Remove(
            GetEntryKey(
                pageIndex,
                elementIndex
            )
        );


        ApplyHighlightToEntry(
            config.meshEntries[elementIndex]
        );
    }


    // ==========================================================
    // DISABLE SINGLE ELEMENT
    // ==========================================================

    public void DisableElementHighlight(
        int pageIndex,
        int elementIndex
    )
    {
        PageHighlightConfig config =
            GetConfigByPageIndex(pageIndex);


        if (config == null)
            return;


        if (
            elementIndex < 0 ||
            elementIndex >= config.meshEntries.Count
        )
            return;


        disabledEntries.Add(
            GetEntryKey(
                pageIndex,
                elementIndex
            )
        );


        RemoveHighlightFromEntry(
            config.meshEntries[elementIndex]
        );
    }


    // ==========================================================
    // ENABLE ALL PAGE ELEMENTS
    // ==========================================================

    public void EnableAllHighlightsForPageIndex(
        int pageIndex
    )
    {
        PageHighlightConfig config =
            GetConfigByPageIndex(pageIndex);


        if (config == null)
            return;


        for (
            int i = 0;
            i < config.meshEntries.Count;
            i++
        )
        {
            disabledEntries.Remove(
                GetEntryKey(
                    pageIndex,
                    i
                )
            );


            ApplyHighlightToEntry(
                config.meshEntries[i]
            );
        }
    }


    // ==========================================================
    // DISABLE ALL PAGE ELEMENTS
    // ==========================================================

    public void DisableAllHighlightsForPageIndex(
        int pageIndex
    )
    {
        PageHighlightConfig config =
            GetConfigByPageIndex(pageIndex);


        if (config == null)
            return;


        for (
            int i = 0;
            i < config.meshEntries.Count;
            i++
        )
        {
            disabledEntries.Add(
                GetEntryKey(
                    pageIndex,
                    i
                )
            );


            RemoveHighlightFromEntry(
                config.meshEntries[i]
            );
        }
    }


    // ==========================================================
    // APPLY HIGHLIGHT TO TARGET + ALL CHILDREN
    // ==========================================================

    private void ApplyHighlightToEntry(
        MeshHighlightEntry entry
    )
    {
        if (entry == null)
            return;


        GameObject target =
            entry.TargetObject;


        if (target == null)
            return;


        if (highlightMaterial == null)
        {
            Debug.LogWarning(
                "PageMeshHighlightManager: " +
                "Highlight Material is not assigned."
            );

            return;
        }


        // ======================================================
        // GET TARGET + ALL CHILD OBJECTS
        // ======================================================

        Renderer[] allRenderers =
            target.GetComponentsInChildren<Renderer>(
                true
            );


        // ======================================================
        // APPLY HIGHLIGHT TO EVERY CHILD RENDERER
        // ======================================================

        foreach (Renderer renderer in allRenderers)
        {
            if (renderer == null)
                continue;


            // Only MeshRenderer and SkinnedMeshRenderer.
            if (
                renderer is MeshRenderer ||
                renderer is SkinnedMeshRenderer
            )
            {
                ApplyHighlightMaterial(renderer);
            }
        }
    }


    // ==========================================================
    // REMOVE HIGHLIGHT FROM TARGET + ALL CHILDREN
    // ==========================================================

    private void RemoveHighlightFromEntry(
        MeshHighlightEntry entry
    )
    {
        if (entry == null)
            return;


        GameObject target =
            entry.TargetObject;


        if (target == null)
            return;


        // ======================================================
        // GET TARGET + ALL CHILD OBJECTS
        // ======================================================

        Renderer[] allRenderers =
            target.GetComponentsInChildren<Renderer>(
                true
            );


        // ======================================================
        // REMOVE HIGHLIGHT FROM EVERY CHILD
        // ======================================================

        foreach (Renderer renderer in allRenderers)
        {
            if (renderer == null)
                continue;


            if (
                renderer is MeshRenderer ||
                renderer is SkinnedMeshRenderer
            )
            {
                RemoveHighlightMaterial(renderer);
            }
        }
    }


    // ==========================================================
    // APPLY MATERIAL
    // ==========================================================

    private void ApplyHighlightMaterial(
        Renderer renderer
    )
    {
        if (renderer == null)
            return;


        if (highlightMaterial == null)
            return;


        Material[] materials =
            renderer.sharedMaterials;


        // Check if highlight is already applied.
        foreach (Material material in materials)
        {
            if (material == highlightMaterial)
            {
                highlightedRenderers.Add(renderer);
                return;
            }
        }


        // Create a new material list.
        List<Material> newMaterials =
            new List<Material>(materials);


        // Add highlight material.
        newMaterials.Add(
            highlightMaterial
        );


        renderer.sharedMaterials =
            newMaterials.ToArray();


        highlightedRenderers.Add(renderer);
    }


    // ==========================================================
    // REMOVE MATERIAL
    // ==========================================================

    private void RemoveHighlightMaterial(
        Renderer renderer
    )
    {
        if (renderer == null)
            return;


        Material[] materials =
            renderer.sharedMaterials;


        List<Material> newMaterials =
            new List<Material>();


        foreach (Material material in materials)
        {
            if (material != highlightMaterial)
            {
                newMaterials.Add(material);
            }
        }


        renderer.sharedMaterials =
            newMaterials.ToArray();


        highlightedRenderers.Remove(renderer);
    }


    // ==========================================================
    // CLEAR ALL HIGHLIGHTS
    // ==========================================================

    private void ClearAllHighlightsGlobal()
    {
        // Create a temporary list because
        // RemoveHighlightMaterialDirect modifies
        // renderer materials.
        List<Renderer> renderersToClear =
            new List<Renderer>(
                highlightedRenderers
            );


        foreach (Renderer renderer in renderersToClear)
        {
            if (renderer != null)
            {
                RemoveHighlightMaterialDirect(
                    renderer
                );
            }
        }


        highlightedRenderers.Clear();


        // ======================================================
        // FALLBACK
        // ======================================================

        // Also search every configured target and
        // all of its children.
        foreach (
            PageHighlightConfig config
            in pageConfigs
        )
        {
            if (config == null)
                continue;


            foreach (
                MeshHighlightEntry entry
                in config.meshEntries
            )
            {
                RemoveHighlightFromEntry(entry);
            }
        }
    }


    // ==========================================================
    // REMOVE MATERIAL DIRECTLY
    // ==========================================================

    private void RemoveHighlightMaterialDirect(
        Renderer renderer
    )
    {
        if (renderer == null)
            return;


        Material[] materials =
            renderer.sharedMaterials;


        List<Material> newMaterials =
            new List<Material>();


        bool materialRemoved = false;


        foreach (Material material in materials)
        {
            if (material == highlightMaterial)
            {
                materialRemoved = true;
            }
            else
            {
                newMaterials.Add(material);
            }
        }


        if (materialRemoved)
        {
            renderer.sharedMaterials =
                newMaterials.ToArray();
        }
    }


    // ==========================================================
    // HELPERS
    // ==========================================================

    private string GetEntryKey(
        int pageIndex,
        int elementIndex
    )
    {
        return $"{pageIndex}_{elementIndex}";
    }


    private bool IsEntryDisabled(
        int pageIndex,
        int elementIndex
    )
    {
        return disabledEntries.Contains(
            GetEntryKey(
                pageIndex,
                elementIndex
            )
        );
    }


    private PageHighlightConfig GetConfigByPageIndex(
        int pageIndex
    )
    {
        foreach (
            PageHighlightConfig config
            in pageConfigs
        )
        {
            if (
                config != null &&
                config.pageIndex == pageIndex
            )
            {
                return config;
            }
        }


        return null;
    }
}