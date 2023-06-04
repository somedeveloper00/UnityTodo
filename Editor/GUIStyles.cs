using System;
using UnityEditor;
using UnityEngine;

namespace UnityTodo {
    internal static class GUIStyles {
        
        const int BIG_FONT_SIZE = 20;
        const int NORMAL_FONT_SIZE = 15;
        const int SMALL_FONT_SIZE = 12;

        [NonSerialized] static GUIStyle _bigTextField;
        public static GUIStyle GetBigTextField() {
            if (_bigTextField == null) {
                var style = new GUIStyle( EditorStyles.textField );
                style.fontSize = BIG_FONT_SIZE;
                style.fontStyle = FontStyle.Bold;
                style.alignment = TextAnchor.MiddleCenter;
                _bigTextField = style;
            }

            return _bigTextField;
        }

        [NonSerialized] static GUIStyle _bigLabel;
        public static GUIStyle GetBigLabel() {
            if (_bigLabel == null) {
                var style = new GUIStyle( EditorStyles.label );
                style.fontSize = BIG_FONT_SIZE;
                style.fontStyle = FontStyle.Bold;
                style.alignment = TextAnchor.MiddleCenter;
                _bigLabel = style;
            }

            return _bigLabel;
        }

        [NonSerialized] static GUIStyle _normalTextField;
        public static GUIStyle GetNormalTextField() {
            if (_normalTextField == null) {
                var style = new GUIStyle( EditorStyles.textField );
                style.fontSize = NORMAL_FONT_SIZE;
                style.wordWrap = true;
                style.richText = true;
                style.alignment = TextAnchor.MiddleCenter;
                _normalTextField = style;
            }

            return _normalTextField;
        }

        [NonSerialized] static GUIStyle _normalLabel;
        public static GUIStyle GetNormalLabel() {
            if (_normalLabel == null) {
                var style = new GUIStyle( EditorStyles.label );
                style.fontSize = NORMAL_FONT_SIZE;
                style.wordWrap = true;
                style.richText = true;
                style.alignment = TextAnchor.MiddleCenter;
                _normalLabel = style;
            }

            return _normalLabel;
        }

        [NonSerialized] static GUIStyle _smallTextField;
        public static GUIStyle GetSmallTextField() {
            if (_smallTextField == null) {
                var style = new GUIStyle( EditorStyles.textField );
                style.fontSize = SMALL_FONT_SIZE;
                style.wordWrap = true;
                style.richText = true;
                _smallTextField = style;
            }

            return _smallTextField;
        }

        [NonSerialized] static GUIStyle _smallLabel;
        public static GUIStyle GetSmallLabel() {
            if (_smallLabel == null) {
                var style = new GUIStyle( EditorStyles.label );
                style.fontSize = SMALL_FONT_SIZE;
                style.wordWrap = true;
                style.alignment = TextAnchor.MiddleLeft;
                style.richText = true;
                _smallLabel = style;
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
                var style = new GUIStyle( EditorStyles.label );
                style.fontSize = SMALL_FONT_SIZE;
                style.fontStyle = FontStyle.BoldAndItalic;
                Color color = EditorGUIUtility.isProSkin ? new(0.7f, 0.7f, 0.7f) : new(0.3f, 0.3f, 0.3f);
                style.normal.textColor = color;
                style.alignment = TextAnchor.MiddleCenter;
                _taskList_ProgText = style;
            }
            return _taskList_ProgText;
        }

        [NonSerialized] static GUIStyle _taskList_TitleText;
        public static GUIStyle TaskList_GetTitleText() {
            if (_taskList_TitleText == null) {
                var style = new GUIStyle( EditorStyles.label );
                style.fontSize = BIG_FONT_SIZE;
                Color color = EditorGUIUtility.isProSkin ? new(1f, 1f, 1f) : new(0f, 0f, 0f);
                style.normal.textColor = color;
                style.alignment = TextAnchor.MiddleCenter;
                _taskList_TitleText = style;
            }
            return _taskList_TitleText;
        }

        [NonSerialized] static GUIStyle _taskList_ToolbarButton;
        public static GUIStyle TaskList_GetToolbarButton() {
            if (_taskList_ToolbarButton == null) {
                var style = new GUIStyle( EditorStyles.toolbarButton );
                style.fontSize = 10;
                _taskList_ToolbarButton = style;
            }
            return _taskList_ToolbarButton;
        }

        [NonSerialized] static Texture2D _taskList_DeleteTex;
        public static Texture2D TaskList_GetDeleteTex() {
            if (_taskList_DeleteTex == null) {
                _taskList_DeleteTex = EditorGUIUtility.isProSkin
                    ? EditorGUIUtility.FindTexture( "TreeEditor.Trash" )
                    : EditorGUIUtility.FindTexture( "d_TreeEditor.Trash" );
            }
            return _taskList_DeleteTex;
        }

        [NonSerialized] static Texture2D _taskListCopyTex;
        public static Texture2D TaskList_GetCopyTex() {
            if (_taskListCopyTex == null) {
                _taskListCopyTex = EditorGUIUtility.isProSkin
                    ? EditorGUIUtility.FindTexture( "d_Clipboard" )
                    : EditorGUIUtility.FindTexture( "Clipboard" );
            }
            return _taskListCopyTex;
        }

