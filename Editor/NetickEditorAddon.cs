#if NETICK
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Netick.Unity.Editor.Addon
{

    using Object = UnityEngine.Object;
    using Editor = UnityEditor.Editor;

    public static class NetickEditorAddon
    {
        private static Dictionary<int, string> _previousNames = new Dictionary<int, string>();

        private static readonly Type prefabImporterEditorType =
        Type.GetType("UnityEditor.PrefabImporterEditor, UnityEditor.CoreModule");

        private static readonly Type gameObjectInspectorType =
            Type.GetType("UnityEditor.GameObjectInspector, UnityEditor.CoreModule");

        [InitializeOnLoadMethod]
        static void Init()
        {
#if NETICK
            EditorApplication.hierarchyWindowItemOnGUI += OnHierarchyGUI;
#endif
            /*EditorApplication.hierarchyChanged += OnHierarchyChanged;*/

            Editor.finishedDefaultHeaderGUI += Editor_finishedDefaultHeaderGUI;
        }

        /// <summary>
        /// draw netick prefab controls in prefab inspector
        /// </summary>
        /// <param name="editor"></param>
        private static void Editor_finishedDefaultHeaderGUI(Editor editor)
        {
            if (gameObjectInspectorType.IsInstanceOfType(editor))
            {
                DrawPrefabAssetGUI(editor);
            }
        }

        private static bool DrawControls(bool value, bool showSelectRoots)
        {
            EditorGUI.BeginDisabledGroup(false);
            var rect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight + 2f);
            var bgRect = rect;
            bgRect.xMin -= 4f;
            bgRect.xMax += 4f;
            EditorGUI.DrawRect(bgRect, Color.black);
            var labelStyle = GUIStyles.richWhiteLabel;
            var content = value ? GUIContents.syncWithOn : GUIContents.syncWithOff;

            EditorGUI.BeginDisabledGroup(showSelectRoots);
            var toggleRect = rect;
            toggleRect.xMax = GUIStyle.none.CalcSize(content).x;
            value = EditorGUI.ToggleLeft(toggleRect, content, value, labelStyle);

            toggleRect.x += toggleRect.width - 20;
            GUI.Label(toggleRect, "NETICK", labelStyle);

            EditorGUI.EndDisabledGroup();

            // avoid button clicks to trigger a change for this control
            var changed = GUI.changed;

            var registryButtonRect = bgRect;
            registryButtonRect.yMin += 2f;
            registryButtonRect.xMin = registryButtonRect.xMax - 18f;

            EditorGUI.EndDisabledGroup();

            changed = GUI.changed;
            if (GUI.Button(registryButtonRect, GUIContents.openInWindow, GUIStyles.iconButton))
            {
                // open a window
            }
            GUI.changed = changed;

            return value;
        }

        private static void DrawPrefabAssetGUI(Editor editor)
        {
            var hasRegistered = false;
            var hasUnregistered = false;
            foreach (var target in editor.targets)
            {

                if(target is not GameObject gameObject)
                {
                    continue;
                }

                if (gameObject.TryGetComponent(out NetworkObject _))
                {
                    hasRegistered = true;
                }
                else
                {
                    hasUnregistered = true;
                }

                // in order to determine mixed value, we need to know if there's at least
                // one value of each state (true/false) - only then we can stop iterating
                if (hasRegistered && hasUnregistered)
                {
                    break;
                }
            }

            EditorGUI.BeginChangeCheck();
            EditorGUI.showMixedValue = hasRegistered && hasUnregistered;
            var create = DrawControls(hasRegistered, editor.targets.Any(x=> Application.IsPlaying(x)));
            EditorGUI.showMixedValue = false;
            if (EditorGUI.EndChangeCheck())
            {
                var prefabPathsWithMissingScripts = new List<string>();

                foreach (var target in editor.targets)
                {
                    if (target is not GameObject gameObject)
                    {
                        continue;
                    }

                    if (PrefabUtility.IsPartOfAnyPrefab(gameObject))
                    {
                        continue;
                    }

                    var hasSync = gameObject.TryGetComponent(out NetworkObject _);

                    if (create && !hasSync)
                    {
                        var no = Undo.AddComponent<NetworkObject>(gameObject);
                        // move to the top of the component list
                        MoveComponentToTop(gameObject, no);
                    }
                    else if (!create && hasSync)
                    {
                        if (PrefabUtility.IsPartOfAnyPrefab(gameObject) &&
                            !PrefabUtility.IsPartOfPrefabThatCanBeAppliedTo(gameObject))
                        {
                            prefabPathsWithMissingScripts.Add(AssetDatabase.GetAssetPath(gameObject));
                            continue;
                        }

                        if (gameObject.TryGetComponent<NetworkObject>(out var sync))
                        {
                            Undo.DestroyObjectImmediate(sync);
                        }
                    }
                }

                if (prefabPathsWithMissingScripts.Count > 0)
                {
                    _ = EditorUtility.DisplayDialog("Can't save Prefab",
                        $"Prefabs cannot be saved while they contain missing scripts. The following prefabs were not saved:\n\n{string.Join("\n", prefabPathsWithMissingScripts)}",
                        "OK");
                }

                GUIUtility.ExitGUI();
            }
        }

        private static void InitializePreviousNames()
        {
            foreach (GameObject go in Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
            {
                _previousNames[go.GetInstanceID()] = go.name;
            }
        }

/*        private static void OnHierarchyChanged()
        {
            if (true)
            {
                return;
            }

            List<int> removedIDs = new List<int>(_previousNames.Keys);

            foreach (GameObject go in Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
            {
                int instanceID = go.GetInstanceID();
                if (_previousNames.ContainsKey(instanceID))
                {
                    removedIDs.Remove(instanceID); // Object still exists
                    if (_previousNames[instanceID] != go.name)
                    {
                        Debug.Log($"GameObject renamed: {_previousNames[instanceID]} -> {go.name}");
                        if (go.name.StartsWith("#"))
                        {
                            var txt = go.GetComponentInChildren<Text>();
                            if (txt)
                            {
                                txt.text = go.name.Substring(1);
                            }
                            else
                            {
                                var txt2 = go.GetComponentInChildren<TMP_Text>();
                                if (txt2)
                                {
                                    txt2.text = go.name.Substring(1);
                                }
                            }
                        }
                        _previousNames[instanceID] = go.name; // Update stored name
                    }
                }
                else
                {
                    // New GameObject created
                    _previousNames.Add(instanceID, go.name);
                }
            }

            // Handle deleted GameObjects (optional)
            foreach (int id in removedIDs)
            {
                _previousNames.Remove(id);
            }
        }*/
#if NETICK
        private static void OnHierarchyGUI(int instanceID, Rect selectionRect)
        {
            if (!Netick.Unity.Network.IsRunning || !(EditorUtility.InstanceIDToObject(instanceID) == null))
            {
                return;
            }
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene sceneAt = SceneManager.GetSceneAt(i);
                if (sceneAt.handle != instanceID)
                {
                    continue;
                }
                NetworkSandbox networkSandbox = null;
                foreach (NetworkSandbox sandbox in Netick.Unity.Network.Sandboxes)
                {
                    if (sandbox.Scene == sceneAt)
                    {
                        networkSandbox = sandbox;
                    }
                }
                if (!(networkSandbox == null))
                {
                    /*                GUI.Button(new Rect(selectionRect.xMax - 100f + 120, selectionRect.y, 100f, selectionRect.height), "Visible", networkSandbox.IsVisible ? EditorStyles.boldLabel : EditorStyles.miniLabel);

                                    GUI.Button(new Rect(selectionRect.xMax - 100f + 120, selectionRect.y, 100f, selectionRect.height), "Focus", networkSandbox.IsVisible ? EditorStyles.boldLabel : EditorStyles.miniLabel);*/

                    if (GUI.Button(new Rect(selectionRect.xMax - 70f + 20, selectionRect.y, 20f, selectionRect.height), EditorGUIUtility.IconContent("ViewToolZoom")))
                    {
                        Netick.Unity.Network.Focus(networkSandbox);
                        EditorApplication.RepaintHierarchyWindow();
                    }
                    if (GUI.Button(new Rect(selectionRect.xMax - 70f + 40, selectionRect.y, 20f, selectionRect.height), networkSandbox.IsVisible ? EditorGUIUtility.IconContent("animationvisibilitytoggleon") : EditorGUIUtility.IconContent("animationvisibilitytoggleoff")))
                    {
                        networkSandbox.IsVisible = !networkSandbox.IsVisible;
                        EditorApplication.RepaintHierarchyWindow();
                    }
                }
                break;
            }
        }
#endif
        private static void MoveComponentToTop(GameObject gameObject, Component component)
        {
            if (component == null || gameObject == null)
            {
                return;
            }

            try
            {
                // Get all components except Transform (which is always first)
                var components = gameObject.GetComponents<Component>();
                int currentIndex = Array.IndexOf(components, component);
                
                if (currentIndex <= 1) // Index 0 is Transform, so 1 is already at top
                {
                    return;
                }

                // Move component up repeatedly until it's right after Transform
                while (currentIndex > 1)
                {
                    if (!UnityEditorInternal.ComponentUtility.MoveComponentUp(component))
                    {
                        break; // Stop if we can't move up anymore
                    }
                    currentIndex--;
                }

                // Mark the GameObject as dirty to ensure changes are saved
                EditorUtility.SetDirty(gameObject);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Failed to move NetworkObject component to top: {ex.Message}");
            }
        }
    }
}
#endif