using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Netick;
using Netick.Unity;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

internal static class ReflectionEx
{
    public static TResult Call<TResult>(this object obj, string methodName, params object[] @params)
    {
        return (TResult)obj.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Invoke(obj, @params);
    }

    public static void Call(this object obj, string methodName, params object[] @params)
    {
        obj.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Invoke(obj, @params);
    }

    public static TResult GetField<TResult>(this object obj, string fieldName)
    {
        return (TResult)obj.GetType().GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).GetValue(obj);
    }

    public static TResult GetProperty<TResult>(this object obj, string fieldName)
    {
        return (TResult)obj.GetType().GetProperty(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).GetValue(obj);
    }

    public static object GetProperty(this object obj, string fieldName)
    {
        return GetProperty<object>(obj, fieldName);
    }

    public static void SetField(this object obj, string fieldName,object value)
    {
        obj.GetType().GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).SetValue(obj,value);
    }

    public static void ReflectSetProperty(this object obj, string fieldName,object value)
    {
        obj.GetType().GetProperty(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).SetValue(obj,value);
    }
}

/// <summary>
/// modified copy of unity propertyfield
/// </summary>
internal class PropertyField2 : VisualElement, IBindable
{
    //
    // 摘要:
    //     Instantiates a PropertyField using the data read from a UXML file.
    public new class UxmlFactory : UxmlFactory<PropertyField2, UxmlTraits>
    {
    }

    //
    // 摘要:
    //     Defines UxmlTraits for the PropertyField.
    public new class UxmlTraits : VisualElement.UxmlTraits
    {
        private UxmlStringAttributeDescription m_PropertyPath;

        private UxmlStringAttributeDescription m_Label;

        //
        // 摘要:
        //     Constructor.
        public UxmlTraits()
        {
            m_PropertyPath = new UxmlStringAttributeDescription
            {
                name = "binding-path"
            };
            m_Label = new UxmlStringAttributeDescription
            {
                name = "label",
                defaultValue = null
            };
        }

        public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
        {
            base.Init(ve, bag, cc);
            if (ve is PropertyField2 propertyField)
            {
                propertyField.label = m_Label.GetValueFromBag(bag, cc);
                string valueFromBag = m_PropertyPath.GetValueFromBag(bag, cc);
                propertyField.bindingPath = (string.IsNullOrEmpty(valueFromBag) ? string.Empty : valueFromBag);
            }
        }
    }

    private static readonly Regex s_MatchPPtrTypeName = new Regex("PPtr\\<(\\w+)\\>");

    internal static readonly string foldoutTitleBoundLabelProperty = "unity-foldout-bound-title";

    internal static readonly string decoratorDrawersContainerClassName = "unity-decorator-drawers-container";

    internal static readonly string listViewBoundFieldProperty = "unity-list-view-property-field-bound";

    private static readonly string listViewNamePrefix = "unity-list-";

    private string m_Label;

    private SerializedObject m_SerializedObject;

    private SerializedProperty m_SerializedProperty;

    private string m_SerializedPropertyReferenceTypeName;

    private PropertyField2 m_ParentPropertyField;

    private int m_FoldoutDepth;

    private VisualElement m_InspectorElement;

    private VisualElement m_ContextWidthElement;

    private int m_DrawNestingLevel;

    private PropertyField2 m_DrawParentProperty;

    private VisualElement m_DecoratorDrawersContainer;

    //
    // 摘要:
    //     USS class name of elements of this type.
    public static readonly string ussClassName = "unity-property-field";

    //
    // 摘要:
    //     USS class name of labels in elements of this type.
    public static readonly string labelUssClassName = ussClassName + "__label";

    //
    // 摘要:
    //     USS class name of input elements in elements of this type.
    public static readonly string inputUssClassName = ussClassName + "__input";

    //
    // 摘要:
    //     USS class name of property fields in inspector elements
    public static readonly string inspectorElementUssClassName = ussClassName + "__inspector-property";

    internal static readonly string imguiContainerPropertyUssClassName = ussClassName + "__imgui-container-property";

    private List<PropertyField2> m_ChildrenProperties;

    private VisualElement m_ChildField;

    private VisualElement m_CustomPropertyGUI;

    private VisualElement m_imguiChildField;

    private VisualElement m_ChildrenContainer;

    private int m_PropertyChangedCounter = 0;

    //
    // 摘要:
    //     Binding object that will be updated.
    public IBinding binding { get; set; }

    //
    // 摘要:
    //     Path of the target property to be bound.
    public string bindingPath { get; set; }

    //
    // 摘要:
    //     Optionally overwrite the label of the generate property field. If no label is
    //     provided the string will be taken from the SerializedProperty.
    public string label
    {
        get
        {
            return m_Label;
        }
        set
        {
            if (!(m_Label == value))
            {
                m_Label = value;
                Rebind();
            }
        }
    }

    private SerializedProperty serializedProperty => m_SerializedProperty;

    //
    // 摘要:
    //     PropertyField constructor.
    public PropertyField2()
        : this(null, null)
    {
    }

    //
    // 摘要:
    //     PropertyField constructor.
    //
    // 参数:
    //   property:
    //     Providing a SerializedProperty in the construct just sets the bindingPath. You
    //     will still have to call Bind() on the PropertyField afterwards.
    public PropertyField2(SerializedProperty property)
        : this(property, null)
    {
    }

