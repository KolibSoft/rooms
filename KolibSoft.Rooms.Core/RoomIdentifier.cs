using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace KolibSoft.Rooms.Core;

public struct RoomIdentifier(ArraySegment<byte> data)
{

    public ArraySegment<byte> Data { get; } = data;

    public override string ToString()
    {
        return Encoding.UTF8.GetString(Data);
    }

    public override bool Equals([NotNullWhen(true)] object? obj)
    {
        if (obj == null || GetType() != obj.GetType())
            return false;

        var other = (RoomIdentifier)obj;
        return this == other;
    }

    public override int GetHashCode()
    {
        return Data.GetHashCode();
    }

    public static RoomIdentifier Parse(string @string)
    {
        if (@string.Length != 8 || @string.Any(x => !char.IsDigit(x)))
            throw new FormatException();
        var data = Encoding.UTF8.GetBytes(@string);
        return new RoomIdentifier(data);
    }

    public static bool operator ==(RoomIdentifier lhs, RoomIdentifier rhs)
    {
        return lhs.Data.SequenceEqual(rhs.Data);
    }

    public static bool operator !=(RoomIdentifier lhs, RoomIdentifier rhs)
    {
        return !lhs.Data.SequenceEqual(rhs.Data);
    }

    public static readonly RoomIdentifier None = RoomIdentifier.Parse("00000000");

}