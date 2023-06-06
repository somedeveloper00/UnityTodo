using System;
using Palmmedia.ReportGenerator.Core.Reporting.Builders;
using UnityEditor;
using UnityEngine;
using static UnityTodo.GUIStyles;
using static UnityTodo.GUIUtilities;

namespace UnityTodo {
    [Serializable] internal class Task {
        public string title;
        public string description;
        public float progress;
        public bool isEditing = true;


        public const string TITLE_CONTROL_NAME = "task-title";
        public const string DESCRIPTION_CONTROL_NAME = "task-description";
        public const string PROGRESS_CONTROL_NAME = "task-progress";

        [CustomPropertyDrawer(typeof(Task))]
        class drawer : PropertyDrawer {

            [NonSerialized] float lastTitleWidth = 280;
            [NonSerialized] float lastDescriptionWidth = 280;
            
            public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
                var titleProp = property.FindPropertyRelative( nameof(title) );
                var descriptionProp = property.FindPropertyRelative( nameof(description) );
                var isEditingProp = property.FindPropertyRelative( nameof(isEditing) );
                var progressProp = property.FindPropertyRelative( nameof(progress) );

                
                using (new EditorGUI.PropertyScope( position, label, property )) {
                    
                    position.height = EditorGUIUtility.singleLineHeight;
                    position.y += 4;

                    // title prop
                    position.x += 25;
                    position.width -= 50;
                    if (Event.current.type == EventType.Repaint)
                        lastTitleWidth = position.width; // save for later height calculation (used for word wrap)
                    position.height += 10;
                    var titleText = progressProp.floatValue < 1 || isEditingProp.boolValue ? titleProp.stringValue : StrikeThrough( titleProp.stringValue );
                    var titleStyle = isEditingProp.boolValue
                        ? Task_GetTitleTextEdit()
                        : progressProp.floatValue < 1 ? Task_GetUnfinishedTitleText() : Task_GetFinishedTitleText();
                    var titleHeight = titleStyle.CalcHeight( new GUIContent( titleText ), position.width );
                    position.height = titleHeight;
                    using (var check = new EditorGUI.ChangeCheckScope()) {
                        GUI.SetNextControlName( TITLE_CONTROL_NAME );
                        var r = EditorGUI.TextField( position, titleText, titleStyle );
                        if (check.changed && isEditingProp.boolValue) 
                            titleProp.stringValue = r;
                    }
                    position.y += position.height + EditorGUIUtility.standardVerticalSpacing;
                    position.x -= 25;
                    position.width += 50 - 15;
                    
                    if (Event.current.type == EventType.Repaint)
                        lastDescriptionWidth = position.width;
                    
                    // description prop
                    if (isEditingProp.boolValue || !string.IsNullOrEmpty( descriptionProp.stringValue )) {
                        position.y += 5;
                        var descStyle = isEditingProp.boolValue
                            ? Task_GetDescTextEdit()
                            : progressProp.floatValue < 1 ? Task_GetUnfinishedDescText() : Task_GetFinishedDescText();
                        var height = isEditingProp.boolValue
                            ? Mathf.Max( descStyle.CalcHeight( new GUIContent( descriptionProp.stringValue ), position.width ), 50 )
                            : descStyle.CalcHeight( new GUIContent( descriptionProp.stringValue ), position.width );
                        position.height = height;
                        using (var check = new EditorGUI.ChangeCheckScope()) {
                            GUI.SetNextControlName( DESCRIPTION_CONTROL_NAME );
                            var r = EditorGUI.TextArea( position, descriptionProp.stringValue, descStyle );
                            if (check.changed && isEditingProp.boolValue) 
                                descriptionProp.stringValue = r;
                        }
                        position.y += position.height + EditorGUIUtility.standardVerticalSpacing;
                    }


                    // progress prop
                    position.height = EditorGUIUtility.singleLineHeight;
                    if (isEditingProp.boolValue) {
                        GUI.SetNextControlName( PROGRESS_CONTROL_NAME );
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
                var titleProp = property.FindPropertyRelative( nameof(title) );
                var isEditingProp = property.FindPropertyRelative( nameof(isEditing) );
                var progressProp = property.FindPropertyRelative( nameof(progress) );
                float h = 4;

                // title
                var titleStyle = isEditingProp.boolValue
                    ? Task_GetTitleTextEdit()
                    : progressProp.floatValue < 1 ? Task_GetUnfinishedTitleText() : Task_GetFinishedTitleText();
                h += titleStyle.CalcHeight( new GUIContent( titleProp.stringValue ), lastTitleWidth);
                h += EditorGUIUtility.standardVerticalSpacing;

                // desc
                if (isEditingProp.boolValue || !string.IsNullOrEmpty( descriptionProp.stringValue )) {
                    h += 5;
                    var descStyle = isEditingProp.boolValue
                        ? Task_GetDescTextEdit()
                        : progressProp.floatValue < 1 ? Task_GetUnfinishedDescText() : Task_GetFinishedDescText();
                    h += isEditingProp.boolValue
                        ? Mathf.Max( descStyle.CalcHeight( new GUIContent( descriptionProp.stringValue ), lastDescriptionWidth ), 50 )
                        : descStyle.CalcHeight( new GUIContent( descriptionProp.stringValue ), lastDescriptionWidth );
                    h += EditorGUIUtility.standardVerticalSpacing;
                }

                h += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
                return h;
            }

        }
    }
}