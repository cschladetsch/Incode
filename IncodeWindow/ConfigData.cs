namespace Incode
{
    using System.Collections.Generic;

    /// <summary>
    /// Configuration for the app. Stored as a Json file.
    ///
    /// Be careful to avoid putting sensitive data into Abbreviations in the repo.
    ///
    /// TODO: Store abbreviations in a separate file.
    /// </summary>
    internal struct ConfigData
    {
        public Dictionary<string, string> Abbreviations;
        public float Speed;
        public float Accel;
        public float ScrollScale;
        public float ScrollAccel;
        public int ScrollAmount;
    }
}
