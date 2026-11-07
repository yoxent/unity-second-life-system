using System;
using UnityEngine;

namespace SecondLife.Characters
{
    /// <summary>
    /// The fixed facts about who a character is. Grouped rather than left as loose
    /// fields on <see cref="CharacterProfile"/> so the profile does not become a
    /// flat bag, and so the status screen can be handed one block instead of six
    /// unrelated values.
    ///
    /// Nothing here changes during play. A name that the player sets at character
    /// creation is authored data; a title earned in play is not.
    /// </summary>
    [Serializable]
    public sealed class CharacterIdentity
    {
        [SerializeField] string displayName = "Unnamed";

        [Tooltip("Species. CHAR-001 confirms the protagonist is a reincarnated human.")]
        [SerializeField] string species = string.Empty;

        [Tooltip("Left empty when the player has not chosen. Character creation sets his sex (CHAR-001).")]
        [SerializeField] string gender = string.Empty;

        [Tooltip("The public role he presents. A social cover, not a class lock (Systems 4.1).")]
        [SerializeField] string publicDiscipline = string.Empty;

        [Tooltip("Optional. The portrait frame stays visible without one.")]
        [SerializeField] Sprite portrait;

        [Tooltip("Short prose shown under the name.")]
        [TextArea(2, 6)]
        [SerializeField] string description = string.Empty;

        public string DisplayName => displayName ?? string.Empty;

        public string Species => species ?? string.Empty;

        public string Gender => gender ?? string.Empty;

        public string PublicDiscipline => publicDiscipline ?? string.Empty;

        public Sprite Portrait => portrait;

        public string Description => description ?? string.Empty;
    }
}
