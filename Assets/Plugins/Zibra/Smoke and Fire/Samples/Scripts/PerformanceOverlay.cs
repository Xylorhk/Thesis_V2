using com.zibraai.smoke_and_fire.Solver;
using UnityEngine;

public class PerformanceOverlay : MonoBehaviour
{

    string FPSLabel = "";
    private int frameCount;
    private float elapsedTime;

    private void Update()
    {
        // FPS calculation
        frameCount++;
        elapsedTime += Time.unscaledDeltaTime;
        if (elapsedTime > 0.5f)
        {
            double frameRate = System.Math.Round(frameCount / elapsedTime);
            frameCount = 0;
            elapsedTime = 0;

            FPSLabel = "FPS: " + frameRate;
        }
    }

    void OnGUI()
    {
        const int BOX_WIDTH = 220;
        const int BOX_HEIGHT = 25;
        const int START_X = 30;
        const int START_Y = 30 + BOX_HEIGHT * 3;
        int y = -3; // Show FPS above all instances
        int x = 0;

        GUI.Box(new Rect(START_X + x * BOX_WIDTH, START_Y + y++ * BOX_HEIGHT, BOX_WIDTH, BOX_HEIGHT), FPSLabel);
        GUI.Box(new Rect(START_X + x * BOX_WIDTH, START_Y + y++ * BOX_HEIGHT, BOX_WIDTH, BOX_HEIGHT),
                $"OS: {SystemInfo.operatingSystem}");
        GUI.Box(new Rect(START_X + x * BOX_WIDTH, START_Y + y++ * BOX_HEIGHT, BOX_WIDTH, BOX_HEIGHT),
                $"Graphics API: {SystemInfo.graphicsDeviceType}");

        ZibraSmokeAndFire[] components = FindObjectsOfType<ZibraSmokeAndFire>();
        foreach (var instance in components)
        {
            if (!instance.isActiveAndEnabled)
                continue;

            float ResolutionScale = instance.EnableDownscale ? instance.DownscaleFactor : 1.0f;
            float PixelCountScale = ResolutionScale * ResolutionScale;
            GUI.Box(new Rect(START_X + x * BOX_WIDTH, START_Y + y++ * BOX_HEIGHT, BOX_WIDTH, BOX_HEIGHT),
                    $"Instance: {instance.name}");
            GUI.Box(new Rect(START_X + x * BOX_WIDTH, START_Y + y++ * BOX_HEIGHT, BOX_WIDTH, BOX_HEIGHT),
                    $"Grid size: {instance.GridSize}");
            GUI.Box(new Rect(START_X + x * BOX_WIDTH, START_Y + y++ * BOX_HEIGHT, BOX_WIDTH, BOX_HEIGHT),
                    $"Render resolution: {ResolutionScale * 100.0f}%");
            GUI.Box(new Rect(START_X + x * BOX_WIDTH, START_Y + y++ * BOX_HEIGHT, BOX_WIDTH, BOX_HEIGHT),
                    $"Render pixel count: {PixelCountScale * 100.0f}%");
            x++;
            y = 0;
        }
    }
}