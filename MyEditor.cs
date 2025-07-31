using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Netick;
using Netick.Unity;
using NetickEditor;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Cjx.Unity.Netick.Editor
{
    using Editor = UnityEditor.Editor;

    [InitializeOnLoad]
    internal static class Entry
    {
        static Entry()
        {
            EditorApplication.delayCall += Init;
        }

        static void Init()
        {
#if UNITY_6000_0_OR_NEWER
            Init_6X();
#else
            Init_2022_X();
#endif
        }

        private static void Init_6X()
        {
            var o = typeof(Editor).Assembly.GetType("UnityEditor.CustomEditorAttributes");//
            var instanceGet = o.GetProperty("instance", BindingFlags.Static | BindingFlags.NonPublic);
            var instance = instanceGet.GetValue(null);
            var cache = instance.GetType().GetField("m_Cache", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(instance);
            var dictField = cache.GetType().GetField("m_CustomEditorCache", BindingFlags.Instance | BindingFlags.NonPublic);
            var dict = dictField.GetValue(cache);
            var storage = Activator.CreateInstance(dict.GetType().GetGenericArguments()[1]);
            var lsField = storage.GetType().GetField("customEditors");
            var lsType = lsField.FieldType;
            var lsInst = Activator.CreateInstance(lsType);
            lsField.SetValue(storage, lsInst);
            var monoEditorInst = Activator.CreateInstance(lsType.GetGenericArguments()[0], new object[] { typeof(MyEditor), null, true, false });
            lsType.GetMethod("Add").Invoke(lsInst, new[] { monoEditorInst });
            var addMd = dictField.FieldType.GetMethod("Add");
            var removeMd = dictField.FieldType.GetMethods().FirstOrDefault(x => x.Name == "Remove" && x.GetParameters().Length == 1);
            removeMd.Invoke(dict, new[] { typeof(NetworkBehaviour) });
            addMd.Invoke(dict, new object[] { typeof(NetworkBehaviour), storage });
        }

        private static void Init_2022_X()
        {
            var o = typeof(Editor).Assembly.GetType("UnityEditor.CustomEditorAttributes");//
            var dictField = o.GetField("kSCustomEditors", BindingFlags.Static | BindingFlags.NonPublic);
            var dict = dictField.GetValue(null);
            var listType = dict.GetType().GetGenericArguments()[1];
            var monoEditorType = listType.GetGenericArguments()[0];
            var bindingFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            var m_InspectedTypeField = monoEditorType.GetField("m_InspectedType", bindingFlags);
            var m_InspectorTypeField = monoEditorType.GetField("m_InspectorType", bindingFlags);
            var m_RenderPipelineTypeField = monoEditorType.GetField("m_RenderPipelineType", bindingFlags);
            var m_EditorForChildClassesField = monoEditorType.GetField("m_EditorForChildClasses", bindingFlags);
            var m_IsFallbackField = monoEditorType.GetField("m_IsFallback", bindingFlags);
            var monoEditorTypeInst = Activator.CreateInstance(monoEditorType);

            m_InspectedTypeField.SetValue(monoEditorTypeInst, typeof(NetworkBehaviour));
            m_InspectorTypeField.SetValue(monoEditorTypeInst, typeof(MyEditor));
            //m_RenderPipelineTypeField.SetValue(monoEditorTypeInst, GraphicsSettings.defaultRenderPipeline.GetType());
            m_EditorForChildClassesField.SetValue(monoEditorTypeInst, true);
            m_IsFallbackField.SetValue(monoEditorTypeInst, false);

            var lsInst = Activator.CreateInstance(listType);
            listType.GetMethod("Add").Invoke(lsInst, new[] { monoEditorTypeInst });

            var addMd = dictField.FieldType.GetMethod("Add");

            var removeMd = dictField.FieldType.GetMethods().FirstOrDefault(x => x.Name == "Remove" && x.GetParameters().Length == 1);

            removeMd.Invoke(dict, new[] { typeof(NetworkBehaviour) });
            addMd.Invoke(dict, new object[] { typeof(NetworkBehaviour), lsInst });
        }
    }

    [CustomPropertyDrawer(typeof(NetworkBool))]
    public class NetworkBoolPropertyDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var field = new Toggle();
            field.label = property.displayName;
            field.RegisterValueChangedCallback(e => {
                property.FindPropertyRelative("RawValue").intValue = e.newValue ? 1 : 0;
            });
            field.value = property.FindPropertyRelative("RawValue").intValue != 0;
            EditorEx.ConfigureStyle<Toggle,bool>(field);
            return field;
        }
    }

    internal class MyEditor : Editor
    {
        EditorApplication.CallbackFunction update;

        public unsafe override VisualElement CreateInspectorGUI()
        {

            var root = new VisualElement();

            DefaultInspector(root);
            
            if (((NetworkBehaviour)target).StatePtr != null)
            {
                CreateDebugEditor(root);
            }
            return root;
        }

        private void DefaultInspector(VisualElement root)
        {
            var networkProperties = new VisualElement();
            SerializedProperty iterator = serializedObject.GetIterator();
            if (iterator.NextVisible(enterChildren: true))
            {
                do
                {
                    switch (iterator.name)
                    {
                        case "m_Name":
                        case "m_EditorClassIdentifier":
                            continue;
                    }

                    var propertyField = new PropertyField2(iterator)
                    {
                        name = "PropertyField:" + iterator.propertyPath
                    };
                    if (iterator.propertyPath == "m_Script")
                    {
                        propertyField.SetEnabled(value: false);
                    }

                    var parameters = new object[] { iterator, null };
                    FieldInfo fieldInfoFromProperty = PropertyField2.CallStatic<FieldInfo>(PropertyField2.ScriptAttributeUtilityType, "GetFieldInfoFromProperty", parameters);
                    var type = parameters[1] as Type;
                    if (type != null && iterator.name.EndsWith(">k__BackingField"))
                    {
                        var p = fieldInfoFromProperty.DeclaringType.GetProperty(iterator.name.Substring(1, iterator.name.Length - 1 - ">k__BackingField".Length));
                        if (p != null && p.CustomAttributes.Any(x => x.AttributeType == typeof(Networked)))
                        {
                            networkProperties.Add(propertyField);
                            continue;
                        }
                    }

                    root.Add(propertyField);
                }
                while (iterator.Next(enterChildren: false));
            }
            if (networkProperties.childCount > 0)
            {
                var label = new Label("Network Properties");
                label.style.unityFontStyleAndWeight = new StyleEnum<FontStyle>(FontStyle.BoldAndItalic);
                label.style.fontSize = 14;
                ColorUtility.TryParseHtmlString("#5FA6F3", out var color);
                label.style.color = new StyleColor(color);
                networkProperties.Insert(0, label);
                networkProperties.Insert(1,CreateSplitLine());
                networkProperties.Add(CreateSplitLine());
                //networkProperties.style.backgroundColor = new StyleColor(new Color(0.2013172f, 0.365789f, 0.490566f, 0.7607843f));
                root.Insert(1, networkProperties);
            }
        }

        private VisualElement CreateSplitLine()
        {
            var splitLine = new VisualElement();
            splitLine.style.backgroundColor = new StyleColor(Color.grey);
            splitLine.style.marginTop = new StyleLength(8f);
            splitLine.style.marginBottom = new StyleLength(8f);
            splitLine.style.height = new StyleLength(1f);
            return splitLine;
        }

        private unsafe void CreateDebugEditor(VisualElement root)
        {
            root.Add(CreateSplitLine());
            var foldOut = new Foldout();
            foldOut.value = false;
            foldOut.text = "Network State (Runtime)";
            Action update = null;
            var content = EditorEx.Configure(target.GetType(), () => target, ref update);
            foldOut.Add(content);
            root.Add(foldOut);
            this.update = () => {
                if (((NetworkBehaviour)target).StatePtr != null){
                    update();
                }
            };
            EditorApplication.update += this.update;
        }

        private void OnDestroy()
        {
            if (update != null)
            {
                EditorApplication.update -= update;
            }
        }
    }

    internal static class EditorEx
    {

        static BindingFlags All = BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic;

        static MethodInfo configureStyleMethod = typeof(PropertyField).GetMethod("ConfigureFieldStyles",BindingFlags.Static | BindingFlags.NonPublic);

        public static VisualElement Configure(Type type, Func<object> targetGet, ref Action update)
        {
            object source = null;

            update += () =>
            {
                source = targetGet();
            };

            var root = new VisualElement();
            bool isNetworkBehaviour = typeof(NetworkBehaviour).IsAssignableFrom(type);

            if (isNetworkBehaviour && type.BaseType != typeof(NetworkBehaviour))
            {
                var baseRoot = Configure(type.BaseType, targetGet, ref update);
                root.Add(baseRoot);
            }

            foreach (var prop in type.GetProperties(All).Where(x => x.DeclaringType == type && x.CustomAttributes.Any(x => x.AttributeType == typeof(Networked))))
            {
                AddDisplayItem(root, prop.Name, prop.PropertyType, () => prop.GetMethod.Invoke(source, Array.Empty<object>()), ref update);
            }

            if (!isNetworkBehaviour)
            {
                foreach (var field in type.GetFields(All))
                {
                    AddDisplayItem(root, field.Name, field.FieldType, () => field.GetValue(source), ref update);
                }
            }
            else
            {
                foreach (var field in type.GetFields(All).Where(x => x.CustomAttributes.Any(x => x.AttributeType == typeof(Networked))))
                {
                    AddDisplayItem(root, field.Name, field.FieldType, () => field.GetValue(source), ref update);
                }
            }
            return root;
        }

        static TField ConfigureField<TField, TValue>(VisualElement root, string name, Func<object> getValue, ref Action update) where TField : BaseField<TValue>, new()
        {
            var fd = new TField();
            fd.label = name;
            root.Add(fd);
            update += () =>
            {
                fd.value = (TValue)getValue();
            };
            fd.SetEnabled(false);
            ConfigureStyle<TField,TValue>(fd);
            return fd;
        }

        public static void ConfigureStyle<TField,TValue>(TField field)
        {
            configureStyleMethod.MakeGenericMethod(typeof(TField),typeof(TValue)).Invoke(null, new object[] { field });
        }

        static void AddDisplayItem(VisualElement root, string name, Type type, Func<object> getValue, ref Action update)
        {
            if (type.IsValueType)
            {
                if (type == typeof(int))
                {
                    ConfigureField<IntegerField,int>(root, name, getValue, ref update);
                }
                else if (type == typeof(short))
                {
                    ConfigureField<IntegerField, int>(root, name, () => (int)(short)getValue(), ref update);
                }
                else if (type == typeof(uint))
                {
                    ConfigureField<UnsignedIntegerField, uint>(root, name, () => (uint)getValue(), ref update);
                }
                else if (type == typeof(ushort))
                {
                    ConfigureField<UnsignedIntegerField, uint>(root, name, () => (uint)(ushort)getValue(), ref update);
                }
                else if (type == typeof(long))
                {
                    ConfigureField<LongField, long>(root, name, getValue, ref update);
                }
                else if (type == typeof(ulong))
                {
                    ConfigureField<UnsignedLongField, ulong>(root, name, getValue, ref update);
                }
                else if (type == typeof(float))
                {
                    ConfigureField<FloatField, float>(root, name, getValue, ref update);
                }
                else if (type == typeof(double))
                {
                    ConfigureField<DoubleField, double>(root, name, getValue, ref update);
                }
                else if (type == typeof(Vector3))
                {
                    ConfigureField<Vector3Field, Vector3>(root, name, getValue, ref update);
                }
                else if (type == typeof(Vector3Int))
                {
                    ConfigureField<Vector3IntField, Vector3Int>(root, name, getValue, ref update);
                }
                else if (type == typeof(Vector2Int))
                {
                    ConfigureField<Vector2IntField, Vector2Int>(root, name, getValue, ref update);
                }
                else if (type == typeof(Vector2))
                {
                    ConfigureField<Vector2Field, Vector2>(root, name, getValue, ref update);
                }
                else if (type == typeof(Vector4))
                {
                    ConfigureField<Vector4Field, Vector4>(root, name, getValue, ref update);
                }
                else if (type == typeof(Color))
                {
                    ConfigureField<ColorField, Color>(root, name, getValue, ref update);
                }
                else if (type == typeof(Rect))
                {
                    ConfigureField<RectField, Rect>(root, name, getValue, ref update);
                }
                else if (type == typeof(Bounds))
                {
                    ConfigureField<BoundsField, Bounds>(root, name, getValue, ref update);
                }
                else if (type == typeof(Hash128))
                {
                    ConfigureField<Hash128Field, Hash128>(root, name, getValue, ref update);
                }
                else if (type == typeof(bool))
                {
                    ConfigureField<Toggle, bool>(root, name, getValue, ref update);
                }
                else if (type == typeof(NetworkBool))
                {
                    ConfigureField<Toggle, bool>(root, name, ()=> (bool)(NetworkBool)getValue(), ref update);
                }
                else if (type == typeof(Quaternion))
                {
                    ConfigureField<Vector4Field, Vector4>(root, name, () => {
                        var raw = (Quaternion)getValue();
                        return new Vector4(raw.x,raw.y,raw.z,raw.w);
                    }, ref update);
                    ConfigureField<Vector3Field, Vector3>(root, name + ".eulerAngles", () => ((Quaternion)getValue()).eulerAngles, ref update);
                }
                else if (type.IsEnum)
                {
                    if (type.CustomAttributes.Any(x => x.AttributeType == typeof(FlagsAttribute)))
                    {
                        var fd = ConfigureField<EnumFlagsField,Enum>(root, name, getValue, ref update);
                        fd.Init((Enum)Activator.CreateInstance(type), false);
                    }
                    else
                    {
                        var fd = ConfigureField<EnumField, Enum>(root, name, getValue, ref update);
                        fd.Init((Enum)Activator.CreateInstance(type), false);
                    }
                }
                else if (type.FullName.StartsWith("Netick.NetworkString"))
                {
                    ConfigureField<TextField, string>(root, name, () => getValue().ToString(), ref update);
                }
                else if (type.FullName.StartsWith("Netick.FixedSize"))
                {
                    Action lsUpdate = null;
                    update += () => lsUpdate?.Invoke();
                    List<object> source = new List<object>();
                    var ls = new ListView();
                    var fields = type.GetFields(All).ToArray();
                    
                    ls.makeItem = () =>
                    {
                        var item = new VisualElement();
                        return item;
                    };
                    ls.bindItem = (v, i) =>
                    {
                        Action itemUpdate = null;
                        AddDisplayItem(v, $"Element{i}", type.GetGenericArguments()[0], () => source[i], ref itemUpdate);
                        Action action = () => itemUpdate?.Invoke();
                        v.userData = action;
                        lsUpdate += action;
                    };
                    ls.unbindItem = (v, i) =>
                    {
                        v.Clear();
                        Action updateItem = v.userData as Action;
                        lsUpdate -= updateItem;
                    };
                    ls.virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight;
                    update += () =>
                    {
                        if (ls.itemsSource == null)
                        {
                            var buffer = getValue();
                            for (int i = 0; i < fields.Length; i++)
                            {
                                var element = fields[i].GetValue(buffer);
                                source.Add(element);
                            }
                            ls.itemsSource = source;
                        }
                    };
                    var foldOut = new Foldout();
                    foldOut.text = name;
                    var scroll = new ScrollView();
                    scroll.Add(ls);
                    foldOut.Add(scroll);
                    root.Add(foldOut);
                    scroll.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
                    scroll.verticalScrollerVisibility = ScrollerVisibility.Auto;
                    scroll.style.maxHeight = new StyleLength(240);
                }
                else if (!type.IsPrimitive)
                {
                    var content = Configure(type, getValue, ref update);
                    bool needFoldOut = true;
                    if (type.IsConstructedGenericType && typeof(KeyValuePair<,>) == type.GetGenericTypeDefinition())
                    {
                        content.style.flexDirection = new StyleEnum<FlexDirection>(FlexDirection.Row);
                        needFoldOut = false;
                    }
                    if (needFoldOut)
                    {
                        var foldOut = new Foldout();
                        foldOut.text = name;
                        foldOut.Add(content);
                        foldOut.value = false;
                        root.Add(foldOut);
                    }
                    else
                    {
                        var label = new Label(name);
                        label.style.unityTextAlign = new StyleEnum<TextAnchor>(TextAnchor.MiddleCenter);
                        content.Insert(0, label);
                        root.Add(content);
                    }
                }
            }
            else
            {
                if (typeof(IEnumerable).IsAssignableFrom(type))
                {
                    int displayCount = 0;
                    Action lsUpdate = null;
                    update += () => lsUpdate?.Invoke();
                    List<object> source = new List<object>();
                    var ls = new ListView();
                    ls.virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight;
                    ls.makeItem = () =>
                    {
                        var item = new VisualElement();
                        return item;
                    };
                    var elementType = type.GetInterface("IEnumerable`1").GetGenericArguments()[0];
                    ls.bindItem = (v, i) =>
                    {
                        Action itemUpdate = null;
                        AddDisplayItem(v, $"Element{i}", elementType, () => source[i], ref itemUpdate);
                        v.userData = itemUpdate;
                        lsUpdate += itemUpdate;
                    };
                    ls.unbindItem = (v, i) =>
                    {
                        v.Clear();
                        Action updateItem = v.userData as Action;
                        lsUpdate -= updateItem;
                    };
                    update += () =>
                    {
                        source.Clear();
                        var buffer = (getValue() as IEnumerable).GetEnumerator();
                        while (buffer.MoveNext())
                        {
                            source.Add(buffer.Current);
                        }
                        ls.itemsSource = source;

                        if (displayCount != source.Count)
                        {
                            displayCount = source.Count;
                            ls.Rebuild();
                        }
                    };
                    var foldOut = new Foldout();
                    foldOut.text = name;
                    var scroll = new ScrollView();
                    scroll.Add(ls);
                    foldOut.Add(scroll);
                    root.Add(foldOut);
                    scroll.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
                    scroll.verticalScrollerVisibility = ScrollerVisibility.Auto;
                    scroll.style.maxHeight = new StyleLength(240);
                }
            }
        }

    }

}