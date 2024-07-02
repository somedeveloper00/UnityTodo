using System;
using UnityEngine;

namespace UnityTodo
{
    internal sealed class GUIUtilities
    {
        public class GUIColor : IDisposable
        {
            Color col;
            public GUIColor(Color color)
            {
                col = GUI.color;
                GUI.color = color;
            }
            public void Dispose() => GUI.color = col;
        }

        public sealed class GUIBackgroundColor : IDisposable
        {
            Color col;
            public GUIBackgroundColor(Color color)
            {
                col = GUI.backgroundColor;
                GUI.backgroundColor = color;
            }
            public void Dispose() => GUI.backgroundColor = col;
        }

        public static string StrikeThrough(string text)
        {
            string strikethrough = "";
            foreach (char c in text) strikethrough = strikethrough + c + '\u0336';
            return strikethrough;
        }
    }
}