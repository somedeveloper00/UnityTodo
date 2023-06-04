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
    }
}