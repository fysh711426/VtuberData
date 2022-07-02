using CsvHelper;
using CsvHelper.Configuration;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VtuberData.Extensions;
using VtuberData.Models;

namespace VtuberData.Storages
{
    public class Storage<TKey, T> where TKey : notnull where T : class
    {
        private string _path;
        private Dictionary<TKey, T> _storage;
        private Func<T, TKey> _keySelector;
        private CsvConfiguration _configuration = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            ShouldQuote = (args) => true
        };

        public Storage(string path, Func<T, TKey> keySelector)
        {
            _path = path;
            _keySelector = keySelector;
            _storage = new();
        }

        public IReadOnlyList<T> GetAll()
        {
            return _storage
                .ToList()
                .Select(it => it.Value)
                .ToList()
                .AsReadOnly();
        }

        public T? Get(TKey key)
        {
            if (_storage.ContainsKey(key))
                return _storage[key];
            return default(T);
        }

        public void Create(T model)
        {
            _storage[_keySelector(model)] = model;
        }
        
        public void Delete(T model)
        {
            _storage.Remove(_keySelector(model));
        }

        public async Task Load()
        {
            _storage = new();

            if (File.Exists(_path))
            {
                using (var reader = new StreamReader(_path, new UTF8Encoding(true)))
                using (var csv = new CsvReader(reader, _configuration))
                {
                    var records = await csv.GetAllRecordsAsync<T>();
                    _storage = records.ToDictionary(_keySelector);
                }
            }
        }

        public async Task Save(Func<IReadOnlyList<T>, IEnumerable<T>> orderBy)
        {
            using (var writer = new StreamWriter(_path, false, new UTF8Encoding(true)))
            using (var csv = new CsvWriter(writer, _configuration))
            {
                csv.WriteHeader<T>();
                csv.NextRecord();

                if (_storage.Count > 0)
                {
                    var list = GetAll();
                    await csv.WriteRecordsAsync(orderBy(list));
                }
            }
        }
    }
}