        [NonSerialized] static Texture2D _taskListPasteTex;
        public static Texture2D TaskList_GetPasteTex() {
            if (_taskListPasteTex == null) {
                _taskListPasteTex = EditorGUIUtility.isProSkin
                    ? EditorGUIUtility.FindTexture( "d_Toolbar Plus More" )
                    : EditorGUIUtility.FindTexture( "Toolbar Plus More" );
            }
            return _taskListPasteTex;
        }

#endregion

#region Task Drawer

        public static Color Taskt_ProgOutlineCol => EditorGUIUtility.isProSkin ? new ( 0.5f, 0.5f, 0.5f ) : new ( 0.3f, 0.3f, 0.3f );
        public static Color Taskt_ProgressBackCol => EditorGUIUtility.isProSkin ? new ( 0.2f, 0.2f, 0.2f ) : new ( 0.8f, 0.8f, 0.8f );
        public static Color Taskt_FinishedProgressCol => EditorGUIUtility.isProSkin ? new ( 0, 0.3f, 0 ) : new ( 0, 0.3f, 0 );
        public static Color Taskt_UnfinishedProgressCol => EditorGUIUtility.isProSkin ? new ( 0, 0.5f, 0 ) : new ( 0, 0.5f, 0 );

        [NonSerialized] static GUIStyle _task_FinishedTitleText;
        public static GUIStyle Task_GetFinishedTitleText() {
            if (_task_FinishedTitleText == null) {
                var style = new GUIStyle( EditorStyles.label );
                style.fontSize = NORMAL_FONT_SIZE;
                style.wordWrap = true;
                style.alignment = TextAnchor.MiddleCenter;
                style.richText = false;
                style.normal.textColor =
                    style.hover.textColor =
                    style.active.textColor =
                    EditorGUIUtility.isProSkin ? new(0.75f, 0.75f, 0.75f) : new(0.25f, 0.25f, 0.25f);
                _task_FinishedTitleText = style;
            }

            return _task_FinishedTitleText;
        }

        [NonSerialized] static GUIStyle _task_UnfinishedTitleText;
        public static GUIStyle Task_GetUnfinishedTitleText() {
            if (_task_UnfinishedTitleText == null) {
                var style = new GUIStyle( EditorStyles.label );
                style.fontSize = NORMAL_FONT_SIZE;
                style.wordWrap = true;
                style.fontStyle = FontStyle.Bold;
                style.richText = false;
                style.alignment = TextAnchor.MiddleCenter;
                style.normal.textColor =
                    style.hover.textColor =
                    style.active.textColor =
                    EditorGUIUtility.isProSkin ? new(0.95f, 0.95f, 0.95f) : new(0.05f, 0.05f, 0.05f);
                _task_UnfinishedTitleText = style;
            }

            return _task_UnfinishedTitleText;
        }

        [NonSerialized] static GUIStyle _task_TitleTextEdit;
        public static GUIStyle Task_GetTitleTextEdit() {
            if (_task_TitleTextEdit == null) {
                var style = new GUIStyle( EditorStyles.textField );
                style.fontSize = NORMAL_FONT_SIZE;
                style.wordWrap = true;
                style.richText = false;
                style.alignment = TextAnchor.MiddleCenter;
                _task_TitleTextEdit = style;
            }

            return _task_TitleTextEdit;
        }

        [NonSerialized] static GUIStyle _task_UnfinishedDescText;
        public static GUIStyle Task_GetUnfinishedDescText() {
            if (_task_UnfinishedDescText == null) {
                var style = new GUIStyle( EditorStyles.label );
                style.normal.textColor =
                    style.hover.textColor =
                        style.active.textColor =
                            EditorGUIUtility.isProSkin ? new(0.9f, 0.9f, 0.9f) : new(0.07f, 0.07f, 0.07f);
                style.wordWrap = true;
                style.richText = true;
                style.fontSize = SMALL_FONT_SIZE;
                style.alignment = TextAnchor.UpperLeft;
                _task_UnfinishedDescText = style;
            }

            return _task_UnfinishedDescText;
        }

        [NonSerialized] static GUIStyle _task_FinishedDescText;
        public static GUIStyle Task_GetFinishedDescText() {
            if (_task_FinishedDescText == null) {
                var style = new GUIStyle( EditorStyles.label );
                style.normal.textColor =
                    style.hover.textColor =
                        style.active.textColor =
                            EditorGUIUtility.isProSkin ? new(0.8f, 0.8f, 0.8f) : new(0.3f, 0.3f, 0.3f);
                style.wordWrap = true;
                style.richText = true;
                style.fontStyle = FontStyle.Italic;
                style.fontSize = SMALL_FONT_SIZE;
                style.alignment = TextAnchor.UpperLeft;
                _task_FinishedDescText = style;
            }

            return _task_FinishedDescText;
        }

        [NonSerialized] static GUIStyle _task_DescTextEdit;
        public static GUIStyle Task_GetDescTextEdit() {
            if (_task_DescTextEdit == null) {
                var style = new GUIStyle( EditorStyles.textArea );
                style.wordWrap = true;
                style.richText = false;
                style.fontSize = SMALL_FONT_SIZE;
                style.alignment = TextAnchor.UpperLeft;
                _task_DescTextEdit = style;
            }

            return _task_DescTextEdit;
        }
#endregion

        public static Texture2D CreateTexture(Color color) {
            var tex = new Texture2D( Texture2D.normalTexture.height, Texture2D.normalTexture.width, Texture2D.normalTexture.format, false );
            // set all pixels to clear
            var cols = new Color[16];
            Array.Fill( cols, color, 0, 16 );
            tex.SetPixels( cols );
            tex.Apply();
            return tex;
        }
    }
}