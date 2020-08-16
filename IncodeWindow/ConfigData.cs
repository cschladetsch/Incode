namespace Incode
{
    using System.Collections.Generic;

    /// <summary>
    /// Configuration for the app. Stored as a Json file.
    ///
    /// For security reasons, the underlying datafile is not stored in the repo.
    /// This is because it is intended to store things like passwords and credit-card details.
    /// 
    /// </summary>
    internal struct ConfigData
    {
        public Dictionary<string, string> Abbreviations;
        public float Speed;
        public float Accel;
        public float ScrollScale;
        public float ScrollAccel;
        public int ScrollAmount;
        public float MouseFilterResonance;
        public float MouseFilterFrequency;
    }
}
