using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Codice.Client.Common.WebApi;
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

        void OnDestroy() {
            forceSaveAllTaskEditors();
            foreach (var taskEditor in taskEditors) DestroyImmediate( taskEditor.editor );
        }

        void OnGUI() {
            ensureTasksLoaded();

            drawToolbar();
            
            // horizontal scroll wheel
            if (Event.current.type == EventType.ScrollWheel && Event.current.modifiers == EventModifiers.Shift) {
                mainScrollPos += new Vector2( Event.current.delta.y * 13, 0 );
                Event.current.Use();
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
            Directory.CreateDirectory( Settings.TODO_DIRECTORY_LIST_PATH );
            AssetDatabase.CreateAsset( newTaskList, Path.Combine( Settings.TODO_DIRECTORY_LIST_PATH, newTaskList.name + ".asset" ) );
            AssetDatabase.SaveAssets();
            forceReloadAllTaskEditors();
        }

        void forceSaveAllTaskEditors() {
            bool changed = false;
            foreach (var taskEditor in taskEditors) {
                foreach (var task in taskEditor.taskList.tasks) {
                    task.isEditing = false;
                }
                if (taskEditor.editor) 
                {
                    taskEditor.editor.serializedObject.ApplyModifiedProperties();
                    if (taskEditor.taskList.name != taskEditor.taskList.title) {
                        AssetDatabase.RenameAsset( AssetDatabase.GetAssetPath( taskEditor.taskList ), taskEditor.taskList.title );
                        changed = true;
                    }
                }
            }
            if (changed)
                AssetDatabase.Refresh();
        }

        void forceReloadAllTaskEditors() {
            taskEditors = Directory.GetFiles( Settings.TODO_DIRECTORY_LIST_PATH, "*.asset", SearchOption.AllDirectories )
                .Select( AssetDatabase.LoadAssetAtPath<TaskList> )
                .Where( elem => elem != null )
                .OrderBy( task => task.order )
                .Select( task => (task, Editor.CreateEditor( task )) )
                .ToList();
        }
        
        void ensureTasksLoaded() {
            if (taskEditors == null) forceReloadAllTaskEditors();
        }
    }
}