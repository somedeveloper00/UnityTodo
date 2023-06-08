using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using static UnityTodo.GUIStyles;

namespace UnityTodo {
    internal class TaskList : ScriptableObject {
        public int order;
        public string title;
        public List<Task> tasks = new();

        public float GetProgress() => tasks.Count == 0 ? 1 : Mathf.Clamp01( tasks.Sum( t => t.progress ) / tasks.Count );

        [CustomEditor(typeof(TaskList))]
        class editor : Editor {
            
            [NonSerialized] ExposedReorderableList _list;
            [NonSerialized] Vector3 tasksScrollPos;
            [NonSerialized] bool firstTime = true;

            void OnEnable() {
                var tasksProp = serializedObject.FindProperty( nameof(tasks) );
                _list = new ExposedReorderableList( serializedObject, tasksProp, true, false, false, false );
                
                _list.drawElementCallback += (rect, index, active, focused) => {

                    var isEditingProp = _list.serializedProperty.GetArrayElementAtIndex( index )
                        .FindPropertyRelative( nameof(Task.isEditing) );
                    
                    bool ContextButtonClick() => GUI.Button( new Rect( rect.x + rect.width - 15, rect.y, 20, 20 ), TaskList_Get_TaskMenuTex(), EditorStyles.iconButton );
                    bool contextMenuClick() => Event.current.type == EventType.ContextClick && rect.Contains( Event.current.mousePosition );
                    
                    if ( ContextButtonClick() || contextMenuClick()) {
                        Event.current.Use();
                        var progressProp = _list.serializedProperty.GetArrayElementAtIndex( index )
                            .FindPropertyRelative( nameof(Task.progress) );
                        
                        var menu = new GenericMenu();
                        menu.AddItem( new GUIContent("Delete Task"), false, () => {
                            _list.serializedProperty.DeleteArrayElementAtIndex( index );
                            _list.serializedProperty.serializedObject.ApplyModifiedProperties();
                            Repaint();
                        } );
                        menu.AddItem( new GUIContent("Duplicate Task"), false, () => {
                            _list.serializedProperty.InsertArrayElementAtIndex( index );
                            _list.serializedProperty.serializedObject.ApplyModifiedProperties();
                            Repaint();
                        } );
                        if (isEditingProp.boolValue) {
                            menu.AddItem( new GUIContent("Save Task"), false, () => {
                                isEditingProp.boolValue = false;
                                _list.serializedProperty.serializedObject.ApplyModifiedProperties();
                                GUIUtility.keyboardControl = 0;
                                GUIUtility.hotControl = 0;
                                Repaint();
                            } );
                        }
                        else {
                            menu.AddItem( new GUIContent("Edit Task"), false, () => {
                                isEditingProp.boolValue = true;
                                _list.serializedProperty.serializedObject.ApplyModifiedProperties();
                                Repaint();
                            } );
                        }
                        if (progressProp.floatValue >= 1) {
                            menu.AddItem( new GUIContent("Mark Task Not Done"), false, () => {
                                progressProp.floatValue = 0;
                                _list.serializedProperty.serializedObject.ApplyModifiedProperties();
                                Repaint();
                            } );
                        }
                        else {
                            menu.AddItem( new GUIContent("Mark Task Done"), false, () => {
                                progressProp.floatValue = 1;
                                _list.serializedProperty.serializedObject.ApplyModifiedProperties();
                                Repaint();
                            } );
                        }

                        if (EditorWindow.focusedWindow is TodoWindow todoWindow) {
                            foreach (var (taskList, _) in todoWindow.taskEditors) {
                                var isSelf = taskList == target;
                                if (isSelf) { menu.AddDisabledItem( new GUIContent( $"Move to/{taskList.title}" ), true ); }
                                else {
                                    menu.AddItem( new GUIContent( $"Move to/{taskList.title}" ), false, () => {
                                        var task = tasksProp.GetArrayElementAtIndex( index );
                                        Undo.RecordObject( taskList, "Moved task" );
                                        taskList.tasks.Add( new Task {
                                            title = task.FindPropertyRelative( nameof(Task.title) ).stringValue,
                                            description = task.FindPropertyRelative( nameof(Task.description) ).stringValue,
                                            progress = task.FindPropertyRelative( nameof(Task.progress) ).floatValue,
                                            isEditing = task.FindPropertyRelative( nameof(Task.isEditing) ).boolValue,
                                        } );
                                        tasksProp.DeleteArrayElementAtIndex( index );
                                        _list.serializedProperty.serializedObject.ApplyModifiedProperties();
                                    } );
                                }
                            }
                        }
                        
                        menu.ShowAsContext();
                        return;
                    }

                    if (tasksProp.arraySize <= index) return; 
                    
                    // check for click anywhere to enter edit mode
                    // the reason for doing it here is EditorGUI.PropertyField uses up the mouse event 
                    bool leftClickedAnywhere() => Event.current.type == EventType.MouseUp && Event.current.button == 0;
                    bool mouseOverProp() => rect.Contains( Event.current.mousePosition );
                    var wantsToGoEditMode = !isEditingProp.boolValue && leftClickedAnywhere() && mouseOverProp();

                    var enterClicked = Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Return;

                    EditorGUI.PropertyField( rect, tasksProp.GetArrayElementAtIndex( index ) );

                    // save with ENTER if title has focus
                    if (isEditingProp.boolValue) {
                        if (GUI.GetNameOfFocusedControl() is Task.TITLE_CONTROL_NAME or Task.PROGRESS_CONTROL_NAME && enterClicked) {
                            isEditingProp.boolValue = false;
                            _list.serializedProperty.serializedObject.ApplyModifiedProperties();
                            EditorWindow.focusedWindow?.Repaint();
                            _list.ClearCache();
                            _list.CacheIfNeeded();
                            Event.current.Use();
                            GUIUtility.keyboardControl = 0;
                            GUIUtility.hotControl = 0;
                            return;
                        }
                    }
                    
                    // enter edit mode if has clicked and has not been in edit mode already
                    if (wantsToGoEditMode && !GUI.GetNameOfFocusedControl().StartsWith( Task.BULLETPOINT_TOGGLE_CONTROL_NAME ) ) {
                        if (!isEditingProp.boolValue) {
                            for (int i = 0; i < _list.serializedProperty.arraySize; i++)
                                _list.serializedProperty.GetArrayElementAtIndex( i )
                                    .FindPropertyRelative( nameof(Task.isEditing) ).boolValue = i == index;

                            EditorWindow.focusedWindow?.Repaint();
                            _list.ClearCache();
                            _list.CacheIfNeeded();
                        }
                    }
                    

                };
                _list.elementHeightCallback += index =>
                    EditorGUI.GetPropertyHeight( tasksProp.GetArrayElementAtIndex( index ) );
                _list.drawFooterCallback += rect => {
                    if (GUI.Button( rect, new GUIContent( " New Task", EditorGUIUtility.FindTexture( "d_CreateAddNew" ) ) )) {
                        _list.serializedProperty.arraySize++;
                        var prop = _list.serializedProperty.GetArrayElementAtIndex( _list.serializedProperty.arraySize - 1 );
                        prop.FindPropertyRelative( nameof(Task.isEditing) ).boolValue = true;
                        prop.FindPropertyRelative( nameof(Task.title) ).stringValue = "New Task";
                        prop.FindPropertyRelative( nameof(Task.description) ).stringValue = "";
                        prop.FindPropertyRelative( nameof(Task.progress) ).floatValue = 0;
                    }
                };
                _list.footerHeight = 30;
                
                
            }

