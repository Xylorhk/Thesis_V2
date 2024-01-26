#if UNITY_2019_4_OR_NEWER
using com.zibraai.smoke_and_fire.Plugins.Editor;
using UnityEditor;
using UnityEngine.UIElements;

namespace com.zibraai.smoke_and_fire
{
    internal class SettingsTab : BaseTab
    {
        readonly VisualElement m_StackVisualizersRoot;

        public SettingsTab() : base($"{ZibraAIPackage.WindowTabsPath}/SettingsTab")
        {
        }

        void ModeChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingPlayMode)
            {
                m_StackVisualizersRoot.Clear();
            }
        }
    }
}
#endif