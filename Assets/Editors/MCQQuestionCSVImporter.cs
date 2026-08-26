using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.Reflection;

public class MCQQuestionCSVImporter : EditorWindow
{
    // ============================================================
    // SETTINGS
    // ============================================================

    private TextAsset csvFile;

    private GameObject managerTemplate;

    private string scriptableObjectFolder =
        "Assets/MCQQuestions";

    // ============================================================
    // MENU
    // ============================================================

    [MenuItem("Evaluation/MCQ CSV Importer")]
    public static void ShowWindow()
    {
        GetWindow<MCQQuestionCSVImporter>(
            "MCQ CSV Importer"
        );
    }

    // ============================================================
    // GUI
    // ============================================================

    private void OnGUI()
    {
        GUILayout.Space(10);

        EditorGUILayout.LabelField(
            "MCQ CSV → ScriptableObject + Manager Generator",
            EditorStyles.boldLabel
        );

        GUILayout.Space(15);

        // ========================================================
        // CSV FILE
        // ========================================================

        EditorGUILayout.LabelField(
            "CSV Settings",
            EditorStyles.boldLabel
        );

        csvFile = (TextAsset)EditorGUILayout.ObjectField(
            "CSV File",
            csvFile,
            typeof(TextAsset),
            false
        );

        if (csvFile != null)
        {
            string csvPath =
                AssetDatabase.GetAssetPath(csvFile);

            if (!csvPath.EndsWith(
                ".csv",
                StringComparison.OrdinalIgnoreCase))
            {
                EditorGUILayout.HelpBox(
                    "Please select a CSV file.",
                    MessageType.Error
                );
            }
            else
            {
                EditorGUILayout.HelpBox(
                    "Selected CSV:\n" + csvPath,
                    MessageType.Info
                );
            }
        }

        GUILayout.Space(15);

        // ========================================================
        // TEMPLATE GAMEOBJECT
        // ========================================================

        EditorGUILayout.LabelField(
            "Manager Template",
            EditorStyles.boldLabel
        );

        managerTemplate = (GameObject)EditorGUILayout.ObjectField(
            "Template GameObject",
            managerTemplate,
            typeof(GameObject),
            true
        );

        if (managerTemplate != null)
        {
            EditorGUILayout.HelpBox(
                "The referenced GameObject will be duplicated " +
                "for every question.\n\n" +
                "Generated name format:\n" +
                managerTemplate.name + " 1\n" +
                managerTemplate.name + " 2\n" +
                managerTemplate.name + " 3\n" +
                "...\n\n" +
                "The importer will automatically find the " +
                "MCQQuestionData field in its components.",
                MessageType.Info
            );
        }

        GUILayout.Space(15);

        // ========================================================
        // SCRIPTABLE OBJECT SETTINGS
        // ========================================================

        EditorGUILayout.LabelField(
            "ScriptableObject Settings",
            EditorStyles.boldLabel
        );

        scriptableObjectFolder =
            EditorGUILayout.TextField(
                "SO Output Folder",
                scriptableObjectFolder
            );

        GUILayout.Space(15);

        // ========================================================
        // IMPORT BUTTON
        // ========================================================

        bool validCSVFile =
            csvFile != null &&
            AssetDatabase.GetAssetPath(csvFile)
                .EndsWith(
                    ".csv",
                    StringComparison.OrdinalIgnoreCase
                );

        bool validTemplate =
            managerTemplate != null;

        GUI.enabled =
            validCSVFile &&
            validTemplate;

        if (GUILayout.Button(
            "CREATE MCQ DATA + DUPLICATE MANAGERS",
            GUILayout.Height(45)))
        {
            ImportAll();
        }

        GUI.enabled = true;

        GUILayout.Space(15);

        EditorGUILayout.HelpBox(
            "CSV format:\n\n" +
            "QuestionText,Option0,Option1,Option2," +
            "Option3,CorrectOptionIndex,ExplanationText\n\n" +
            "One ScriptableObject and one duplicated Manager " +
            "will be created for every question row.",
            MessageType.Info
        );
    }

    // ============================================================
    // MAIN IMPORT
    // ============================================================

