using System;
using System.Collections.Generic;
using System.Linq;

namespace test_pcm_sys
{
	public struct DataId : IEquatable<DataId>
	{
		// ключ находится под ограничением значения
		// в данной ситуации все кроме ноля
		// ключа со значением 0 в хранилище нет

		public readonly int Value;

		public bool IsWild => Value == 0;

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

		DataId IdParent { get; }

		double Square { get; }
	}

	public abstract class DataShapeEquation<T> where T : class, IDataShape
	{
		public abstract DataId Id { get; }

		public bool Equals(IDataShape other)
		{
			return
				ReferenceEquals(this, other) ||
				Id.Equals(other.Id);
		}

		public bool Equals(T other)
		{
			return
				ReferenceEquals(this, other) ||
				Id.Equals(other.Id);
		}

		public override bool Equals(object @object)
		{
			if(ReferenceEquals(null, @object))
			{
				return false;
			}

			if(@object is T other)
			{
				return Equals(other);
			}

			return false;
		}

		public override int GetHashCode()
		{
			return Id.GetHashCode();
		}
	}

	public class DataCircle : DataShapeEquation<DataCircle>, IDataShape
	{
		public override DataId Id { get; }

		public DataId IdParent { get; }

		public double Square => Radius * Radius * Math.PI;

		public double Radius { get; set; }

		public DataCircle(DataId idSelf, DataId idParent)
		{
			Id = idSelf;
			IdParent = idParent;
		}
	}

	public class DataSquare : DataShapeEquation<DataSquare>, IDataShape
	{
		public override DataId Id { get; }

		public DataId IdParent { get; }

		public double Square => Side * Side;

		public double Side { get; set; }

		public DataSquare(DataId idSelf, DataId idParent)
		{
			Id = idSelf;
			IdParent = idParent;
		}
	}

	public class DataRectangle : DataShapeEquation<DataRectangle>, IDataShape
	{
		public override DataId Id { get; }

		public DataId IdParent { get; }

		public double Square => Height * Width;

		public double Height { get; set; }

		public double Width { get; set; }

		public DataRectangle(DataId idSelf, DataId idParent)
		{
			Id = idSelf;
			IdParent = idParent;
		}
	}

	public interface IAdapter<out T> : IDisposable where T : class, IDataShape
	{
		T Get();
	}

	public class IdGenerator
	{
		private int _current;

		public DataId Create()
		{
			return new DataId(++_current);
		}
	}

	public class Repository
	{
		private class Adapter<T> : IAdapter<T> where T : class, IDataShape
		{
			private readonly T _shape;
			private readonly HashSet<IDataShape> _modified;
			private readonly double _square;

			public Adapter(T shape, HashSet<IDataShape> modified)
			{
				_shape = shape;
				_square = _shape.Square;
				_modified = modified;
			}

			public T Get()
			{
				return _shape;
			}

			public void Dispose()
			{
				// approximation
				if(_shape.Square != _square)
				{
					_modified.Add(_shape);
				}
			}
		}

		private readonly IdGenerator _idGenerator = new IdGenerator();
		private readonly SortedList<DataId, IDataShape> _storage = new SortedList<DataId, IDataShape>();
		private readonly HashSet<IDataShape> _modified = new HashSet<IDataShape>();
		private readonly HashSet<IDataShape> _deleted = new HashSet<IDataShape>();
		// так хотяб контрольная точка будет
		private readonly Dictionary<Type, Func<DataId, DataId, IDataShape>> _factories = new Dictionary<Type, Func<DataId, DataId, IDataShape>>
		{
			{ typeof(DataCircle), (id, idParent) => new DataCircle(id, idParent) },
			{ typeof(DataSquare), (id, idParent) => new DataSquare(id, idParent) },
			{ typeof(DataRectangle), (id, idParent) => new DataRectangle(id, idParent) },
		};

