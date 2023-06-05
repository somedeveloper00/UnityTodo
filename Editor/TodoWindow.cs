using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace UnityTodo {
    internal class TodoWindow : EditorWindow {
        
        [MenuItem( "Window/Tasks" )]
        static void OpenWindow() {
            var window = GetWindow<TodoWindow>();
            window.titleContent = new GUIContent( "Tasks" );
            window.Show();
        }

        [NonSerialized] List<(TaskList taskList, Editor editor)> taskEditors;
        [NonSerialized] Vector2 mainScrollPos;
        List<TaskListDirectory> taskListPaths = new();

        [Serializable]
        struct TaskListDirectory {
            public string path;
            public string name;

            public TaskListDirectory(string directory, string substring) {
                path = directory;
                name = substring;
            }
        }

        void OnDestroy() {
            forceSaveAllTaskEditors();
            foreach (var taskEditor in taskEditors) DestroyImmediate( taskEditor.editor );
        }

        void OnGUI() {
            ensureTasksLoaded();
            drawToolbar();
            
            // horizontal scroll wheel
            if (Event.current.type == EventType.ScrollWheel && Event.current.modifiers == EventModifiers.Shift) {
                mainScrollPos += new Vector2( Event.current.delta.y * Settings.HORIZONTAL_SCROLL_SPEED, 0 );
                Event.current.Use();
                Repaint();
            }
            
            using var scroll = new GUILayout.ScrollViewScope( mainScrollPos );
            mainScrollPos = scroll.scrollPosition;
            

            using (new GUILayout.HorizontalScope()) {
                for (var i = 0; i < taskEditors.Count; i++) {
                    var task = taskEditors[i];
                    drawTask( task );
                    if (!task.taskList) {
                        taskEditors.RemoveAt( i );
                        AssetDatabase.DeleteAsset( AssetDatabase.GetAssetPath( task.taskList ) );
                        AssetDatabase.Refresh();
                    }
                }

                if (GUILayout.Button( new GUIContent(" New Task List", EditorGUIUtility.FindTexture( "d_CreateAddNew" )),
                        GUILayout.Height( 30 ) )) 
                {
                    addNewTaskList();
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
            
            GUILayout.Label( new GUIContent("Directories:", GUIStyles.TodoWindow_GetTaskListDirectoriesTex()), GUILayout.Width( 80 ), GUILayout.Height( 20 ) );
            
            using (var scope = new EditorGUILayout.HorizontalScope( EditorStyles.selectionRect, GUILayout.ExpandWidth( false ) )) {
                
                for (var i = 0; i < taskListPaths.Count; i++) {
                    if (!Directory.Exists( taskListPaths[i].path )) {
                        taskListPaths.RemoveAt( i-- );
                        continue;
                    }
                    
                    if (GUILayout.Button( new GUIContent( taskListPaths[i].name ), GUIStyles.TodoWindow_GetTaskListPathItem(), GUILayout.ExpandWidth( false ) )) {
                        taskListPaths.RemoveAt( i );
                        forceSaveAllTaskEditors();
                        forceReloadAllTaskEditors();
                        return;
                    }
                }

                if (GUILayout.Button(
                        new GUIContent( EditorGUIUtility.FindTexture( "d_icon dropdown" ),
                            "Add new directory of Task Lists to show" ), EditorStyles.miniButton, GUILayout.Width( 25 ) )) 
                {
                    var menu = new GenericMenu();
                    foreach (var directory in IOUtils.FindAllDirectoriesWithTaskList()) {
                        menu.AddItem( new GUIContent( directory ), taskListPaths.Any( t => t.path == directory ), () => {
                            if (taskListPaths.All( t => t.path != directory )) {
                                taskListPaths.Add( new (directory, new DirectoryInfo(directory).Name ) );
                                forceSaveAllTaskEditors();
                                forceReloadAllTaskEditors();
                            }
                        } );
                    }
                    menu.ShowAsContext();
                }
            }
        }

        void drawTask((TaskList taskList, Editor editor) task) {
            using (new GUILayout.VerticalScope( EditorStyles.helpBox, GUILayout.Width( 300 ) )) {
                task.editor.OnInspectorGUI();
            }
        }

        void addNewTaskList() {
            forceSaveAllTaskEditors();
            var newTaskList = CreateInstance<TaskList>();
            newTaskList.order = taskEditors.Count > 0 ? taskEditors.Max( task => task.taskList.order ) + 1 : 0;
            newTaskList.name = "New Task " + newTaskList.order;
            Directory.CreateDirectory( Settings.TODO_DIRECTORY_PATH );
            AssetDatabase.CreateAsset( newTaskList, Path.Combine( Settings.TODO_DIRECTORY_PATH, newTaskList.name + ".asset" ) );
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
                .SelectMany( tpath => IOUtils.GetAllTaskListsAtPath(tpath.path) )
                .Select( AssetDatabase.LoadAssetAtPath<TaskList> )
                .OrderBy( task => task.order )
                .Select( task => (task, Editor.CreateEditor( task )) )
                .ToList();
        }
        
        void ensureTasksLoaded() {
            if (taskEditors == null) forceReloadAllTaskEditors();
        }
    }
}