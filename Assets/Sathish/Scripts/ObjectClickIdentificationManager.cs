using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Events;
using UnityEngine.EventSystems;

public class ObjectClickIdentificationManager : MonoBehaviour
{
    // =========================================================
    // IDENTIFICATION TARGET
    // =========================================================

    [System.Serializable]
    public class IdentificationTarget
    {
        // -----------------------------------------------------
        // OBJECT
        // -----------------------------------------------------

        [Header("Target Identification Item")]

        public string itemName = "Rheostat";

        [Tooltip(
            "The 3D clickable object. Must have a Collider."
        )]
        public Collider clickableCollider;

        [Tooltip(
            "The specific page index where this item is allowed to be clicked."
        )]
        public int allowedPageIndex = 0;


        // -----------------------------------------------------
        // CAMERA
        // -----------------------------------------------------

        [Header("Camera Point")]

        [Tooltip(
            "Camera moves to this point when this object is clicked."
        )]
        public Transform cameraPoint;


        // -----------------------------------------------------
        // DESCRIPTION
        // -----------------------------------------------------

        [Header("Description Content")]

        [Tooltip(
            "Title shown in the common Description UI."
        )]
        public string title = "Rheostat";

        [TextArea(3, 10)]
        [Tooltip(
            "Description shown in the common Description UI."
        )]
        public string description =
            "Enter the description for this object here.";
    }


    // =========================================================
    // CAMERA CONFIGURATION
    // =========================================================

    [Header("Camera Configuration")]

    [Tooltip(
        "The camera that will move."
    )]
    [SerializeField]
    private Camera mainCamera;

    [Tooltip(
        "The default camera position."
    )]
    [SerializeField]
    private Transform defaultCameraPoint;


    // =========================================================
    // CAMERA MOVEMENT
    // =========================================================

    [Header("Camera Movement")]

    [Tooltip(
        "Time taken to move the camera."
    )]
    [SerializeField]
    private float cameraMoveDuration = 1f;

    [Tooltip(
        "Animation Curve used for smooth camera movement."
    )]
    [SerializeField]
    private AnimationCurve cameraMovementCurve =
        AnimationCurve.EaseInOut(
            0f,
            0f,
            1f,
            1f
        );


    // =========================================================
    // COMMON DESCRIPTION UI
    // =========================================================

    [Header("Common Description UI")]

    [Tooltip(
        "The complete description UI panel. It is shared by every object."
    )]
    [SerializeField]
    private GameObject descriptionUI;

    [Tooltip(
        "Common title text. The clicked object's title is placed here."
    )]
    [SerializeField]
    private TMP_Text titleText;

    [Tooltip(
        "Common description text. The clicked object's description is placed here."
    )]
    [SerializeField]
    private TMP_Text descriptionText;

    [Tooltip(
        "Back button used to return to the default camera."
    )]
    [SerializeField]
    private Button backButton;


    // =========================================================
    // CORRECT TARGETS
    // =========================================================

    [Header("Correct Clickable Items Setup")]

    [SerializeField]
    private List<IdentificationTarget> correctTargets =
        new List<IdentificationTarget>();


    // =========================================================
    // WRONG TARGETS
    // =========================================================

    [System.Serializable]
    public class WrongTargetRule
    {
        [Tooltip("The wrong collider object.")]
        public Collider wrongCollider;

        [Tooltip(
            "The specific page index where this wrong collider is active."
        )]
        public int allowedPageIndex = 0;
    }

    [Header("Wrong Clickable Items Setup")]

    [Tooltip(
        "Objects that are considered wrong answers, mapped to specific pages."
    )]
    [SerializeField]
    private List<WrongTargetRule> wrongTargets =
        new List<WrongTargetRule>();


    // =========================================================
    // AUDIO SETTINGS
    // =========================================================

    [Header("Audio Settings")]

    [SerializeField]
    private AudioSource audioSource;

    [SerializeField]
    private AudioClip correctSound;

    [SerializeField]
    private AudioClip wrongSound;


    // =========================================================
    // PAGE-SPECIFIC CANVAS PARENTS
    // =========================================================

    [System.Serializable]
    public class PageCanvasParentRule
    {
        [Header("Page")]

