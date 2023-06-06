using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using static UnityTodo.GUIStyles;
using static UnityTodo.IOUtils;

namespace UnityTodo {
    internal class TodoWindow : EditorWindow {
        
        [MenuItem( "Window/Tasks" )]
        static void OpenWindow() {
            var window = GetWindow<TodoWindow>();
            window.titleContent = new GUIContent( "Tasks" );
            window.Show();
        }

        List<(TaskList taskList, Editor editor)> taskEditors;
        [SerializeField] Vector2 mainScrollPos;
        [SerializeField] List<TaskListDirectory> taskListPaths;

        [Serializable]
        struct TaskListDirectory {
            public string path;
            public string name;

            public TaskListDirectory(string directory) {
                path = directory;
                name = new DirectoryInfo( directory ).Name;
            }
        }

        void OnDestroy() {
            forceSaveAllTaskEditors();
            foreach (var taskEditor in taskEditors) DestroyImmediate( taskEditor.editor );
        }

        void OnGUI() {
            ensureTaskListPathsLoaded();
            ensureTasksLoaded();
            drawToolbar();
            
            GUILayout.Space( 5 );
            
            // horizontal scroll wheel
            if (Event.current.type == EventType.ScrollWheel && Event.current.modifiers == EventModifiers.Shift) {
                mainScrollPos += new Vector2( Event.current.delta.y * Settings.HORIZONTAL_SCROLL_SPEED, 0 );
                Event.current.Use();
                Repaint();
            }
            
            using var scroll = new GUILayout.ScrollViewScope( mainScrollPos );
            mainScrollPos = scroll.scrollPosition;


            if (taskEditors.Count > 0) {
                using (new GUILayout.HorizontalScope()) {
                    for (var i = 0; i < taskEditors.Count; i++) {
                        var task = taskEditors[i];
                        drawTask( task );
                        if (!task.taskList) { taskEditors.RemoveAt( i-- ); }
                    }

                    using (new GUILayout.VerticalScope()) {
                        foreach (var path in taskListPaths) {
                            var label = taskListPaths.Count == 1 ? " New Task List" : $" New Task List <i>({path.name})</i>";
                            if (GUILayout.Button( new GUIContent( label, TodoWindow_GetNewTaskListTex() ),
                                    TodoWindow_GetNewTaskList(),
                                    GUILayout.Height( 30 ) )) 
                            {
                                addNewTaskList( path.path );
                            }
                        }
                        // Repaint();
                    }
                }

            }
            else {
                GUILayout.Space(100);
                using (new GUILayout.HorizontalScope(  )) {
                    GUILayout.FlexibleSpace();
                    drawWelcome();
                    GUILayout.FlexibleSpace();
                }
                GUILayout.FlexibleSpace();
            }

        }

        void drawWelcome() {
            using (new GUILayout.VerticalScope()) {
                GUILayout.Label( "Let's Get You Started!", GetBigLabel() );
                GUILayout.Space( 30 );
                GUILayout.Label(
                    "You're seeing this because no Task List Directory is open. You can use the bellow buttons to get started.",
                    GetNormalLabel() );
                using (new GUILayout.HorizontalScope()) {
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button( "Open A Task List Directory", GUILayout.Height( 30 ) )) {
                        OpenTaskListSelectionGenericMenu();
                    }

                    if (GUILayout.Button( "Open All Available Task List Directories", GUILayout.Height( 30 ) )) {
                        OpenAllTaskListDirectories();
                    }

                    GUILayout.FlexibleSpace();
                }
            }
        }

        void drawToolbar() {
            using (new EditorGUILayout.HorizontalScope( EditorStyles.toolbar )) {

                if (GUILayout.Button(
                        new GUIContent( "Save", EditorGUIUtility.FindTexture( "SaveActive" ) ),
                        EditorStyles.toolbarButton,
                        GUILayout.ExpandWidth( false ) )) 
                {
                    forceSaveAllTaskEditors();
                }

                if (GUILayout.Button(
                        new GUIContent( "Reload", EditorGUIUtility.FindTexture( "RotateTool" ) ),
                        EditorStyles.toolbarButton,
                        GUILayout.ExpandWidth( false ) )) 
                {
                    forceReloadAllTaskEditors();
                    AssetDatabase.SaveAssets();
                }
                
                if (GUILayout.Button( 
                        new GUIContent( "Sort", EditorGUIUtility.FindTexture( "AlphabeticalSorting" ) ) , 
                        EditorStyles.toolbarButton,
                        GUILayout.ExpandWidth( false ))) 
                {
                    foreach (var taskEditor in taskEditors) {
                        Undo.RecordObject( taskEditor.taskList, "Sort Tasks" );
                        taskEditor.editor.serializedObject.ApplyModifiedProperties();
                        taskEditor.taskList.tasks.Sort( (t1, t2) => t2.progress.CompareTo( t1.progress ) );
                        taskEditor.editor.serializedObject.Update();
                    }
                }

                GUILayout.Space( 20 );
                drawDirectorySelection();

            }
        }

