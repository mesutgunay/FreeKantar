using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Data.Sqlite;
using FreeKantar.Models;

namespace FreeKantar.Data
{
    public class DbService
    {
        private readonly string _connectionString;
        private const string DbPassword = "FreeKantar2026!"; 
        private const string OldPassword = "FreeKantarSecureKey2026!";

        public DbService()
        {
            var dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "kantar.db");
            _connectionString = $"Data Source={dbPath};Password={DbPassword};";
            
            // Check if DB exists, if not, it will be created during InitializeDatabase.Open()
            bool dbExists = File.Exists(dbPath);
            
            if (dbExists)
            {
                TryMigratePassword(dbPath);
            }
            
            InitializeDatabase();
        }

        public string GetDatabasePath()
        {
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "kantar.db");
        }

        private void TryMigratePassword(string dbPath)
        {
            if (!File.Exists(dbPath)) return;
            try
            {
                using (var conn = new SqliteConnection(_connectionString))
                {
                    conn.Open();
                    return; 
                }
            }
            catch (SqliteException ex) when (ex.SqliteErrorCode == 26 || ex.Message.Contains("not a database")) { }
            catch { return; }

            try
            {
                var oldConnStr = $"Data Source={dbPath};Password={OldPassword};";
                using (var conn = new SqliteConnection(oldConnStr))
                {
                    conn.Open();
                    var command = conn.CreateCommand();
                    command.CommandText = $"PRAGMA rekey = '{DbPassword}'";
                    command.ExecuteNonQuery();
                }
            }
            catch { }
        }

        private void InitializeDatabase()
        {
            try
            {
                using (var connection = new SqliteConnection(_connectionString))
                {
                    connection.Open();
                    var command = connection.CreateCommand();
                    command.CommandText = @"
                        CREATE TABLE IF NOT EXISTS Products (
                            Id INTEGER PRIMARY KEY AUTOINCREMENT,
                            Name TEXT NOT NULL,
                            Code TEXT NOT NULL
                        );
                        CREATE TABLE IF NOT EXISTS WeighingRecords (
                            Id INTEGER PRIMARY KEY AUTOINCREMENT,
                            TransactionType TEXT NOT NULL,
                            ProductId INTEGER NOT NULL,
                            DriverName TEXT,
                            DriverSurname TEXT,
                            DriverPhone TEXT,
                            Plate TEXT,
                            Destination TEXT,
                            Description TEXT,
                            FirstWeight REAL NOT NULL,
                            SecondWeight REAL,
                            ThirdWeight REAL,
                            WeightType TEXT DEFAULT 'Kantardan Tartıldı',
                            FirstWeightDate TEXT NOT NULL,
                            SecondWeightDate TEXT,
                            ThirdWeightDate TEXT,
                            IsCompleted INTEGER DEFAULT 0,
                            IsDeleted INTEGER DEFAULT 0
                        );
                        CREATE TABLE IF NOT EXISTS Settings (
                            Key TEXT PRIMARY KEY,
                            Value TEXT NOT NULL
                        );
                    ";
                    command.ExecuteNonQuery();

                    // Migration / Schema checks
                    string[] columns = { 
                        "ALTER TABLE WeighingRecords ADD COLUMN DriverPhone TEXT",
                        "ALTER TABLE WeighingRecords ADD COLUMN ThirdWeight REAL", 
                        "ALTER TABLE WeighingRecords ADD COLUMN ThirdWeightDate TEXT",
                        "ALTER TABLE WeighingRecords ADD COLUMN IsDeleted INTEGER DEFAULT 0",
                        "ALTER TABLE WeighingRecords ADD COLUMN WeightType TEXT DEFAULT 'Kantardan Tartıldı'",
                        "ALTER TABLE WeighingRecords ADD COLUMN Plate TEXT"
                    };

                    foreach (var col in columns) {
                        try {
                            var migrationCmd = connection.CreateCommand();
                            migrationCmd.CommandText = col;
                            migrationCmd.ExecuteNonQuery();
                        } catch { }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Veri tabanı başlatılamadı: {ex.Message}", ex);
            }
        }

        public void AddProduct(Product product)
        {
            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText = "INSERT INTO Products (Name, Code) VALUES ($name, $code)";
                command.Parameters.AddWithValue("$name", product.Name);
                command.Parameters.AddWithValue("$code", product.Code);
                command.ExecuteNonQuery();
            }
        }

        public void UpdateProduct(Product product)
        {
            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText = "UPDATE Products SET Name = $name, Code = $code WHERE Id = $id";
                command.Parameters.AddWithValue("$name", product.Name);
                command.Parameters.AddWithValue("$code", product.Code);
                command.Parameters.AddWithValue("$id", product.Id);
                command.ExecuteNonQuery();
            }
        }

        public void DeleteProduct(int id)
        {
            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText = "DELETE FROM Products WHERE Id = $id";
                command.Parameters.AddWithValue("$id", id);
                command.ExecuteNonQuery();
            }
        }

        public List<Product> GetProducts()
        {
            var products = new List<Product>();
            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText = "SELECT Id, Name, Code FROM Products";
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        products.Add(new Product { 
                            Id = reader.GetInt32(0), 
                            Name = reader.GetString(1), 
                            Code = reader.GetString(2) 
                        });
                    }
                }
            }
            return products;
        }

        public void SaveWeighingFirst(WeighingRecord record)
        {
            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText = @"
                    INSERT INTO WeighingRecords 
                    (TransactionType, ProductId, DriverName, DriverSurname, DriverPhone, Plate, Destination, Description, FirstWeight, WeightType, FirstWeightDate, IsCompleted) 
                    VALUES 
                    ($type, $productId, $dname, $dsurname, $dphone, $plate, $dest, $desc, $weight, $wtype, $date, 0)";
                
                command.Parameters.AddWithValue("$type", record.TransactionType);
                command.Parameters.AddWithValue("$productId", record.ProductId);
                command.Parameters.AddWithValue("$dname", record.DriverName ?? "");
                command.Parameters.AddWithValue("$dsurname", record.DriverSurname ?? "");
                command.Parameters.AddWithValue("$dphone", record.DriverPhone ?? "");
                command.Parameters.AddWithValue("$plate", record.Plate ?? "");
                command.Parameters.AddWithValue("$dest", record.Destination ?? "");
                command.Parameters.AddWithValue("$desc", record.Description ?? "");
                command.Parameters.AddWithValue("$weight", record.FirstWeight);
                command.Parameters.AddWithValue("$wtype", record.WeightType ?? "Kantardan Tartıldı");
                command.Parameters.AddWithValue("$date", record.FirstWeightDate.ToString("yyyy-MM-dd HH:mm:ss"));
                command.ExecuteNonQuery();
            }
        }

        public void UpdateWeighingSecond(WeighingRecord record)
        {
            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText = @"
                    UPDATE WeighingRecords 
                    SET 
                        TransactionType = $type,
                        ProductId = $productId,
                        DriverName = $dname,
                        DriverSurname = $dsurname,
                        DriverPhone = $dphone,
                        Plate = $plate,
                        Destination = $dest,
                        Description = $desc,
                        SecondWeight = $weight, 
                        WeightType = $wtype,
                        SecondWeightDate = $date, 
                        IsCompleted = $completed
                    WHERE Id = $id";
                
                command.Parameters.AddWithValue("$type", record.TransactionType);
                command.Parameters.AddWithValue("$productId", record.ProductId);
                command.Parameters.AddWithValue("$dname", record.DriverName ?? "");
                command.Parameters.AddWithValue("$dsurname", record.DriverSurname ?? "");
                command.Parameters.AddWithValue("$dphone", record.DriverPhone ?? "");
                command.Parameters.AddWithValue("$plate", record.Plate ?? "");
                command.Parameters.AddWithValue("$dest", record.Destination ?? "");
                command.Parameters.AddWithValue("$desc", record.Description ?? "");
                command.Parameters.AddWithValue("$weight", record.SecondWeight ?? 0);
                command.Parameters.AddWithValue("$wtype", record.WeightType ?? "Kantardan Tartıldı");
                command.Parameters.AddWithValue("$date", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                command.Parameters.AddWithValue("$completed", record.TransactionType == "İADE + İLAVE SEVK" ? 0 : 1);
                command.Parameters.AddWithValue("$id", record.Id);
                command.ExecuteNonQuery();
            }
        }

        public List<WeighingRecord> GetWeighingRecordsPaged(int page, int pageSize, bool includeDeleted = false)
        {
            var list = new List<WeighingRecord>();
            int offset = (page - 1) * pageSize;
            string filter = includeDeleted ? "" : " WHERE w.IsDeleted = 0 ";
            
            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText = $@"
                    SELECT 
                        w.Id, w.TransactionType, w.ProductId, w.DriverName, w.DriverSurname, 
                        w.Plate, w.Destination, w.Description, w.FirstWeight, w.SecondWeight, 
                        w.ThirdWeight, w.WeightType, w.FirstWeightDate, w.SecondWeightDate, 
                        w.ThirdWeightDate, w.IsCompleted, w.IsDeleted,
                        p.Name as ProductName, w.DriverPhone 
                    FROM WeighingRecords w
                    JOIN Products p ON w.ProductId = p.Id
                    {filter}
                    ORDER BY w.IsCompleted ASC, w.FirstWeightDate DESC
                    LIMIT $limit OFFSET $offset";
                
                command.Parameters.AddWithValue("$limit", pageSize);
                command.Parameters.AddWithValue("$offset", offset);
                
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        list.Add(new WeighingRecord
                        {
                            Id = reader.GetInt32(0),
                            TransactionType = reader.GetString(1),
                            ProductId = reader.GetInt32(2),
                            DriverName = reader.IsDBNull(3) ? "" : reader.GetString(3),
                            DriverSurname = reader.IsDBNull(4) ? "" : reader.GetString(4),
                            Plate = reader.IsDBNull(5) ? "" : reader.GetString(5),
                            Destination = reader.IsDBNull(6) ? "" : reader.GetString(6),
                            Description = reader.IsDBNull(7) ? "" : reader.GetString(7),
                            FirstWeight = reader.GetDouble(8),
                            SecondWeight = reader.IsDBNull(9) ? (double?)null : reader.GetDouble(9),
                            ThirdWeight = reader.IsDBNull(10) ? (double?)null : reader.GetDouble(10),
                            WeightType = reader.IsDBNull(11) ? "Kantardan Tartıldı" : reader.GetString(11),
                            FirstWeightDate = DateTime.Parse(reader.GetString(12)),
                            SecondWeightDate = reader.IsDBNull(13) ? (DateTime?)null : DateTime.Parse(reader.GetString(13)),
                            ThirdWeightDate = reader.IsDBNull(14) ? (DateTime?)null : DateTime.Parse(reader.GetString(14)),
                            IsCompleted = reader.GetInt32(15) == 1,
                            IsDeleted = reader.GetInt32(16) == 1,
                            ProductName = reader.GetString(17),
                            DriverPhone = reader.IsDBNull(18) ? "" : reader.GetString(18)
                        });
                    }
                }
            }
            return list;
        }

        public void AdminUpdateWeighing(WeighingRecord record)
        {
            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText = @"
                    UPDATE WeighingRecords 
                    SET 
                        TransactionType = $type,
                        ProductId = $productId,
                        DriverName = $dname,
                        DriverSurname = $dsurname,
                        DriverPhone = $dphone,
                        Plate = $plate,
                        Destination = $dest,
                        Description = $desc,
                        FirstWeight = $w1,
                        SecondWeight = $w2, 
                        ThirdWeight = $w3,
                        WeightType = $wtype,
                        IsCompleted = $completed,
                        IsDeleted = $deleted
                    WHERE Id = $id";
                
                command.Parameters.AddWithValue("$type", record.TransactionType);
                command.Parameters.AddWithValue("$productId", record.ProductId);
                command.Parameters.AddWithValue("$dname", record.DriverName ?? "");
                command.Parameters.AddWithValue("$dsurname", record.DriverSurname ?? "");
                command.Parameters.AddWithValue("$dphone", record.DriverPhone ?? "");
                command.Parameters.AddWithValue("$plate", record.Plate ?? "");
                command.Parameters.AddWithValue("$dest", record.Destination ?? "");
                command.Parameters.AddWithValue("$desc", record.Description ?? "");
                command.Parameters.AddWithValue("$w1", record.FirstWeight);
                command.Parameters.AddWithValue("$w2", (object)record.SecondWeight ?? DBNull.Value);
                command.Parameters.AddWithValue("$w3", (object)record.ThirdWeight ?? DBNull.Value);
                command.Parameters.AddWithValue("$wtype", record.WeightType ?? "Manuel Düzeltildi");
                command.Parameters.AddWithValue("$completed", record.IsCompleted ? 1 : 0);
                command.Parameters.AddWithValue("$deleted", record.IsDeleted ? 1 : 0);
                command.Parameters.AddWithValue("$id", record.Id);
                command.ExecuteNonQuery();
            }
        }

        public void DeleteWeighing(int id)
        {
            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText = "UPDATE WeighingRecords SET IsDeleted = 1 WHERE Id = $id";
                command.Parameters.AddWithValue("$id", id);
                command.ExecuteNonQuery();
            }
        }

        public void RestoreWeighing(int id)
        {
            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText = "UPDATE WeighingRecords SET IsDeleted = 0 WHERE Id = $id";
                command.Parameters.AddWithValue("$id", id);
                command.ExecuteNonQuery();
            }
        }

        public void UpdateWeighingThird(WeighingRecord record)
        {
            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText = @"
                    UPDATE WeighingRecords 
                    SET 
                        ThirdWeight = $weight, 
                        ThirdWeightDate = $date, 
                        IsCompleted = 1 
                    WHERE Id = $id";
                
                command.Parameters.AddWithValue("$weight", record.ThirdWeight ?? 0);
                command.Parameters.AddWithValue("$date", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                command.Parameters.AddWithValue("$id", record.Id);
                command.ExecuteNonQuery();
            }
        }

        public int GetTotalWeighingCount(bool includeDeleted = false)
        {
            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Open();
                var command = connection.CreateCommand();
                string filter = includeDeleted ? "" : " WHERE IsDeleted = 0 ";
                command.CommandText = $"SELECT COUNT(*) FROM WeighingRecords {filter}";
                return Convert.ToInt32(command.ExecuteScalar());
            }
        }

        public List<string> GetDistinctDriverNames()
        {
            var names = new List<string>();
            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText = "SELECT DISTINCT DriverName FROM WeighingRecords WHERE DriverName != ''";
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read()) names.Add(reader.GetString(0));
                }
            }
            return names;
        }

        public List<string> GetDistinctDriverSurnames()
        {
            var names = new List<string>();
            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText = "SELECT DISTINCT DriverSurname FROM WeighingRecords WHERE DriverSurname != ''";
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read()) names.Add(reader.GetString(0));
                }
            }
            return names;
        }

        public List<string> GetDistinctPlates()
        {
            var names = new List<string>();
            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText = "SELECT DISTINCT Plate FROM WeighingRecords WHERE Plate != ''";
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read()) names.Add(reader.GetString(0));
                }
            }
            return names;
        }

        public List<string> GetDistinctDestinations()
        {
            var names = new List<string>();
            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText = "SELECT DISTINCT Destination FROM WeighingRecords WHERE Destination != ''";
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read()) names.Add(reader.GetString(0));
                }
            }
            return names;
        }

        public WeighingRecord GetLastRecordByPlate(string plate)
        {
            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText = @"
                    SELECT DriverName, DriverSurname, DriverPhone 
                    FROM WeighingRecords 
                    WHERE Plate = $plate 
                    ORDER BY FirstWeightDate DESC 
                    LIMIT 1";
                command.Parameters.AddWithValue("$plate", plate);
                using (var reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return new WeighingRecord {
                            DriverName = reader.IsDBNull(0) ? "" : reader.GetString(0),
                            DriverSurname = reader.IsDBNull(1) ? "" : reader.GetString(1),
                            DriverPhone = reader.IsDBNull(2) ? "" : reader.GetString(2)
                        };
                    }
                }
            }
            return null;
        }

        public void SaveSetting(string key, string value)
        {
            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText = "INSERT OR REPLACE INTO Settings (Key, Value) VALUES ($key, $val)";
                command.Parameters.AddWithValue("$key", key);
                command.Parameters.AddWithValue("$val", value);
                command.ExecuteNonQuery();
            }
        }

        public string GetSetting(string key, string defaultValue)
        {
            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText = "SELECT Value FROM Settings WHERE Key = $key";
                command.Parameters.AddWithValue("$key", key);
                var val = command.ExecuteScalar();
                return val != null ? val.ToString() : defaultValue;
            }
        }

        public void ResetAllData()
        {
            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText = "DELETE FROM WeighingRecords; DELETE FROM Products;";
                command.ExecuteNonQuery();
            }
        }
    }
}
