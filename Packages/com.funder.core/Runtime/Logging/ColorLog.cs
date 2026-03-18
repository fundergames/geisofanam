using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using UnityEngine;

public static class ColorLog
{
    private static readonly Dictionary<Type, LogCategoryInfo> CategoryCache = new Dictionary<Type, LogCategoryInfo>();
    private static readonly Dictionary<string, Type> CallSiteCache = new Dictionary<string, Type>();

#if UNITY_EDITOR
    private static readonly StringBuilder StringBuilder = new StringBuilder(256);
#endif

    private class LogCategoryInfo
    {
        public string Category;
        public string Color;
#if UNITY_EDITOR
        public string FormattedPrefix;
#endif
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Info(string message, [CallerFilePath] string filePath = "", [CallerMemberName] string memberName = "", [CallerLineNumber] int lineNumber = 0)
    {
        Log(message, LogType.Log, filePath, memberName, lineNumber);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Debug(string message, [CallerFilePath] string filePath = "", [CallerMemberName] string memberName = "", [CallerLineNumber] int lineNumber = 0)
    {
        Log(message, LogType.Log, filePath, memberName, lineNumber);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Warn(string message, [CallerFilePath] string filePath = "", [CallerMemberName] string memberName = "", [CallerLineNumber] int lineNumber = 0)
    {
        Log(message, LogType.Warning, filePath, memberName, lineNumber);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Error(string message, [CallerFilePath] string filePath = "", [CallerMemberName] string memberName = "", [CallerLineNumber] int lineNumber = 0)
    {
        Log(message, LogType.Error, filePath, memberName, lineNumber);
    }

    private static void Log(string message, LogType logType, string filePath, string memberName, int lineNumber)
    {
#if UNITY_EDITOR
        string callSiteKey = $"{filePath}:{memberName}:{lineNumber}";

        if (!CallSiteCache.TryGetValue(callSiteKey, out Type callerType))
        {
            callerType = GetCallerType();
            if (callerType != null)
            {
                CallSiteCache[callSiteKey] = callerType;
            }
        }

        if (callerType == null)
        {
            UnityEngine.Debug.Log(message);
            return;
        }

        LogCategoryInfo info = GetCategoryInfo(callerType);
        StringBuilder.Clear();
        StringBuilder.Append(info.FormattedPrefix);
        StringBuilder.Append(message);
        string formattedMessage = StringBuilder.ToString();

        switch (logType)
        {
            case LogType.Log:
                UnityEngine.Debug.Log(formattedMessage);
                break;
            case LogType.Warning:
                UnityEngine.Debug.LogWarning(formattedMessage);
                break;
            case LogType.Error:
                UnityEngine.Debug.LogError(formattedMessage);
                break;
        }
#else
        switch (logType)
        {
            case LogType.Log:
                UnityEngine.Debug.Log(message);
                break;
            case LogType.Warning:
                UnityEngine.Debug.LogWarning(message);
                break;
            case LogType.Error:
                UnityEngine.Debug.LogError(message);
                break;
        }
#endif
    }

    private static Type GetCallerType()
    {
        StackTrace stackTrace = new StackTrace();
        StackFrame[] frames = stackTrace.GetFrames();

        if (frames == null || frames.Length < 3)
        {
            return null;
        }

        for (int i = 2; i < frames.Length; i++)
        {
            Type declaringType = frames[i].GetMethod()?.DeclaringType;
            if (declaringType != null && declaringType != typeof(ColorLog))
            {
                return declaringType;
            }
        }

        return null;
    }

    private static LogCategoryInfo GetCategoryInfo(Type type)
    {
        if (CategoryCache.TryGetValue(type, out LogCategoryInfo cached))
        {
            return cached;
        }

        LogCategoryInfo info = new LogCategoryInfo
        {
            Category = type.Name,
            Color = null
        };

        object[] attributes = type.GetCustomAttributes(typeof(LogCategoryAttribute), true);
        if (attributes.Length > 0)
        {
            LogCategoryAttribute attr = (LogCategoryAttribute)attributes[0];
            info.Category = attr.Category;
            info.Color = attr.Color;
        }

#if UNITY_EDITOR
        info.FormattedPrefix = info.Color != null
            ? $"<color={info.Color}>[{info.Category}]</color> "
            : $"[{info.Category}] ";
#endif

        CategoryCache[type] = info;
        return info;
    }
}

[AttributeUsage(AttributeTargets.Class, Inherited = true)]
public class LogCategoryAttribute : Attribute
{
    public string Category { get; }
    public string Color { get; }

    public LogCategoryAttribute(string category, string color = null)
    {
        Category = category;
        Color = color;
    }
}

public static class LogColors
{
    public const string Red = "#FF4444";
    public const string Orange = "#FF8844";
    public const string Yellow = "#FFFF44";
    public const string Green = "#44FF44";
    public const string Cyan = "#44FFFF";
    public const string Blue = "#4444FF";
    public const string Magenta = "#FF44FF";
    public const string Purple = "#BB44FF";
    public const string Pink = "#FF88BB";
    public const string White = "#FFFFFF";
    public const string Gray = "#888888";
    public const string LightBlue = "#88CCFF";
    public const string Lime = "#AAFF44";
    public const string Aqua = "#44FFAA";
    public const string Gold = "#FFD700";
}
