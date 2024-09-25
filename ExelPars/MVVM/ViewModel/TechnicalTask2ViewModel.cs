using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ExcelPars.MVVM.Model;
using ExcelPars.Repository;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace ExcelPars.MVVM.ViewModel
{
    public class TechnicalTask2ViewModel : ObservableObject
    {
        private string _selectedNameDb = string.Empty;
        private int _storeCount;
        private int _documentCount;
        private Visibility _isVisibilyGetTables = Visibility.Visible;
        private Visibility _isVisibleProgressRing = Visibility.Collapsed;
        private Visibility _isVisibilyResult = Visibility.Collapsed;
        private Visibility _isVisibilyResult1 = Visibility.Collapsed;
        private Visibility _isVisibilyResult2 = Visibility.Collapsed;
        private Visibility _isVisibleButtons = Visibility.Collapsed;
        private ObservableCollection<GetTablesDbModel> _taskModel;
        private ObservableCollection<StoreData> _storeModel;

        public int StoreCount
        {
            get => _storeCount;
            set
            {
                SetProperty(ref _storeCount, value);
            }
        }
        public int DocumentCount
        {
            get => _documentCount;
            set
            {
                SetProperty(ref _documentCount, value);
            }
        }
        public Visibility IsVisibilyGetTables
        {
            get => _isVisibilyGetTables;
            set
            {
                SetProperty(ref _isVisibilyGetTables, value);
            }
        }
        public Visibility IsVisibleProgressRing
        {
            get => _isVisibleProgressRing;
            set
            {
                SetProperty(ref _isVisibleProgressRing, value);
            }
        }
        public Visibility IsVisibilyResult
        {
            get => _isVisibilyResult;
            set
            {
                SetProperty(ref _isVisibilyResult, value);
            }
        }
        public Visibility IsVisibilyResult1
        {
            get => _isVisibilyResult1;
            set
            {
                SetProperty(ref _isVisibilyResult1, value);
            }
        }
        public Visibility IsVisibilyResult2
        {
            get => _isVisibilyResult2;
            set
            {
                SetProperty(ref _isVisibilyResult2, value);
            }
        }
        public Visibility IsVisibleButtons
        {
            get => _isVisibleButtons;
            set
            {
                SetProperty(ref _isVisibleButtons, value);
            }
        }


        public ObservableCollection<GetTablesDbModel> TaskModel
        {
            get => _taskModel;
            set
            {
                SetProperty(ref _taskModel, value);
            }
        }
        public ObservableCollection<StoreData> StoreModel
        {
            get => _storeModel;
            set
            {
                SetProperty(ref _storeModel, value);
            }
        }

        public AsyncRelayCommand<object> ButtonGetTableCommand { get; set; }
        public AsyncRelayCommand ButtonGetResult1Command { get; set; }
        public AsyncRelayCommand ButtonGetResult2Command { get; set; }

        public TechnicalTask2ViewModel()
        {
            TaskModel = [];
            StoreModel = [];

            ButtonGetTableCommand = new AsyncRelayCommand<object>(GetTable);
            ButtonGetResult1Command = new AsyncRelayCommand(FilterStore);
            ButtonGetResult2Command = new AsyncRelayCommand(FilterStoreOneMonth);

            GetAllTables();
        }

        private void GetAllTables()
        {
            using(ApplicationContextDb db = new())
            {
                var connection = db.Database.GetDbConnection();
                connection.OpenAsync();

                DataTable schema = connection.GetSchema("Tables");

                foreach (DataRow row in schema.Rows)
                {
                    string tableName = row[2].ToString();
                    TaskModel.Add(new GetTablesDbModel { Name = tableName});
                }
            }
        }
        private async Task GetTable(object parameter)
        {
            if(parameter is GetTablesDbModel selected)
            {
                try
                {
                    IsVisibilyGetTables = Visibility.Collapsed;
                    IsVisibleProgressRing = Visibility.Visible;
                    IsVisibilyResult = Visibility.Collapsed;
                    IsVisibleButtons = Visibility.Collapsed;

                    _selectedNameDb = selected.Name;
                    await ExecuteTableQuery();

                    IsVisibleProgressRing = Visibility.Collapsed;
                    IsVisibilyResult = Visibility.Visible;
                    IsVisibleButtons = Visibility.Visible;
                }
                catch (SqlException)
                {
                    MessageBox.Show("Неверный формат БД. Отсутствуют столбцы store, docNumber...");
                    IsVisibilyGetTables = Visibility.Visible;
                    IsVisibleProgressRing = Visibility.Collapsed;
                    IsVisibilyResult = Visibility.Collapsed;
                    IsVisibleButtons = Visibility.Collapsed;
                }
            }
        }
        private async Task ExecuteTableQuery()
        {
            using (ApplicationContextDb db = new())
            {
                var connection = db.Database.GetDbConnection();
                await connection.OpenAsync();

                string sqlQuery = $@"SELECT (SELECT COUNT(DISTINCT store) FROM {_selectedNameDb}) AS StoreCount, (SELECT COUNT(DISTINCT docNumber) FROM {_selectedNameDb}) AS DocumentCount";

                using (var command = connection.CreateCommand())
                {
                    command.CommandText = sqlQuery;
                    using(var reader = await command.ExecuteReaderAsync())
                    {
                        while(await reader.ReadAsync())
                        {
                            StoreCount = reader.GetInt32(0);
                            DocumentCount = reader.GetInt32(1);
                        }
                    }
                }
            }
        }
        private async Task FilterStore()
        {
            IsVisibilyResult1 = Visibility.Visible;
            IsVisibilyResult2 = Visibility.Collapsed;

            using (ApplicationContextDb db = new())
            {
                var connection = db.Database.GetDbConnection();
                await connection.OpenAsync();

                string sqlQuery = $@"SELECT Store, MONTH(DocDate) AS Month, YEAR(DocDate) AS Year, COUNT(*) AS DocumentCount FROM {_selectedNameDb} GROUP BY Store, MONTH(DocDate), YEAR(DocDate)
                                    ORDER BY Store, YEAR(DocDate), MONTH(DocDate)";

                using (var command = connection.CreateCommand())
                {
                    StoreModel.Clear();
                    command.CommandText = sqlQuery;

                    using(var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var storeModels = new StoreData
                            {
                                Store = reader.GetString(0),
                                Month = reader.GetInt32(1),
                                Year = reader.GetInt32(2),
                                DocumentCount = reader.GetInt32(3)
                            };

                            StoreModel.Add(storeModels);
                        }
                    }
                }
            }
        }
        private async Task FilterStoreOneMonth()
        {
            IsVisibilyResult2 = Visibility.Visible;
            IsVisibilyResult1 = Visibility.Collapsed;

            using (ApplicationContextDb db = new())
            {
                var connection = db.Database.GetDbConnection();
                await connection.OpenAsync();

                string sqlQuery = $@"SELECT Store, MONTH(DocDate) AS Month, YEAR(DocDate) AS Year, COUNT(*) AS DocumentCount FROM {_selectedNameDb} GROUP BY Store, MONTH(DocDate), YEAR(DocDate)
                                    ORDER BY Store, YEAR(DocDate), MONTH(DocDate)";

                using (var command = connection.CreateCommand())
                {
                    StoreModel.Clear();
                    command.CommandText = sqlQuery;

                    Dictionary<string, List<(int Month, int Year, int DocumentCount)>> storeMonths = [];

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string Store = reader.GetString(0);
                            int Month = reader.GetInt32(1);
                            int Year = reader.GetInt32(2);
                            int DocumentCount = reader.GetInt32(3);

                            if (!storeMonths.ContainsKey(Store))
                            {
                                storeMonths[Store] = [];
                            }

                            storeMonths[Store].Add((Month, Year, DocumentCount));
                        }
                    }

                    foreach (var store in storeMonths)
                    {
                        if(store.Value.Distinct().Count() == 1)
                        {
                            var storeData = store.Value.First();
                            var storeModels = new StoreData
                            {
                                Store = store.Key,
                                Month = storeData.Month,
                                Year = storeData.Year,
                                DocumentCount = storeData.DocumentCount,
                            };

                            StoreModel.Add(storeModels);
                        }
                    }
                }
            }
        }
    }
}
