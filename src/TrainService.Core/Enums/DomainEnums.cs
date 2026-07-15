namespace TrainService.Core.Enums;

public enum NodeRole { Plain, SwitchNode, RfidAnchor, StationEntry }
public enum SwitchState { Main, Diverging }
public enum DeviceKind { PC, StationEsp32, TrainEsp32 }
public enum TravelDirection { Forward, Backward }
public enum PortRole { Uplink, Device, Cascade, Empty }