        [Tooltip(
            "0-based page index."
        )]
        public int pageIndex;

        [Header("Canvas Parent")]

        [Tooltip(
            "Canvas/Transform where popups for this page will be spawned."
        )]
        public Transform canvasParent;
    }

    [Header("Page-Specific Canvas Parents")]

    [Tooltip(
        "Assign a different Canvas parent for each page. " +
        "The current page is automatically detected using " +
        "PageNavigationController.CurrentIndex."
    )]
    [SerializeField]
    private List<PageCanvasParentRule> pageCanvasParents =
        new List<PageCanvasParentRule>();


    // =========================================================
    // WORLD SPACE POPUP PREFABS
    // =========================================================

    [Header("World Space Popup Prefabs")]

    [Tooltip(
        "Popup prefab shown when the correct object is clicked."
    )]
    [SerializeField]
    private GameObject correctWorldSpacePrefab;

    [Tooltip(
        "Popup prefab shown when the wrong object is clicked."
    )]
    [SerializeField]
    private GameObject wrongWorldSpacePrefab;

    [Tooltip(
        "Vertical offset above the clicked collider."
    )]
    [SerializeField]
    private float yOffsetDistance = 0.05f;

    [Tooltip(
        "Scale multiplier applied to spawned popup."
    )]
    [SerializeField]
    private Vector3 spawnScaleMultiplier =
        new Vector3(
            0.005f,
            0.005f,
            0.005f
        );


    // =========================================================
    // EVENTS
    // =========================================================

    [Header("Events")]

    public UnityEvent OnCorrectObjectClicked;

    public UnityEvent OnWrongObjectClicked;

    public UnityEvent OnAllObjectsCompleted;


    // =========================================================
    // RUNTIME DATA
    // =========================================================

    private readonly HashSet<int> completedIndices =
        new HashSet<int>();

    private readonly HashSet<Collider> spawnedWrongColliders =
        new HashSet<Collider>();

    private readonly List<Transform> activePopups =
        new List<Transform>();

    private Coroutine cameraMoveCoroutine;

    private bool isCameraMoving;

    private int currentTargetIndex = -1;


    // =========================================================
    // START
    // =========================================================

    private void Start()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();

