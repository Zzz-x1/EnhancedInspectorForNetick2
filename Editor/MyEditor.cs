using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
#if NETICK
using Netick;
using Netick.Unity;
#endif
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Cjx.Unity.Netick.Editor
{
    using Editor = UnityEditor.Editor;

#if NETICK

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
            var multilsField = storage.GetType().GetField("customEditorsMultiEdition");
            var lsType = lsField.FieldType;
            var lsInst = Activator.CreateInstance(lsType);
            lsField.SetValue(storage, lsInst);
            multilsField.SetValue(storage, lsInst);
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
            var dictField2 = o.GetField("kSCustomMultiEditors", BindingFlags.Static | BindingFlags.NonPublic);

            var dict = dictField.GetValue(null);
            var dict2 = dictField2.GetValue(null);

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

            removeMd.Invoke(dict2, new[] { typeof(NetworkBehaviour) });
            addMd.Invoke(dict2, new object[] { typeof(NetworkBehaviour), lsInst });
        }
    } 
#endif

#if NETICK
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
            EditorEx.ConfigureStyle<Toggle, bool>(field);
            return field;
        }
    } 
#endif

    [CanEditMultipleObjects]
    [CustomEditor(typeof(MonoBehaviour),true)]
    internal class MyEditor : Editor
    {

        [SerializeField]
        VisualTreeAsset buttonAsset;

        public unsafe override VisualElement CreateInspectorGUI()
        {
            _buttonAsset = buttonAsset;
            var root = new VisualElement();
            root.userData = target;
            DefaultInspector(root);

#if NETICK
            if (targets.Length == 1)
            {
                if (target is NetworkBehaviour nb && nb.StatePtr != null)
                {
                    CreateDebugEditor(root);
                }
            } 
#endif
            AddButtons(root);

            return root;
        }

        private void AddButtons(VisualElement root)
        {
            var methods = target.GetType()
                .GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static)
                .Where(x=>x.CustomAttributes.Any(x=>x.AttributeType.Name == "ButtonAttribute"));

            if (methods.Any())
            {
                root.Add(CreateSplitLine());
                var foldOut = CreateFoldOut("Functions");
                root.Add(foldOut);
                foreach (var method in methods)
                {
                    var item = buttonAsset.Instantiate();
                    var btn = item.Q<Button>();
                    var argsContent = item.Q("args");
                    btn.text = ">";
                    object[] args = new object[method.GetParameters().Length];

                    btn.clicked += () => {
                        foreach (var t in targets)
                        {
                            method.Invoke(method.IsStatic ? null : t, args);
                        }
                    };

                    Action update = null;

                    var @params = method.GetParameters();
                    for (int i = 0; i < @params.Length; i++)
                    {
                        var index = i;
                        var param = @params[i];
                        if (param.ParameterType == typeof(string))
                        {
                            args[i] = string.Empty;
                        }
                        else
                        {
                            if (param.ParameterType.IsValueType)
                            {
                                args[i] = Activator.CreateInstance(param.ParameterType);
                            }
                        }
                        EditorEx.AddDisplayItem(argsContent, param.Name, param.ParameterType, () => args[index], x => args[index] = x);
                    }
                    item.Q<Label>().text = method.Name;
                    update?.Invoke();
                    foldOut.Add(item);
                }
            }
        }

        static VisualTreeAsset _buttonAsset;

        public static VisualElement AddFunction(object target, MethodInfo method , VisualTreeAsset buttonAsset = null)
        {
            if (buttonAsset == null) {
                buttonAsset = _buttonAsset;
            }
            var item = buttonAsset.Instantiate();
            var btn = item.Q<Button>();
            var argsContent = item.Q("args");
            btn.text = ">";
            object[] args = new object[method.GetParameters().Length];

            btn.clicked += () => {
                method.Invoke(method.IsStatic ? null : target, args);
            };

            Action update = null;

            var @params = method.GetParameters();
            for (int i = 0; i < @params.Length; i++)
            {
                var index = i;
                var param = @params[i];
                if (param.ParameterType == typeof(string))
                {
                    args[i] = string.Empty;
                }
                else
                {
                    if (param.ParameterType.IsValueType)
                    {
                        args[i] = Activator.CreateInstance(param.ParameterType);
                    }
                }
                EditorEx.AddDisplayItem(argsContent, param.Name, param.ParameterType, () => args[index], x => args[index] = x);
            }
            item.Q<Label>().text = method.Name;
            update?.Invoke();
            return item;
        }

        private void DefaultInspector(VisualElement root)
        {
            var defaultInspector = new VisualElement();
            InspectorElement.FillDefaultInspector(defaultInspector, this.serializedObject, this);

            var defaultContent = CreateFoldOut("Default");
            bool isFirstItem = true;
            foreach(var item in defaultInspector.Children().ToArray())
            {
                if (isFirstItem)
                {
                    isFirstItem = false;
                    root.Add(item);
                    continue;
                }
                else
                {
                    defaultContent.Add(item);
                }
            }
            if (defaultContent.childCount > 0)
            {
                root.Add(CreateSplitLine());
                root.Add(defaultContent);
            }

            AddNetworkProperties(root);
        }

        private void AddNetworkProperties(VisualElement root)
        {
#if NETICK
            var networkProperties = CreateFoldOut("Network Properties");
            var serializedObject = new SerializedObject(target);
            serializedObject.forceChildVisibility = true;
            SerializedProperty iterator = serializedObject.GetIterator();
            if (iterator.NextVisible(enterChildren: true))
            {
                do
                {
                    try
                    {
                        var propertyField = new PropertyField(iterator)
                        {
                            name = "PropertyField:" + iterator.propertyPath
                        };

                        var parameters = new object[] { iterator, null };
                        FieldInfo fieldInfoFromProperty = ReflectionEx.CallStatic<FieldInfo>(ReflectionEx.ScriptAttributeUtilityType, "GetFieldInfoFromProperty", parameters);
                        var type = parameters[1] as Type;
                        if (type != null && iterator.name.EndsWith(">k__BackingField"))
                        {
                            var p = fieldInfoFromProperty.DeclaringType.GetProperty(iterator.name.Substring(1, iterator.name.Length - 1 - ">k__BackingField".Length));
                            if (p != null && p.CustomAttributes.Any(x => x.AttributeType == typeof(Networked)))
                            {
                                networkProperties.Add(propertyField);
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.LogException(e);
                    }
                }
                while (iterator.Next(enterChildren: false));
            }
            if (networkProperties.childCount > 0)
            {
                root.Add(CreateSplitLine());
                root.Add(networkProperties);
            } 
#endif
        }

        private VisualElement CreateTitle(string text, Color? color = null)
        {
            var label = new Label(text);
            label.style.unityFontStyleAndWeight = new StyleEnum<FontStyle>(FontStyle.BoldAndItalic);
            label.style.fontSize = 14;
            label.style.color = new StyleColor(color ?? Color.gray);
            label.style.marginBottom = 10f;
            label.style.marginLeft = 3f;
            return label;
        }

        private Foldout CreateFoldOut(string text, Color? color = null)
        {
            var foldOut = new Foldout();
            foldOut.text = text;
            var label = foldOut.Q<Label>();
            label.style.unityFontStyleAndWeight = new StyleEnum<FontStyle>(FontStyle.BoldAndItalic);
            label.style.fontSize = 14;
            label.style.color = new StyleColor(color ?? Color.gray);
            foldOut.style.marginBottom = 10f;
            foldOut.style.marginLeft = 3f;
            var content = foldOut.Q("unity-content");
            content.style.marginLeft = 0;
            return foldOut;
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
#if NETICK
            root.Add(CreateSplitLine());
            var netRole = ((NetworkBehaviour)target).IsServer ? "Server" : "Client";
            var foldOut = CreateFoldOut($"Network State (Runtime) ({netRole})");
            var content = EditorEx.Configure(target.GetType(), () => target, null, target is NetworkBehaviour nb && nb.IsServer);
            foldOut.Add(content);
            root.Add(foldOut); 
#endif
        }
    }

    internal static class EditorEx
    {

        static BindingFlags All = BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic;

        static MethodInfo configureStyleMethod = typeof(PropertyField).GetMethod("ConfigureFieldStyles", BindingFlags.Static | BindingFlags.NonPublic);

        public static VisualElement Configure(Type type, Func<object> targetGet, Action<object> targetSet, bool writable)
        {
            object source = targetGet?.Invoke();

            var root = new VisualElement();
            root.schedule.Execute(() =>
            {
                source = targetGet();
            }).Every(0);
#if NETICK
            bool isNetworkBehaviour = typeof(NetworkBehaviour).IsAssignableFrom(type);

            if (isNetworkBehaviour && type.BaseType != typeof(NetworkBehaviour))
            {
                var baseRoot = Configure(type.BaseType, targetGet, targetSet, writable);
                root.Add(baseRoot);
            }

            foreach (var prop in type.GetProperties(All).Where(x => x.DeclaringType == type && x.CustomAttributes.Any(x => x.AttributeType == typeof(Networked))))
            {
                Action<object> set = !writable ||prop.SetMethod == null ? null : val =>
                {
                    prop.SetValue(source, val);
                    targetSet?.Invoke(source);
                };

                AddDisplayItem(root, prop.Name, prop.PropertyType, () => prop.GetValue(source), set);
            }

            if (isNetworkBehaviour)
            {
                foreach (var field in type.GetFields(All).Where(x => x.CustomAttributes.Any(x => x.AttributeType == typeof(Networked))))
                {
                    Action<object> set = !writable ? null : val =>
                    {
                        field.SetValue(source, val);
                        targetSet?.Invoke(source);
                    };
                    AddDisplayItem(root, field.Name, field.FieldType, () => field.GetValue(source), set);
                }
            }
            else
            { 
#endif
            foreach (var field in type.GetFields(All))
                {
                    Action<object> set = !writable || (type.IsValueType && targetSet == null) ? null : val =>
                    {
                        source = targetGet();
                        field.SetValue(source, val);
                        targetSet?.Invoke(source);
                    };
                    AddDisplayItem(root, field.Name, field.FieldType, () => field.GetValue(source), set);
                }
#if NETICK
        }  
#endif
            return root;
        }

        public static TField ConfigureField<TField, TValue, TSource>(VisualElement root, string name, Func<object> getValue, Action<object> setValue) where TField : BaseField<TValue>, new()
        {
            return ConfigureField<TField, TValue>(root, name,
                getValue == null ? null : () => System.Convert.ChangeType((TSource)getValue(), typeof(TValue)),
                setValue == null ? null : val => setValue(System.Convert.ChangeType((TValue)val, typeof(TSource)))
);
        }

        public static TField ConfigureField<TField, TValue>(VisualElement root, string name, Func<object> getValue, Action<object> setValue) where TField : BaseField<TValue>, new()
        {
            var fd = new TField();
            fd.label = name;
            root.Add(fd);
            bool execChangeEvent = true;
            fd.schedule.Execute(() =>
            {
                execChangeEvent = false;
                fd.value = (TValue)getValue();
                execChangeEvent = true;
            }).Every(0);
            fd.SetEnabled(setValue != null);
#if !UNITY_2021
            ConfigureStyle<TField, TValue>(fd);
#endif
            if (setValue != null)
            {
                fd.RegisterValueChangedCallback(evt =>
                {
                    if (execChangeEvent)
                    {
                        var val = evt.newValue;
                        setValue(val);
                    }
                });
            }
            return fd;
        }

        public static void ConfigureStyle<TField, TValue>(TField field)
        {
            configureStyleMethod.MakeGenericMethod(typeof(TField), typeof(TValue)).Invoke(null, new object[] { field });
        }

        public static void AddDisplayItem(VisualElement root, string name, Type type, Func<object> getValue, Action<object> setValue)
        {
            if (type.IsValueType)
            {
                if (type == typeof(int))
                {
                    ConfigureField<IntegerField, int>(root, name, getValue, setValue);
                }
                else if (type == typeof(short))
                {
                    ConfigureField<IntegerField, int, short>(root, name, getValue, setValue);
                }
                else if (type == typeof(uint))
                {
#if !UNITY_2021
                    ConfigureField<UnsignedIntegerField, uint>(root, name, getValue, setValue);
#else
                    ConfigureField<IntegerField,int, uint>(root, name, getValue, setValue);
#endif
                }
                else if (type == typeof(ushort))
                {
#if !UNITY_2021
                    ConfigureField<UnsignedIntegerField, uint, ushort>(root, name, getValue, setValue);
#else
                    ConfigureField<IntegerField, int , ushort>(root, name, getValue, setValue);
#endif
                }
                else if (type == typeof(long))
                {
                    ConfigureField<LongField, long>(root, name, getValue, setValue);
                }
                else if (type == typeof(ulong))
                {
#if !UNITY_2021
                    ConfigureField<UnsignedLongField, ulong>(root, name, getValue, setValue);
#else
                    ConfigureField<LongField,long,ulong>(root, name, getValue, setValue);
#endif
                }
                else if (type == typeof(float))
                {
                    ConfigureField<FloatField, float>(root, name, getValue, setValue);
                }
                else if (type == typeof(double))
                {
                    ConfigureField<DoubleField, double>(root, name, getValue, setValue);
                }
                else if (type == typeof(Vector3))
                {
                    ConfigureField<Vector3Field, Vector3>(root, name, getValue, setValue);
                }
                else if (type == typeof(Vector3Int))
                {
                    ConfigureField<Vector3IntField, Vector3Int>(root, name, getValue, setValue);
                }
                else if (type == typeof(Vector2Int))
                {
                    ConfigureField<Vector2IntField, Vector2Int>(root, name, getValue, setValue);
                }
                else if (type == typeof(Vector2))
                {
                    ConfigureField<Vector2Field, Vector2>(root, name, getValue, setValue);
                }
                else if (type == typeof(Vector4))
                {
                    ConfigureField<Vector4Field, Vector4>(root, name, getValue, setValue);
                }
                else if (type == typeof(Color))
                {
                    ConfigureField<ColorField, Color>(root, name, getValue, setValue);
                }
                else if (type == typeof(Rect))
                {
                    ConfigureField<RectField, Rect>(root, name, getValue, setValue);
                }
                else if (type == typeof(Bounds))
                {
                    ConfigureField<BoundsField, Bounds>(root, name, getValue, setValue);
                }
                else if (type == typeof(Hash128))
                {
                    ConfigureField<Hash128Field, Hash128>(root, name, getValue, setValue);
                }
                else if (type == typeof(bool))
                {
                    ConfigureField<Toggle, bool>(root, name, getValue, setValue);
                }
#if NETICK
                else if (type == typeof(NetworkBool))
                {
                    ConfigureField<Toggle, bool>(root, name, () => (bool)(NetworkBool)getValue(), setValue == null ? null : val => setValue((bool)(NetworkBool)val));
                } 
#endif
                else if (type == typeof(Quaternion))
                {
                    ConfigureField<Vector4Field, Vector4>(root, name, () =>
                    {
                        var raw = (Quaternion)getValue();
                        return new Vector4(raw.x, raw.y, raw.z, raw.w);
                    }, null);
                    ConfigureField<Vector3Field, Vector3>(root, name + ".eulerAngles", () => ((Quaternion)getValue()).eulerAngles, setValue == null ? null : val => setValue(Quaternion.Euler((Vector3)val)));
                }
                else if (type.IsEnum)
                {
                    if (type.CustomAttributes.Any(x => x.AttributeType == typeof(FlagsAttribute)))
                    {
                        var fd = ConfigureField<EnumFlagsField, Enum>(root, name, getValue, setValue);
                        fd.Init((Enum)Activator.CreateInstance(type), false);
                    }
                    else
                    {
                        var fd = ConfigureField<EnumField, Enum>(root, name, getValue, setValue);
                        fd.Init((Enum)Activator.CreateInstance(type), false);
                    }
                }
                else if (type.FullName.StartsWith("Netick.NetworkString"))
                {
                    var fd = ConfigureField<TextField, string>(root, name, () => getValue().ToString(), setValue == null ? null : val => setValue(Activator.CreateInstance(type, val)));
                    fd.maxLength = int.Parse(type.Name.Substring("NetworkString".Length));
                }
                else if (type.FullName.StartsWith("Netick.NetworkArrayStruct"))
                {
                    var bufferField = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).First();
                    Action<object> set = setValue == null ? null : val =>
                    {
                        var source = getValue();
                        bufferField.SetValue(source, val);
                        setValue(source);
                    };
                    var content = Configure(bufferField.FieldType, () => bufferField.GetValue(getValue()), set, setValue != null);
                    var foldOut = new Foldout();
                    foldOut.text = name;
                    foldOut.Add(content);
                    root.Add(foldOut);
                    if(setValue == null)
                    {
                        foldOut.Q<Label>().style.color = Color.grey;
                    }
                }
                else if (type.FullName.StartsWith("Netick.FixedSize"))
                {
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
                        Action<object> set = setValue == null ? null : val =>
                        {
                            var temp = getValue();
                            fields[i].SetValue(temp, val);
                            setValue?.Invoke(temp);
                        };
                        AddDisplayItem(v, $"Element{i}", type.GetGenericArguments()[0], () => source[i], set);
                    };
                    ls.unbindItem = (v, i) =>
                    {
                        v.Clear();
                    };
                    ls.virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight;
                    ls.schedule.Execute(() => {
                        source.Clear();
                        var buffer = getValue();
                        for (int i = 0; i < fields.Length; i++)
                        {
                            var element = fields[i].GetValue(buffer);
                            source.Add(element);
                        }
                        ls.itemsSource = source;
                    }).Every(0);
                    var foldOut = new Foldout();
                    foldOut.text = name;
                    var scroll = new ScrollView();
                    scroll.Add(ls);
                    foldOut.Add(scroll);
                    if (setValue == null)
                    {
                        foldOut.Q<Label>().style.color = Color.grey;
                    }
                    root.Add(foldOut);
                    scroll.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
                    scroll.verticalScrollerVisibility = ScrollerVisibility.Auto;
                    scroll.style.maxHeight = new StyleLength(240);
                }
                else if (!type.IsPrimitive)
                {
#if NETICK
                    if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(NetworkBehaviourRef<>))
                    {
                        var field = ConfigureField<ObjectField, UnityEngine.Object>(root, name, () =>
                        {
                            var md = type.GetMethods().First(x => x.Name == "GetBehaviour" && !x.ContainsGenericParameters);
                            var rootVisual = root;
                            NetworkBehaviour targetObj = null;
                            while (rootVisual != null)
                            {
                                if (rootVisual.userData is NetworkBehaviour target)
                                {
                                    targetObj = target;
                                    break;
                                }
                                rootVisual = rootVisual.parent;
                            }
                            var br = targetObj ? md?.Invoke(getValue(), new[] { targetObj.Sandbox }) : null;
                            return br;
                        }, val =>
                        {
                            setValue(Activator.CreateInstance(type, val));
                        });
                        field.objectType = type.GenericTypeArguments.FirstOrDefault();
                        field.SetEnabled(setValue != null);
                        name += " (Raw)";
                    }
                    else if (type == typeof(NetworkObjectRef))
                    {
                        var field = ConfigureField<ObjectField, UnityEngine.Object>(root, name, () =>
                        {
                            var md = type.GetMethods().First(x => x.Name == "GetObject");
                            var rootVisual = root;
                            NetworkBehaviour targetObj = null;
                            while (rootVisual != null)
                            {
                                if (rootVisual.userData is NetworkBehaviour target)
                                {
                                    targetObj = target;
                                    break;
                                }
                                rootVisual = rootVisual.parent;
                            }
                            var br = targetObj ? md?.Invoke(getValue(), new[] { targetObj.Sandbox }) : null;
                            return br;
                        }, val =>
                        {
                            setValue(Activator.CreateInstance(type, val));
                        });
                        field.objectType = typeof(NetworkObject);
                        field.SetEnabled(setValue != null);
                        name += " (Raw)";
                    } 
#endif
                    var content = Configure(type, getValue, setValue, setValue != null);
                    bool needFoldOut = true;
                    if (type.IsConstructedGenericType && typeof(KeyValuePair<,>) == type.GetGenericTypeDefinition())
                    {
                        content.style.flexDirection = new StyleEnum<FlexDirection>(FlexDirection.Row);
                        foreach(var child in content.Children())
                        {
                            child.style.flexGrow = 1;
                        }
                        needFoldOut = false;
                    }
                    if (needFoldOut)
                    {
                        var foldOut = new Foldout();
                        foldOut.text = name;
                        foldOut.Add(content);
                        foldOut.value = false;
                        if (setValue == null)
                        {
                            foldOut.Q<Label>().style.color = Color.grey;
                        }
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
                if (type == typeof(string))
                {
                    ConfigureField<TextField, string>(root, name, getValue, setValue);
                }
                else if (typeof(UnityEngine.Object).IsAssignableFrom(type))
                {
                    var fd = ConfigureField<ObjectField, UnityEngine.Object>(root, name, getValue, setValue);
                    fd.objectType = type;
                }
                else if (typeof(IEnumerable).IsAssignableFrom(type))
                {
                    int displayCount = 0;
                    List<object> source = new List<object>();
                    var ls = new ListView();
                    ls.virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight;
                    ls.makeItem = () =>
                    {
                        return new VisualElement();
                    };
                    var elementType = type.GetInterface("IEnumerable`1").GetGenericArguments()[0];
                    ls.bindItem = (v, i) =>
                    {
                        AddDisplayItem(v, $"Element{i}", elementType, () => source[i], null);
                    };
                    ls.unbindItem = (v, i) =>
                    {
                        v.Clear();
                    };
                    ls.schedule.Execute(() =>
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
                    }).Every(0);
                    var foldOut = new Foldout();
                    foldOut.text = name;
                    var scroll = new ScrollView();
                    scroll.Add(ls);
                    foldOut.Add(scroll);
                    root.Add(foldOut);
                    scroll.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
                    scroll.verticalScrollerVisibility = ScrollerVisibility.Auto;
                    scroll.style.maxHeight = new StyleLength(240);
/*                    var methodsToExpose = new[] {
                        "Add","Remove","Enqueue","Dequeue","Push","Pop","Clear"
                    };

                    foreach (var method in methodsToExpose)
                    {
                        if (type.GetMethods().FirstOrDefault(x => x.Name == method) is MethodInfo addMd)
                        {
                            foldOut.Add(MyEditor.AddFunction(getValue(), addMd));
                        }
                    }*/
                }
            }
        }

    }

}