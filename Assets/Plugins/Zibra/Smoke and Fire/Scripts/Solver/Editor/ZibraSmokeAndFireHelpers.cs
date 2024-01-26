using UnityEditor;
using UnityEngine;
using com.zibraai.smoke_and_fire.Utilities;
#if ZIBRA_SMOKE_AND_FIRE_PAID_VERSION
using com.zibraai.smoke_and_fire.Editor.SDFObjects;
#endif
using com.zibraai.smoke_and_fire.Solver;

namespace com.zibraai.smoke_and_fire.Editor.Solver
{
    public static class ZibraSmokeAndFireHelpers
    {
        [MenuItem("Zibra AI/Zibra AI - Smoke And Fire/Copy diagnostic information to clipboard", false, 30)]
        public static void Copy()
        {
            string diagInfo = "";
            diagInfo += "////////////////////////////" + "\n";
            diagInfo += "Zibra Smoke & Fire Diagnostic Information" + "\n";
            diagInfo += "Plugin Version: " + ZibraSmokeAndFire.PluginVersion;
#if ZIBRA_SMOKE_AND_FIRE_PAID_VERSION
            diagInfo += " Paid";
#else
            diagInfo += " Free";
#endif
            diagInfo += "\n";
            diagInfo += "Unity Version: " + Application.unityVersion + "\n";
            diagInfo += "Render Pipeline: " + RenderPipelineDetector.GetRenderPipelineType() + "\n";
            diagInfo += "Render Pipelines Imported: SRP";
#if UNITY_PIPELINE_HDRP
            diagInfo += " HDRP";
#endif
#if UNITY_PIPELINE_URP
            diagInfo += " URP";
#endif
            diagInfo += "\n";
            diagInfo += "OS: " + SystemInfo.operatingSystem + "\n";
            diagInfo += "Target Platform: " + EditorUserBuildSettings.activeBuildTarget + "\n";
            diagInfo += "Graphic API: " + SystemInfo.graphicsDeviceType + "\n";
            diagInfo += "GPU: " + SystemInfo.graphicsDeviceName + "\n";
            diagInfo += "GPU Feature Level: " + SystemInfo.graphicsDeviceVersion + "\n";
#if ZIBRA_SMOKE_AND_FIRE_PAID_VERSION
            diagInfo += "Server status: " + ZibraServerAuthenticationManager.GetInstance().GetStatus() + "\n";
            diagInfo +=
                "Key: " + (ZibraServerAuthenticationManager.GetInstance().PluginLicenseKey == "" ? "Unset" : "Set") +
                "\n";
#endif

            if (RenderPipelineDetector.IsURPMissingRenderComponent())
            {
                diagInfo += "URP Smoke And Fire Rendering Component is missing!!!" + "\n";
            }
            diagInfo += "////////////////////////////" + "\n";
            GUIUtility.systemCopyBuffer = diagInfo;
        }
    }
}