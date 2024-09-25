using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ExcelPars.Repository;
using Microsoft.EntityFrameworkCore;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace ExcelPars.MVVM.ViewModel
{
    public class ExcelViewModel : ObservableObject
    {
        private string _tableName = string.Empty;
        private DataTable _excelData;
        private Visibility _saveDbFile = Visibility.Collapsed;
        private Visibility _isVisibleProgressRing = Visibility.Collapsed;
        private Visibility _isVisibleDataGrid= Visibility.Collapsed;

        public RelayCommand FileLoadCommand { get; set; }
        public RelayCommand SaveDbCommand { get; set; }

        public Visibility SaveDbFile
        {
            get => _saveDbFile;
            set
            {
                SetProperty(ref _saveDbFile, value);
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
        public Visibility IsVisibleDataGrid
        {
            get => _isVisibleDataGrid;
            set
            {
                SetProperty(ref _isVisibleDataGrid, value);
            }
        }
        public DataTable ExcelData
        {
            get => _excelData;
            set => SetProperty(ref _excelData, value);
        }

        public ExcelViewModel()
        {
            FileLoadCommand = new RelayCommand(async () => await FileLoad());
            SaveDbCommand = new RelayCommand(async () => await SaveToDatabase(_excelData, _tableName));
        }

        private async Task FileLoad()
        {
            OpenFileDialog openFileDialog = new()
            {
                Filter = "Excel files | *.xls;*.xlsx"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                await LoadExcelData(openFileDialog.FileName);
            }
        }
        private async Task LoadExcelData(string filePath)
        {
            DataTable dataTable = await ReadExcel(filePath);

            if (dataTable != null)
            {
                ExcelData = dataTable;

                DateTime now = DateTime.Now;
                string formattedDate = now.ToString("ddMMyyHHmmss");

                _tableName = string.Concat(System.IO.Path.GetFileNameWithoutExtension(filePath).Where(char.IsLetter)).Replace(" ", "_") + formattedDate;
            }
        }
        private Task<DataTable> ReadExcel(string filePath)
        {
            return Task.Run(() =>
            {
                IsVisibleProgressRing = Visibility.Visible;
                SaveDbFile = Visibility.Collapsed;
                IsVisibleDataGrid = Visibility.Collapsed;

                string connectionString = $"Provider=Microsoft.ACE.OLEDB.12.0;Data Source={filePath};Extended Properties='Excel 12.0 Xml;HDR=YES;'";
                DataTable dataTable = new();

                try
                {
                    using (OleDbConnection connection = new(connectionString))
                    {
                        connection.Open();

                        DataTable schemaTable = connection.GetOleDbSchemaTable(OleDbSchemaGuid.Tables, null);

                        if (schemaTable != null && schemaTable.Rows.Count > 0)
                        {
                            string sheetName = schemaTable.Rows[0]["TABLE_NAME"].ToString();
                            string query = $"SELECT * FROM [{sheetName}]";
                            using (OleDbDataAdapter adapter = new(query, connection))
                            {
                                adapter.Fill(dataTable);

                                IsVisibleProgressRing = Visibility.Collapsed;
                                IsVisibleDataGrid = Visibility.Visible;
                                SaveDbFile = Visibility.Visible;
                            }
                        }
                    }
                }
                catch (FormatException ex)
                {
                    MessageBox.Show($"Ошибка формата: {ex.Message}");
                }
                catch (FileNotFoundException ex)
                {
                    MessageBox.Show($"Файл не найден: {ex.Message}");
                }
                catch (UnauthorizedAccessException ex)
                {
                    MessageBox.Show($"Ошибка доступа: {ex.Message}");
                }
                catch (InvalidOperationException ex)
                {
                    MessageBox.Show($"Недопустимая операция: {ex.Message}");
                }
                catch (OleDbException ex)
                {
                    MessageBox.Show($"Ошибка OLE DB: {ex.Message}");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при чтении Excel файла: {ex.Message}");
                }

                return dataTable;
            });
        }
        private async Task SaveToDatabase(DataTable dataTable, string tableName)
        {
            using (ApplicationContextDb db = new())
            {
                string createTableSql = GenerateCreateTableQuery(tableName, dataTable);
                await db.Database.ExecuteSqlRawAsync(createTableSql);

                foreach (DataRow row in dataTable.Rows)
                {
                    string insertSql = GenerateInsertQuery(tableName, dataTable, row);
                    await db.Database.ExecuteSqlRawAsync(insertSql);
                }

                MessageBox.Show("Успешное сохранение в БД");
            }
        }
        private string GenerateCreateTableQuery(string tableName, DataTable dataTable)
        {
            List<string> columns = [];

            foreach(DataColumn column in dataTable.Columns)
            {
                columns.Add($"[{column.ColumnName}] NVARCHAR(MAX)");
            }

            string columnsJoined = string.Join(", ", columns);
            return $"CREATE TABLE {tableName} ({columnsJoined})";
        }
        private string GenerateInsertQuery(string tableName, DataTable dataTable, DataRow row)
        {
            List<string> columnName = [];
            List<string> values = [];

            foreach (DataColumn column in dataTable.Columns)
            {
                columnName.Add($"[{column.ColumnName}]");
                values.Add($"N'{row[column].ToString().Replace("'", "''")}'");
            }

            string columnsJoined = string.Join(", ", columnName);
            string valuesJoined = string.Join(", ", values);
            return $"INSERT INTO {tableName} ({columnsJoined}) VALUES ({valuesJoined})";
        }
    }
}
