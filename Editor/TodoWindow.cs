using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using static UnityTodo.GUIStyles;
using static UnityTodo.IOUtils;

namespace UnityTodo
{
    internal class TodoWindow : EditorWindow
    {
        [MenuItem("Window/Tasks")]
        static void OpenWindow()
        {
            var window = GetWindow<TodoWindow>();
            window.titleContent = new GUIContent("Tasks");
            window.Show();
        }

        public List<(TaskList taskList, Editor editor)> taskEditors;
        [SerializeField] Vector2 mainScrollPos;
        [SerializeField] List<TaskListDirectory> taskListPaths;

        [Serializable]
        struct TaskListDirectory
        {
            public string path;
            public string name;

            public TaskListDirectory(string directory)
            {
                path = directory;
                name = new DirectoryInfo(directory).Name;
            }
        }

        void OnDestroy()
        {
            forceSaveAllTaskEditors();
            foreach (var taskEditor in taskEditors) DestroyImmediate(taskEditor.editor);
        }

        public TaskList GetNextTaskList(TaskList taskList)
        {
            var index = taskEditors.FindIndex(t => t.taskList == taskList);
            if (index == -1) throw new Exception("Task List not found");
            return index < taskEditors.Count - 1 ? taskEditors[index + 1].taskList : taskEditors[index].taskList;
        }

        public TaskList GetPrevTaskList(TaskList taskList)
        {
            var index = taskEditors.FindIndex(t => t.taskList == taskList);
            if (index == -1) throw new Exception("Task List not found");
            return index > 0 ? taskEditors[index - 1].taskList : taskEditors[index].taskList;
        }

        public void SortTaskLists()
        {
            taskEditors.Sort((t1, t2) =>
            {
                var c = t1.taskList.order.CompareTo(t2.taskList.order);
                if (c == 0)
                { // compare by task list directory index
                    var p1 = AssetDatabase.GetAssetPath(t1.taskList);
                    var i1 = taskListPaths.FindIndex(t => Directory.GetFiles(t.path, "*.asset", SearchOption.TopDirectoryOnly).Any(p => p == p1));
                    var p2 = AssetDatabase.GetAssetPath(t2.taskList);
                    var i2 = taskListPaths.FindIndex(t => Directory.GetFiles(t.path, "*.asset", SearchOption.TopDirectoryOnly).Any(p => p == p2));
                    c = i1.CompareTo(i2);
                }
                return c;
            });
        }


        void OnGUI()
        {
            ensureTaskListPathsLoaded();
            ensureTasksLoaded();
            drawToolbar();

            GUILayout.Space(5);

            // horizontal scroll wheel
            if (Event.current.type == EventType.ScrollWheel && Event.current.modifiers == EventModifiers.Shift)
            {
                mainScrollPos += new Vector2(Event.current.delta.y * Settings.HORIZONTAL_SCROLL_SPEED, 0);
                Event.current.Use();
                Repaint();
            }

            using var scroll = new GUILayout.ScrollViewScope(mainScrollPos);
            mainScrollPos = scroll.scrollPosition;


            if (taskEditors.Count > 0)
            {
                using (new GUILayout.HorizontalScope())
                {
                    for (var i = 0; i < taskEditors.Count; i++)
                    {
                        var task = taskEditors[i];
                        drawTask(task);
                        if (!task.taskList) { taskEditors.RemoveAt(i--); }
                    }

                    using (new GUILayout.VerticalScope())
                    {
                        foreach (var path in taskListPaths)
                        {
                            var label = taskListPaths.Count == 1 ? " New Task List" : $" New Task List <i>({path.name})</i>";
                            if (GUILayout.Button(new GUIContent(label, TodoWindow_GetNewTaskListTex()),
                                    TodoWindow_GetNewTaskList(),
                                    GUILayout.Height(30)))
                            {
                                addNewTaskList(path.path);
                            }
                        }
                        // Repaint();
                    }
                }

            }
            else
            {
                GUILayout.Space(100);
                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.FlexibleSpace();
                    drawWelcome();
                    GUILayout.FlexibleSpace();
                }
                GUILayout.FlexibleSpace();
            }

        }

