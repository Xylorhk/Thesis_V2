#if UNITY_2019_4_OR_NEWER
using UnityEditor;

namespace com.zibraai.smoke_and_fire
{
    internal static class SmokeAndFireEditorMenu
    {

        [MenuItem(ZibraAIPackage.RootMenu + "Info", false, 0)]
        public static void OpenSettings()
        {
            var windowTitle = SmokeAndFireSettingsWindow.WindowTitle;
            SmokeAndFireSettingsWindow.ShowTowardsInspector(windowTitle.text, windowTitle.image);
        }
    }
}
#endif