    private void ImportAll()
    {
        if (csvFile == null)
        {
            EditorUtility.DisplayDialog(
                "Error",
                "Please select a CSV file.",
                "OK"
            );

            return;
        }

        string csvAssetPath =
            AssetDatabase.GetAssetPath(csvFile);

        if (!csvAssetPath.EndsWith(
            ".csv",
            StringComparison.OrdinalIgnoreCase))
        {
            EditorUtility.DisplayDialog(
                "Error",
                "The selected file is not a CSV file.",
                "OK"
            );

            return;
        }

        if (managerTemplate == null)
        {
            EditorUtility.DisplayDialog(
                "Error",
                "Please select the Manager Template GameObject.",
                "OK"
            );

            return;
        }

        CreateFolderIfNeeded(
            scriptableObjectFolder
        );

        int totalCreated = 0;
        int totalSkipped = 0;

        ImportCSV(
            csvFile,
            ref totalCreated,
            ref totalSkipped
        );

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog(
            "Import Complete",

            "MCQ Import Completed!\n\n" +
            "CSV File:\n" +
            csvAssetPath +
            "\n\n" +
            "Created: " +
            totalCreated +
            "\n" +
            "Skipped: " +
            totalSkipped +
            "\n\n" +
            "ScriptableObjects:\n" +
            scriptableObjectFolder,

            "OK"
        );
    }

    // ============================================================
    // IMPORT CSV
    // ============================================================

