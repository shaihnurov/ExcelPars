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
    public class TechnicalTask3ViewModel : ObservableObject
    {
        private int state = 0;
        private string _nameDbRecipes = string.Empty;
        private string _nameDbDrug = string.Empty;
        private string _nameDbOwner = string.Empty;
        private int _filterRowCount;
        private string _selectedDbText = "Пожалуйста, выберите основную БД отпуска (Recipes)";
        private ObservableCollection<GetTablesDbModel> _taskModel;
        private ObservableCollection<RecipesModel> _recipesModel;
        private Visibility _isVisibilyGetTables = Visibility.Visible;
        private Visibility _isVisibilyTitleText = Visibility.Visible;
        private Visibility _isVisibilyDataGrid = Visibility.Collapsed;
        private Visibility _isVisibleProgressRing = Visibility.Collapsed;
        private Visibility _isVisibilyCountRow = Visibility.Collapsed;
        private Visibility _isVisibilyButtonStart = Visibility.Collapsed;

        public Visibility IsVisibilyGetTables
        {
            get => _isVisibilyGetTables;
            set
            {
                SetProperty(ref _isVisibilyGetTables, value);
            }
        }
        public Visibility IsVisibilyTitleText
        {
            get => _isVisibilyTitleText;
            set
            {
                SetProperty(ref _isVisibilyTitleText, value);
            }
        }
        public Visibility IsVisibilyDataGrid
        {
            get => _isVisibilyDataGrid;
            set
            {
                SetProperty(ref _isVisibilyDataGrid, value);
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
        public Visibility IsVisibilyCountRow
        {
            get => _isVisibilyCountRow;
            set
            {
                SetProperty(ref _isVisibilyCountRow, value);
            }
        }
        public Visibility IsVisibilyButtonStart
        {
            get => _isVisibilyButtonStart;
            set
            {
                SetProperty(ref _isVisibilyButtonStart, value);
            }
        }

        public int FilterRowCount
        {
            get => _filterRowCount;
            set
            {
                SetProperty(ref _filterRowCount, value);
            }
        }
        public string SelectedDbText
        {
            get => _selectedDbText;
            set
            {
                SetProperty(ref _selectedDbText, value);
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
        public ObservableCollection<RecipesModel> RecipesModel
        {
            get => _recipesModel;
            set
            {
                SetProperty(ref _recipesModel, value);
            }
        }

        public RelayCommand<object> ButtonGetTableCommand { get; set; }
        public AsyncRelayCommand ButtonStartSearch { get; set; }

        public TechnicalTask3ViewModel()
        {
            TaskModel = [];
            RecipesModel = [];

            ButtonGetTableCommand = new RelayCommand<object>(GetTable);
            ButtonStartSearch = new AsyncRelayCommand(StartSearch);

            GetAllTables();
        }

        private void GetAllTables()
        {
            using (ApplicationContextDb db = new())
            {
                var connection = db.Database.GetDbConnection();
                connection.OpenAsync();

                DataTable schema = connection.GetSchema("Tables");

                foreach (DataRow row in schema.Rows)
                {
                    string tableName = row[2].ToString();
                    TaskModel.Add(new GetTablesDbModel { Name = tableName });
                }
            }
        }
        private void GetTable(object parameter)
        {
            if (parameter is GetTablesDbModel selected)
            {
                switch (state)
                {
                    case 0: _nameDbRecipes = selected.Name; SelectedDbText = "Пожалуйста, выберите БД, которая связывается с БД отпуска по nomk_ls (Drug)"; state++; break;
                    case 1: _nameDbDrug = selected.Name; SelectedDbText = "Пожалуйста, выберите БД, которая связывается с БД отпуска по owner (Owner)"; state++; break;
                    case 2: _nameDbOwner = selected.Name;
                        SelectedDbText = "Пожалуйста, укажите число строк для фильтрации";
                        IsVisibilyCountRow = Visibility.Visible;
                        IsVisibilyGetTables = Visibility.Collapsed;
                        IsVisibilyButtonStart = Visibility.Visible;

                        break;
                }
            }
        }
        private async Task StartSearch()
        {
            IsVisibleProgressRing = Visibility.Visible;
            IsVisibilyTitleText = Visibility.Collapsed;

            await FilterRecipes();
        }
        private void AddTotalRow()
        {
            long totalHumans = RecipesModel.Sum(r => r.CountHuman);
            decimal totalPackages = RecipesModel.Sum(r => r.CountPackages);
            decimal totalSum = RecipesModel.Sum(r => r.SumCost);

            RecipesModel.Add(new RecipesModel
            {
                Year = "Итог",
                CountHuman = totalHumans,
                CountPackages = totalPackages,
                SumCost = totalSum,
                IsTotalRow = true
            });
        }
        private async Task FilterRecipes()
        {
            try
            {
                RecipesModel.Clear();
                using (ApplicationContextDb db = new())
                {
                    var command = db.Database.GetDbConnection().CreateCommand();
                    command.CommandText = $"SELECT TOP (@rowCount) * FROM [{_nameDbRecipes}]";
                    command.Parameters.Add(new SqlParameter("@rowCount", FilterRowCount));
                    await db.Database.OpenConnectionAsync();

                    HashSet<string> proccessIds = [];
                    var allRecords = new List<RecipeModel>();

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            string id = reader["id"].ToString();
                            string date = reader["date"].ToString();
                            int nomkLs = Convert.ToInt32(reader["nomk_ls"]);
                            int owner = Convert.ToInt32(reader["owner"]);

                            if (proccessIds.Contains(id))
                                continue;

                            if (!decimal.TryParse(reader["ko_all"].ToString(), out decimal koAll))
                            {
                                Console.WriteLine($"Error value from table ko_all: {reader["ko_all"]}, ID {reader["id"]}");
                                continue;
                            }
                            if (!decimal.TryParse(reader["sl_all"].ToString(), out decimal slAll))
                            {
                                Console.WriteLine($"Error value from table sl_all: {reader["sl_all"]}, ID {reader["id"]}");
                                continue;
                            }

                            allRecords.Add(new RecipeModel
                            {
                                id = id,
                                date = date,
                                nomk_ls = nomkLs,
                                Owner = owner,
                                ko_all = koAll,
                                sl_all = slAll
                            });
                        }
                    }

                    foreach (var record in allRecords)
                    {
                        command.CommandText = $"SELECT * FROM [{_nameDbRecipes}] WHERE date = @p0 AND nomk_ls = @p1 AND owner = @p2";
                        command.Parameters.Clear();
                        command.Parameters.Add(new SqlParameter("@p0", record.date));
                        command.Parameters.Add(new SqlParameter("@p1", record.nomk_ls));
                        command.Parameters.Add(new SqlParameter("@p2", record.Owner));

                        var matchingRecords = new List<RecipeModel>();

                        using (var matchingReader = await command.ExecuteReaderAsync())
                        {
                            while (await matchingReader.ReadAsync())
                            {
                                var matchingRecord = new RecipeModel
                                {
                                    id = matchingReader["id"].ToString(),
                                    date = matchingReader["date"].ToString(),
                                    nomk_ls = Convert.ToInt32(matchingReader["nomk_ls"]),
                                    Owner = Convert.ToInt32(matchingReader["owner"]),
                                    ko_all = Convert.ToDecimal(matchingReader["ko_all"]),
                                    sl_all = Convert.ToDecimal(matchingReader["sl_all"])
                                };
                                matchingRecords.Add(matchingRecord);
                            }
                        }

                        command.CommandText = $"SELECT * FROM [{_nameDbOwner}] WHERE owner = @own";
                        command.Parameters.Clear();
                        command.Parameters.Add(new SqlParameter("@own", matchingRecords.Select(x => x.Owner).First()));

                        string direction = string.Empty;
                        using (var directionReader = await command.ExecuteReaderAsync())
                        {
                            while (await directionReader.ReadAsync())
                            {
                                direction = directionReader["Name"].ToString();
                            }
                        };

                        int totalHumans = matchingRecords.Count;
                        decimal totalPackages = matchingRecords.Sum(x => x.ko_all);
                        decimal totalSum = matchingRecords.Sum(x => x.sl_all);

                        DateTime date = Convert.ToDateTime(record.date);

                        var newRecipe = new RecipesModel
                        {
                            Year = date.Year.ToString(),
                            Direction = direction,
                            CountHuman = totalHumans,
                            CountPackages = totalPackages,
                            SumCost = totalSum
                        };

                        RecipesModel.Add(newRecipe);

                        foreach (var matchingRecord in matchingRecords)
                        {
                            proccessIds.Add(matchingRecord.id);
                        }
                    }
                }
                IsVisibilyDataGrid = Visibility.Visible;
                IsVisibleProgressRing = Visibility.Collapsed;
                AddTotalRow();
            }
            catch(IndexOutOfRangeException)
            {
                MessageBox.Show("Возникла ошибка при формировании отчета. Пожалуйста, убедитесь в корректности выбора БД");

                StartElementUIVisibility();
            }
            catch (SqlException)
            {
                MessageBox.Show("Возникла ошибка при формировании отчета. Пожалуйста, убедитесь в корректности выбора БД");

                StartElementUIVisibility();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Возникла ошибка при формировании отчета. {ex.Message}");

                StartElementUIVisibility();
            }
        }
        private void StartElementUIVisibility()
        {
            state = 0;
            IsVisibilyCountRow = Visibility.Collapsed;
            IsVisibilyDataGrid = Visibility.Collapsed;
            IsVisibilyGetTables = Visibility.Visible;
            IsVisibilyButtonStart = Visibility.Collapsed;
            IsVisibleProgressRing = Visibility.Collapsed;
            IsVisibilyTitleText = Visibility.Visible;
        }
    }
}