namespace SecondLife.Characters
{
    /// <summary>
    /// The Adventurers' Guild's demonstrated Capability Grade, F to S
    /// (Systems 8.1).
    ///
    /// This is not power, potential, or a level. It records what the character
    /// showed in an assessment and can miss concealed ability, untested skills,
    /// and equipment changes. <see cref="Unrated"/> covers a character who has
    /// not been assessed at all.
    /// </summary>
    public enum CapabilityGrade
    {
        Unrated = 0,
        F = 1,
        E = 2,
        D = 3,
        C = 4,
        B = 5,
        A = 6,
        S = 7
    }
}
