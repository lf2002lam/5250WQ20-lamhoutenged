﻿using Mine.Services;
using Mine.Models;
using Mine.Views;
using System.Diagnostics;
using System;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using Xamarin.Forms;
using System.Linq;

namespace Mine.ViewModels
{
    /// <summary>
    /// Index View Model
    /// Manages the list of data records
    /// </summary>
    public class ItemIndexViewModel : BaseViewModel
    {

        #region Singleton

        // Make this a singleton so it only exist one time because holds all the data records in memory
        private static volatile ItemIndexViewModel instance;
        private static readonly object syncRoot = new Object();

        public static ItemIndexViewModel Instance
        {
            get
            {
                if (instance == null)
                {
                    lock (syncRoot)
                    {
                        if (instance == null)
                        {
                            instance = new ItemIndexViewModel();
                        }
                    }
                }

                return instance;
            }
        }

        #endregion Singleton

        // The Data set of records
        public ObservableCollection<ItemModel> Dataset { get; set; }

        /// <summary>
        /// Connection to the Data store
        /// </summary>
        public IDataStore<ItemModel> DataSource_Mock => new MockDataStore();
        public IDataStore<ItemModel> DataSource_SQL => new DatabaseServices();

        public IDataStore<ItemModel> DataStore;

        public int CurrentDataSource = 0;

        // Command to force a Load of data
        public Command LoadDatasetCommand { get; set; }

        private bool _needsRefresh;

        /// <summary>
        /// Constructor
        /// 
        /// The constructor subscribes message listeners for crudi operations
        /// </summary>
        public ItemIndexViewModel()
        {
            SetDataSource(CurrentDataSource);   // Set to Mock to start with

            Title = "Items";

            Dataset = new ObservableCollection<ItemModel>();
            LoadDatasetCommand = new Command(async () => await ExecuteLoadDataCommand());


            // Register the Create Message
            MessagingCenter.Subscribe<ItemCreatePage, ItemModel>(this, "Create", async (obj, data) =>
            {
                await Add(data as ItemModel);
            });

            // Register the Delete Message
            MessagingCenter.Subscribe<ItemDeletePage, ItemModel>(this, "Delete", async (obj, data) =>
            {
                await Delete(data as ItemModel);
            });

            MessagingCenter.Subscribe<ItemUpdatePage, ItemModel>(this, "Update", async (obj, data) =>
            {
                await Update(data as ItemModel);
            });

            MessagingCenter.Subscribe<AboutPage, int>(this, "SetDataSource", (obj, data) => 
            {
                SetDataSource(data); 
            });

            MessagingCenter.Subscribe<AboutPage, bool>(this, "WipeDataList", (obj, data) =>
            {
                WipeDataList();
            });
        }

        /// <summary>
        /// API to add the Data
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public async Task<bool> Add(ItemModel data)
        {
            Dataset.Add(data);
            var result = await DataStore.CreateAsync(data);

            return result;
        }

        /// <summary>
        /// API to read the Data
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<ItemModel> Read(string id)
        {
            var result = await DataStore.ReadAsync(id);
            return result;
        }

        /// <summary>
        /// API to delete the Data
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<bool> Delete(ItemModel data)
        {
            var record = await Read(data.Id);
            if(record == null)
            {
                return false;
            }
            Dataset.Remove(data);
            var result = await DataStore.DeleteAsync(data.Id);
            return result;
        }

        /// <summary>
        /// API to delete the Data
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<bool> Update(ItemModel data)
        {
            var record = await Read(data.Id);
            if (record == null)
            {
                return false;
            }
            record.Update(data);

            var result = await DataStore.UpdateAsync(record);

            await ExecuteLoadDataCommand();

            return result;
        }

        #region Refresh
        // Return True if a refresh is needed
        // It sets the refresh flag to false
        public bool NeedsRefresh()
        {
            if (_needsRefresh)
            {
                _needsRefresh = false;
                return true;
            }

            return false;
        }

        // Sets the need to refresh
        public void SetNeedsRefresh(bool value)
        {
            _needsRefresh = value;
        }

        // Command that Loads the Data
        private async Task ExecuteLoadDataCommand()
        {
            if (IsBusy)
            {
                return;
            }

            IsBusy = true;

            try
            {
                Dataset.Clear();
                var dataset = await DataStore.IndexAsync(true);

                // Example of how to sort the database output using a linq query.
                // Sort the list
                dataset = dataset
                    .OrderBy(a => a.Name)
                    .ThenBy(a => a.Description)
                    .ToList();

                foreach (var data in dataset)
                {
                    // Make a Copy of the Item Model to add to the List
                    Dataset.Add(new ItemModel(data));
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>
        /// Force data to refresh
        /// </summary>
        public void ForceDataRefresh()
        {
            // Reset
            var canExecute = LoadDatasetCommand.CanExecute(null);
            LoadDatasetCommand.Execute(null);
        }
        #endregion Refresh

        public bool SetDataSource(int isSQL) 
        { 
            if (isSQL == 1) 
            { 
                DataStore = DataSource_SQL;
                CurrentDataSource = 1;
            } 
            else 
            { 
                DataStore = DataSource_Mock;
                CurrentDataSource = 0;
            }

            SetNeedsRefresh(true);

            return true; 
        }

        public void WipeDataList()
        {
            DataStore.WipeDataList();
            SetNeedsRefresh(true);
        }

    }
}