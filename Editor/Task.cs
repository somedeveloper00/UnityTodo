using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions.Must;
using static UnityTodo.GUIStyles;
using static UnityTodo.GUIUtilities;

namespace UnityTodo {
    [Serializable] internal class Task {
        public string title;
        public string description;
        public float progress;
        public bool isEditing = true;

        [CustomPropertyDrawer(typeof(Task))]
        class drawer : PropertyDrawer {

            float lastDescriptionWidth;
            
            public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
                var titleProp = property.FindPropertyRelative( nameof(title) );
                var descriptionProp = property.FindPropertyRelative( nameof(description) );
                var isEditingProp = property.FindPropertyRelative( nameof(isEditing) );
                var progressProp = property.FindPropertyRelative( nameof(progress) );

                    
                using (new EditorGUI.PropertyScope( position, label, property )) {
                    
                    position.height = EditorGUIUtility.singleLineHeight;
                    position.y += 4;
                    
                    // edit button
                    var editRect = new Rect( position.x, position.y, 25, 25 );
                    var editTexture = isEditingProp.boolValue
                        ? EditorGUIUtility.FindTexture( "SaveAs@2x" )
                        : EditorGUIUtility.FindTexture( "d_editicon.sml" );
                    if (GUI.Button( editRect, editTexture )) 
                    {
                        isEditingProp.boolValue = !isEditingProp.boolValue;
                        if (!isEditingProp.boolValue) {
                            property.serializedObject.ApplyModifiedProperties();
                        }
                    }

                    position.x += 30;
                    position.width -= 50;
                    
                    // title prop
                    position.height += 10;
                    var titleText = progressProp.floatValue < 1 || isEditingProp.boolValue ? titleProp.stringValue : StrikeThrough( titleProp.stringValue );
                    var titleStyle = isEditingProp.boolValue
                        ? Task_GetTitleTextEdit()
                        : progressProp.floatValue < 1 ? Task_GetUnfinishedTitleText() : Task_GetFinishedTitleText();
                    using (var check = new EditorGUI.ChangeCheckScope()) {
                        var r = EditorGUI.TextField( position, titleText, titleStyle );
                        if (check.changed && isEditingProp.boolValue) {
                            titleProp.stringValue = r;
                        }
                    }

                    position.y += position.height + EditorGUIUtility.standardVerticalSpacing;
                    position.x -= 30;
                    position.width += 50 - 15;
                    
                    // description prop
                    var descStyle = isEditingProp.boolValue
                        ? Task_GetDescTextEdit()
                        : progressProp.floatValue < 1 ? Task_GetUnfinishedDescText() : Task_GetFinishedDescText();
                    var height = isEditingProp.boolValue
                        ? Mathf.Max( descStyle.CalcHeight( new GUIContent( descriptionProp.stringValue ), position.width ), 50 )
                        : descStyle.CalcHeight( new GUIContent( descriptionProp.stringValue ), position.width );
                    position.height = height;
                    using (var check = new EditorGUI.ChangeCheckScope()) {
                        var r = EditorGUI.TextArea( position, descriptionProp.stringValue, descStyle );
                        if (check.changed && isEditingProp.boolValue) 
                            descriptionProp.stringValue = r;
                    }
                    position.y += position.height + EditorGUIUtility.standardVerticalSpacing;

                    
                    if (Event.current.type == EventType.Repaint)
                        lastDescriptionWidth = position.width;
                    position.height = EditorGUIUtility.singleLineHeight;

                    // progress prop
                    if (isEditingProp.boolValue) {
                        progressProp.floatValue = EditorGUI.Slider( position, progressProp.floatValue, 0, 1 );
                    }
                    else {
                        var rect = new Rect(
                            position.x + position.width * 0,
                            position.y + position.height * 0.25f,
                            position.width * (1 - 0 * 2),
                            position.height * (1 - 0.25f * 2) );
                        EditorGUI.DrawRect( rect, Taskt_ProgOutlineCol );
                        rect.x += 1;
                        rect.y += 1;
                        rect.width -= 2;
                        rect.height -= 2;
                        EditorGUI.DrawRect( rect, Taskt_ProgressBackCol );
                        rect.width *= progressProp.floatValue;
                        EditorGUI.DrawRect( rect, progressProp.floatValue < 1 ? Taskt_UnfinishedProgressCol : Taskt_FinishedProgressCol );
                    }
                }
            }

            public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
                var descriptionProp = property.FindPropertyRelative( nameof(description) );
                var isEditingProp = property.FindPropertyRelative( nameof(isEditing) );
                var progressProp = property.FindPropertyRelative( nameof(progress) );
                var h = 4 + (EditorGUIUtility.singleLineHeight + 10) + EditorGUIUtility.standardVerticalSpacing;
                
                var descStyle = isEditingProp.boolValue
                    ? Task_GetDescTextEdit()
                    : progressProp.floatValue < 1 ? Task_GetUnfinishedDescText() : Task_GetFinishedDescText();
                h += isEditingProp.boolValue
                    ? Mathf.Max( descStyle.CalcHeight( new GUIContent( descriptionProp.stringValue ), lastDescriptionWidth ), 50 )
                    : descStyle.CalcHeight( new GUIContent( descriptionProp.stringValue ), lastDescriptionWidth );
                h += EditorGUIUtility.standardVerticalSpacing;

                h += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
                return h;
            }

        }
    }
}