            if (audioSource == null)
            {
                audioSource =
                    gameObject.AddComponent<AudioSource>();
            }
        }

        InitializeDescriptionUI();

        InitializeBackButton();
    }


    // =========================================================
    // UPDATE
    // =========================================================

    private void Update()
    {
        UpdatePopupsFacingCamera();

        if (Input.GetMouseButtonDown(0))
        {
            HandleClick(
                Input.mousePosition
            );
        }
    }


    // =========================================================
    // UPDATE POPUP BILLBOARDING
    // =========================================================

    private void UpdatePopupsFacingCamera()
    {
        if (mainCamera == null)
            return;

        for (
            int i = activePopups.Count - 1;
            i >= 0;
            i--
        )
        {
            if (activePopups[i] == null)
            {
                activePopups.RemoveAt(i);
                continue;
            }

            activePopups[i].rotation =
                mainCamera.transform.rotation;
        }
    }


    // =========================================================
    // INITIALIZE DESCRIPTION UI
    // =========================================================

    private void InitializeDescriptionUI()
    {
        if (descriptionUI != null)
        {
            descriptionUI.SetActive(false);
        }

        if (titleText != null)
        {
            titleText.text = "";
        }

        if (descriptionText != null)
        {
            descriptionText.text = "";
        }
    }


    // =========================================================
    // INITIALIZE BACK BUTTON
    // =========================================================

    private void InitializeBackButton()
    {
        if (backButton == null)
            return;

        backButton.onClick.RemoveListener(
            GoBackToDefaultCamera
        );

        backButton.onClick.AddListener(
            GoBackToDefaultCamera
        );
    }


    // =========================================================
    // HANDLE CLICK
    // =========================================================

    private void HandleClick(
        Vector3 screenPoint
    )
    {
        if (mainCamera == null)
            return;

        // -----------------------------------------------------
        // DO NOT ALLOW OBJECT CLICKS WHILE CAMERA IS MOVING
        // -----------------------------------------------------

        if (isCameraMoving)
            return;

        // -----------------------------------------------------
        // DO NOT ALLOW CLICKS WHILE DESCRIPTION UI IS OPEN
        // -----------------------------------------------------

        if (
            descriptionUI != null &&
            descriptionUI.activeSelf
        )
        {
            return;
        }

        // -----------------------------------------------------
        // PREVENT RAYCAST THROUGH UI
        // -----------------------------------------------------

        if (
            EventSystem.current != null &&
            EventSystem.current.IsPointerOverGameObject()
        )
        {
            return;
        }

        // -----------------------------------------------------
        // CREATE CAMERA RAY
        // -----------------------------------------------------

        Ray ray =
            mainCamera.ScreenPointToRay(
                screenPoint
            );

        // -----------------------------------------------------
        // RAYCAST
        // -----------------------------------------------------

        if (
            Physics.Raycast(
                ray,
                out RaycastHit hit
            )
        )
        {
            // -------------------------------------------------
            // CHECK CORRECT TARGET
            // -------------------------------------------------

            int matchedIndex =
                GetMatchingCorrectTargetIndex(
                    hit.collider
                );

            if (matchedIndex != -1)
            {
                ProcessCorrectClick(
                    matchedIndex,
                    hit.collider
                );
            }

            // -------------------------------------------------
            // CHECK WRONG TARGET
            // -------------------------------------------------

            else if (
                IsWrongColliderMatch(
                    hit.collider,
                    out Collider matchedWrongCollider
                )
            )
            {
                ProcessWrongClick(
                    matchedWrongCollider
                );
            }
        }
    }


    // =========================================================
    // FIND CORRECT TARGET
    // PAGE FILTERED
    // =========================================================

    private int GetMatchingCorrectTargetIndex(
        Collider hitCollider
    )
    {
        int currentPage =
            PageNavigationController.CurrentIndex;

        for (
            int i = 0;
            i < correctTargets.Count;
            i++
        )
        {
            IdentificationTarget target =
                correctTargets[i];

            if (target == null)
                continue;

            // -------------------------------------------------
            // ONLY MATCH CURRENT PAGE
            // -------------------------------------------------

            if (
                target.allowedPageIndex !=
                currentPage
            )
            {
                continue;
            }

            if (
                target.clickableCollider == null
            )
            {
                continue;
            }

            // -------------------------------------------------
            // DIRECT COLLIDER MATCH
            // -------------------------------------------------

            if (
                target.clickableCollider ==
                hitCollider
            )
            {
                return i;
            }

            // -------------------------------------------------
            // CHILD COLLIDER MATCH
            // -------------------------------------------------

            if (
                hitCollider.transform.IsChildOf(
                    target.clickableCollider.transform
                )
            )
            {
                return i;
            }
        }

        return -1;
    }


    // =========================================================
    // FIND WRONG TARGET
    // PAGE FILTERED
    // =========================================================

    private bool IsWrongColliderMatch(
        Collider hitCollider,
        out Collider matchedCollider
    )
    {
        int currentPage =
            PageNavigationController.CurrentIndex;

        for (
            int i = 0;
            i < wrongTargets.Count;
            i++
        )
        {
            WrongTargetRule wrongRule =
                wrongTargets[i];

            if (wrongRule == null)
                continue;

            if (wrongRule.wrongCollider == null)
                continue;

            // -------------------------------------------------
            // ONLY MATCH CURRENT PAGE
            // -------------------------------------------------

            if (
                wrongRule.allowedPageIndex !=
                currentPage
            )
            {
                continue;
            }

            // -------------------------------------------------
            // DIRECT OR CHILD COLLIDER MATCH
            // -------------------------------------------------

            if (
                wrongRule.wrongCollider ==
                hitCollider
                ||
                hitCollider.transform.IsChildOf(
                    wrongRule.wrongCollider.transform
                )
            )
            {
                matchedCollider =
                    wrongRule.wrongCollider;

                return true;
            }
        }

        matchedCollider = null;

        return false;
    }


    // =========================================================
    // CORRECT CLICK
    // =========================================================

    private void ProcessCorrectClick(
        int index,
        Collider hitCollider
    )
    {
        if (
            index < 0 ||
            index >= correctTargets.Count
        )
        {
            return;
        }

        IdentificationTarget target =
            correctTargets[index];

        if (target == null)
            return;

        currentTargetIndex = index;

        // -----------------------------------------------------
        // PLAY CORRECT SOUND
        // -----------------------------------------------------

        PlaySound(correctSound);


        // -----------------------------------------------------
        // MARK AS COMPLETED
        // -----------------------------------------------------

        bool wasAlreadyCompleted =
            completedIndices.Contains(index);

        if (!wasAlreadyCompleted)
        {
            completedIndices.Add(index);
        }


        // -----------------------------------------------------
        // SPAWN CORRECT EFFECT
        // -----------------------------------------------------

        if (
            !wasAlreadyCompleted &&
            correctWorldSpacePrefab != null &&
            hitCollider != null
        )
        {
            Vector3 spawnPosition =
                CalculateTopPosition(
                    hitCollider
                );

            SpawnPopUpEffect(
                correctWorldSpacePrefab,
                spawnPosition
            );
        }


        // -----------------------------------------------------
        // EVENT
        // -----------------------------------------------------

        if (!wasAlreadyCompleted)
        {
            OnCorrectObjectClicked?.Invoke();
        }


        // -----------------------------------------------------
        // MOVE CAMERA
        // -----------------------------------------------------

        if (target.cameraPoint != null)
        {
            StartCameraMovement(
                target.cameraPoint,
                index,
                true
            );
        }
        else
        {
            Debug.LogWarning(
                "[Identification] Camera Point is missing for: " +
                target.itemName,
                this
            );

            ShowDescriptionUI(index);
        }


        // -----------------------------------------------------
        // ALL COMPLETED
        // -----------------------------------------------------

        if (
            !wasAlreadyCompleted &&
            completedIndices.Count >=
            correctTargets.Count
        )
        {
            OnAllObjectsCompleted?.Invoke();
        }
    }


    // =========================================================
    // WRONG CLICK
    // =========================================================

    private void ProcessWrongClick(
        Collider hitCollider
    )
    {
        PlaySound(wrongSound);

        if (
            hitCollider != null &&
            wrongWorldSpacePrefab != null
        )
        {
            if (
                !spawnedWrongColliders.Contains(
                    hitCollider
                )
            )
            {
                spawnedWrongColliders.Add(
                    hitCollider
                );

                Vector3 spawnPosition =
                    CalculateTopPosition(
                        hitCollider
                    );

                SpawnPopUpEffect(
                    wrongWorldSpacePrefab,
                    spawnPosition
                );
            }
        }

        OnWrongObjectClicked?.Invoke();
    }


    // =========================================================
    // AUDIO HELPER
    // =========================================================

    private void PlaySound(
        AudioClip clip
    )
    {
        if (
            audioSource != null &&
            clip != null
        )
        {
            audioSource.PlayOneShot(clip);
        }
    }


    // =========================================================
    // GET CANVAS PARENT FOR CURRENT PAGE
    // =========================================================

    private Transform GetCanvasParentForCurrentPage()
    {
        int currentPage =
            PageNavigationController.CurrentIndex;

        // -----------------------------------------------------
        // SEARCH PAGE CANVAS RULES
        // -----------------------------------------------------

        for (
            int i = 0;
            i < pageCanvasParents.Count;
            i++
        )
        {
            PageCanvasParentRule rule =
                pageCanvasParents[i];

            if (rule == null)
                continue;

            // -------------------------------------------------
            // FIND CURRENT PAGE
            // -------------------------------------------------

            if (
                rule.pageIndex !=
                currentPage
            )
            {
                continue;
            }

            // -------------------------------------------------
            // CHECK CANVAS ASSIGNMENT
            // -------------------------------------------------

            if (rule.canvasParent == null)
            {
                Debug.LogWarning(
                    "[Identification] Canvas Parent is not assigned for Page " +
                    currentPage,
                    this
                );

                return null;
            }

            return rule.canvasParent;
        }

        // -----------------------------------------------------
        // NO PAGE RULE FOUND
        // -----------------------------------------------------

        Debug.LogWarning(
            "[Identification] No Canvas Parent rule found for Page " +
            currentPage,
            this
        );

        return null;
    }


    // =========================================================
    // START CAMERA MOVEMENT
    // =========================================================

    private void StartCameraMovement(
        Transform destination,
        int targetIndex,
        bool showDescriptionAfterMovement
    )
    {
        if (
            mainCamera == null ||
            destination == null
        )
        {
            return;
        }

        if (cameraMoveCoroutine != null)
        {
            StopCoroutine(
                cameraMoveCoroutine
            );
        }

        cameraMoveCoroutine =
            StartCoroutine(
                MoveCameraSmoothly(
                    destination,
                    targetIndex,
                    showDescriptionAfterMovement
                )
            );
    }


    // =========================================================
    // SMOOTH CAMERA MOVEMENT
    // =========================================================

    private IEnumerator MoveCameraSmoothly(
        Transform destination,
        int targetIndex,
        bool showDescriptionAfterMovement
    )
    {
        isCameraMoving = true;

        Vector3 startPosition =
            mainCamera.transform.position;

        Quaternion startRotation =
            mainCamera.transform.rotation;

        Vector3 targetPosition =
            destination.position;

        Quaternion targetRotation =
            destination.rotation;

        float elapsed = 0f;

        while (
            elapsed <
            cameraMoveDuration
        )
        {
            if (
                mainCamera == null ||
                destination == null
            )
            {
                isCameraMoving = false;

                yield break;
            }

            elapsed +=
                Time.deltaTime;

            float normalizedTime =
                Mathf.Clamp01(
                    elapsed /
                    cameraMoveDuration
                );

            float curveValue =
                cameraMovementCurve.Evaluate(
                    normalizedTime
                );

            mainCamera.transform.position =
                Vector3.Lerp(
                    startPosition,
                    targetPosition,
                    curveValue
                );

            mainCamera.transform.rotation =
                Quaternion.Slerp(
                    startRotation,
                    targetRotation,
                    curveValue
                );

            yield return null;
        }

        // -----------------------------------------------------
        // FORCE FINAL POSITION
        // -----------------------------------------------------

        if (mainCamera != null)
        {
            mainCamera.transform.position =
                targetPosition;

            mainCamera.transform.rotation =
                targetRotation;
        }

        isCameraMoving = false;

        cameraMoveCoroutine = null;

        // -----------------------------------------------------
        // SHOW DESCRIPTION
        // -----------------------------------------------------

        if (
            showDescriptionAfterMovement &&
            targetIndex >= 0
        )
        {
            ShowDescriptionUI(
                targetIndex
            );
        }
    }


    // =========================================================
    // SHOW COMMON DESCRIPTION UI
    // =========================================================

    private void ShowDescriptionUI(
        int index
    )
    {
        if (
            index < 0 ||
            index >= correctTargets.Count
        )
        {
            return;
        }

        IdentificationTarget target =
            correctTargets[index];

        if (target == null)
            return;

        // -----------------------------------------------------
        // TITLE
        // -----------------------------------------------------

        if (titleText != null)
        {
            titleText.text =
                target.title;
        }

        // -----------------------------------------------------
        // DESCRIPTION
        // -----------------------------------------------------

        if (descriptionText != null)
        {
            descriptionText.text =
                target.description;
        }

        // -----------------------------------------------------
        // SHOW UI
        // -----------------------------------------------------

        if (descriptionUI != null)
        {
            descriptionUI.SetActive(true);
        }
    }


    // =========================================================
    // BACK BUTTON
    // =========================================================

    public void GoBackToDefaultCamera()
    {
        // -----------------------------------------------------
        // HIDE DESCRIPTION UI
        // -----------------------------------------------------

        if (descriptionUI != null)
        {
            descriptionUI.SetActive(false);
        }

        if (titleText != null)
        {
            titleText.text = "";
        }

        if (descriptionText != null)
        {
            descriptionText.text = "";
        }

        currentTargetIndex = -1;


        // -----------------------------------------------------
        // CHECK DEFAULT CAMERA
        // -----------------------------------------------------

        if (
            defaultCameraPoint == null
        )
        {
            Debug.LogWarning(
                "[Identification] Default Camera Point is not assigned.",
                this
            );

            return;
        }


        // -----------------------------------------------------
        // MOVE CAMERA BACK
        // -----------------------------------------------------

        StartCameraMovement(
            defaultCameraPoint,
            -1,
            false
        );
    }


    // =========================================================
    // CALCULATE POPUP POSITION
    // =========================================================

    private Vector3 CalculateTopPosition(
        Collider col
    )
    {
        Bounds bounds =
            col.bounds;

        return new Vector3(
            bounds.center.x,
            bounds.max.y +
            yOffsetDistance,
            bounds.center.z
        );
    }


    // =========================================================
    // SPAWN POPUP
    // =========================================================

    private void SpawnPopUpEffect(
        GameObject prefab,
        Vector3 worldPosition
    )
    {
        if (prefab == null)
        {
            Debug.LogWarning(
                "[Identification] Popup prefab is missing.",
                this
            );

            return;
        }

        // -----------------------------------------------------
        // GET CURRENT PAGE
        // -----------------------------------------------------

        int currentPage =
            PageNavigationController.CurrentIndex;


        // -----------------------------------------------------
        // GET PAGE-SPECIFIC CANVAS
        // -----------------------------------------------------

        Transform canvasParent =
            GetCanvasParentForCurrentPage();


        // -----------------------------------------------------
        // CREATE POPUP
        // -----------------------------------------------------

        GameObject instance;

        if (canvasParent != null)
        {
            // ---------------------------------------------
            // SPAWN UNDER CURRENT PAGE CANVAS
            // ---------------------------------------------

            instance =
                Instantiate(
                    prefab,
                    canvasParent
                );
        }
        else
        {
            // ---------------------------------------------
            // FALLBACK
            // ---------------------------------------------

            instance =
                Instantiate(prefab);

            Debug.LogWarning(
                "[Identification] Popup spawned without Canvas Parent for Page " +
                currentPage,
                this
            );
        }


        // -----------------------------------------------------
        // SET WORLD POSITION
        // -----------------------------------------------------

        instance.transform.position =
            worldPosition;


        // -----------------------------------------------------
        // FACE CAMERA
        // -----------------------------------------------------

        if (mainCamera != null)
        {
            instance.transform.rotation =
                mainCamera.transform.rotation;
        }


        // -----------------------------------------------------
        // ADD TO ACTIVE POPUPS
        // -----------------------------------------------------

        activePopups.Add(
            instance.transform
        );


        // -----------------------------------------------------
        // PLAY POPUP ANIMATION
        // -----------------------------------------------------

        StartCoroutine(
            PopUpAnimation(
                instance.transform
            )
        );
    }


    // =========================================================
    // POPUP ANIMATION
    // =========================================================

    private IEnumerator PopUpAnimation(
        Transform targetTransform
    )
    {
        if (targetTransform == null)
            yield break;

        float duration = 0.3f;

        float elapsed = 0f;


        // -----------------------------------------------------
        // CALCULATE FINAL SCALE
        // -----------------------------------------------------

        Vector3 targetScale =
            Vector3.Scale(
                targetTransform.localScale,
                spawnScaleMultiplier
            );


        // -----------------------------------------------------
        // START FROM ZERO
        // -----------------------------------------------------

        targetTransform.localScale =
            Vector3.zero;


        // -----------------------------------------------------
        // ANIMATION
        // -----------------------------------------------------

        while (
            elapsed <
            duration
        )
        {
            if (targetTransform == null)
            {
                yield break;
            }

            elapsed +=
                Time.deltaTime;

            float t =
                Mathf.Clamp01(
                    elapsed /
                    duration
                );

            float smoothValue =
                Mathf.SmoothStep(
                    0f,
                    1f,
                    t
                );

            targetTransform.localScale =
                Vector3.Lerp(
                    Vector3.zero,
                    targetScale,
                    smoothValue
                );

            yield return null;
        }


        // -----------------------------------------------------
        // FORCE FINAL SCALE
        // -----------------------------------------------------

        if (targetTransform != null)
        {
            targetTransform.localScale =
                targetScale;
        }
    }
}