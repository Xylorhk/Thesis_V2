using com.zibraai.smoke_and_fire.Solver;
using UnityEngine;

public class TimingsOverlay : MonoBehaviour
{
    public Vector2 scrollPosition = Vector2.zero;
    private string[] DebugEventNames = new string[]{
        "StepPhysics",
        "DrawEffectParticleQuads",
        "SimulateEffectParticles",
        "InitializeEffectParticles",
        "Advection",
        "ApplyManipulators",
        "UpdateRenderTexture",
        "ComputeProperties",
        "ComputeDivergence",
        "SmoothingLOD0",
        "SmoothingLOD1",
        "SmoothingLOD2",
        "ResidualLOD0",
        "ResidualLOD1",
        "ResidualLOD2",
        "PressureProjection",
        "RestrictLOD1",
        "RestrictLOD2",
        "ProlongateLOD0",
        "ProlongateLOD1",
        "JacobiLOD0",
        "JacobiLOD1",
        "JacobiLOD2",
        "VCycle",
        "PressureSolve",
        "Downsample",
        "Solver",
        "SaveAPIState",
        "RestoreAPIState",
        "UpdateSimulationParameters",
        "UpdateRenderParameters",
        "UpdateIndirectDispatchCounts",
        "UnknownEvevnt"
    };
    private GUIStyle currentStyle = null;
    void OnGUI()
    {
        const int SCROLLVIEW_VIEWPORT_MARGIN = 10;
        const int SCROLLVIEW_VIEWPORT_WIDTH = 300;
        const int SCROLLVIEW_VIEWPORT_HEIGHT = 400;

        const int BOX_WIDTH = 275;
        const int BOX_HEIGHT = 25;
        if (SystemInfo.graphicsDeviceType != UnityEngine.Rendering.GraphicsDeviceType.Vulkan)
        {
            GUI.Box(new Rect(Screen.width - SCROLLVIEW_VIEWPORT_WIDTH - SCROLLVIEW_VIEWPORT_MARGIN, SCROLLVIEW_VIEWPORT_MARGIN, BOX_WIDTH, BOX_HEIGHT),
                        $"Stats unavailable");
            return;
        }
        InitStyles();

#if UNITY_ANDROID && !UNITY_EDITOR
        GUI.skin.verticalScrollbar.fixedWidth = 50;
        GUI.skin.verticalScrollbarThumb.fixedWidth = 50;
#endif
        scrollPosition = GUI.BeginScrollView(new Rect(Screen.width - SCROLLVIEW_VIEWPORT_WIDTH - SCROLLVIEW_VIEWPORT_MARGIN, SCROLLVIEW_VIEWPORT_MARGIN, SCROLLVIEW_VIEWPORT_WIDTH, SCROLLVIEW_VIEWPORT_HEIGHT), scrollPosition,
            new Rect(0, 0, 230, BOX_HEIGHT * DebugEventNames.Length));

        int START_X = 0, x = 0, y = 0;
        ZibraSmokeAndFire[] components = FindObjectsOfType<ZibraSmokeAndFire>();
        foreach (var instance in components)
        {
            if (!instance.isActiveAndEnabled)
                continue;
            GUI.Box(new Rect(START_X + x * BOX_WIDTH, y++ * BOX_HEIGHT, BOX_WIDTH, BOX_HEIGHT),
                    $"Smoke & Fire Instance: {instance.name}", currentStyle);
            for (int i = 0; i < instance.DebugTimestampsItemsCount; i++)
            {
                var eventName = DebugEventNames[instance.DebugTimestampsItems[i].EventType];
                var timeVal = instance.DebugTimestampsItems[i].ExecutionTime.ToString("0.00");
                GUI.Box(new Rect(START_X + x * BOX_WIDTH, y++ * BOX_HEIGHT, BOX_WIDTH, BOX_HEIGHT), $"{eventName}: {timeVal}ms.", currentStyle);
            }
        }
        GUI.EndScrollView();
    }

    private void InitStyles()
    {
        if (currentStyle == null)
        {
            currentStyle = new GUIStyle(GUI.skin.box);
            currentStyle.normal.background = MakeTex(2, 2, new Color(0f, 0f, 0f, 0.8f));
        }
    }

    private Texture2D MakeTex(int width, int height, Color col)
    {
        Color[] pix = new Color[width * height];
        for (int i = 0; i < pix.Length; ++i)
        {
            pix[i] = col;
        }
        Texture2D result = new Texture2D(width, height);
        result.SetPixels(pix);
        result.Apply();
        return result;
    }
}