        void RepaintAll()
        {
            Repaint();
            foreach (var taskEditor in taskEditors)
                ((TaskList.Editor)taskEditor.editor).RepaintList();
        }

        void drawWelcome()
        {
            using (new GUILayout.VerticalScope())
            {
                GUILayout.Label("Let's Get You Started!", GetBigLabel());
                GUILayout.Space(30);
                GUILayout.Label(
                    "You're seeing this because no Task List Directory is open. You can use the bellow buttons to get started.",
                    GetNormalLabel());
                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("Open A Task List Directory", GUILayout.Height(30)))
                    {
                        OpenTaskListSelectionGenericMenu();
                    }

                    if (GUILayout.Button("Open All Available Task List Directories", GUILayout.Height(30)))
                    {
                        OpenAllTaskListDirectories();
                    }

                    GUILayout.FlexibleSpace();
                }
            }
        }

        void drawToolbar()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {

                if (GUILayout.Button(
                        new GUIContent("Save", EditorGUIUtility.FindTexture("SaveActive")),
                        EditorStyles.toolbarButton,
                        GUILayout.ExpandWidth(false)))
                {
                    forceSaveAllTaskEditors();
                }

                if (GUILayout.Button(
                        new GUIContent("Reload", EditorGUIUtility.FindTexture("RotateTool")),
                        EditorStyles.toolbarButton,
                        GUILayout.ExpandWidth(false)))
                {
                    forceReloadAllTaskEditors();
                    AssetDatabase.SaveAssets();
                }

                if (GUILayout.Button(
                        new GUIContent("Sort", EditorGUIUtility.FindTexture("AlphabeticalSorting")),
                        EditorStyles.toolbarButton,
                        GUILayout.ExpandWidth(false)))
                {
                    foreach (var taskEditor in taskEditors)
                    {
                        Undo.RecordObject(taskEditor.taskList, "Sort Tasks");
                        taskEditor.editor.serializedObject.ApplyModifiedProperties();
                        taskEditor.taskList.tasks = taskEditor.taskList.tasks.OrderBy(t => t.progress >= 1 ? 1 : -t.progress).ToList();
                        taskEditor.editor.serializedObject.Update();
                    }
                    RepaintAll();
                }

                GUILayout.Space(20);
                drawDirectorySelection();

            }
        }

        void drawDirectorySelection()
        {

            GUILayout.Label(new GUIContent("Directories:", TodoWindow_GetTaskListDirectoriesTex()), GUILayout.Width(80), GUILayout.Height(20));

            using (var scope = new EditorGUILayout.HorizontalScope(EditorStyles.selectionRect, GUILayout.ExpandWidth(false)))
            {

                for (var i = 0; i < taskListPaths.Count; i++)
                {
                    if (!Directory.Exists(taskListPaths[i].path))
                    {
                        taskListPaths.RemoveAt(i--);
                        continue;
                    }

                    if (GUILayout.Button(new GUIContent(taskListPaths[i].name), TodoWindow_GetTaskListPathItem(), GUILayout.ExpandWidth(false)))
                    {
                        taskListPaths.RemoveAt(i);
                        forceSaveAllTaskEditors();
                        forceReloadAllTaskEditors();
                        SaveActiveDirectoriesToPrefs(taskListPaths.Select(t => t.path).ToList());
                        return;
                    }
                }

                if (GUILayout.Button(new GUIContent(TodoWindow_GetNewTaskListDirTex(), "Add new directory of Task Lists to show"), EditorStyles.miniButton,
                        GUILayout.Width(25)))
                {
                    OpenTaskListSelectionGenericMenu();
                }
            }
        }

        void OpenTaskListSelectionGenericMenu()
        {
            var menu = new GenericMenu();
            foreach (var directory in FindAllDirectoriesWithTaskList())
            {
                menu.AddItem(new GUIContent(directory), taskListPaths.Any(t => t.path == directory), () =>
                {
                    if (taskListPaths.All(t => t.path != directory))
                    {
                        taskListPaths.Add(new(directory));
                        SaveActiveDirectoriesToPrefs(taskListPaths.Select(t => t.path).ToList());
                        forceSaveAllTaskEditors();
                        forceReloadAllTaskEditors();
                    }
                });
            }
            menu.AddItem(new GUIContent("New"), false, () =>
            {
                var path = EditorUtility.OpenFolderPanel("Select Task List Directory", Application.dataPath, "");
                if (Directory.Exists(path))
                {
                    forceSaveAllTaskEditors();
                    path = path.Replace(Application.dataPath, "Assets"); // make path relative
                    taskListPaths.Add(new(path));
                    SaveActiveDirectoriesToPrefs(taskListPaths.Select(t => t.path).ToList());
                    addNewTaskList(path);
                }
            });

            menu.ShowAsContext();
        }

        void OpenAllTaskListDirectories()
        {
            forceSaveAllTaskEditors();
            foreach (var dir in FindAllDirectoriesWithTaskList())
                taskListPaths.Add(new TaskListDirectory(dir));
            SaveActiveDirectoriesToPrefs(taskListPaths.Select(t => t.path).ToList());
            forceReloadAllTaskEditors();
        }

        void drawTask((TaskList taskList, Editor editor) task)
        {
            using (new GUILayout.VerticalScope(EditorStyles.helpBox, GUILayout.Width(300)))
            {
                task.editor.OnInspectorGUI();
            }
        }

        void addNewTaskList(string path)
        {
            forceSaveAllTaskEditors();
            var newTaskList = CreateInstance<TaskList>();
            newTaskList.order = taskEditors.Count > 0 ? taskEditors.Max(task => task.taskList.order) + 1 : 0;
            newTaskList.name = "New Task " + newTaskList.order;
            Directory.CreateDirectory(path);
            AssetDatabase.CreateAsset(newTaskList, Path.Combine(path, newTaskList.name + ".asset"));
            AssetDatabase.SaveAssets();
            forceReloadAllTaskEditors();
        }

        void forceSaveAllTaskEditors()
        {
            for (var i = 0; i < taskEditors.Count; i++)
            {
                var taskEditor = taskEditors[i];
                foreach (var task in taskEditor.taskList.tasks) task.isEditing = false;

                if (taskEditor.editor)
                {
                    taskEditor.editor.serializedObject.ApplyModifiedProperties();
                    if (taskEditor.taskList.name != taskEditor.taskList.title)
                    {
                        AssetDatabase.RenameAsset(AssetDatabase.GetAssetPath(taskEditor.taskList), taskEditor.taskList.title);

                        // notice for dot in asset file name issue
                        if (!string.IsNullOrEmpty(taskEditor.taskList.title) && taskEditor.taskList.title.Contains("."))
                        {
                            Debug.Log($"If you got warning about Object name not matching the file name, it's because you have a dot (.) in your TaskList title, " +
                                       $"and Unity's AssetDatabase doesn't work well with that sort of name. Solutions:\n" +
                                       $"1. Select the file and click on Fix Object Name button in the inspector.\n" +
                                       $"2. Don't use dots (.) in your TaskList title. \n\n" +
                                       $"If this is making the usage experience too poorly, consider contacting me about it to prioritize fixing this issue. Thanks.",
                                taskEditor.taskList);
                        }
                    }
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        void forceReloadAllTaskEditors()
        {
            // reimport path
            AssetDatabase.Refresh();
            taskEditors =
                taskListPaths
                .SelectMany(tpath => GetAllTaskListsAtPath(tpath.path))
                .Select(AssetDatabase.LoadAssetAtPath<TaskList>)
                .OrderBy(task => task.order)
                .Select(task => (task, Editor.CreateEditor(task)))
                .ToList();
        }

        void ensureTaskListPathsLoaded()
        {
            if (taskListPaths == null)
            {
                taskListPaths = GetActiveDirectoriesFromPrefs().Select(s => new TaskListDirectory(s)).ToList();
                Repaint();
            }
        }

        void ensureTasksLoaded()
        {
            if (taskEditors == null) forceReloadAllTaskEditors();
        }
    }
}