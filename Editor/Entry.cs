using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
#if NETICK
using Netick.Unity;
using NetickEditor;
#endif

using UnityEditor;
using UnityEngine;

namespace Cjx.Unity.Netick.Editor
{
    using Editor = UnityEditor.Editor;

    [InitializeOnLoad]
    public static class Entry
    {
        static Entry()
        {
            EditorApplication.delayCall += () =>
            {
                EditorApplication.delayCall += Init;
            };
        }

        static void Init()
        {
            BackupOriginalEditorTypes();
            ClearCustomEditorCache();
#if UNITY_6000_0_OR_NEWER
            Init_6X();
#else
            Init_2022_X();
#endif
        }

        private static Dictionary<Type, object> _originalEditors = new Dictionary<Type, object>();

        [MenuItem("Test/1")]
        private static void Test()
        {
            BackupOriginalEditorTypes();
        }

        public static Type GetOriginalEditorType(Type type)
        {
            if (type == null)
            {
                return null;
            }

            if (_originalEditors.TryGetValue(type, out var editorData))
            {
                return ExtractEditorType(editorData);
            }

            var currentType = type.BaseType;
            while (currentType != null && currentType != typeof(object))
            {
                if (_originalEditors.TryGetValue(currentType, out editorData))
                {
                    if (IsEditorForChildClasses(editorData))
                    {
                        return ExtractEditorType(editorData);
                    }
                }
                currentType = currentType.BaseType;
            }

            return null;
        }

