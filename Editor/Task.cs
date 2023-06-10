using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using static UnityTodo.GUIStyles;
using static UnityTodo.GUIUtilities;
using Object = UnityEngine.Object;

namespace UnityTodo {
    [Serializable] internal class Task {
        public string title;
        public string description;
        public float progress;
        public bool isEditing = true;
        public List<BulletPoint> bulletPoints = new();
        public List<Reference> references = new();

        [Serializable] public struct Reference {
            public string name;
            public string path;
        }
        [Serializable] public struct BulletPoint {
            public string description;
            public bool done;
        }

        public const string TITLE_CONTROL_NAME = "task-title";
        public const string DESCRIPTION_CONTROL_NAME = "task-description";
        public const string PROGRESS_CONTROL_NAME = "task-progress";
        public const string BULLETPOINT_TOGGLE_CONTROL_NAME = "task-bulletpoint-toggle";


        [CustomPropertyDrawer(typeof(Task))]
        class drawer : PropertyDrawer {

            [NonSerialized] float lastTitleWidth = 280;
            [NonSerialized] float lastDescriptionWidth = 280;
            
            [NonSerialized] static Dictionary<string, Object> _path2Obj = new();

            public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
                var titleProp = property.FindPropertyRelative( nameof(title) );
                var descriptionProp = property.FindPropertyRelative( nameof(description) );
                var isEditingProp = property.FindPropertyRelative( nameof(isEditing) );
                var progressProp = property.FindPropertyRelative( nameof(progress) );
                var bulletPointsProp = property.FindPropertyRelative( nameof(bulletPoints) );
                var referencesProp = property.FindPropertyRelative( nameof(references) );

                
                using (new EditorGUI.PropertyScope( position, label, property )) {
                    
                    position.height = EditorGUIUtility.singleLineHeight;
                    position.y += 10;

                    // title prop
                    position.x += 10;
                    position.width -= 20;
                    if (Event.current.type == EventType.Repaint)
                        lastTitleWidth = position.width; // save for later height calculation (used for word wrap)
                    position.height += 10;
                    var titleText = progressProp.floatValue < 1 || isEditingProp.boolValue ? titleProp.stringValue : StrikeThrough( titleProp.stringValue );
                    var titleStyle = isEditingProp.boolValue
                        ? Task_GetTitleTextEdit()
                        : progressProp.floatValue < 1 ? Task_GetUnfinishedTitleText() : Task_GetFinishedTitleText();
                    var titleHeight = titleStyle.CalcHeight( new GUIContent( titleText ), position.width );
                    position.height = titleHeight;

                    { // handle click on text to enter edit mode AND select the title text as it normally would
                        if (Event.current.type == EventType.MouseDown && Event.current.button == 0 && position.Contains( Event.current.mousePosition )) {
                            titleText = titleProp.stringValue;
                        }
                    }
                    
                    using (var check = new EditorGUI.ChangeCheckScope()) {
                        GUI.SetNextControlName( TITLE_CONTROL_NAME );
                        var r = EditorGUI.TextField( position, titleText, titleStyle );
                        if (check.changed && isEditingProp.boolValue) 
                            titleProp.stringValue = r;
                    }
                    position.y += position.height + EditorGUIUtility.standardVerticalSpacing;
                    position.x -= 10;
                    position.width += 20;
                    
                    if (Event.current.type == EventType.Repaint)
                        lastDescriptionWidth = position.width;
                    
                    // description prop
                    {
                        position.y += 10;
                        var descStyle = isEditingProp.boolValue
                            ? Task_GetDescTextEdit()
                            : progressProp.floatValue < 1 ? Task_GetUnfinishedDescText() : Task_GetFinishedDescText();
                        var height = !isEditingProp.boolValue && string.IsNullOrEmpty( descriptionProp.stringValue )
                            ? 0
                            : isEditingProp.boolValue
                                ? Mathf.Max( descStyle.CalcHeight( new GUIContent( descriptionProp.stringValue ), position.width ), 50 )
                                : descStyle.CalcHeight( new GUIContent( descriptionProp.stringValue ), position.width );
                        position.height = height;
                        using (var check = new EditorGUI.ChangeCheckScope()) {
                            GUI.SetNextControlName( DESCRIPTION_CONTROL_NAME );
                            var r = EditorGUI.TextArea( position, descriptionProp.stringValue, descStyle );
                            if (check.changed && isEditingProp.boolValue) {
                                descriptionProp.stringValue = r;
                            }
                        }
                        position.y += position.height + EditorGUIUtility.standardVerticalSpacing;
                    }
                    
                    position.y += 5;

                    // bullet-points prop
                    {
                        position.height = EditorGUIUtility.singleLineHeight;
                        const int checkboxWidth = 20;
                        const int removeButtonWidth = 30;
                        var lastWidth = position.width;
                        var lastX = position.x;
                        
                        for (int i = 0; i < bulletPointsProp.arraySize; i++) {
                            var prop = bulletPointsProp.GetArrayElementAtIndex( i );
                            var toggleProp = prop.FindPropertyRelative( nameof(BulletPoint.done) );
                            var descProp = prop.FindPropertyRelative( nameof(BulletPoint.description) );
                            
                            // checkbox
                            position.width = checkboxWidth;
                            GUI.SetNextControlName( BULLETPOINT_TOGGLE_CONTROL_NAME + i );
                            toggleProp.boolValue = EditorGUI.Toggle( position, toggleProp.boolValue );

                            // description
                            position.x += checkboxWidth;
                            position.width = lastWidth - checkboxWidth;
                            if (isEditingProp.boolValue) position.width -= removeButtonWidth;
                            var descStyle = isEditingProp.boolValue
                                ? Task_GetBulletpointNameEdit()
                                : progressProp.floatValue < 1 && !toggleProp.boolValue ? EditorStyles.label : Task_GetFinishedBulletpoint();
                            var descText = isEditingProp.boolValue || progressProp.floatValue < 1 && !toggleProp.boolValue
                                ? descProp.stringValue
                                : StrikeThrough( descProp.stringValue );
                            using (var check = new EditorGUI.ChangeCheckScope()) {
                                var r = EditorGUI.TextField( position, descText, descStyle );
                                if (check.changed && isEditingProp.boolValue) 
                                    descProp.stringValue = r;
                            }

                            // remove button
                            if (isEditingProp.boolValue) {
                                position.x += position.width;
                                position.width = removeButtonWidth;
                                if (GUI.Button( position, new GUIContent( TaskList_GetDeleteTex(), "Delete Bullet-Point" ) )) {
                                    bulletPointsProp.DeleteArrayElementAtIndex( i );
                                    i--;
                                }
                            }

                            position.x = lastX; 
                            position.y += 20 + EditorGUIUtility.standardVerticalSpacing;
                        }
                        
                        // new bullet-point
                        if (isEditingProp.boolValue) {
                            position.width = 20;
                            GUI.enabled = false;
                            EditorGUI.Toggle( position, false );
                            GUI.enabled = true;
                            position.x += 20;
                            position.width = lastWidth - 20;
                            using (var check = new EditorGUI.ChangeCheckScope()) {
                                var r = EditorGUI.TextField( position, "(New bullet point item)", Task_GetBulletpointNew() );
                                if (check.changed) {
                                    bulletPointsProp.arraySize++;
                                    var prop = bulletPointsProp.GetArrayElementAtIndex( bulletPointsProp.arraySize - 1 );
                                    prop.FindPropertyRelative( nameof(BulletPoint.description) ).stringValue = r;
                                    prop.FindPropertyRelative( nameof(BulletPoint.done) ).boolValue = false;
                                }
                            }

                            position.y += 20 + EditorGUIUtility.standardVerticalSpacing;
                        }
                        position.x = lastX;
                        position.width = lastWidth;
                    }
                    
                    // references prop
                    {
                        
                        if (isEditingProp.boolValue || referencesProp.arraySize > 0) position.y += 5;
                        var lastWidth = position.width;
                        var lastX = position.x;
                        position.height = 20;
                        const int removeWidth = 30;
                        
                        for (int i = 0; i < referencesProp.arraySize; i++) {
                            
                            var element = referencesProp.GetArrayElementAtIndex( i );
                            var nameProp = element.FindPropertyRelative( nameof(Reference.name) );
                            var pathProp = element.FindPropertyRelative( nameof(Reference.path) );
                            
                            position.width = Mathf.Min( EditorStyles.label.CalcSize( new GUIContent( nameProp.stringValue ) ).x + 10, lastWidth * 0.5f );
                            var nameStyle = isEditingProp.boolValue ? Task_GetReferenceNameEdit() 
                                : progressProp.floatValue < 1 ? EditorStyles.label : Task_GetFinishedReferenceName();
                            using (var check = new EditorGUI.ChangeCheckScope()) {
                                var r = EditorGUI.TextField( position, nameProp.stringValue, nameStyle );
                                if (check.changed && isEditingProp.boolValue)
                                    nameProp.stringValue = r;
                            }
                            
                            // object reference
                            position.x += position.width;
                            position.width = lastWidth - position.width;
                            if (isEditingProp.boolValue) position.width -= removeWidth;
                            if (!_path2Obj.TryGetValue( pathProp.stringValue, out var reference ))
                                _path2Obj[pathProp.stringValue] = reference = AssetDatabase.LoadAssetAtPath<Object>( pathProp.stringValue );
                            using (new EditorGUI.DisabledScope( !isEditingProp.boolValue && progressProp.floatValue >= 1 ))
                            using (var check = new EditorGUI.ChangeCheckScope()) {
                                var r = EditorGUI.ObjectField( position, reference, typeof(Object), false );
                                if (check.changed) {
                                    pathProp.stringValue = AssetDatabase.GetAssetPath( r );
                                    if (!isEditingProp.boolValue) isEditingProp.boolValue = true;
                                }
                            }

                            position.x += position.width; position.width = removeWidth;
                            // remove button
                            if (isEditingProp.boolValue) {
                                if (GUI.Button( position, new GUIContent( TaskList_GetDeleteTex(), "Delete Object Reference" ) )) {
                                    referencesProp.DeleteArrayElementAtIndex( i );
                                    i--;
                                }
                            }
                            
                            position.x = lastX;
                            position.y += position.height + EditorGUIUtility.standardVerticalSpacing;
                        }
                        
                        // new object reference button
                        if (isEditingProp.boolValue) {
                            position.width = lastWidth * 0.55f - 10;
                            using (var check = new EditorGUI.ChangeCheckScope()) {
                                var name = EditorGUI.TextField( position, "(New Object name)", Task_GetBulletpointNew() );
                                position.x += position.width + 10;
                                position.width = lastWidth - 10 - position.width;
                                var reference = EditorGUI.ObjectField( position, null, typeof(Object), false );
                                if (check.changed) {
                                    referencesProp.arraySize++;
                                    var prop = referencesProp.GetArrayElementAtIndex( referencesProp.arraySize - 1 );
                                    prop.FindPropertyRelative( nameof(Reference.name) ).stringValue = name;
                                    prop.FindPropertyRelative( nameof(Reference.path) ).stringValue = AssetDatabase.GetAssetPath( reference );
                                }

                            }

                            position.y += 20 + EditorGUIUtility.standardVerticalSpacing;
                        }
                        position.x = lastX;
                        position.width = lastWidth;
                        if (isEditingProp.boolValue || referencesProp.arraySize > 0) position.y += 5;
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
                var referencesProp = property.FindPropertyRelative( nameof(references) );
                var bulletPointsProp = property.FindPropertyRelative( nameof(bulletPoints) );
                
                float h = 10;

                // title
                var titleStyle = isEditingProp.boolValue
                    ? Task_GetTitleTextEdit()
                    : progressProp.floatValue < 1 ? Task_GetUnfinishedTitleText() : Task_GetFinishedTitleText();
                h += titleStyle.CalcHeight( new GUIContent( titleProp.stringValue ), lastTitleWidth);
                h += EditorGUIUtility.standardVerticalSpacing;

                // desc
                h += 10;
                if (isEditingProp.boolValue || !string.IsNullOrEmpty( descriptionProp.stringValue )) {
                    var descStyle = isEditingProp.boolValue
                        ? Task_GetDescTextEdit()
                        : progressProp.floatValue < 1 ? Task_GetUnfinishedDescText() : Task_GetFinishedDescText();
                    h += isEditingProp.boolValue
                        ? Mathf.Max( descStyle.CalcHeight( new GUIContent( descriptionProp.stringValue ), lastDescriptionWidth ), 50 )
                        : descStyle.CalcHeight( new GUIContent( descriptionProp.stringValue ), lastDescriptionWidth );
                    h += EditorGUIUtility.standardVerticalSpacing;
                }
                
                // bullet-points
                h += 5;
                h += bulletPointsProp.arraySize * (20 + EditorGUIUtility.standardVerticalSpacing);
                if (isEditingProp.boolValue) // new element
                    h += 20 + EditorGUIUtility.standardVerticalSpacing;
                
                // references
                if (isEditingProp.boolValue || referencesProp.arraySize > 0) h += 5 + 5; // start and end padding
                h += referencesProp.arraySize * (20 + EditorGUIUtility.standardVerticalSpacing);
                if (isEditingProp.boolValue) // add button
                    h += 20 + EditorGUIUtility.standardVerticalSpacing;

                h += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
                h += 5;
                return h;
            }

        }
    }
}