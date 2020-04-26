using System;
using System.Collections.Generic;

namespace test_pcm_sys
{
	public struct DataId : IEquatable<DataId>
	{
		public readonly int Value;

		public DataId(int value)
		{
			Value = value;
		}

		public bool Equals(DataId other)
		{
			return Value == other.Value;
		}

		public override bool Equals(object obj)
		{
			return obj is DataId other && Equals(other);
		}

		public override int GetHashCode()
		{
			return Value;
		}
	}

	public interface IDataShape : IEquatable<IDataShape>
	{
		DataId Id { get; }
	}

	public abstract class DataShapeEquation
	{
		public abstract DataId Id { get; }

		protected bool Equals(DataCircle other)
		{
			return Id.Equals(other.Id);
		}

		public override bool Equals(object obj)
		{
			if(ReferenceEquals(null, obj))
			{
				return false;
			}
			if(ReferenceEquals(this, obj))
			{
				return true;
			}
			if(obj.GetType() != GetType())
			{
				return false;
			}
			return Equals((DataCircle)obj);
		}

		public override int GetHashCode()
		{
			return Id.GetHashCode();
		}
	}

	public class DataCircle : DataShapeEquation, IDataShape
	{
		public override DataId Id { get; }
	}

	public class DataSquare : DataShapeEquation, IDataShape
	{
		public DataId Id { get; }
		public int Side { get; }
	}

	public class DataRectangle : DataShapeEquation, IDataShape
	{
		public DataId Id { get; }
		public int Hight { get; }
		public int Width { get; }
	}

	public class Repository
	{
		private HashSet<IDataShape> _storage = new HashSet<IDataShape>();
	}

	internal class Program
	{
		private static void Main(string[] args) { }
	}
}
