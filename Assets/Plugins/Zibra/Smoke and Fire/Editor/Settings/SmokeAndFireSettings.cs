using com.zibraai.smoke_and_fire.Plugins;

namespace com.zibraai.smoke_and_fire
{
    /// <summary>
    /// Scene Management Settings scriptable object.
    /// You can modify this settings using C# or Scene Management Editor Window.
    /// </summary>
    internal class SmokeAndFireSettings : PackageScriptableSettingsSingleton<SmokeAndFireSettings>
    {
        protected override bool IsEditorOnly => true;
        public override string PackageName => ZibraAIPackage.PackageName;
    }
}
