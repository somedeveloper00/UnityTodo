using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace UnityTodo {
    internal class TaskList : ScriptableObject {
        public int order;
        public string title;
        public List<Task> tasks = new();

        public float GetProgress() => tasks.Count == 0 ? 1 : Mathf.Clamp01( tasks.Sum( t => t.progress ) / tasks.Count );

        public static HashSet<TaskList> AllLoadedTaskLists = new();

        void Awake() => AllLoadedTaskLists.Add( this );
        void OnEnable() => AllLoadedTaskLists.Add( this );
        void OnDisable() => AllLoadedTaskLists.Remove( this );
        void OnDestroy() => AllLoadedTaskLists.Remove( this );

        [CustomEditor(typeof(TaskList))]
        class editor : Editor {
            [NonSerialized] ReorderableList _list;

            [NonSerialized] Vector3 tasksScrollPos;

            [NonSerialized] static readonly Color progOutlineCol = new Color( 0.1f, 0.1f, 0.1f ); 
            [NonSerialized] static readonly Color progBckCol = new Color( 0.2f, 0.2f, 0.2f ); 
            [NonSerialized] static readonly Color progFillCol = new Color( 0f, 0.4f, 0f ); 
            [NonSerialized] static readonly Color progTextCol = new Color( 0.7f, 0.7f, 0.7f ); 
            
            void OnEnable() {
                var tasksProp = serializedObject.FindProperty( nameof(tasks) );
                _list = new ReorderableList( serializedObject, tasksProp, true, false, false, false );
                _list.drawElementCallback += (rect, index, active, focused) => {

                    if ((focused || active) && Event.current.type == EventType.KeyDown && Event.current.keyCode is KeyCode.Escape) {
                        _list.ClearSelection();
                    }

                    bool buttonClick() => GUI.Button( new Rect( rect.x + rect.width - 15, rect.y, 20, 20 ), EditorGUIUtility.FindTexture( "d__Menu" ), EditorStyles.iconButton );
                    bool contextMenuClick() => Event.current.type == EventType.ContextClick && rect.Contains( Event.current.mousePosition );
                    
                    if ( buttonClick() || contextMenuClick()) {
                        Event.current.Use();
                        var progressProp = _list.serializedProperty.GetArrayElementAtIndex( index )
                            .FindPropertyRelative( nameof(Task.progress) );
                        var isEditingProp = _list.serializedProperty.GetArrayElementAtIndex( index )
                            .FindPropertyRelative( nameof(Task.isEditing) );
                        
                        var menu = new GenericMenu();
                        menu.AddItem( new GUIContent("Delete Task"), false, () => {
                            _list.serializedProperty.DeleteArrayElementAtIndex( index );
                            _list.serializedProperty.serializedObject.ApplyModifiedProperties();
                        } );
                        menu.AddItem( new GUIContent("Duplicate Task"), false, () => {
                            _list.serializedProperty.InsertArrayElementAtIndex( index );
                            _list.serializedProperty.serializedObject.ApplyModifiedProperties();
                        } );
                        menu.AddItem( new GUIContent("Edit Task"), false, () => {
                            isEditingProp.boolValue = true;
                            _list.serializedProperty.serializedObject.ApplyModifiedProperties();
                        } );
                        if (progressProp.floatValue == 1) {
                            menu.AddItem( new GUIContent("Mark Task Not Done"), false, () => {
                                progressProp.floatValue = 0;
                                _list.serializedProperty.serializedObject.ApplyModifiedProperties();
                            } );
                        }
                        else {
                            menu.AddItem( new GUIContent("Mark Task Done"), false, () => {
                                progressProp.floatValue = 1;
                                _list.serializedProperty.serializedObject.ApplyModifiedProperties();
                            } );
                        }

                        foreach (var taskList in AllLoadedTaskLists) {
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
                        
                        menu.ShowAsContext();
                        return;
                    }

                    if (tasksProp.arraySize <= index) return; 
                    
                    EditorGUI.PropertyField( rect, tasksProp.GetArrayElementAtIndex( index ) );
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


                var headerRect = EditorGUILayout.GetControlRect( false, 40, GUIStyle.none );
                
                var progress = ((TaskList)target).GetProgress();
                if (progress < 1) {
                    var progRect = headerRect;
                    EditorGUI.DrawRect( progRect, progOutlineCol );
                    progRect.x += 1; progRect.y += 1;
                    progRect.width -= 2; progRect.height -= 2;
                    EditorGUI.DrawRect( progRect, progBckCol );
                    progRect.width *= progress;
                    EditorGUI.DrawRect( progRect, progFillCol );
                    progRect.x += 5;
                    progRect.y += 5;
                    using (new GUIUtilities.GUIColor( progTextCol ))
                        EditorGUI.LabelField( progRect, $"<b><i>{(int)(progress * 100)}%</i></b>", GUIStyles.GetSmallLabel() );
                }

                var bottomHeaderRect = new Rect( headerRect.x + headerRect.width - 20, headerRect.y + headerRect.height - 20, 20, 20 );
                
                if (GUI.Button( bottomHeaderRect, new GUIContent(EditorGUIUtility.FindTexture( "TreeEditor.Trash" ), "Delete Task List"), EditorStyles.iconButton )) {
                    AssetDatabase.DeleteAsset( AssetDatabase.GetAssetPath( target ) );
                    return;
                }
                
                bottomHeaderRect.x -= bottomHeaderRect.width;

                if (GUI.Button( bottomHeaderRect, new GUIContent(EditorGUIUtility.FindTexture( "Clipboard" ), "Copy To Clipboard"), EditorStyles.iconButton )) {
                    var json = JsonUtility.ToJson( target );
                    EditorGUIUtility.systemCopyBuffer = json;
                }
                
                bottomHeaderRect.x -= bottomHeaderRect.width;
                
                if (GUI.Button( bottomHeaderRect, new GUIContent(EditorGUIUtility.FindTexture( "d_Toolbar Plus More" ), "Import From Clipboard"), EditorStyles.iconButton )) {
                    Undo.RecordObject( target, "Pasted task list" );
                    var json = EditorGUIUtility.systemCopyBuffer;
                    JsonUtility.FromJsonOverwrite( json, target );
                }

                headerRect.size = new Vector2( headerRect.width - 50, headerRect.height );
                titleProp.stringValue = EditorGUI.TextField( headerRect, titleProp.stringValue, GUIStyles.GetBigLabel() );

                using (var scroll = new EditorGUILayout.ScrollViewScope( tasksScrollPos )) {
                    tasksScrollPos = scroll.scrollPosition;
                    _list.DoLayoutList();
                }
                
                EditorUtility.SetDirty( _list.serializedProperty.serializedObject.targetObject );
                serializedObject.ApplyModifiedProperties();
            }
        }
    }
}