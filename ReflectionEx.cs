using System;
using System.Reflection;
using UnityEditor;

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

    public static void ReflectSetField(this object obj, string fieldName, object value)
    {
        obj.GetType().GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).SetValue(obj, value);
    }

    public static TResult GetStaticField<T, TResult>(string fieldName)
    {
        return (TResult)typeof(T).GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static).GetValue(null);
    }

    public static void SetStaticField<T>(string fieldName, object value)
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

    public static void SetStaticProperty(Type classType, string fieldName, object value)
    {
        classType.GetProperty(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static).SetValue(null, value);
    }

    public static void SetStaticProperty<TClass>(string fieldName, object value)
    {
        SetStaticProperty(typeof(TClass), fieldName, value);
    }

    public static TResult GetField<T, TResult>(T obj, string fieldName)
    {
        return (TResult)typeof(T).GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).GetValue(obj);
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


    public static readonly Type ScriptAttributeUtilityType = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.ScriptAttributeUtility");
    public static readonly Type NativeClassExtensionUtilitiesType = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.NativeClassExtensionUtilities");
    public static readonly Type EnumDataUtilityType = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.EnumDataUtility");
    public static readonly Type EnumDataType = typeof(UnityEngine.Object).Assembly.GetType("UnityEngine.EnumData");
    public static readonly Type EditorGUI_EnumNamesCacheType = typeof(EditorGUI).GetNestedType("EnumNamesCache", BindingFlags.NonPublic);
}
