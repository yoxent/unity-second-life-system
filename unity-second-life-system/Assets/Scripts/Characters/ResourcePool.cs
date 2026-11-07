using System;

namespace SecondLife.Characters
{
    /// <summary>
    /// A spent-and-recovered resource, such as HP or mana.
    ///
    /// The maximum is derived from attributes and modifiers; the current value is
    /// played state. Keeping them in one type makes the distinction explicit and
    /// stops a derived maximum from being saved as if it were authored data.
    ///
    /// CMB-006 requires HP and mana to stay distinct mechanisms. They are
    /// separate instances of this type, not a shared pool.
    /// </summary>
    public sealed class ResourcePool
    {
        int _current;
        int _max;

        public ResourcePool(int max)
        {
            Max = max;
            _current = max;
        }

        public int Current => _current;

        public int Max
        {
            get => _max;
            private set => _max = value < 0 ? 0 : value;
        }

        public bool IsEmpty => _current <= 0;

        public float Fraction => _max <= 0 ? 0f : (float)_current / _max;

        public void Spend(int amount)
        {
            if (amount < 0)
                throw new ArgumentOutOfRangeException(nameof(amount), amount, "Cannot spend a negative amount.");

            _current = amount >= _current ? 0 : _current - amount;
        }

        public void Restore(int amount)
        {
            if (amount < 0)
                throw new ArgumentOutOfRangeException(nameof(amount), amount, "Cannot restore a negative amount.");

            var restored = _current + amount;
            _current = restored > _max ? _max : restored;
        }

        public void RestoreToFull()
        {
            _current = _max;
        }

        /// <summary>
        /// Applies a new maximum after a derived-stat change.
        ///
        /// Raising the maximum leaves the current value alone, so the character
        /// does not heal from equipping a Vitality item. Lowering it clamps the
        /// current value, because a character cannot hold more than their maximum.
        /// </summary>
        public void SetMax(int max)
        {
            if (max < 0)
                throw new ArgumentOutOfRangeException(nameof(max), max, "A maximum cannot be negative.");

            Max = max;
            if (_current > _max)
                _current = _max;
        }

        /// <summary>
        /// Restores a saved current value. Used on load only; the maximum comes
        /// from recalculating derived stats, never from the save.
        /// </summary>
        public void RestoreSaved(int current)
        {
            if (current < 0)
                throw new ArgumentOutOfRangeException(nameof(current), current, "A saved value cannot be negative.");

            _current = current > _max ? _max : current;
        }
    }
}
