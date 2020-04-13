namespace Incode
{
    using System;

    /// <summary>
    /// A pending thing to do - also used to map keys to actions.
    /// </summary>
    internal class Action
    {
        public readonly Command Command; // what to do/emulate
        public DateTime Started; // when the key was pressed

        public Action(Command dir) => Command = dir;
    }
}
