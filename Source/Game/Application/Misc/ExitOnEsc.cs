using FlaxEngine;

namespace ElusiveLife.Application;

public class ExitOnEsc : Script
{
    public override void OnUpdate()
    {
        if (Input.GetKeyUp(KeyboardKeys.Escape))
            Engine.RequestExit();
    }
}