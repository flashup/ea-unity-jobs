using System;

namespace Data
{
	public readonly struct Bit : IEquatable<Bit>, IComparable<Bit>, IComparable
	{
		private readonly bool _value;

		private Bit(byte value)
		{
			if (value != 0 && value != 1)
			{
				throw new ArgumentOutOfRangeException(nameof(value), "Value must be 0 or 1.");
			}

			_value = value == 1;
		}

		// public static readonly Bit Zero = new Bit(0);
		// public static readonly Bit One = new Bit(1);

		public static implicit operator Bit(byte value) => new Bit(value);
		public static explicit operator byte(Bit bit) => (byte)(bit._value ? 1 : 0);

		public static bool operator ==(Bit c1, Bit c2)
		{
			return c1.Equals(c2);
		}

		public static bool operator !=(Bit c1, Bit c2)
		{
			return !c1.Equals(c2);
		}

		public override string ToString() => (_value ? 1 : 0).ToString();
		
		public int CompareTo(object obj)
		{
			return obj switch
			{
				Bit bitValue => CompareTo(bitValue),
				bool boolValue => CompareTo(boolValue ? 1 : 0),
				int intValue => CompareTo(intValue),
				byte byteValue => CompareTo(byteValue),
				_ => 0
			};
		}

		public int CompareTo(Bit other)
		{
			return CompareTo((byte)other);
		}

		private int CompareTo(int other)
		{
			if ((int)this == other) return 0;
			return (int)this > other ? 1 : -1;
		}
		
		private int CompareTo(byte other)
		{
			if ((byte)this == other) return 0;
			return (byte)this > other ? 1 : -1;
		}

		public override bool Equals(object obj)
		{
			return obj switch
			{
				Bit bitValue => _value == bitValue._value,
				bool boolValue => _value == boolValue,
				int intValue => (_value ? 1 : 0) == intValue,
				byte byteValue => (_value ? 1 : 0) == byteValue,
				_ => false
			};
		}

		public bool Equals(Bit other)
		{
			return _value == other._value;
		}

		public override int GetHashCode()
		{
			return _value.GetHashCode();
		}
	}
}