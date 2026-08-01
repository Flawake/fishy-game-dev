using System;
using Mirror;

namespace Grants
{
	/// <summary>
	/// Identifies one optimistic grant. Its own type on purpose: a grant id and an item uuid used to
	/// both be a bare Guid, so a call site could pass either one and the compiler would not care.
	/// </summary>
	public readonly struct GrantId : IEquatable<GrantId>
	{
		public static readonly GrantId None = new GrantId(Guid.Empty);

		private readonly Guid value;

		public Guid Value => value;
		public bool IsValid => value != Guid.Empty;

		public GrantId(Guid value)
		{
			this.value = value;
		}

		public static GrantId New()
		{
			return new GrantId(Guid.NewGuid());
		}

		public bool Equals(GrantId other)
		{
			return value == other.value;
		}

		public override bool Equals(object obj)
		{
			return obj is GrantId other && Equals(other);
		}

		public override int GetHashCode()
		{
			return value.GetHashCode();
		}

		public override string ToString()
		{
			return value.ToString();
		}

		public static bool operator ==(GrantId left, GrantId right)
		{
			return left.Equals(right);
		}

		public static bool operator !=(GrantId left, GrantId right)
		{
			return !left.Equals(right);
		}
	}

	public static class GrantIdReaderWriter
	{
		// Mirror uses the method name pattern WriteX to auto-register.
		public static void WriteGrantId(this NetworkWriter writer, GrantId grantId)
		{
			writer.WriteGuid(grantId.Value);
		}

		// Mirror uses the method name pattern ReadX to auto-register.
		public static GrantId ReadGrantId(this NetworkReader reader)
		{
			return new GrantId(reader.ReadGuid());
		}
	}
}