		public IAdapter<T> Create<T>(DataId idParent) where T : class, IDataShape
		{
			if(_factories.TryGetValue(typeof(T), out var factory))
			{
				if(_storage.ContainsKey(idParent))
				{
					var shape = factory(idParent, _idGenerator.Create()) as T;
					_storage.Add(shape.Id, shape);
					_modified.Add(shape);
					return new Adapter<T>(shape, _modified);
				}

				if(_storage.Count == 0 && idParent.IsWild)
				{
					var shape = factory(new DataId(), _idGenerator.Create()) as T;
					_storage.Add(shape.Id, shape);
					_modified.Add(shape);
					return new Adapter<T>(shape, _modified);
				}
			}

			return null;
		}

		public IAdapter<T> ReadOrUpdate<T>(DataId id) where T : class, IDataShape
		{
			return
				_storage.TryGetValue(id, out var value) && value is T shape
					? new Adapter<T>(shape, _modified)
					: null;
		}

		public void Delete(DataId id)
		{
			// можно для каждого узла проверять рут ветки
			// и если он не совпадает с рутом репозитория - считать его удаленным
			// и вообще поудобнее все операции на специальной структуре данных можно было бы сделать
			// это увеличило бы производительность, но это отдельное и довольно большое задание и
			// в ТЗ про скорость ничего не написано, по этому будет вот так

			if(_storage.TryGetValue(id, out var shape))
			{
				var set = new Queue<IDataShape>();
				set.Enqueue(shape);
				while(set.Count > 0)
				{
					var item = set.Dequeue();
					_deleted.Add(item);
					_modified.RemoveWhere(_ => _.Id.Equals(item.Id));
					foreach(var child in _storage.Where(_ => _.Value.IdParent.Equals(item.Id)))
					{
						set.Enqueue(child.Value);
					}
				}
			}
		}

		public void Commit()
		{
			// commit modified as table
			// delete modified as table

			// реализация зависит от слоя подключения к базе
			// метод может даже отправлять один массив
			// с помеченными на удаление элементами
			// но в два массива мне кажется компактнее

			_modified.Clear();
			_deleted.Clear();
		}
	}

	internal class Program
	{
		private static void Main(string[] args)
		{
			// пример использования
			// можно сделать какие нибудь расширения чтобы конфигурирование
			// было больше похоже на класическое флюент кодом c билдером в конце

			var repository = new Repository();

			DataId root;
			var idChildren = new DataId[10];
			using(var adapter = repository.Create<DataCircle>(new DataId()))
			{
				adapter.Get().Radius = 5d;
				root = adapter.Get().Id;
			}

			// create
			for(var index = 0; index < idChildren.Length; index++)
			{
				if(index % 3 == 0)
				{
					using(var adapter = repository.Create<DataCircle>(root))
					{
						adapter.Get().Radius = 5d;
						idChildren[index] = adapter.Get().Id;
					}
				}
				else if(index % 3 == 1)
				{
					using(var adapter = repository.Create<DataSquare>(root))
					{
						adapter.Get().Side = 5d;
						idChildren[index] = adapter.Get().Id;
					}
				}
				else if(index % 3 == 2)
				{
					using(var adapter = repository.Create<DataRectangle>(root))
					{
						adapter.Get().Width = 5d;
						adapter.Get().Height = 5d;
						idChildren[index] = adapter.Get().Id;
					}
				}
			}

			// комит к стати тоже можно во что нибудь такое же обернуть
			repository.Commit();

			// modify
			for(var index = 0; index < idChildren.Length; index++)
			{
				if(index % 3 == 0)
				{
					using(var adapter = repository.ReadOrUpdate<DataCircle>(idChildren[index]))
					{
						// не изменяется, не сохраняется
						adapter.Get().Radius = 5d;
						idChildren[index] = adapter.Get().Id;
					}
				}
				else if(index % 3 == 2)
				{
					using(var adapter = repository.ReadOrUpdate<DataRectangle>(idChildren[index]))
					{
						// изменяется, сохраняется
						adapter.Get().Width *= 2d;
						adapter.Get().Height = 5d;
						idChildren[index] = adapter.Get().Id;
					}
				}
			}

			repository.Commit();

			// delete
			for(var index = 0; index < idChildren.Length; index++)
			{
				if(index % 3 == 0)
				{
					repository.Delete(idChildren[index]);
				}
			}

			repository.Commit();
		}
	}
}
