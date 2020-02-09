﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SQLite;
using Mine.Models;

namespace Mine
{
    class DatabaseServices
    {
        static readonly Lazy<SQLiteAsyncConnection> lazyInitializer = new Lazy<SQLiteAsyncConnection>(() =>
        {
            return new SQLiteAsyncConnection(Constants.DatabasePath, Constants.Flags);
        });

        static SQLiteAsyncConnection Database => lazyInitializer.Value;
        static bool initialized = false;

        public DatabaseServices()
        {
            InitializeAsync().SafeFireAndForget(false);
        }

        async Task InitializeAsync()
        {
            if (!initialized)
            {
                if (!Database.TableMappings.Any(m => m.MappedType.Name == typeof(ItemModel).Name))
                {
                    await Database.CreateTablesAsync(CreateFlags.None, typeof(ItemModel)).ConfigureAwait(false);
                    initialized = true;
                }
            }
        }

        public Task<List<ItemModel>> IndexAsync()
        {
            return Database.Table<ItemModel>().ToListAsync();
        }

        public Task<int> CreateAsync(ItemModel item)
        {
            return Database.InsertAsync(item);
        }

        public Task<ItemModel> ReadAsync(string id)
        {
            return Database.Table<ItemModel>().Where(i => i.Id.Equals(id)).FirstOrDefaultAsync();
        }

        public Task<int> UpdateAsync(ItemModel item)
        {
            return Database.UpdateAsync(item);
        }

        public Task<int> DeleteAsync(ItemModel item)
        {
            return Database.DeleteAsync(item);
        }


    }
}