    //
    // 摘要:
    //     PropertyField constructor.
    //
    // 参数:
    //   property:
    //     Providing a SerializedProperty in the construct just sets the bindingPath. You
    //     will still have to call Bind() on the PropertyField afterwards.
    //
    //   label:
    //     Optionally overwrite the property label.
    public PropertyField2(SerializedProperty property, string label)
    {
        AddToClassList(ussClassName);
        this.label = label;
        RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
        RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanelEvent);
        if (property != null)
        {
            bindingPath = property.propertyPath;
        }
    }

    private static readonly Type s_FoldoutType = typeof(Foldout);

    internal static int GetFoldoutDepth(VisualElement element)
    {
        int num = 0;
        if (element.parent != null)
        {
            for (VisualElement parent = element.parent; parent != null; parent = parent.parent)
            {
                if (s_FoldoutType.IsAssignableFrom(parent.GetType()))
                {
                    num++;
                }
            }
        }

        return num;
    }

    private void OnAttachToPanel(AttachToPanelEvent evt)
    {
        if (evt.destinationPanel == null)
        {
            return;
        }

        m_FoldoutDepth = GetFoldoutDepth(this);
        for (VisualElement visualElement = base.parent; visualElement != null; visualElement = visualElement.parent)
        {
            if (visualElement.ClassListContains(InspectorElement.ussClassName))
            {
                AddToClassList(inspectorElementUssClassName);
                m_InspectorElement = visualElement;
            }

            if (visualElement.ClassListContains("unity-inspector-main-container"))
            {
                m_ContextWidthElement = visualElement;
                break;
            }
        }
    }

    private void OnDetachFromPanelEvent(DetachFromPanelEvent evt)
    {
        RemoveFromClassList(inspectorElementUssClassName);
    }

    private static readonly Type SerializedPropertyBindEventType = typeof(VisualElement).Assembly.GetType("SerializedPropertyBindEvent");

    protected override void ExecuteDefaultActionAtTarget(EventBase evt)
    {
        base.ExecuteDefaultActionAtTarget(evt);
        if (evt.GetType().FullName == "UnityEditor.UIElements.SerializedPropertyBindEvent")
        {
            Reset(evt.GetProperty<SerializedProperty>("bindProperty"));
            evt.StopPropagation();
        }
    }

    public static TResult GetStaticField<T, TResult>(string fieldName)
    {
        return (TResult)typeof(T).GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static).GetValue(null);
    }

    public static void SetStaticField<T>(string fieldName,object value)
    {
        typeof(T).GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static).SetValue(null, value);
    }

    public static TResult GetStaticProperty<T, TResult>(string fieldName)
    {
        return (TResult)typeof(T).GetProperty(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static).GetValue(null);
    }

    public static object GetStaticProperty(Type classType, string fieldName)
    {
        return classType.GetProperty(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static).GetValue(null);
    }

    public static void SetStaticProperty(Type classType, string fieldName,object value)
    {
        classType.GetProperty(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static).SetValue(null,value);
    }

    public static void SetStaticProperty<TClass>(string fieldName, object value)
    {
        SetStaticProperty(typeof(TClass),fieldName, value);
    }

    public static TResult GetField<T,TResult>(T obj, string fieldName)
    {
        return (TResult)typeof(T).GetField(fieldName,BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).GetValue(obj);
    }

    public static TResult GetProperty<T, TResult>(T obj, string fieldName)
    {
        return (TResult)typeof(T).GetProperty(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).GetValue(obj);
    }

    public static TResult ObjectGetField<TResult>(object obj, string fieldName)
    {
        return (TResult)obj.GetType().GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).GetValue(obj);
    }

    public static TResult ObjectGetProperty<TResult>(object obj, string fieldName)
    {
        return (TResult)obj.GetType().GetProperty(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).GetValue(obj);
    }

    public static TResult CallStatic<TResult>(Type classType, string methodName, params object[] @params)
    {
        return (TResult)classType.GetMethod(methodName, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic).Invoke(null, @params);
    }

    public static object CallStatic(Type classType, string methodName, params object[] @params)
    {
        return CallStatic<object>(classType, methodName, @params);
    }

    public static TResult CallStatic<TClass, TResult>(string methodName, params object[] @params)
    {
        return (TResult)typeof(TClass).GetMethod(methodName, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic).Invoke(null, @params);
    }

    public static void VoidCallStatic<TClass>(string methodName, params object[] @params)
    {
        typeof(TClass).GetMethod(methodName, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic).Invoke(null, @params);
    }

    private void Reset(SerializedProperty newProperty)
    {
        string text = null;
        SerializedPropertyType propertyType = newProperty.propertyType;
        if (propertyType == SerializedPropertyType.ManagedReference)
        {
            text = newProperty.managedReferenceFullTypename;
        }

        bool flag = true;
        if (m_SerializedObject != null
            && /*m_SerializedObject.m_NativeObjectPtr*/ GetField<SerializedObject, IntPtr>(m_SerializedObject, "m_NativeObjectPtr")  != IntPtr.Zero 
            && /*m_SerializedObject.isValid*/GetProperty<SerializedObject, bool>(m_SerializedObject, "isValid") 
            && m_SerializedProperty != null 
            && /*m_SerializedProperty.isValid*/ GetProperty<SerializedProperty,bool>(m_SerializedProperty,"isValid") 
            && propertyType == m_SerializedProperty.propertyType)
        {
            flag = propertyType == SerializedPropertyType.ManagedReference && text != m_SerializedPropertyReferenceTypeName;
        }

        m_SerializedProperty = newProperty;
        m_SerializedPropertyReferenceTypeName = text;
        m_SerializedObject = newProperty.serializedObject;
        if (m_ChildField != null && !flag)
        {
            var handler = CallStatic<object>(ScriptAttributeUtilityType, "GetHandler", m_SerializedProperty);
            ResetDecoratorDrawers(handler);
            VisualElement visualElement = CreateOrUpdateFieldFromProperty(newProperty, m_ChildField);
            if (visualElement != m_ChildField)
            {
                m_ChildField.Unbind();
                int num = IndexOf(m_ChildField);
                if (num >= 0)
                {
                    m_ChildField.RemoveFromHierarchy();
                    m_ChildField = visualElement;
                    base.hierarchy.Insert(num, m_ChildField);
                }
            }

            return;
        }

        Clear();
        m_ChildField?.Unbind();
        m_ChildField = null;
        m_CustomPropertyGUI?.Unbind();
        m_CustomPropertyGUI = null;
        m_DecoratorDrawersContainer = null;
        if (m_SerializedProperty == null || !ObjectGetProperty<bool>(m_SerializedProperty, "isValid") /*!m_SerializedProperty.isValid*/)
        {
            return;
        }

        ComputeNestingLevel();
        VisualElement visualElement2 = null;
        var handler2 = CallStatic<object>(ScriptAttributeUtilityType, "GetHandler", m_SerializedProperty); /*ScriptAttributeUtility.GetHandler(m_SerializedProperty);*/
        using (handler2.Call<IDisposable>("ApplyNestingContext",m_DrawNestingLevel))
        {
            if (!handler2.GetProperty<bool>("hasPropertyDrawer"))
            {
                visualElement2 = (m_ChildField = CreateOrUpdateFieldFromProperty(m_SerializedProperty));
            }
            else
            {
                handler2.GetProperty<PropertyDrawer>("propertyDrawer").SetField("m_PreferredLabel", label ?? serializedProperty.GetProperty<string>("localizedDisplayName"));
                visualElement2 = (m_CustomPropertyGUI = handler2.GetProperty<PropertyDrawer>("propertyDrawer").CreatePropertyGUI(m_SerializedProperty.Copy()));
                if (visualElement2 == null)
                {
                    visualElement2 = CreatePropertyIMGUIContainer();
                    AddToClassList(imguiContainerPropertyUssClassName);
                    m_imguiChildField = visualElement2;
                }
                else
                {
                    RegisterPropertyChangesOnCustomDrawerElement(visualElement2);
                }
            }
        }

        ResetDecoratorDrawers(handler2);
        if (visualElement2 != null)
        {
            PropagateNestingLevel(visualElement2);
            base.hierarchy.Add(visualElement2);
        }

        if (m_SerializedProperty.propertyType == SerializedPropertyType.ManagedReference)
        {
            VisualElement element = ((m_ChildField == null) ? m_CustomPropertyGUI : m_ChildField);
            element.TrackPropertyValue(m_SerializedProperty, delegate
            {
                this.Bind(m_SerializedProperty.serializedObject);
            });
        }
    }

    private void ResetDecoratorDrawers(object handler)
    {

        List<DecoratorDrawer> decoratorDrawers = ObjectGetProperty<List<DecoratorDrawer>>(handler, "decoratorDrawers");
        if (decoratorDrawers == null || decoratorDrawers.Count == 0 || m_DrawNestingLevel > 0)
        {
            if (m_DecoratorDrawersContainer != null)
            {
                Remove(m_DecoratorDrawersContainer);
                m_DecoratorDrawersContainer = null;
            }

            return;
        }

        if (m_DecoratorDrawersContainer == null)
        {
            m_DecoratorDrawersContainer = new VisualElement();
            m_DecoratorDrawersContainer.AddToClassList(decoratorDrawersContainerClassName);
            Insert(0, m_DecoratorDrawersContainer);
        }
        else
        {
            m_DecoratorDrawersContainer.Clear();
        }

        foreach (DecoratorDrawer decorator in decoratorDrawers)
        {
            VisualElement ve = decorator.CreatePropertyGUI();
            if (ve == null)
            {
                ve = new IMGUIContainer(delegate
                {
                    Rect position = default(Rect);
                    position.height = decorator.GetHeight();
                    position.width = base.resolvedStyle.width;
                    decorator.OnGUI(position);
                    ve.style.height = position.height;
                });
                ve.style.height = decorator.GetHeight();
            }

            m_DecoratorDrawersContainer.Add(ve);
        }
    }

    private VisualElement CreatePropertyIMGUIContainer()
    {
        return new IMGUIContainer(delegate
        {
            bool wideMode = CallStatic<InspectorElement,bool>("SetWideModeForWidth",this);
            bool hierarchyMode = EditorGUIUtility.hierarchyMode;
            EditorGUIUtility.hierarchyMode = true;
            float labelWidth = EditorGUIUtility.labelWidth;
            try
            {
                if (serializedProperty.GetProperty<bool>("isValid"))
                {
                    EditorGUILayout.BeginVertical(GetStaticProperty<EditorStyles,GUIStyle>("inspectorHorizontalDefaultMargins"));
                    if (m_InspectorElement is InspectorElement inspectorElement)
                    {
                        SetStaticProperty(ScriptAttributeUtilityType, "propertyHandlerCache", inspectorElement.GetProperty("editor").GetProperty("propertyHandlerCache"));
                    }

                    EditorGUI.BeginChangeCheck();
                    serializedProperty.serializedObject.Update();
                    if (this.GetProperty<List<string>>("classList").Contains(inspectorElementUssClassName))
                    {
                        float num = 0f;
                        if (m_imguiChildField != null)
                        {
                            num = m_imguiChildField.worldBound.x - m_InspectorElement.worldBound.x - m_InspectorElement.resolvedStyle.paddingLeft - base.resolvedStyle.marginLeft;
                        }

                        float num2 = 40f;
                        VisualElement visualElement = m_ContextWidthElement ?? m_InspectorElement;
                        float width = visualElement.resolvedStyle.width;
                        float a = width * 0.45f - num2 - num;
                        float num3 = 123f;
                        float b = Mathf.Max(num3 - num, 0f);
                        EditorGUIUtility.labelWidth = Mathf.Max(a, b);
                    }
                    else if (m_FoldoutDepth > 0)
                    {
                        EditorGUI.indentLevel += m_FoldoutDepth;
                    }

                    var handler = CallStatic(ScriptAttributeUtilityType,"GetHandler",serializedProperty);
                    using (handler.Call<IDisposable>("ApplyNestingContext",m_DrawNestingLevel))
                    {
                        handler.ReflectSetProperty("skipDecoratorDrawers", true);
                        float leftMarginCoord = GetStaticField<EditorGUIUtility,float>("leftMarginCoord");
                        if (m_InspectorElement != null && m_imguiChildField != null)
                        {
                            float x = m_InspectorElement.worldBound.x;
                            SetStaticField<EditorGUIUtility>("leftMarginCoord", 0f - m_imguiChildField.worldBound.x + x);
                        }

                        if (label == null)
                        {
                            EditorGUILayout.PropertyField(serializedProperty, true);
                        }
                        else if (label == string.Empty)
                        {
                            EditorGUILayout.PropertyField(serializedProperty, GUIContent.none, true);
                        }
                        else
                        {
                            EditorGUILayout.PropertyField(serializedProperty, new GUIContent(label), true);
                        }

                        if (m_InspectorElement != null && m_imguiChildField != null)
                        {
                            SetStaticField<EditorGUIUtility>("leftMarginCoord", leftMarginCoord);
                        }
                    }

                    if (!this.GetProperty<List<string>>("classList").Contains(inspectorElementUssClassName) && m_FoldoutDepth > 0)
                    {
                        EditorGUI.indentLevel -= m_FoldoutDepth;
                    }

                    serializedProperty.serializedObject.ApplyModifiedProperties();
                    if (EditorGUI.EndChangeCheck())
                    {
                        DispatchPropertyChangedEvent();
                    }

                    EditorGUILayout.EndVertical();
                }
            }
            finally
            {
                EditorGUIUtility.wideMode = wideMode;
                EditorGUIUtility.hierarchyMode = hierarchyMode;
                if (this.GetProperty<List<string>>("classList").Contains(inspectorElementUssClassName))
                {
                    EditorGUIUtility.labelWidth = labelWidth;
                }
            }
        });
    }
    
    public static readonly Type ScriptAttributeUtilityType = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.ScriptAttributeUtility");
    public static readonly Type NativeClassExtensionUtilitiesType = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.NativeClassExtensionUtilities");
    public static readonly Type EnumDataUtilityType = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.EnumDataUtility");
    public static readonly Type EnumDataType = typeof(UnityEngine.Object).Assembly.GetType("UnityEngine.EnumData");
    public static readonly Type EditorGUI_EnumNamesCacheType = typeof(EditorGUI).GetNestedType("EnumNamesCache", BindingFlags.NonPublic);

    private void ComputeNestingLevel()
    {
        m_DrawNestingLevel = 0;
        for (PropertyField2 drawParentProperty = m_DrawParentProperty; drawParentProperty != null; drawParentProperty = drawParentProperty.m_DrawParentProperty)
        {
            if (drawParentProperty.m_SerializedProperty == m_SerializedProperty || CallStatic<bool>(ScriptAttributeUtilityType,"CanUseSameHandler",drawParentProperty.m_SerializedProperty, m_SerializedProperty))
            {
                m_DrawNestingLevel = drawParentProperty.m_DrawNestingLevel + 1;
                break;
            }
        }
    }

    private void PropagateNestingLevel(VisualElement customPropertyGUI)
    {
        if (customPropertyGUI is PropertyField2 propertyField)
        {
            propertyField.m_DrawParentProperty = this;
        }

        int num = customPropertyGUI.hierarchy.childCount;
        for (int i = 0; i < num; i++)
        {
            PropagateNestingLevel(customPropertyGUI.hierarchy[i]);
        }
    }

    private void Rebind()
    {
        if (m_SerializedProperty != null)
        {
            SerializedObject serializedObject = m_SerializedProperty.serializedObject;
            this.Unbind();
            this.Bind(serializedObject);
        }
    }

    private void UpdateArrayFoldout(ChangeEvent<int> changeEvent, PropertyField2 targetPropertyField, PropertyField2 parentPropertyField)
    {
        int intValue = targetPropertyField.GetField<SerializedProperty>("m_SerializedProperty").intValue;
        if (targetPropertyField != null && targetPropertyField.GetField<SerializedProperty>("m_SerializedProperty") != null && (parentPropertyField != null || targetPropertyField.GetField<SerializedProperty>("m_SerializedProperty").intValue != changeEvent.newValue))
        {
            SerializedObject serializedObject = parentPropertyField?.GetField<SerializedProperty>("m_SerializedProperty")?.serializedObject;
            if (intValue != changeEvent.newValue)
            {
                SerializedObject serializedObject2 = targetPropertyField.GetField<SerializedProperty>("m_SerializedProperty").serializedObject;
                serializedObject2.UpdateIfRequiredOrScript();
                targetPropertyField.GetField<SerializedProperty>("m_SerializedProperty").intValue = changeEvent.newValue;
                serializedObject2.ApplyModifiedProperties();
            }

            parentPropertyField?.Call("RefreshChildrenProperties",parentPropertyField.GetField<SerializedProperty>("m_SerializedProperty").Copy(), true);
        }
    }

    private void TrimChildrenContainerSize(int targetSize)
    {
        if (m_ChildrenProperties != null)
        {
            while (m_ChildrenProperties.Count > targetSize)
            {
                int index = m_ChildrenProperties.Count - 1;
                PropertyField2 propertyField = m_ChildrenProperties[index];
                propertyField.Unbind();
                propertyField.RemoveFromHierarchy();
                m_ChildrenProperties.RemoveAt(index);
            }
        }
    }

    private void RefreshChildrenProperties(SerializedProperty property, bool bindNewFields)
    {
        if (m_ChildrenContainer == null)
        {
            return;
        }

        SerializedProperty endProperty = property.GetEndProperty(true);
        int num = 0;
        if (m_ChildrenProperties == null)
        {
            m_ChildrenProperties = new List<PropertyField2>();
        }

        property.Next(enterChildren: true);
        while (!SerializedProperty.EqualContents(property, endProperty))
        {
            PropertyField2 propertyField = null;
            string propertyPath = property.propertyPath;
            if (num < m_ChildrenProperties.Count)
            {
                propertyField = m_ChildrenProperties[num];
                propertyField.bindingPath = propertyPath;
            }
            else
            {
                propertyField = new PropertyField2(property);
                propertyField.m_ParentPropertyField = this;
                m_ChildrenProperties.Add(propertyField);
                propertyField.bindingPath = property.propertyPath;
            }

            propertyField.name = "unity-property-field-" + property.propertyPath;
            if (bindNewFields)
            {
                propertyField.Bind(property.serializedObject);
            }

            m_ChildrenContainer.Add(propertyField);
            num++;
            if (!property.Next(enterChildren: false))
            {
                break;
            }
        }

        TrimChildrenContainerSize(num);
    }

    private VisualElement CreateFoldout(SerializedProperty property, object originalField = null)
    {
        property = property.Copy();
        Foldout foldout = originalField as Foldout;
        if (foldout == null)
        {
            foldout = new Foldout();
        }

        bool flag = !string.IsNullOrEmpty(this.label);
        foldout.text = (flag ? this.label : property.GetProperty<string>("localizedDisplayName"));
        foldout.bindingPath = property.propertyPath;
        foldout.name = "unity-foldout-" + property.propertyPath;
        Toggle toggle = foldout.Q<Toggle>(null, Foldout.toggleUssClassName);
        toggle.GetField<Clickable>("m_Clickable").ReflectSetProperty("acceptClicksIfDisabled", true);
        Label label = toggle.Q<Label>(null, Toggle.textUssClassName);
        if (flag)
        {
            label.text = foldout.text;
        }
        else
        {
            label.bindingPath = property.propertyPath;
            label.Call("SetProperty",(PropertyName)foldoutTitleBoundLabelProperty, true);
        }

        m_ChildrenContainer = foldout;
        if (property.isExpanded)
        {
            RefreshChildrenProperties(property, bindNewFields: false);
        }
        else
        {
            foldout.RegisterValueChangedCallback(RefreshOnlyWhenExpanded);
        }

        return foldout;
    }

    private void RefreshOnlyWhenExpanded(ChangeEvent<bool> evt)
    {
        if (evt.newValue && evt.target is Foldout foldout && evt.target == m_ChildField)
        {
            foldout.UnregisterCallback<ChangeEvent<bool>>(RefreshOnlyWhenExpanded);
            RefreshChildrenProperties(m_SerializedProperty, bindNewFields: true);
        }
    }

    private void OnFieldValueChanged(EventBase evt)
    {
        if (evt.target == m_ChildField && m_SerializedProperty.GetProperty<bool>("isValid"))
        {
            if (m_SerializedProperty.propertyType == SerializedPropertyType.ArraySize && evt is ChangeEvent<int> changeEvent)
            {
                UpdateArrayFoldout(changeEvent, this, m_ParentPropertyField);
            }

            DispatchPropertyChangedEvent();
        }
    }

    private TField ConfigureField<TField, TValue>(TField field, SerializedProperty property, Func<TField> factory) where TField : BaseField<TValue>
    {
        if (field == null)
        {
            field = factory();
            field.RegisterValueChangedCallback(delegate (ChangeEvent<TValue> evt)
            {
                OnFieldValueChanged(evt);
            });
        }

        string text = label ?? property.GetProperty<string>("localizedDisplayName");
        field.bindingPath = property.propertyPath;
        field.Call("SetProperty", GetStaticField<BaseField<TValue>, PropertyName>("serializedPropertyCopyName"), property.Copy());
        field.name = "unity-input-" + property.propertyPath;
        field.label = text;
        ConfigureFieldStyles<TField, TValue>(field);
        ConfigureTooltip(property, field);
        return field;
    }

    private void ConfigureTooltip<TField>(SerializedProperty property, BaseField<TField> field)
    {
        var parameters = new object[] { property, null };
        FieldInfo fieldInfoFromProperty = CallStatic<FieldInfo>(ScriptAttributeUtilityType,"GetFieldInfoFromProperty", parameters);
        var type = parameters[1] as Type;
        if (!(fieldInfoFromProperty == null))
        {
            bool flag = fieldInfoFromProperty.IsDefined(typeof(TooltipAttribute), inherit: false);
            field.labelElement.displayTooltipWhenElided = !flag;
        }
    }

    private VisualElement ConfigureLabelOnly(SerializedProperty property)
    {
        VisualElement visualElement = new VisualElement();
        visualElement.AddToClassList(BaseField<ObjectField>.ussClassName);
        visualElement.AddToClassList(BaseField<ObjectField>.alignedFieldUssClassName);
        Label label = new Label();
        string text = this.label ?? property.GetProperty<string>("localizedDisplayName");
        label.name = "unity-input-" + property.propertyPath;
        label.text = text;
        label.AddToClassList(labelUssClassName);
        visualElement.Add(label);
        return visualElement;
    }

    internal static void ConfigureFieldStyles<TField, TValue>(TField field) where TField : BaseField<TValue>
    {
        field.labelElement.AddToClassList(labelUssClassName);
        field.GetProperty<VisualElement>("visualInput").AddToClassList(inputUssClassName);
        field.AddToClassList(BaseField<TValue>.alignedFieldUssClassName);
        field.GetProperty<VisualElement>("visualInput").Query<VisualElement>(null, new string[2]
        {
            BaseField<TValue>.ussClassName,
            BaseCompositeField<int, IntegerField, int>.ussClassName
        }).ForEach(delegate (VisualElement x)
        {
            x.AddToClassList(BaseField<TValue>.alignedFieldUssClassName);
        });
    }

    private VisualElement ConfigureListView(ListView listView, SerializedProperty property, Func<ListView> factory)
    {
        if (listView == null)
        {
            listView = factory();
            listView.showBorder = true;
            listView.selectionType = SelectionType.Multiple;
            listView.showAddRemoveFooter = true;
            listView.showBoundCollectionSize = true;
            listView.showFoldoutHeader = true;
            listView.virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight;
            listView.showAlternatingRowBackgrounds = AlternatingRowBackground.None;
            listView.ReflectSetProperty("itemsSourceSizeChanged", listView.GetProperty<Action>("itemsSourceSizeChanged") + DispatchPropertyChangedEvent) ;
        }

        SerializedProperty serializedProperty = property.Copy();
        string text = listViewNamePrefix + property.propertyPath;
        listView.headerTitle = (string.IsNullOrEmpty(label) ? serializedProperty.GetProperty<string>("localizedDisplayName") : label);
        listView.userData = serializedProperty;
        listView.bindingPath = property.propertyPath;
        listView.viewDataKey = text;
        listView.name = text;
        listView.ReflectSetProperty(listViewBoundFieldProperty, this);
        Toggle toggle = listView.Q<Toggle>(null, Foldout.toggleUssClassName);
        if (toggle != null)
        {
            toggle.GetField<Clickable>("m_Clickable").ReflectSetProperty("acceptClicksIfDisabled", true);
        }

        return listView;
    }

    internal static bool HasVisibleChildFields(SerializedProperty property, bool isUIElements = false)
    {
        switch (property.propertyType)
        {
            case SerializedPropertyType.Vector2:
            case SerializedPropertyType.Vector3:
            case SerializedPropertyType.Rect:
            case SerializedPropertyType.Bounds:
            case SerializedPropertyType.Vector2Int:
            case SerializedPropertyType.Vector3Int:
            case SerializedPropertyType.RectInt:
            case SerializedPropertyType.BoundsInt:
            case SerializedPropertyType.Hash128:
                return false;
            default:
                if (CallStatic<bool>(typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.PropertyHandler"), "UseReorderabelListControl", property))
                {
                    return false;
                }

                var fieldInfo = CallStatic<FieldInfo>(ScriptAttributeUtilityType, "GetFieldInfoAndStaticTypeFromProperty", property, null);
                if (fieldInfo != null && fieldInfo.DeclaringType.IsSubclassOf(typeof(NetworkBehaviour))  && fieldInfo.Name.EndsWith(">k__BackingField"))
                {
                    var propertyName = fieldInfo.Name.Substring(1, fieldInfo.Name.Length - ">k__BackingField".Length - 1);
                    var flag = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
                    var prop = fieldInfo.DeclaringType.GetProperty(propertyName, flag);
                    if (prop != null && prop.CustomAttributes.Any(x => x.AttributeType == typeof(Networked)))
                    {
                        return property.hasChildren;
                    }
                }

                return property.hasVisibleChildren;
        }
    }


    private VisualElement CreateOrUpdateFieldFromProperty(SerializedProperty property, object originalField = null)
    {
        SerializedPropertyType propertyType = property.propertyType;
        if (HasVisibleChildFields(property,true) && !property.isArray)
        {
            return CreateFoldout(property, originalField);
        }

        TrimChildrenContainerSize(0);
        m_ChildrenContainer = null;
        switch (propertyType)
        {
            case SerializedPropertyType.Integer:
                {
                    if (property.type == "long")
                    {
                        return ConfigureField<LongField, long>(originalField as LongField, property, () => new LongField());
                    }

                    if (property.type == "ulong")
                    {
                        return ConfigureField<UnsignedLongField, ulong>(originalField as UnsignedLongField, property, () => new UnsignedLongField());
                    }

                    if (property.type == "uint")
                    {
                        return ConfigureField<UnsignedIntegerField, uint>(originalField as UnsignedIntegerField, property, () => new UnsignedIntegerField());
                    }

                    IntegerField integerField = ConfigureField<IntegerField, int>(originalField as IntegerField, property, () => new IntegerField());
                    if (integerField != null)
                    {
                        integerField.isDelayed = false;
                    }

                    return integerField;
                }
            case SerializedPropertyType.Boolean:
                return ConfigureField<Toggle, bool>(originalField as Toggle, property, () => new Toggle());
            case SerializedPropertyType.Float:
                if (property.type == "double")
                {
                    return ConfigureField<DoubleField, double>(originalField as DoubleField, property, () => new DoubleField());
                }

                return ConfigureField<FloatField, float>(originalField as FloatField, property, () => new FloatField());
            case SerializedPropertyType.String:
                {
                    TextField textField2 = ConfigureField<TextField, string>(originalField as TextField, property, () => new TextField());
                    textField2.maxLength = -1;
                    return textField2;
                }
            case SerializedPropertyType.Color:
                return ConfigureField<ColorField, Color>(originalField as ColorField, property, () => new ColorField());
            case SerializedPropertyType.ManagedReference:
                return ConfigureLabelOnly(property);
            case SerializedPropertyType.ObjectReference:
                {
                    ObjectField objectField = ConfigureField<ObjectField, UnityEngine.Object>(originalField as ObjectField, property, () => new ObjectField());
                    Type type = null;
                    UnityEngine.Object targetObject = property.serializedObject.targetObject;
                    if ((bool)NativeClassExtensionUtilitiesType.GetMethods().First(x=>x.Name == "ExtendsANativeType" && x.GetParameters()[0].ParameterType == typeof(UnityEngine.Object)).Invoke(null, new object[] { targetObject }))
                    {
                        var paramters = new object[] { property, null };
                        CallStatic(ScriptAttributeUtilityType,"GetFieldInfoFromProperty", paramters);
                        type = paramters[1] as Type;
                    }

                    if (type == null)
                    {
                        string value = s_MatchPPtrTypeName.Match(property.type).Groups[1].Value;
                        foreach (Type item in TypeCache.GetTypesDerivedFrom<UnityEngine.Object>())
                        {
                            if (!item.Name.Equals(value, StringComparison.OrdinalIgnoreCase) || typeof(MonoBehaviour).IsAssignableFrom(item) || typeof(ScriptableObject).IsAssignableFrom(item))
                            {
                                continue;
                            }

                            type = item;
                            break;
                        }
                    }

                    objectField.Call("SetObjectTypeWithoutDisplayUpdate",type);
                    objectField.Call("UpdateDisplay");
                    return objectField;
                }
            case SerializedPropertyType.LayerMask:
                return ConfigureField<LayerMaskField, int>(originalField as LayerMaskField, property, () => new LayerMaskField());
            case SerializedPropertyType.Enum:
                {
                    var parameters = new object[] { property, null };
                    CallStatic(ScriptAttributeUtilityType, "GetFieldInfoFromProperty", parameters);
                    var enumType = parameters[1] as Type;
                    //ScriptAttributeUtility.GetFieldInfoFromProperty(property, out var enumType);
                    if (enumType != null && enumType.IsDefined(typeof(FlagsAttribute), inherit: false))
                    {
                        var enumData = EnumDataUtilityType.GetMethods(BindingFlags.Static | BindingFlags.NonPublic).First(x => x.Name == "GetCachedEnumData" && x.GetParameters().Length == 2 && x.GetParameters()[0].ParameterType == typeof(Type) && x.GetParameters()[1].ParameterType == typeof(bool)).Invoke(null, new object[] { enumType, true });
                        if (originalField != null && originalField is EnumFlagsField enumFlagsField)
                        {
                            enumFlagsField.choices = enumData.GetField<IEnumerable<string>>("displayNames").ToList();
                            enumFlagsField.value = (Enum)Enum.ToObject(enumType, property.intValue);
                        }

                        return ConfigureField<EnumFlagsField, Enum>(originalField as EnumFlagsField, property, () => new EnumFlagsField
                        {
                            choices = enumData.GetField<IEnumerable<string>>("displayNames").ToList(),
                            value = (Enum)Enum.ToObject(enumType, property.intValue)
                        });
                    }
                    //Activator.CreateInstance(EnumDataType)
                    object enumData2 = null;
                    if(enumType != null)
                    {
                        enumData2 = EnumDataUtilityType.GetMethods(BindingFlags.Static | BindingFlags.NonPublic).First(x=>x.Name == "GetCachedEnumData" && x.GetParameters().Length == 2 && x.GetParameters()[0].ParameterType == typeof(Type) && x.GetParameters()[1].ParameterType == typeof(bool)).Invoke(null, new object[] { enumType , true} );
                    }
                    string[] enumDisplayNames = CallStatic<string[]>(EditorGUI_EnumNamesCacheType,"GetEnumDisplayNames",property);
                    List<string> popupEntries = (enumData2 != null ? enumData2.GetField<IEnumerable<string>>("displayNames") : enumDisplayNames).ToList();
                    int propertyFieldIndex = ((property.enumValueIndex < 0 || property.enumValueIndex >= enumDisplayNames.Length) ? (-1) : (enumData2 != null ? Array.IndexOf(enumData2.GetField<string[]>("displayNames"), enumDisplayNames[property.enumValueIndex]) : property.enumValueIndex));
                    if (originalField != null && originalField is PopupField<string> popupField)
                    {
                        popupField.choices = popupEntries;
                        popupField.index = propertyFieldIndex;
                    }

                    return ConfigureField<PopupField<string>, string>(originalField as PopupField<string>, property, () => new PopupField<string>(popupEntries, property.enumValueIndex)
                    {
                        index = propertyFieldIndex
                    });
                }
            case SerializedPropertyType.Vector2:
                return ConfigureField<Vector2Field, Vector2>(originalField as Vector2Field, property, () => new Vector2Field());
            case SerializedPropertyType.Vector3:
                return ConfigureField<Vector3Field, Vector3>(originalField as Vector3Field, property, () => new Vector3Field());
            case SerializedPropertyType.Vector4:
                return ConfigureField<Vector4Field, Vector4>(originalField as Vector4Field, property, () => new Vector4Field());
            case SerializedPropertyType.Rect:
                return ConfigureField<RectField, Rect>(originalField as RectField, property, () => new RectField());
            case SerializedPropertyType.ArraySize:
                {
                    IntegerField integerField2 = originalField as IntegerField;
                    if (integerField2 == null)
                    {
                        integerField2 = new IntegerField();
                        integerField2.RegisterValueChangedCallback(delegate (ChangeEvent<int> evt)
                        {
                            OnFieldValueChanged(evt);
                        });
                    }

                    integerField2.SetValueWithoutNotify(property.intValue);
                    integerField2.isDelayed = true;
                    return ConfigureField<IntegerField, int>(integerField2, property, () => new IntegerField());
                }
            case SerializedPropertyType.FixedBufferSize:
                return ConfigureField<IntegerField, int>(originalField as IntegerField, property, () => new IntegerField());
            case SerializedPropertyType.Character:
                {
                    TextField textField = originalField as TextField;
                    if (textField != null)
                    {
                        textField.maxLength = 1;
                    }

                    return ConfigureField<TextField, string>(textField, property, () => new TextField
                    {
                        maxLength = 1
                    });
                }
            case SerializedPropertyType.AnimationCurve:
                return ConfigureField<CurveField, AnimationCurve>(originalField as CurveField, property, () => new CurveField());
            case SerializedPropertyType.Bounds:
                return ConfigureField<BoundsField, Bounds>(originalField as BoundsField, property, () => new BoundsField());
            case SerializedPropertyType.Gradient:
                return ConfigureField<GradientField, Gradient>(originalField as GradientField, property, () => new GradientField());
            case SerializedPropertyType.Quaternion:
                return null;
            case SerializedPropertyType.ExposedReference:
                return null;
            case SerializedPropertyType.Vector2Int:
                return ConfigureField<Vector2IntField, Vector2Int>(originalField as Vector2IntField, property, () => new Vector2IntField());
            case SerializedPropertyType.Vector3Int:
                return ConfigureField<Vector3IntField, Vector3Int>(originalField as Vector3IntField, property, () => new Vector3IntField());
            case SerializedPropertyType.RectInt:
                return ConfigureField<RectIntField, RectInt>(originalField as RectIntField, property, () => new RectIntField());
            case SerializedPropertyType.BoundsInt:
                return ConfigureField<BoundsIntField, BoundsInt>(originalField as BoundsIntField, property, () => new BoundsIntField());
            case SerializedPropertyType.Hash128:
                return ConfigureField<Hash128Field, Hash128>(originalField as Hash128Field, property, () => new Hash128Field());
            case SerializedPropertyType.Generic:
                return property.isArray ? ConfigureListView(originalField as ListView, property, () => new ListView()) : null;
            default:
                return null;
        }
    }

    private void RegisterPropertyChangesOnCustomDrawerElement(VisualElement customPropertyDrawer)
    {
        customPropertyDrawer.RegisterCallback<ChangeEvent<SerializedProperty>>(delegate
        {
            AsyncDispatchPropertyChangedEvent();
        });
        customPropertyDrawer.RegisterCallback<ChangeEvent<int>>(delegate
        {
            AsyncDispatchPropertyChangedEvent();
        });
        customPropertyDrawer.RegisterCallback<ChangeEvent<bool>>(delegate
        {
            AsyncDispatchPropertyChangedEvent();
        });
        customPropertyDrawer.RegisterCallback<ChangeEvent<float>>(delegate
        {
            AsyncDispatchPropertyChangedEvent();
        });
        customPropertyDrawer.RegisterCallback<ChangeEvent<double>>(delegate
        {
            AsyncDispatchPropertyChangedEvent();
        });
        customPropertyDrawer.RegisterCallback<ChangeEvent<string>>(delegate
        {
            AsyncDispatchPropertyChangedEvent();
        });
        customPropertyDrawer.RegisterCallback<ChangeEvent<Color>>(delegate
        {
            AsyncDispatchPropertyChangedEvent();
        });
        customPropertyDrawer.RegisterCallback<ChangeEvent<UnityEngine.Object>>(delegate
        {
            AsyncDispatchPropertyChangedEvent();
        });
        customPropertyDrawer.RegisterCallback<ChangeEvent<Enum>>(delegate
        {
            AsyncDispatchPropertyChangedEvent();
        });
        customPropertyDrawer.RegisterCallback<ChangeEvent<Vector2>>(delegate
        {
            AsyncDispatchPropertyChangedEvent();
        });
        customPropertyDrawer.RegisterCallback<ChangeEvent<Vector3>>(delegate
        {
            AsyncDispatchPropertyChangedEvent();
        });
        customPropertyDrawer.RegisterCallback<ChangeEvent<Vector4>>(delegate
        {
            AsyncDispatchPropertyChangedEvent();
        });
        customPropertyDrawer.RegisterCallback<ChangeEvent<Rect>>(delegate
        {
            AsyncDispatchPropertyChangedEvent();
        });
        customPropertyDrawer.RegisterCallback<ChangeEvent<AnimationCurve>>(delegate
        {
            AsyncDispatchPropertyChangedEvent();
        });
        customPropertyDrawer.RegisterCallback<ChangeEvent<Bounds>>(delegate
        {
            AsyncDispatchPropertyChangedEvent();
        });
        customPropertyDrawer.RegisterCallback<ChangeEvent<Gradient>>(delegate
        {
            AsyncDispatchPropertyChangedEvent();
        });
        customPropertyDrawer.RegisterCallback<ChangeEvent<Quaternion>>(delegate
        {
            AsyncDispatchPropertyChangedEvent();
        });
        customPropertyDrawer.RegisterCallback<ChangeEvent<Vector2Int>>(delegate
        {
            AsyncDispatchPropertyChangedEvent();
        });
        customPropertyDrawer.RegisterCallback<ChangeEvent<Vector3Int>>(delegate
        {
            AsyncDispatchPropertyChangedEvent();
        });
        customPropertyDrawer.RegisterCallback<ChangeEvent<RectInt>>(delegate
        {
            AsyncDispatchPropertyChangedEvent();
        });
        customPropertyDrawer.RegisterCallback<ChangeEvent<BoundsInt>>(delegate
        {
            AsyncDispatchPropertyChangedEvent();
        });
        customPropertyDrawer.RegisterCallback<ChangeEvent<Hash128>>(delegate
        {
            AsyncDispatchPropertyChangedEvent();
        });
    }

    private void AsyncDispatchPropertyChangedEvent()
    {
        m_PropertyChangedCounter++;
        base.schedule.Execute((Action)delegate
        {
            ExecuteAsyncDispatchPropertyChangedEvent();
        });
    }

    private void ExecuteAsyncDispatchPropertyChangedEvent()
    {
        m_PropertyChangedCounter--;
        if (m_PropertyChangedCounter <= 0)
        {
            DispatchPropertyChangedEvent();
            m_PropertyChangedCounter = 0;
        }
    }

    private void DispatchPropertyChangedEvent()
    {
        using SerializedPropertyChangeEvent serializedPropertyChangeEvent = SerializedPropertyChangeEvent.GetPooled(m_SerializedProperty);
        serializedPropertyChangeEvent.target = this;
        SendEvent(serializedPropertyChangeEvent);
    }

    public void RegisterValueChangeCallback(EventCallback<SerializedPropertyChangeEvent> callback)
    {
        if (callback == null)
        {
            return;
        }

        RegisterCallback(delegate (SerializedPropertyChangeEvent evt)
        {
            if (evt.target == this)
            {
                callback(evt);
            }
        });
    }
}