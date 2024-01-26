using com.zibraai.smoke_and_fire.Solver;

namespace com.zibraai.smoke_and_fire
{
    internal class ZibraAiPackageInfo : IPackageInfo
    {
        public string displayName => "Zibra AI - Smoke And Fire";
        public string description =>
            "Real-time smoke and fire GPU accelerated simulation plugin, powered by AI.";
        public string version => ZibraSmokeAndFire.PluginVersion;
    }
}
