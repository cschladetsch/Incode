namespace Incode
{
    using System;

    [Flags]
    internal enum Command
    {
        Up = 1,
        Down = 2,
        Left = 4,
        Right = 8,
        CursorLeft,
        CursorRight,
        CursorUp,
        CursorDown,
        ScrollUp,
        ScrollDown,
        LeftClick,
        RightClick,
        LeftDown,
        RightDown,
        InsertText,
        Escape,
        VolumeUp,
        VolumeDown,
        VolumeMute,
        Abbreviate
    }
}
