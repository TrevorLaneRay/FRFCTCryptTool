using System;

namespace GCLILib.Core;

public class ConsoleOption
{
    public string Name { get; set; }

    public string ShortOp { get; set; }

    public string LongOp { get; set; }

    public string Description { get; set; }

    public bool HasArg { get; set; }

    public object Flag { get; set; }

    public Action<string[]> Func { get; set; }

    public object SpecialObject { get; set; }
}