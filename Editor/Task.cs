using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using static UnityTodo.GUIStyles;
using static UnityTodo.GUIUtilities;
using Object = UnityEngine.Object;

namespace UnityTodo
{
    [Serializable]
    internal sealed class Task
    {
        public string title;
        public string description;

        /// <summary>
        /// if -1, it would mean this task doesn't have a progress bar.
        /// </summary>
        public float progress;

        public bool isEditing = true;
        public List<BulletPoint> bulletPoints = new();
        public List<Reference> references = new();
        public bool useCustomTitleColor;
        public Color titleColor;
        public bool useCustomBackgroundColor;
        public Color backgroundColor;

        public bool HasProgress => progress >= 0 || bulletPoints.Count > 0;

        public float ProgressDisplay => progress >= 0 ? progress : bulletPoints.Count > 0 ? bulletPoints.Count(b => b.done) / (float)bulletPoints.Count : 0;

        public Task Copied()
        {
            var copy = new Task
            {
                title = title,
                description = description,
                progress = progress,
                isEditing = isEditing,
                bulletPoints = new List<BulletPoint>(bulletPoints),
                references = new List<Reference>(references)
            };
            return copy;
        }

        [Serializable]
        public struct Reference
        {
            public string name;
            public string path;
        }
        [Serializable]
        public struct BulletPoint
        {
            public string description;
            public bool done;
        }

        public const string TITLE_CONTROL_NAME = "task-title";
        public const string DESCRIPTION_CONTROL_NAME = "task-description";
        public const string PROGRESS_CONTROL_NAME = "task-progress";
        public const string TITLECOLOR_CONTROL_NAME = "title-color";
        public const string BACKGROUNDCOLOR_CONTROL_NAME = "background-color";
        public const string BULLETPOINT_TOGGLE_CONTROL_NAME = "task-bulletpoint-toggle";


        [CustomPropertyDrawer(typeof(Task))]
        private sealed class Drawer : PropertyDrawer
        {
            [NonSerialized] static Dictionary<string, Object> _path2Obj = new();

            private const float NO_LABEL_TOGGLE_WIDTH = 15;
            private const float NO_LABLE_TOGGLE_SPACE = 5;

