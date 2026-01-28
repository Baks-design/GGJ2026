using FlaxEngine;
using FlaxEngine.GUI;

namespace ElusiveLife.Application;

public class PerfCounter : Script
{
    Label label;
    string format;

    public override void OnEnable()
    {
        label = Actor.As<UIControl>().Get<Label>();
        format = label.Text;
#if !BUILD_RELEASE
        if (label.Visible) ProfilerGPU.Enabled = true; // Force enable GPU profiler to get GPU timings
#endif
    }

    public override void OnUpdate()
    {
        if (!label.Visible) return;
#if !BUILD_RELEASE
        var stats = ProfilingTools.Stats;
        label.Text = string.Format(format, stats.FPS, stats.DrawGPUTimeMs, stats.DrawCPUTimeMs, stats.UpdateTimeMs);
#else
        label.Text = string.Format(format, Engine.FramesPerSecond, 0, 0, 0);
#endif
    }

    public override void OnDisable()
    {
        label.Text = format;
        label = null;
    }
}