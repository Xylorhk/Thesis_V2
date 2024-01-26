#if UNITY_2019_4_OR_NEWER
using com.zibraai.smoke_and_fire.Plugins.Editor;
using UnityEngine;
using UnityEngine.UIElements;
#if ZIBRA_SMOKE_AND_FIRE_PAID_VERSION
using com.zibraai.smoke_and_fire.Editor.SDFObjects;
#endif

namespace com.zibraai.smoke_and_fire
{
    internal class SmokeAndFireSettingsWindow : PackageSettingsWindow<SmokeAndFireSettingsWindow>
    {
        internal override IPackageInfo GetPackageInfo() => new ZibraAiPackageInfo();

        protected override void OnWindowEnable(VisualElement root)
        {
            AddTab("Info", new AboutTab());
        }

        protected void Update()
        {
#if ZIBRA_SMOKE_AND_FIRE_PAID_VERSION
            if (!ZibraServerAuthenticationManager.GetInstance().bNeedRefresh)
                return;

            if (ZibraServerAuthenticationManager.GetInstance().GetStatus() ==
                ZibraServerAuthenticationManager.Status.OK)
            {
                m_Tabs["Info"].Q<Button>("registerKeyBtn").style.display = DisplayStyle.None;
                m_Tabs["Info"].Q<Button>("validateAuthKeyBtn").style.display = DisplayStyle.None;
                m_Tabs["Info"].Q<TextField>("authKeyInputField").style.display = DisplayStyle.None;
                m_Tabs["Info"].Q<Label>("registeredKeyLabel").style.display = DisplayStyle.Flex;
                m_Tabs["Info"].Q<Label>("invalidKeyLabel").style.display = DisplayStyle.None;
            }
            else
            {
                m_Tabs["Info"].Q<Label>("invalidKeyLabel").style.display = DisplayStyle.Flex;
                m_Tabs["Info"].Q<Label>("registeredKeyLabel").style.display = DisplayStyle.None;
            }

            ZibraServerAuthenticationManager.GetInstance().bNeedRefresh = false;
#endif
        }

        public static GUIContent WindowTitle => new GUIContent(ZibraAIPackage.DisplayName);
    }
}
#endif