            public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
            {
                var titleProp = property.FindPropertyRelative(nameof(title));
                var descriptionProp = property.FindPropertyRelative(nameof(description));
                var isEditingProp = property.FindPropertyRelative(nameof(isEditing));
                var progressProp = property.FindPropertyRelative(nameof(progress));
                var bulletPointsProp = property.FindPropertyRelative(nameof(bulletPoints));
                var referencesProp = property.FindPropertyRelative(nameof(references));
                var useCustomTitleColorProp = property.FindPropertyRelative(nameof(useCustomTitleColor));
                var titleColorProp = property.FindPropertyRelative(nameof(titleColor));
                var useCustomBackgroundColorProp = property.FindPropertyRelative(nameof(useCustomBackgroundColor));
                var backgroundColorProp = property.FindPropertyRelative(nameof(backgroundColor));

                using (new EditorGUI.PropertyScope(position, label, property))
                {
                    // draw background color
                    {
                        if (!isEditingProp.boolValue && useCustomBackgroundColorProp.boolValue)
                        {
                            var rect = new Rect(position)
                            {
                                height = 10 + 10 + GetPropertyHeight(property, label)
                            };
                            rect.x -= 40;
                            rect.width += 40 + 30;
                            rect.y -= 10;
                            GUI.DrawTexture(rect, GetFadedColorTexture(backgroundColorProp.colorValue));
                        }
                    }

                    position.height = EditorGUIUtility.singleLineHeight;
                    position.y += 10;

                    // title prop
                    position.x += 7;
                    position.width -= 7 * 2;
                    position.height += 10;
                    var titleText = progressProp.floatValue < 1 || isEditingProp.boolValue ? titleProp.stringValue : StrikeThrough(titleProp.stringValue);
                    var titleStyle = isEditingProp.boolValue
                        ? Task_GetTitleTextEdit()
                        : progressProp.floatValue < 1 ? Task_GetUnfinishedTitleText() : Task_GetFinishedTitleText();
                    titleStyle.normal.textColor = !isEditingProp.boolValue && useCustomTitleColorProp.boolValue ? titleColorProp.colorValue : Color.white;

                    var titleHeight = titleStyle.CalcHeight(new GUIContent(titleText), position.width);
                    position.height = titleHeight;

                    { // handle click on text to enter edit mode AND select the title text as it normally would
                        if (Event.current.type == EventType.MouseDown && Event.current.button == 0 && position.Contains(Event.current.mousePosition))
                        {
                            titleText = titleProp.stringValue;
                        }
                    }

                    using (var check = new EditorGUI.ChangeCheckScope())
                    {
                        GUI.SetNextControlName(TITLE_CONTROL_NAME);

                        var r = EditorGUI.TextField(position, titleText, titleStyle);
                        if (check.changed && isEditingProp.boolValue)
                            titleProp.stringValue = r;
                    }
                    position.y += position.height + EditorGUIUtility.standardVerticalSpacing;
                    position.x -= 7;
                    position.width += 7 * 2;

                    // description prop
                    {
                        position.y += 10;
                        var descStyle = isEditingProp.boolValue
                            ? Task_GetDescTextEdit()
                            : progressProp.floatValue < 1 ? Task_GetUnfinishedDescText() : Task_GetFinishedDescText();
                        var height = !isEditingProp.boolValue && string.IsNullOrEmpty(descriptionProp.stringValue)
                            ? 0
                            : isEditingProp.boolValue
                                ? Mathf.Max(descStyle.CalcHeight(new GUIContent(descriptionProp.stringValue), position.width), 50)
                                : descStyle.CalcHeight(new GUIContent(descriptionProp.stringValue), position.width);
                        position.height = height;
                        position.width = position.width;
                        using (var check = new EditorGUI.ChangeCheckScope())
                        {
                            GUI.SetNextControlName(DESCRIPTION_CONTROL_NAME);
                            var r = EditorGUI.TextArea(position, descriptionProp.stringValue, descStyle);
                            if (check.changed && isEditingProp.boolValue)
                            {
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

                        for (int i = 0; i < bulletPointsProp.arraySize; i++)
                        {
                            var prop = bulletPointsProp.GetArrayElementAtIndex(i);
                            var toggleProp = prop.FindPropertyRelative(nameof(BulletPoint.done));
                            var descProp = prop.FindPropertyRelative(nameof(BulletPoint.description));

                            // checkbox
                            position.width = checkboxWidth;
                            GUI.SetNextControlName(BULLETPOINT_TOGGLE_CONTROL_NAME + i);
                            toggleProp.boolValue = EditorGUI.Toggle(position, toggleProp.boolValue);

                            // description
                            position.x += checkboxWidth;
                            position.width = lastWidth - checkboxWidth;
                            if (isEditingProp.boolValue) position.width -= removeButtonWidth;
                            var descStyle = isEditingProp.boolValue
                                ? Task_GetBulletpointNameEdit()
                                : progressProp.floatValue < 1 && !toggleProp.boolValue ? EditorStyles.label : Task_GetFinishedBulletpoint();
                            using (var check = new EditorGUI.ChangeCheckScope())
                            {
                                var r = EditorGUI.TextField(position, descProp.stringValue, descStyle);
                                if (check.changed && isEditingProp.boolValue)
                                    descProp.stringValue = r;
                            }

                            // remove button
                            if (isEditingProp.boolValue)
                            {
                                position.x += position.width;
                                position.width = removeButtonWidth;
                                if (GUI.Button(position, new GUIContent(TaskList_GetDeleteTex(), "Delete Bullet-Point")))
                                {
                                    bulletPointsProp.DeleteArrayElementAtIndex(i);
                                    i--;
                                }
                            }

                            position.x = lastX;
                            position.y += 20 + EditorGUIUtility.standardVerticalSpacing;
                        }

                        // new bullet-point
                        if (isEditingProp.boolValue)
                        {
                            position.width = 20;
                            GUI.enabled = false;
                            EditorGUI.Toggle(position, false);
                            GUI.enabled = true;
                            position.x += 20;
                            position.width = lastWidth - 20;
                            using (var check = new EditorGUI.ChangeCheckScope())
                            {
                                var r = EditorGUI.TextField(position, "(New bullet point item)", Task_GetBulletpointNew());
                                if (check.changed)
                                {
                                    bulletPointsProp.arraySize++;
                                    var prop = bulletPointsProp.GetArrayElementAtIndex(bulletPointsProp.arraySize - 1);
                                    prop.FindPropertyRelative(nameof(BulletPoint.description)).stringValue = r;
                                    prop.FindPropertyRelative(nameof(BulletPoint.done)).boolValue = false;
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

                        for (int i = 0; i < referencesProp.arraySize; i++)
                        {

                            var element = referencesProp.GetArrayElementAtIndex(i);
                            var nameProp = element.FindPropertyRelative(nameof(Reference.name));
                            var pathProp = element.FindPropertyRelative(nameof(Reference.path));

                            position.width = Mathf.Min(EditorStyles.label.CalcSize(new GUIContent(nameProp.stringValue)).x + 10, lastWidth * 0.5f);
                            var nameStyle = isEditingProp.boolValue ? Task_GetReferenceNameEdit()
                                : progressProp.floatValue < 1 ? EditorStyles.label : Task_GetFinishedReferenceName();
                            using (var check = new EditorGUI.ChangeCheckScope())
                            {
                                var r = EditorGUI.TextField(position, nameProp.stringValue, nameStyle);
                                if (check.changed && isEditingProp.boolValue)
                                    nameProp.stringValue = r;
                            }

                            // object reference
                            position.x += position.width;
                            position.width = lastWidth - position.width;
                            if (isEditingProp.boolValue) position.width -= removeWidth;
                            if (!_path2Obj.TryGetValue(pathProp.stringValue, out var reference))
                                _path2Obj[pathProp.stringValue] = reference = AssetDatabase.LoadAssetAtPath<Object>(pathProp.stringValue);
                            using (new EditorGUI.DisabledScope(!isEditingProp.boolValue && progressProp.floatValue >= 1))
                            using (var check = new EditorGUI.ChangeCheckScope())
                            {
                                var r = EditorGUI.ObjectField(position, reference, typeof(Object), false);
                                if (check.changed)
                                {
                                    pathProp.stringValue = AssetDatabase.GetAssetPath(r);
                                    if (!isEditingProp.boolValue) isEditingProp.boolValue = true;
                                }
                            }

                            position.x += position.width; position.width = removeWidth;
                            // remove button
                            if (isEditingProp.boolValue)
                            {
                                if (GUI.Button(position, new GUIContent(TaskList_GetDeleteTex(), "Delete Object Reference")))
                                {
                                    referencesProp.DeleteArrayElementAtIndex(i);
                                    i--;
                                }
                            }

                            position.x = lastX;
                            position.y += position.height + EditorGUIUtility.standardVerticalSpacing;
                        }

                        // new object reference button
                        if (isEditingProp.boolValue)
                        {
                            position.width = lastWidth * 0.55f - 10;
                            using (var check = new EditorGUI.ChangeCheckScope())
                            {
                                var name = EditorGUI.TextField(position, "(New Object name)", Task_GetBulletpointNew());
                                position.x += position.width + 10;
                                position.width = lastWidth - 10 - position.width;
                                var reference = EditorGUI.ObjectField(position, null, typeof(Object), false);
                                if (check.changed)
                                {
                                    referencesProp.arraySize++;
                                    var prop = referencesProp.GetArrayElementAtIndex(referencesProp.arraySize - 1);
                                    prop.FindPropertyRelative(nameof(Reference.name)).stringValue = name;
                                    prop.FindPropertyRelative(nameof(Reference.path)).stringValue = AssetDatabase.GetAssetPath(reference);
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
                    var oldWidth = position.width;
                    var oldX = position.x;
                    if (isEditingProp.boolValue)
                    {
                        GUI.SetNextControlName(PROGRESS_CONTROL_NAME);
                        bool hasProgress = progressProp.floatValue != -1;
                        using (var check = new EditorGUI.ChangeCheckScope())
                        {
                            position.width = NO_LABEL_TOGGLE_WIDTH;
                            hasProgress = EditorGUI.ToggleLeft(position, GUIContent.none, hasProgress);
                            if (check.changed)
                            {
                                progressProp.floatValue = hasProgress ? 0 : -1;
                            }
                            position.x += position.width + NO_LABLE_TOGGLE_SPACE;
                            position.width = oldWidth - NO_LABEL_TOGGLE_WIDTH - NO_LABLE_TOGGLE_SPACE;
                        }
                        if (hasProgress)
                        {
                            GUI.SetNextControlName(PROGRESS_CONTROL_NAME);
                            progressProp.floatValue = EditorGUI.Slider(position, progressProp.floatValue, 0, 1);
                        }
                        else
                        {
                            using (new EditorGUI.DisabledGroupScope(true))
                                EditorGUI.Slider(position, 0, 0, 1);
                        }
                        position.y += position.height + EditorGUIUtility.standardVerticalSpacing;
                    }
                    else
                    {
                        if (progressProp.floatValue != -1 || bulletPointsProp.arraySize > 0)
                        {
                            var rect = new Rect(position.x, position.y + position.height * 0.25f, position.width, position.height * (1 - 0.25f * 2));
                            EditorGUI.DrawRect(rect, Taskt_ProgOutlineCol);
                            rect.x += 1;
                            rect.y += 1;
                            rect.width -= 2;
                            rect.height -= 2;
                            EditorGUI.DrawRect(rect, Taskt_ProgressBackCol);
                            float progress = 0;
                            if (progressProp.floatValue >= 0)
                            {
                                progress = progressProp.floatValue;
                            }
                            else
                            {
                                // sum finished tasks
                                float c = 1f / bulletPointsProp.arraySize;
                                for (int i = 0; i < bulletPointsProp.arraySize; i++)
                                {
                                    if (bulletPointsProp.GetArrayElementAtIndex(i).FindPropertyRelative(nameof(BulletPoint.done)).boolValue)
                                    {
                                        progress += c;
                                    }
                                }
                            }
                            rect.width *= progress;
                            EditorGUI.DrawRect(rect, progressProp.floatValue < 1 ? Taskt_UnfinishedProgressCol : Taskt_FinishedProgressCol);
                            position.y += rect.height * 2 + 2;
                        }
                    }
                    position.width = oldWidth;
                    position.x = oldX;

                    // edit colors
                    if (isEditingProp.boolValue)
                    {
                        const int LabelWidth = 80;

                        // title color
                        GUI.Label(position, "Title:");
                        position.width = NO_LABEL_TOGGLE_WIDTH;
                        position.x += LabelWidth;
                        GUI.SetNextControlName(TITLECOLOR_CONTROL_NAME);
                        useCustomTitleColorProp.boolValue = EditorGUI.ToggleLeft(position, GUIContent.none, useCustomTitleColorProp.boolValue);
                        position.x += position.width + NO_LABLE_TOGGLE_SPACE;
                        position.width = oldWidth - NO_LABEL_TOGGLE_WIDTH - NO_LABLE_TOGGLE_SPACE - LabelWidth;
                        GUI.SetNextControlName(TITLECOLOR_CONTROL_NAME);
                        using (new EditorGUI.DisabledScope(!useCustomTitleColorProp.boolValue))
                            titleColorProp.colorValue = EditorGUI.ColorField(position, GUIContent.none, titleColorProp.colorValue);
                        position.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

                        // background color
                        position.x = oldX;
                        GUI.Label(position, "Background:");
                        position.width = NO_LABEL_TOGGLE_WIDTH;
                        position.x += LabelWidth;
                        GUI.SetNextControlName(TITLECOLOR_CONTROL_NAME);
                        useCustomBackgroundColorProp.boolValue = EditorGUI.ToggleLeft(position, GUIContent.none, useCustomBackgroundColorProp.boolValue);
                        position.x += position.width + NO_LABLE_TOGGLE_SPACE;
                        position.width = oldWidth - NO_LABEL_TOGGLE_WIDTH - NO_LABLE_TOGGLE_SPACE - LabelWidth;
                        GUI.SetNextControlName(TITLECOLOR_CONTROL_NAME);
                        using (new EditorGUI.DisabledScope(!useCustomBackgroundColorProp.boolValue))
                            backgroundColorProp.colorValue = EditorGUI.ColorField(position, GUIContent.none, backgroundColorProp.colorValue);
                        position.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
                    }

                }
            }

            public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
            {
                var descriptionProp = property.FindPropertyRelative(nameof(description));
                var titleProp = property.FindPropertyRelative(nameof(title));
                var isEditingProp = property.FindPropertyRelative(nameof(isEditing));
                var progressProp = property.FindPropertyRelative(nameof(progress));
                var referencesProp = property.FindPropertyRelative(nameof(references));
                var bulletPointsProp = property.FindPropertyRelative(nameof(bulletPoints));

                float h = 10;

                // title
                var titleStyle = isEditingProp.boolValue
                    ? Task_GetTitleTextEdit()
                    : progressProp.floatValue < 1 ? Task_GetUnfinishedTitleText() : Task_GetFinishedTitleText();
                var titleText = progressProp.floatValue < 1 || isEditingProp.boolValue ? titleProp.stringValue : StrikeThrough(titleProp.stringValue);
                h += titleStyle.CalcHeight(new GUIContent(titleText), TaskList.Editor.currentTaskListWidth - 7 * 2);
                h += EditorGUIUtility.standardVerticalSpacing;

                // desc
                h += 10;
                if (isEditingProp.boolValue || !string.IsNullOrEmpty(descriptionProp.stringValue))
                {
                    var descStyle = isEditingProp.boolValue
                        ? Task_GetDescTextEdit()
                        : progressProp.floatValue < 1 ? Task_GetUnfinishedDescText() : Task_GetFinishedDescText();
                    h += isEditingProp.boolValue
                        ? Mathf.Max(descStyle.CalcHeight(new GUIContent(descriptionProp.stringValue), EditorGUIUtility.currentViewWidth), 50)
                        : descStyle.CalcHeight(new GUIContent(descriptionProp.stringValue), TaskList.Editor.currentTaskListWidth);

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

                // progress
                if (progressProp.floatValue != -1 || bulletPointsProp.arraySize > 0 || isEditingProp.boolValue)
                {
                    h += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
                }

                // color editing
                if (isEditingProp.boolValue)
                {
                    h += EditorGUIUtility.singleLineHeight * 2 + EditorGUIUtility.standardVerticalSpacing * 2;
                }
                h += 5;
                return h;
            }
        }
    }
}