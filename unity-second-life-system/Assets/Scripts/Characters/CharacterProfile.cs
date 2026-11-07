using UnityEngine;

namespace SecondLife.Characters
{
    /// <summary>
    /// The reusable authored template for a character. One asset can seed many
    /// runtime instances, and nothing here changes during play.
    ///
    /// The assessment values below are *defaults*, not current values. A Capability
    /// Grade changes when the character is re-tested, Guild Rank rises through
    /// contracts, and titles accumulate (Systems 8.1). Those live on
    /// <see cref="CharacterState"/>; keeping them here would either mutate a shared
    /// asset or lose the change.
    /// </summary>
    [CreateAssetMenu(
        menuName = "Second Life/Characters/Character Profile",
        fileName = "CharacterProfile")]
    public sealed class CharacterProfile : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] CharacterIdentity identity = new CharacterIdentity();

        [Header("Attributes")]
        [SerializeField] CharacterAttributes attributes =
            new CharacterAttributes(10, 10, 10, 10, 10, 10, 10);

        [Tooltip("Required. Turns the attributes above into derived statistics.")]
        [SerializeField] AttributeScaling scaling;

        [Header("Assessment defaults")]
        [Tooltip("Starting grade. The instance may change; this asset does not.")]
        [SerializeField] CapabilityGrade capabilityGrade = CapabilityGrade.Unrated;

        [Tooltip("Starting guild rank. Institutional trust, not combat power.")]
        [SerializeField] string guildRank = string.Empty;

        [Tooltip("Starting titles. Recognition, not a claim of current superiority.")]
        [SerializeField] string[] titles = new string[0];

        public CharacterIdentity Identity => identity ?? (identity = new CharacterIdentity());

        public string DisplayName => Identity.DisplayName;

        public CharacterAttributes Attributes => attributes;

        public AttributeScaling Scaling => scaling;

        public CapabilityGrade DefaultCapabilityGrade => capabilityGrade;

        public string DefaultGuildRank => guildRank ?? string.Empty;

        public string[] DefaultTitles => titles ?? new string[0];

        /// <summary>
        /// Required references and sane starting data. A profile that fails this
        /// cannot produce a character, so the caller reports the failure instead
        /// of showing invented numbers.
        /// </summary>
        public bool IsValid => scaling != null && attributes.HasValidValues && scaling.HasValidCoefficients;
    }
}