        private static Type ExtractEditorType(object editorData)
        {
            if (editorData == null)
            {
                return null;
            }

#if UNITY_6000_0_OR_NEWER
            var storageType = editorData.GetType();
            var customEditorsField = storageType.GetField("customEditors");
            if (customEditorsField != null)
            {
                var customEditorsList = customEditorsField.GetValue(editorData);
                if (customEditorsList is IList list && list.Count > 0)
                {
                    var firstEditor = list[0];
                    var inspectorTypeField = firstEditor.GetType().GetField("inspectorType", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    if (inspectorTypeField != null)
                    {
                        return inspectorTypeField.GetValue(firstEditor) as Type;
                    }
                }
            }
#else
            if (editorData is IList list && list.Count > 0)
            {
                var firstEditor = list[0];
                var inspectorTypeField = firstEditor.GetType().GetField("m_InspectorType", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (inspectorTypeField != null)
                {
                    return inspectorTypeField.GetValue(firstEditor) as Type;
                }
            }
#endif

            return null;
        }

        private static bool IsEditorForChildClasses(object editorData)
        {
            if (editorData == null)
            {
                return false;
            }

#if UNITY_6000_0_OR_NEWER
            var storageType = editorData.GetType();
            var customEditorsField = storageType.GetField("customEditors");
            if (customEditorsField != null)
            {
                var customEditorsList = customEditorsField.GetValue(editorData);
                if (customEditorsList is IList list && list.Count > 0)
                {
                    var firstEditor = list[0];
                    var editorForChildClassesField = firstEditor.GetType().GetField("editorForChildClasses", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    if (editorForChildClassesField != null)
                    {
                        return (bool)editorForChildClassesField.GetValue(firstEditor);
                    }
                }
            }
#else
            if (editorData is IList list && list.Count > 0)
            {
                var firstEditor = list[0];
                var editorForChildClassesField = firstEditor.GetType().GetField("m_EditorForChildClasses", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (editorForChildClassesField != null)
                {
                    return (bool)editorForChildClassesField.GetValue(firstEditor);
                }
            }
#endif

            return false;
        }

        internal static void ClearCustomEditorCache()
        {
#if UNITY_6000_0_OR_NEWER
            var customEditorAttributesType = typeof(Editor).Assembly.GetType("UnityEditor.CustomEditorAttributes");
            var instanceProperty = customEditorAttributesType.GetProperty("instance", BindingFlags.Static | BindingFlags.NonPublic);
            var instance = instanceProperty.GetValue(null);
            
            var cacheField = instance.GetType().GetField("m_Cache", BindingFlags.Instance | BindingFlags.NonPublic);
            if (cacheField != null)
            {
                var cache = cacheField.GetValue(instance);
                if (cache != null)
                {
                    var dictField = cache.GetType().GetField("m_CustomEditorCache", BindingFlags.Instance | BindingFlags.NonPublic);
                    if (dictField != null)
                    {
                        var dict = dictField.GetValue(cache);
                        if (dict != null)
                        {
                            var keysToRemove = new List<Type>();
                            var enumerator = ((IEnumerable)dict).GetEnumerator();
                            while (enumerator.MoveNext())
                            {
                                var kvp = enumerator.Current;
                                var keyProperty = kvp.GetType().GetProperty("Key");
                                var key = keyProperty.GetValue(kvp) as Type;

                                if (key != null && !ShouldPreserveEditor(key))
                                {
                                    keysToRemove.Add(key);
                                }
                            }

                            var removeMethod = dict.GetType().GetMethod("Remove", new[] { typeof(Type) });
                            foreach (var key in keysToRemove)
                            {
                                removeMethod?.Invoke(dict, new object[] { key });
                            }

                            Debug.Log($"[MyEditor] Cleared {keysToRemove.Count} editor(s) from Unity 6.x custom editor cache (preserved AssetImporter/GameObject/Unity types)");
                        }
                    }
                }
            }
            
            var rebuildMethod = instance.GetType().GetMethod("Rebuild", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            if (rebuildMethod != null)
            {
                rebuildMethod.Invoke(instance, null);
                Debug.Log("[MyEditor] Rebuilt Unity 6.x custom editor cache");
            }
#else
            var customEditorAttributesType = typeof(Editor).Assembly.GetType("UnityEditor.CustomEditorAttributes");
            var dictField = customEditorAttributesType.GetField("kSCustomEditors", BindingFlags.Static | BindingFlags.NonPublic);
            var dictField2 = customEditorAttributesType.GetField("kSCustomMultiEditors", BindingFlags.Static | BindingFlags.NonPublic);

            var dict = dictField?.GetValue(null);
            var dict2 = dictField2?.GetValue(null);

            if (dict != null)
            {
                var keysToRemove = new List<Type>();
                var enumerator = ((IEnumerable)dict).GetEnumerator();
                while (enumerator.MoveNext())
                {
                    var kvp = enumerator.Current;
                    var keyProperty = kvp.GetType().GetProperty("Key");
                    var key = keyProperty.GetValue(kvp) as Type;
                    
                    if (key != null && !ShouldPreserveEditor(key))
                    {
                        keysToRemove.Add(key);
                    }
                }

                var removeMethod = dict.GetType().GetMethod("Remove", new[] { typeof(Type) });
                foreach (var key in keysToRemove)
                {
                    removeMethod?.Invoke(dict, new object[] { key });
                }

                Debug.Log($"[MyEditor] Cleared {keysToRemove.Count} editor(s) from kSCustomEditors (preserved AssetImporter/GameObject/Unity types)");
            }

            if (dict2 != null)
            {
                var keysToRemove = new List<Type>();
                var enumerator = ((IEnumerable)dict2).GetEnumerator();
                while (enumerator.MoveNext())
                {
                    var kvp = enumerator.Current;
                    var keyProperty = kvp.GetType().GetProperty("Key");
                    var key = keyProperty.GetValue(kvp) as Type;
                    
                    if (key != null && !ShouldPreserveEditor(key))
                    {
                        keysToRemove.Add(key);
                    }
                }

                var removeMethod = dict2.GetType().GetMethod("Remove", new[] { typeof(Type) });
                foreach (var key in keysToRemove)
                {
                    removeMethod?.Invoke(dict2, new object[] { key });
                }

                Debug.Log($"[MyEditor] Cleared {keysToRemove.Count} editor(s) from kSCustomMultiEditors (preserved AssetImporter/GameObject/Unity types)");
            }

#endif
        }

        private static bool ShouldPreserveEditor(Type type)
        {
            if (type == null)
            {
                return false;
            }
#if NETICK
            if(type == typeof(NetickConfig))
            {
                return true;
            }
#endif

            if (typeof(AssetImporter).IsAssignableFrom(type))
            {
                return true;
            }

            if (type == typeof(UnityEngine.GameObject))
            {
                return true;
            }

            if (type.Assembly != null)
            {
                var assemblyName = type.Assembly.GetName().Name;
                if (assemblyName.StartsWith("UnityEngine") ||
                    assemblyName.StartsWith("UnityEditor") ||
                    assemblyName.StartsWith("Unity."))
                {
                    return true;
                }
            }

            return false;
        }

        internal static void BackupOriginalEditorTypes()//
        {
#if UNITY_6000_0_OR_NEWER
            var customEditorAttributesType = typeof(Editor).Assembly.GetType("UnityEditor.CustomEditorAttributes");
            var instanceProperty = customEditorAttributesType.GetProperty("instance", BindingFlags.Static | BindingFlags.NonPublic);
            var instance = instanceProperty.GetValue(null);
            var cache = instance.GetType().GetField("m_Cache", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(instance);
            var dictField = cache.GetType().GetField("m_CustomEditorCache", BindingFlags.Instance | BindingFlags.NonPublic);
            var dict = dictField.GetValue(cache);

            var enumerator = ((IEnumerable)dict).GetEnumerator();
            while (enumerator.MoveNext())
            {
                var kvp = enumerator.Current;
                var keyProperty = kvp.GetType().GetProperty("Key");
                var valueProperty = kvp.GetType().GetProperty("Value");

                var key = keyProperty.GetValue(kvp) as Type;
                var value = valueProperty.GetValue(kvp);

                if (key != null && value != null)
                {
                    _originalEditors[key] = value;
                }
            }
#else
            var customEditorAttributesType = typeof(Editor).Assembly.GetType("UnityEditor.CustomEditorAttributes");
            var dictField = customEditorAttributesType.GetField("kSCustomEditors", BindingFlags.Static | BindingFlags.NonPublic);
            var dictField2 = customEditorAttributesType.GetField("kSCustomMultiEditors", BindingFlags.Static | BindingFlags.NonPublic);

            var dict = dictField.GetValue(null);
            var dict2 = dictField2.GetValue(null);

            var enumerator = ((IEnumerable)dict).GetEnumerator();
            while (enumerator.MoveNext())
            {
                var kvp = enumerator.Current;
                var keyProperty = kvp.GetType().GetProperty("Key");
                var valueProperty = kvp.GetType().GetProperty("Value");

                var key = keyProperty.GetValue(kvp) as Type;
                var value = valueProperty.GetValue(kvp);

                if (key != null && value != null)
                {
                    _originalEditors[key] = value;
                }
            }

            enumerator = ((IEnumerable)dict2).GetEnumerator();
            while (enumerator.MoveNext())
            {
                var kvp = enumerator.Current;
                var keyProperty = kvp.GetType().GetProperty("Key");
                var valueProperty = kvp.GetType().GetProperty("Value");

                var key = keyProperty.GetValue(kvp) as Type;
                var value = valueProperty.GetValue(kvp);

                if (key != null && value != null && !_originalEditors.ContainsKey(key))
                {
                    _originalEditors[key] = value;
                }
            }
#endif
            Debug.Log($"[MyEditor] Backed up {_originalEditors.Count} original editor(s)");
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
            removeMd.Invoke(dict, new[] { typeof(UnityEngine.Object) });
            addMd.Invoke(dict, new object[] { typeof(UnityEngine.Object), storage });
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

            m_InspectedTypeField.SetValue(monoEditorTypeInst, typeof(MonoBehaviour));
            m_InspectorTypeField.SetValue(monoEditorTypeInst, typeof(MyEditor));
            //m_RenderPipelineTypeField.SetValue(monoEditorTypeInst, GraphicsSettings.defaultRenderPipeline.GetType());
            m_EditorForChildClassesField.SetValue(monoEditorTypeInst, true);
            m_IsFallbackField.SetValue(monoEditorTypeInst, false);

            var lsInst = Activator.CreateInstance(listType);
            listType.GetMethod("Add").Invoke(lsInst, new[] { monoEditorTypeInst });

            var addMd = dictField.FieldType.GetMethod("Add");

            var removeMd = dictField.FieldType.GetMethods().FirstOrDefault(x => x.Name == "Remove" && x.GetParameters().Length == 1);

            removeMd.Invoke(dict, new[] { typeof(MonoBehaviour) });
            addMd.Invoke(dict, new object[] { typeof(MonoBehaviour), lsInst });
        }
    }
}
