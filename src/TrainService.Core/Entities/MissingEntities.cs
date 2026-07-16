using System;
using System.Collections.Generic;

namespace TrainService.Core.Entities;

public class Layer : CadEntity
{
    public string Name { get; set; } = string.Empty;
}

public class HardwareBinding : CadEntity
{
    public Guid DeviceId { get; set; }
    public Guid ElementId { get; set; }
}

public class Scenario : CadEntity
{
    public string Name { get; set; } = string.Empty;
}

public class ScenarioStep : CadEntity
{
    public Guid ScenarioId { get; set; }
    public int Order { get; set; }
}

public class SystemState : CadEntity
{
    public string StateKey { get; set; } = string.Empty;
    public string StateValue { get; set; } = string.Empty;
}

public class TrainState : CadEntity
{
    public Guid TrainId { get; set; }
    public double Speed { get; set; }
}

public class SwitchStateEntity : CadEntity
{
    public Guid SwitchId { get; set; }
    public int State { get; set; }
}
