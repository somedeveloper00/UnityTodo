using System;
using UnityEditor;
using UnityEngine;

namespace UnityTodo {
    internal static class GUIStyles {

        [NonSerialized] static GUIStyle _bigTextField; 
        public static GUIStyle GetBigTextField() {
            if (_bigTextField == null) {
                _bigTextField = new GUIStyle( EditorStyles.textField );
                _bigTextField.fontSize = 20;
                _bigTextField.fontStyle = FontStyle.Bold;
                _bigTextField.alignment = TextAnchor.MiddleCenter;
            }

            return _bigTextField;
        }

        [NonSerialized] static GUIStyle _bigLabel; 
        public static GUIStyle GetBigLabel() {
            if (_bigLabel == null) {
                _bigLabel = new GUIStyle( EditorStyles.label );
                _bigLabel.fontSize = 20;
                _bigLabel.fontStyle = FontStyle.Bold;
                _bigLabel.alignment = TextAnchor.MiddleCenter;
            }

            return _bigLabel;
        }

        [NonSerialized] static GUIStyle _normalTextField; 
        public static GUIStyle GetNormalTextField() {
            if (_normalTextField == null)
            {
                _normalTextField = new GUIStyle( EditorStyles.textField );
                _normalTextField.fontSize = 15;
                _normalTextField.wordWrap = true;
                _normalTextField.richText = true;
                _normalTextField.alignment = TextAnchor.MiddleCenter;
            }

            return _normalTextField;
        }

        [NonSerialized] static GUIStyle _normalLabel; 
        public static GUIStyle GetNormalLabel() {
            if (_normalLabel == null)
            {
                _normalLabel = new GUIStyle( EditorStyles.label );
                _normalLabel.fontSize = 15;
                _normalLabel.wordWrap = true;
                _normalLabel.richText = true;
                _normalLabel.alignment = TextAnchor.MiddleCenter;
            }

            return _normalLabel;
        }

        [NonSerialized] static GUIStyle _smallTextField; 
        public static GUIStyle GetSmallTextField() {
            if (_smallTextField == null)
            {
                _smallTextField = new GUIStyle( EditorStyles.textField );
                _smallTextField.fontSize = 12;
                _smallTextField.wordWrap = true;
                _smallTextField.richText = true;
            }

            return _smallTextField;
        }

        [NonSerialized] static GUIStyle _smallLabel; 
        public static GUIStyle GetSmallLabel() {
            if (_smallLabel == null)
            {
                _smallLabel = new GUIStyle( EditorStyles.label );
                _smallLabel.fontSize = 12;
                _smallLabel.wordWrap = true;
                _smallLabel.alignment = TextAnchor.MiddleLeft;
                _smallLabel.richText = true;
            }

            return _smallLabel;
        }

#region TaskList
        
        public static Color TaskList_ProgOutlineCol => EditorGUIUtility.isProSkin ? new ( 0.1f, 0.1f, 0.1f ) : new (0.9f, 0.9f, 0.9f);
        public static Color TaskList_ProgBackCol => EditorGUIUtility.isProSkin ? new ( 0.2f, 0.2f, 0.2f ) : new (0.8f, 0.8f, 0.8f);
        public static Color TaskList_ProgFillCol => EditorGUIUtility.isProSkin ? new ( 0f, 0.4f, 0f ) : new (0.5f, 0.75f, 0.5f);

        [NonSerialized] static GUIStyle _taskList_ProgText;
        public static GUIStyle TaskList_GetProgText() {
            if (_taskList_ProgText == null) {
                _taskList_ProgText = new GUIStyle( EditorStyles.label );
                _taskList_ProgText.fontSize = 12;
                _taskList_ProgText.fontStyle = FontStyle.BoldAndItalic;
                Color color = EditorGUIUtility.isProSkin ? new(0.7f, 0.7f, 0.7f) : new(0.3f, 0.3f, 0.3f);
                _taskList_ProgText.normal.textColor = color;
                _taskList_ProgText.alignment = TextAnchor.MiddleCenter;
            }
            return _taskList_ProgText;
        }
        

#endregion
    }
}