        void drawDirectorySelection() {
            
            GUILayout.Label( new GUIContent("Directories:", TodoWindow_GetTaskListDirectoriesTex()), GUILayout.Width( 80 ), GUILayout.Height( 20 ) );
            
            using (var scope = new EditorGUILayout.HorizontalScope( EditorStyles.selectionRect, GUILayout.ExpandWidth( false ) )) {
                
                for (var i = 0; i < taskListPaths.Count; i++) {
                    if (!Directory.Exists( taskListPaths[i].path )) {
                        taskListPaths.RemoveAt( i-- );
                        continue;
                    }
                    
                    if (GUILayout.Button( new GUIContent( taskListPaths[i].name ), TodoWindow_GetTaskListPathItem(), GUILayout.ExpandWidth( false ) )) {
                        taskListPaths.RemoveAt( i );
                        forceSaveAllTaskEditors();
                        forceReloadAllTaskEditors();
                        SaveActiveDirectories( taskListPaths.Select( t => t.path ).ToList() );
                        return;
                    }
                }

                if (GUILayout.Button( new GUIContent( TodoWindow_GetNewTaskListDirTex(), "Add new directory of Task Lists to show" ), EditorStyles.miniButton,
                        GUILayout.Width( 25 ) )) 
                {
                    OpenTaskListSelectionGenericMenu();
                }
            }
        }

        void OpenTaskListSelectionGenericMenu() {
            var menu = new GenericMenu();
            foreach (var directory in FindAllDirectoriesWithTaskList()) {
                menu.AddItem( new GUIContent( directory ), taskListPaths.Any( t => t.path == directory ), () => {
                    if (taskListPaths.All( t => t.path != directory )) {
                        taskListPaths.Add( new(directory) );
                        SaveActiveDirectories( taskListPaths.Select( t => t.path ).ToList() );
                        forceSaveAllTaskEditors();
                        forceReloadAllTaskEditors();
                    }
                } );
            }

            menu.ShowAsContext();
        }

        void OpenAllTaskListDirectories() {
            forceSaveAllTaskEditors();
            foreach (var dir in FindAllDirectoriesWithTaskList()) 
                taskListPaths.Add( new TaskListDirectory( dir ) );
            SaveActiveDirectories( taskListPaths.Select( t => t.path ).ToList() );
            forceReloadAllTaskEditors();
        }

        void drawTask((TaskList taskList, Editor editor) task) {
            using (new GUILayout.VerticalScope( EditorStyles.helpBox, GUILayout.Width( 300 ) )) {
                task.editor.OnInspectorGUI();
            }
        }

        void addNewTaskList(string path) {
            forceSaveAllTaskEditors();
            var newTaskList = CreateInstance<TaskList>();
            newTaskList.order = taskEditors.Count > 0 ? taskEditors.Max( task => task.taskList.order ) + 1 : 0;
            newTaskList.name = "New Task " + newTaskList.order;
            Directory.CreateDirectory( path );
            AssetDatabase.CreateAsset( newTaskList, Path.Combine( path, newTaskList.name + ".asset" ) );
            AssetDatabase.SaveAssets();
            forceReloadAllTaskEditors();
        }

        void forceSaveAllTaskEditors() {
            foreach (var taskEditor in taskEditors) {
                foreach (var task in taskEditor.taskList.tasks) {
                    task.isEditing = false;
                }
                if (taskEditor.editor) {
                    taskEditor.editor.serializedObject.ApplyModifiedProperties();
                    if (taskEditor.taskList.name != taskEditor.taskList.title) {
                        AssetDatabase.RenameAsset( AssetDatabase.GetAssetPath( taskEditor.taskList ), taskEditor.taskList.title );
                    }
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        void forceReloadAllTaskEditors() {
            // reimport path
            AssetDatabase.Refresh();
            AssetDatabase.ImportAsset( Settings.TODO_DIRECTORY_PATH, ImportAssetOptions.ImportRecursive );
            taskEditors =
                taskListPaths
                .SelectMany( tpath => GetAllTaskListsAtPath(tpath.path) )
                .Select( AssetDatabase.LoadAssetAtPath<TaskList> )
                .OrderBy( task => task.order )
                .Select( task => (task, Editor.CreateEditor( task )) )
                .ToList();
        }

        void ensureTaskListPathsLoaded() {
            if (taskListPaths == null) {
                taskListPaths = GetActiveDirectories().Select( s => new TaskListDirectory( s ) ).ToList();
                Repaint();
                Debug.Log( $"loaded all" );
            } 
        }
        void ensureTasksLoaded() {
            if (taskEditors == null) forceReloadAllTaskEditors();
        }
    }
}