namespace StockSharp.Algo.Storages
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Text;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.Serialization;

	using StockSharp.Logging;
	using StockSharp.Messages;

	using Key = System.Tuple<Messages.SecurityId, Messages.MarketDataTypes?>;

	/// <summary>
	/// The security based message adapter's provider interface.
	/// </summary>
	public interface ISecurityMessageAdapterProvider : IMappingMessageAdapterProvider<Key>
	{
		/// <summary>
		/// Get adapter by the specified security id.
		/// </summary>
		/// <param name="securityId">Security ID.</param>
		/// <param name="dataType">Data type.</param>
		/// <returns>Found adapter identifier or <see langword="null"/>.</returns>
		Guid? TryGetAdapter(SecurityId securityId, MarketDataTypes? dataType);

		/// <summary>
		/// Make association with adapter.
		/// </summary>
		/// <param name="securityId">Security ID.</param>
		/// <param name="dataType">Data type.</param>
		/// <param name="adapterId">Adapter identifier.</param>
		/// <returns><see langword="true"/> if the association is successfully changed, otherwise, <see langword="false"/>.</returns>
		bool SetAdapter(SecurityId securityId, MarketDataTypes? dataType, Guid adapterId);
	}

	/// <summary>
	/// In memory implementation of <see cref="ISecurityMessageAdapterProvider"/>.
	/// </summary>
	public class InMemorySecurityMessageAdapterProvider : ISecurityMessageAdapterProvider
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="InMemorySecurityMessageAdapterProvider"/>.
		/// </summary>
		public InMemorySecurityMessageAdapterProvider()
		{
		}

		private readonly CachedSynchronizedDictionary<Key, Guid> _adapters = new CachedSynchronizedDictionary<Key, Guid>();

		/// <inheritdoc />
		public IEnumerable<KeyValuePair<Key, Guid>> Adapters => _adapters.CachedPairs;

		/// <inheritdoc />
		public void Init()
		{
		}

		/// <inheritdoc />
		public event Action<Key, Guid, bool> Changed;

		/// <inheritdoc />
		public Guid? TryGetAdapter(Key key)
		{
			return _adapters.TryGetValue2(key);
		}

		/// <inheritdoc />
		public bool SetAdapter(Key key, Guid adapterId)
		{
			if (key == null)
				throw new ArgumentNullException(nameof(key));

			if (adapterId.IsDefault())
				throw new ArgumentNullException(nameof(adapterId));

			lock (_adapters.SyncRoot)
			{
				var prev = TryGetAdapter(key);

				if (prev == adapterId)
					return false;

				_adapters[key] = adapterId;
			}

			Changed?.Invoke(key, adapterId, true);
			return true;
		}

		/// <inheritdoc />
		public bool RemoveAssociation(Key key)
		{
			if (!_adapters.Remove(key))
				return false;

			Changed?.Invoke(key, Guid.Empty, false);
			return true;
		}

		/// <inheritdoc />
		public Guid? TryGetAdapter(SecurityId securityId, MarketDataTypes? dataType)
		{
			return TryGetAdapter(Tuple.Create(securityId, dataType));
		}

		/// <inheritdoc />
		public bool SetAdapter(SecurityId securityId, MarketDataTypes? dataType, Guid adapterId)
		{
			return SetAdapter(Tuple.Create(securityId, dataType), adapterId);
		}
	}

	/// <summary>
	/// CSV implementation of <see cref="ISecurityMessageAdapterProvider"/>.
	/// </summary>
	public class CsvSecurityMessageAdapterProvider : ISecurityMessageAdapterProvider
	{
		private readonly InMemorySecurityMessageAdapterProvider _inMemory = new InMemorySecurityMessageAdapterProvider();

		private readonly string _fileName;

		/// <summary>
		/// Initializes a new instance of the <see cref="CsvSecurityMessageAdapterProvider"/>.
		/// </summary>
		/// <param name="fileName">File name.</param>
		public CsvSecurityMessageAdapterProvider(string fileName)
		{
			if (fileName.IsEmpty())
				throw new ArgumentNullException(nameof(fileName));

			_fileName = fileName;
			_delayAction = new DelayAction(ex => ex.LogError());

			_inMemory.Changed += InMemoryOnChanged;
		}

		private void InMemoryOnChanged(Key key, Guid adapterId, bool changeType)
		{
			Changed?.Invoke(key, adapterId, changeType);
		}

		private DelayAction _delayAction;

		/// <summary>
		/// The time delayed action.
		/// </summary>
		public DelayAction DelayAction
		{
			get => _delayAction;
			set => _delayAction = value ?? throw new ArgumentNullException(nameof(value));
		}

		/// <inheritdoc />
		public IEnumerable<KeyValuePair<Key, Guid>> Adapters => _inMemory.Adapters;

		/// <inheritdoc />
		public void Init()
		{
			_inMemory.Init();

			if (File.Exists(_fileName))
				Load();
		}

		/// <inheritdoc />
		public event Action<Key, Guid, bool> Changed;

		/// <inheritdoc />
		public Guid? TryGetAdapter(Key key) => _inMemory.TryGetAdapter(key);

		/// <inheritdoc />
		public bool SetAdapter(Key key, Guid adapterId) => SetAdapter(key.Item1, key.Item2, adapterId);

		/// <inheritdoc />
		public bool RemoveAssociation(Key key)
		{
			if (!_inMemory.RemoveAssociation(key))
				return false;

			Save(true, _inMemory.Adapters);
			return true;
		}

		/// <inheritdoc />
		public Guid? TryGetAdapter(SecurityId securityId, MarketDataTypes? dataType)
			=> _inMemory.TryGetAdapter(securityId, dataType);

		/// <inheritdoc />
		public bool SetAdapter(SecurityId securityId, MarketDataTypes? dataType, Guid adapterId)
		{
			var has = _inMemory.TryGetAdapter(securityId, dataType) != null;

			if (!_inMemory.SetAdapter(securityId, dataType, adapterId))
				return false;

			Save(has, has ? _inMemory.Adapters : new[] { new KeyValuePair<Key, Guid>(Tuple.Create(securityId, dataType), adapterId) });
			return true;
		}

		private void Load()
		{
			using (var stream = new FileStream(_fileName, FileMode.Open, FileAccess.Read))
			{
				var reader = new FastCsvReader(stream, Encoding.UTF8);

				reader.NextLine();

				while (reader.NextLine())
				{
					var securityId = new SecurityId
					{
						SecurityCode = reader.ReadString(),
						BoardCode = reader.ReadString()
					};

					var dataType = reader.ReadNullableEnum<MarketDataTypes>();
					var adapterId = reader.ReadString().To<Guid>();

					_inMemory.SetAdapter(securityId, dataType, adapterId);
				}
			}
		}

		private void Save(bool overwrite, IEnumerable<KeyValuePair<Key, Guid>> adapters)
		{
			DelayAction.DefaultGroup.Add(() =>
			{
				var appendHeader = overwrite || !File.Exists(_fileName) || new FileInfo(_fileName).Length == 0;
				var mode = overwrite ? FileMode.Create : FileMode.Append;

				using (var writer = new CsvFileWriter(new TransactionFileStream(_fileName, mode)))
				{
					if (appendHeader)
					{
						writer.WriteRow(new[]
						{
							"Symbol",
							"Board",
							"DataType",
							"Adapter"
						});
					}

					foreach (var pair in adapters)
					{
						writer.WriteRow(new[]
						{
							pair.Key.Item1.SecurityCode,
							pair.Key.Item1.BoardCode,
							pair.Key.Item2.To<string>(),
							pair.Value.To<string>()
						});
					}
				}
			});
		}
	}
}