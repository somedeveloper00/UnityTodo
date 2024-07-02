using System;
using System.Collections;
using System.Reflection;
using UnityEditor;
using UnityEditorInternal;

namespace UnityTodo
{
    internal sealed class ExposedReorderableList : ReorderableList
    {
        public ExposedReorderableList(IList elements, Type elementType) : base(elements, elementType) { }
        public ExposedReorderableList(IList elements, Type elementType, bool draggable, bool displayHeader, bool displayAddButton, bool displayRemoveButton) : base(elements, elementType, draggable, displayHeader, displayAddButton, displayRemoveButton) { }
        public ExposedReorderableList(SerializedObject serializedObject, SerializedProperty elements) : base(serializedObject, elements) { }
        public ExposedReorderableList(SerializedObject serializedObject, SerializedProperty elements, bool draggable, bool displayHeader, bool displayAddButton, bool displayRemoveButton) : base(serializedObject, elements, draggable, displayHeader, displayAddButton, displayRemoveButton) { }

        public void ClearCache()
        {
#if UNITY_2022_2_OR_NEWER
            InvokeInternal("InvalidateCache");
#else
            InvokeInternal( nameof(ClearCache) );
#endif
        }

        public void CacheIfNeeded() => InvokeInternal(nameof(CacheIfNeeded));

        void InvokeInternal(string methodName, params object[] args) =>
            typeof(ReorderableList)
                .GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Default)
                !.Invoke(this, args);
    }
}