using AOT;
using System;
using System.Runtime.InteropServices;
#if UNITY_EDITOR
using UnityEditor;
#endif // UNITY_EDITOR
using UnityEngine;

namespace com.zibraai.smoke_and_fire.Solver
{
#if UNITY_EDITOR
    [InitializeOnLoad]
#endif
    public static class ZibraSmokeAndFireDebug
    {
        public static string EditorPrefsKey = "ZibraSmokeAndFiresLogLevel";
        public static ZibraSmokeAndFireBridge.LogLevel CurrentLogLevel;

        public static void SetLogLevel(ZibraSmokeAndFireBridge.LogLevel level)
        {
            CurrentLogLevel = level;
#if UNITY_EDITOR
            EditorPrefs.SetInt(EditorPrefsKey, (int)level);
#endif // UNITY_EDITOR
            InitializeDebug();
        }
        static ZibraSmokeAndFireDebug()
        {
#if UNITY_EDITOR
            CurrentLogLevel =
                (ZibraSmokeAndFireBridge.LogLevel)EditorPrefs.GetInt(EditorPrefsKey, (int)ZibraSmokeAndFireBridge.LogLevel.Error);
#else
            CurrentLogLevel = ZibraSmokeAndFireBridge.LogLevel.Error;
#endif // UNITY_EDITOR
        }

#if UNITY_EDITOR
        [InitializeOnLoadMethod]
#endif
        static void InitializeDebug()
        {
            DebugLogCallbackT callbackDelegate = new DebugLogCallbackT(DebugLogCallback);
            var settings = new ZibraSmokeAndFireBridge.LoggerSettings();
            settings.PFNCallback = Marshal.GetFunctionPointerForDelegate(callbackDelegate);
            settings.LogLevel = CurrentLogLevel;
            IntPtr settingsPtr = Marshal.AllocHGlobal(Marshal.SizeOf(settings));
            Marshal.StructureToPtr(settings, settingsPtr, true);
            SetDebugLogWrapperPointer(settingsPtr);
            Marshal.FreeHGlobal(settingsPtr);
        }

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void DebugLogCallbackT(IntPtr message);
        [MonoPInvokeCallback(typeof(DebugLogCallbackT))]
        static void DebugLogCallback(IntPtr request)
        {
            ZibraSmokeAndFireBridge.DebugMessage message = Marshal.PtrToStructure<ZibraSmokeAndFireBridge.DebugMessage>(request);
            string text = Marshal.PtrToStringAnsi(message.Text);
            switch (message.Level)
            {
                case ZibraSmokeAndFireBridge.LogLevel.Verbose:
                    Debug.Log("ZibraSmokeAndFire[silly]: " + text);
                    break;
                case ZibraSmokeAndFireBridge.LogLevel.Info:
                    Debug.Log("ZibraSmokeAndFire: " + text);
                    break;
                case ZibraSmokeAndFireBridge.LogLevel.Warning:
                    Debug.LogWarning(text);
                    break;
                case ZibraSmokeAndFireBridge.LogLevel.Performance:
                    Debug.LogWarning("ZibraSmokeAndFire | Performance Warning:" + text);
                    break;
                case ZibraSmokeAndFireBridge.LogLevel.Error:
                    Debug.LogError("ZibraSmokeAndFire" + text);
                    break;
                default:
                    Debug.LogError("ZibraSmokeAndFire | Incorrect native log data format.");
                    break;
            }
        }

        [DllImport(ZibraSmokeAndFireBridge.PluginLibraryName)]
        static extern void SetDebugLogWrapperPointer(IntPtr callback);
    }
}