            public override void OnInspectorGUI() {
                serializedObject.Update();
                var titleProp = serializedObject.FindProperty( nameof(title) );
                var orderProp = serializedObject.FindProperty( nameof(order) );
                
                var headerRect = EditorGUILayout.GetControlRect( false, 60, GUIStyle.none );
                
                // draw progress
                var progress = ((TaskList)target).GetProgress();
                if (progress < 1) {
                    var progRect = headerRect;
                    EditorGUI.DrawRect( progRect, TaskList_ProgOutlineCol );
                    progRect.x += 1; progRect.y += 1;
                    progRect.width -= 2; progRect.height -= 2;
                    EditorGUI.DrawRect( progRect, TaskList_ProgBackCol );
                    progRect.width *= progress;
                    EditorGUI.DrawRect( progRect, TaskList_ProgFillCol );
                    progRect.x += 5; progRect.width = 50; 
                    progRect.y += progRect.height - 25; progRect.height = 20;
                    EditorGUI.LabelField( progRect, $"{(int)(progress * 100)}%", TaskList_GetProgText() );
                }


                headerRect.height -= 20;
                titleProp.stringValue = EditorGUI.TextField( headerRect, titleProp.stringValue, TaskList_GetTitleText() );
                
                // toolbar
                {
                    var toolbarRect = new Rect( headerRect.x + 2, headerRect.y + headerRect.height - 2, headerRect.width - 4, 20 - 2 );
                    if (toolbarRect.Contains( Event.current.mousePosition )) {
                        // change mouse cursor
                        EditorGUIUtility.AddCursorRect( toolbarRect, MouseCursor.Arrow );
                    }
                    
                    toolbarRect.x += toolbarRect.width - 25;
                    
                    toolbarRect.width = 25;
                    if (GUI.Button( toolbarRect, new GUIContent( TaskList_Get_TaskMenuTex(), "Options" ), TaskList_GetToolbarButton() )) {
                        var menu = new GenericMenu();
                        if (EditorWindow.focusedWindow is TodoWindow todoWindow) {
                            menu.AddItem( new GUIContent( "Move Right" ), false, () => {
                                orderProp.intValue = todoWindow.GetNextTaskList( (TaskList)target ).order + 1;
                                serializedObject.ApplyModifiedProperties();
                                todoWindow.SortTaskLists();
                                todoWindow.Repaint();
                            } );
                            menu.AddItem( new GUIContent( "Move Left" ), false, () => {
                                orderProp.intValue = todoWindow.GetPrevTaskList( (TaskList)target ).order - 1;
                                serializedObject.ApplyModifiedProperties();
                                todoWindow.SortTaskLists();
                                todoWindow.Repaint();
                            } );
                        }

                        menu.ShowAsContext();
                    }

                    toolbarRect.x -= 60;
                    toolbarRect.width = 60;
                    if (GUI.Button( toolbarRect, new GUIContent("Delete", TaskList_GetDeleteTex(), "Delete Task List"), TaskList_GetToolbarButton() )) {
                        AssetDatabase.DeleteAsset( AssetDatabase.GetAssetPath( target ) );
                        return;
                    }
                    
                    toolbarRect.x -= toolbarRect.width;
                    if (GUI.Button( toolbarRect, new GUIContent("Copy", TaskList_GetCopyTex(), "Copy To Clipboard"), TaskList_GetToolbarButton() )) {
                        var json = JsonUtility.ToJson( target );
                        EditorGUIUtility.systemCopyBuffer = json;
                    }
                    
                    toolbarRect.x -= toolbarRect.width;
                    if (GUI.Button( toolbarRect, new GUIContent("Paste", TaskList_GetPasteTex(), "Import From Clipboard"), TaskList_GetToolbarButton() )) {
                        Undo.RecordObject( target, "Pasted task list" );
                        var json = EditorGUIUtility.systemCopyBuffer;
                        JsonUtility.FromJsonOverwrite( json, target );
                        serializedObject.Update();
                    }
                }

                using (var scroll = new EditorGUILayout.ScrollViewScope( tasksScrollPos )) {
                    tasksScrollPos = scroll.scrollPosition;
                    _list.DoLayoutList();
                }
                
                
                // general shortcuts
                {
                    // cancel edit mode
                    if (Event.current.type == EventType.KeyDown && Event.current.keyCode is KeyCode.Escape) {
                        _list.ClearSelection();
                        GUIUtility.hotControl = 0;
                        GUIUtility.keyboardControl = 0;
                        for (int i = 0; i < _list.serializedProperty.arraySize; i++) {
                            _list.serializedProperty.GetArrayElementAtIndex( i )
                                .FindPropertyRelative( nameof(Task.isEditing) ).boolValue = false;
                        }
    
                        EditorWindow.focusedWindow?.Repaint();
                        _list.ClearCache();
                        _list.CacheIfNeeded();
                    }
                }
                serializedObject.ApplyModifiedProperties();
                
                // first time always make wrong list element heights, so we have to flush it one time
                if (firstTime && Event.current.type == EventType.Repaint) {
                    firstTime = false; 
                    EditorWindow.focusedWindow?.Repaint(); 
                    _list.ClearCache();
                    _list.CacheIfNeeded();
                }
            }
        }
    }
}