    private void ImportCSV(
        TextAsset csv,
        ref int createdCount,
        ref int skippedCount)
    {
        if (csv == null)
        {
            Debug.LogError(
                "CSV file is null."
            );

            return;
        }

        string[] lines =
            csv.text.Split(
                new[] { "\r\n", "\n", "\r" },
                StringSplitOptions.RemoveEmptyEntries
            );

        if (lines.Length <= 1)
        {
            Debug.LogWarning(
                "CSV contains no question data: " +
                csv.name
            );

            return;
        }

        // Row 0 = Header
        // Start from row 1

        for (int row = 1; row < lines.Length; row++)
        {
            string line = lines[row];

            if (string.IsNullOrWhiteSpace(line))
                continue;

            List<string> columns;

            try
            {
                columns = ParseCSVLine(line);
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    "CSV parsing error at row " +
                    (row + 1) +
                    ": " +
                    exception.Message
                );

                skippedCount++;
                continue;
            }

            // ====================================================
            // CHECK COLUMN COUNT
            // ====================================================

            if (columns.Count < 7)
            {
                Debug.LogWarning(
                    "Skipping row " +
                    (row + 1) +
                    ". Expected 7 columns but found " +
                    columns.Count
                );

                skippedCount++;
                continue;
            }

            // ====================================================
            // READ DATA
            // ====================================================

            string questionText =
                columns[0];

            string option0 =
                columns[1];

            string option1 =
                columns[2];

            string option2 =
                columns[3];

            string option3 =
                columns[4];

            string correctIndexText =
                columns[5];

            string explanationText =
                columns[6];

            // ====================================================
            // CORRECT ANSWER
            // ====================================================

            int correctOptionIndex;

            if (!int.TryParse(
                correctIndexText,
                out correctOptionIndex))
            {
                Debug.LogWarning(
                    "Invalid CorrectOptionIndex at row " +
                    (row + 1)
                );

                skippedCount++;
                continue;
            }

            if (
                correctOptionIndex < 0 ||
                correctOptionIndex > 3
            )
            {
                Debug.LogWarning(
                    "CorrectOptionIndex must be between " +
                    "0 and 3 at row " +
                    (row + 1)
                );

                skippedCount++;
                continue;
            }

            // ====================================================
            // CREATE SCRIPTABLE OBJECT
            // ====================================================

            MCQQuestionData questionData =
                ScriptableObject.CreateInstance<MCQQuestionData>();

            questionData.questionText =
                questionText;

            questionData.options =
                new string[]
                {
                    option0,
                    option1,
                    option2,
                    option3
                };

            questionData.correctOptionIndex =
                correctOptionIndex;

            questionData.explanationText =
                explanationText;

            questionData.referenceImage =
                null;

            // ====================================================
            // CREATE ASSET
            // ====================================================

            string assetName =
                "MCQ_" +
                (createdCount + 1).ToString("000");

            string assetPath =
                scriptableObjectFolder +
                "/" +
                assetName +
                ".asset";

            assetPath =
                AssetDatabase.GenerateUniqueAssetPath(
                    assetPath
                );

            AssetDatabase.CreateAsset(
                questionData,
                assetPath
            );

            // ====================================================
            // DUPLICATE MANAGER
            // ====================================================

            GameObject duplicatedManager =
                DuplicateManager();

            if (duplicatedManager == null)
            {
                Debug.LogError(
                    "Failed to duplicate Manager for " +
                    assetName
                );

                UnityEngine.Object.DestroyImmediate(
                    questionData
                );

                skippedCount++;
                continue;
            }

            // ====================================================
            // RENAME
            // ====================================================

            duplicatedManager.name =
                managerTemplate.name +
                " " +
                (createdCount + 1).ToString();

            // ====================================================
            // ASSIGN SCRIPTABLE OBJECT
            // ====================================================

            bool assigned =
                AssignQuestionData(
                    duplicatedManager,
                    questionData
                );

            if (!assigned)
            {
                Debug.LogError(
                    "MCQQuestionData field was not found in " +
                    duplicatedManager.name
                );

                EditorUtility.DisplayDialog(
                    "MCQQuestionData Not Found",

                    "Could not find an MCQQuestionData field " +
                    "inside:\n\n" +
                    duplicatedManager.name +
                    "\n\n" +
                    "Make sure your Manager contains a field like:\n\n" +
                    "public MCQQuestionData questionData;",

                    "OK"
                );
            }

            EditorUtility.SetDirty(
                duplicatedManager
            );

            createdCount++;

            Debug.Log(
                "Created MCQ " +
                createdCount +
                " : " +
                assetName +
                " → " +
                duplicatedManager.name
            );
        }
    }

    // ============================================================
    // DUPLICATE MANAGER
    // ============================================================

    private GameObject DuplicateManager()
    {
        if (managerTemplate == null)
            return null;

        Transform parent =
            managerTemplate.transform.parent;

        GameObject duplicate;

        if (parent != null)
        {
            duplicate =
                Instantiate(
                    managerTemplate,
                    parent
                );
        }
        else
        {
            duplicate =
                Instantiate(
                    managerTemplate
                );
        }

        Undo.RegisterCreatedObjectUndo(
            duplicate,
            "Create MCQ Manager"
        );

        return duplicate;
    }

    // ============================================================
    // ASSIGN MCQ QUESTION DATA
    // ============================================================

    private bool AssignQuestionData(
        GameObject target,
        MCQQuestionData questionData)
    {
        if (target == null)
            return false;

        Component[] components =
            target.GetComponentsInChildren<Component>(
                true
            );

        foreach (Component component in components)
        {
            if (component == null)
                continue;

            Type componentType =
                component.GetType();

            FieldInfo[] fields =
                componentType.GetFields(
                    BindingFlags.Instance |
                    BindingFlags.Public |
                    BindingFlags.NonPublic
                );

            foreach (FieldInfo field in fields)
            {
                if (
                    field.FieldType ==
                    typeof(MCQQuestionData)
                )
                {
                    field.SetValue(
                        component,
                        questionData
                    );

                    EditorUtility.SetDirty(
                        component
                    );

                    Debug.Log(
                        "Assigned " +
                        questionData.name +
                        " to " +
                        componentType.Name +
                        "." +
                        field.Name
                    );

                    return true;
                }
            }
        }

        return false;
    }

    // ============================================================
    // CSV PARSER
    // ============================================================

    private List<string> ParseCSVLine(
        string line)
    {
        List<string> result =
            new List<string>();

        bool insideQuotes = false;

        string currentValue = "";

        for (
            int i = 0;
            i < line.Length;
            i++)
        {
            char character =
                line[i];

            // ====================================================
            // QUOTE
            // ====================================================

            if (character == '"')
            {
                if (
                    insideQuotes &&
                    i + 1 < line.Length &&
                    line[i + 1] == '"'
                )
                {
                    currentValue += '"';

                    i++;

                    continue;
                }

                insideQuotes =
                    !insideQuotes;

                continue;
            }

            // ====================================================
            // COMMA
            // ====================================================

            if (
                character == ',' &&
                !insideQuotes
            )
            {
                result.Add(
                    currentValue.Trim()
                );

                currentValue = "";

                continue;
            }

            // ====================================================
            // NORMAL CHARACTER
            // ====================================================

            currentValue +=
                character;
        }

        result.Add(
            currentValue.Trim()
        );

        return result;
    }

    // ============================================================
    // CREATE FOLDER
    // ============================================================

    private void CreateFolderIfNeeded(
        string folderPath)
    {
        if (
            AssetDatabase.IsValidFolder(
                folderPath
            )
        )
        {
            return;
        }

        string[] folders =
            folderPath.Split('/');

        string currentPath =
            folders[0];

        for (
            int i = 1;
            i < folders.Length;
            i++)
        {
            string nextPath =
                currentPath +
                "/" +
                folders[i];

            if (
                !AssetDatabase.IsValidFolder(
                    nextPath
                )
            )
            {
                AssetDatabase.CreateFolder(
                    currentPath,
                    folders[i]
                );
            }

            currentPath =
                nextPath;
        }

        AssetDatabase.Refresh();
    }
}