#if NETICK
using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

using UnityEngine;
using Netick;

namespace Cjx.Unity.Netick.Editor
{
    [CustomPropertyDrawer(typeof(NetworkBool))]
    public class NetworkBoolPropertyDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var field = new Toggle();
            field.label = property.displayName;
            field.RegisterValueChangedCallback(e =>
            {
                property.FindPropertyRelative("RawValue").intValue = e.newValue ? 1 : 0;
            });
            field.value = property.FindPropertyRelative("RawValue").intValue != 0;
            // EditorEx is in MyEditor file; keep style configuration if available
            var configureMethod = typeof(Cjx.Unity.Netick.Editor.EditorEx).GetMethod("ConfigureStyle", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
            if (configureMethod != null)
            {
                try { configureMethod.MakeGenericMethod(typeof(Toggle), typeof(bool)).Invoke(null, new object[] { field }); } catch { }
            }
            return field;
        }
    }
}
#endif
