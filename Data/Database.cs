using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using LiteDB;
using ScarletCore.Utils;
using ScarletCore.Events;

namespace ScarletCore.Data;

/// <summary>
/// Generic database service that provides LiteDB-based persistence for mods.
/// Each instance gets its own database file based on the database name provided in the constructor.
/// </summary>
public class Database : IDisposable {
  // LiteDB instance
  private readonly LiteDatabase _db;

  // Database name (used for file naming)
  private readonly string _databaseName;

  // Maximum number of backups to keep
  private int _maxBackups = 50;

  // Auto-backup per-instance
  private bool _autoBackupEnabled;
  private string _autoBackupLocation;

  /// <summary>
  /// Gets or sets the maximum number of backups to keep (default: 50)
  /// </summary>
  public int MaxBackups {
    get => _maxBackups;
    set => _maxBackups = value > 0 ? value : 50;
  }

  /// <summary>
  /// Initializes a new instance of the Database class
  /// </summary>
  /// <param name="pluginGuid">The name of the database</param>
  public Database(string pluginGuid) {
    if (string.IsNullOrWhiteSpace(pluginGuid))
      throw new ArgumentException("Database name cannot be null or empty", nameof(pluginGuid));

    _databaseName = pluginGuid;

    // Create database file path
    var configPath = GetConfigPath();
    Directory.CreateDirectory(configPath);
    var dbPath = Path.Combine(configPath, $"{_databaseName}.db");

    // Initialize LiteDB with connection string
    var connectionString = new ConnectionString {
      Filename = dbPath,
      Connection = ConnectionType.Shared // Permite múltiplos acessos
    };

    _db = new LiteDatabase(connectionString);
    _autoBackupEnabled = false;
  }

  /// <summary>
  /// Enable automatic backups for this database instance. When enabled, each server save triggers a backup.
  /// </summary>
  /// <param name="backupLocation">Optional backup folder (defaults to BepInEx config path)</param>
  public void EnableAutoBackup(string backupLocation = null) {
    if (_autoBackupEnabled) return;
    _autoBackupLocation = backupLocation;
    EventManager.On(ServerEvents.OnSave, AutoBackupHandler);
    _autoBackupEnabled = true;
  }

  /// <summary>
  /// Disable automatic backups for this database instance.
  /// </summary>
  public void DisableAutoBackup() {
    if (!_autoBackupEnabled) return;
    try {
      EventManager.Off(ServerEvents.OnSave, AutoBackupHandler);
    } catch { }
    _autoBackupEnabled = false;
  }

  /// <summary>
  /// Performs cleanup for this database instance when its parent assembly is unloaded.
  /// </summary>
  public void UnregisterAssembly() {
    DisableAutoBackup();
    Dispose();
  }

  [EventPriority(-999)]
  private async void AutoBackupHandler(string saveName) {
    try {
      saveName = saveName.Replace(".save", "");
      await CreateBackup(_autoBackupLocation, saveName);
    } catch (Exception ex) {
      Log.Error($"Auto-backup failed for '{_databaseName}': {ex.Message}");
    }
  }

  /// <summary>
  /// Gets the configuration path for this database instance
  /// </summary>
  private string GetConfigPath() {
    return Path.Combine(BepInEx.Paths.ConfigPath, _databaseName);
  }

  /// <summary>
  /// Gets the full filesystem path for the database file
  /// </summary>
  public string GetFullPath() {
    var configPath = GetConfigPath();
    return Path.Combine(configPath, $"{_databaseName}.db");
  }

  /// <summary>
  /// Gets a LiteDB collection by path (collection name)
  /// </summary>
  private ILiteCollection<BsonDocument> GetCollection(string path) {
    // Substituir caracteres inválidos para nome de collection
    var collectionName = path.Replace("/", "_").Replace("\\", "_");
    return _db.GetCollection(collectionName);
  }

  /// <summary>
  /// Saves data to the database
  /// </summary>
  /// <typeparam name="T">Type of data to save</typeparam>
  /// <param name="path">Collection/document identifier</param>
  /// <param name="data">Data to serialize and save</param>
  public void Save<T>(string path, T data) {
    try {
      var collection = GetCollection(path);
      var bson = BsonMapper.Global.ToDocument(data);

      // Usa o path como _id para permitir substituição
      bson["_id"] = path;

      collection.Upsert(bson);
    } catch (Exception ex) {
      Log.Error($"An error occurred while saving data: {ex.Message}");
    }
  }

  /// <summary>
  /// Loads data from the database
  /// </summary>
  /// <typeparam name="T">Type of data to load</typeparam>
  /// <param name="path">Collection/document identifier</param>
  /// <returns>Deserialized data or default value if doesn't exist</returns>
  public T Load<T>(string path) {
    try {
      var collection = GetCollection(path);
      var doc = collection.FindById(path);

      if (doc == null) {
        return default;
      }

      return BsonMapper.Global.ToObject<T>(doc);
    } catch (Exception ex) {
      Log.Error($"An error occurred while loading data: {ex.Message}");
      return default;
    }
  }

  /// <summary>
  /// Gets data from the database (alias for Load for compatibility)
  /// </summary>
  public T Get<T>(string path) {
    return Load<T>(path);
  }

  /// <summary>
  /// Gets from database or creates and saves data if it doesn't exist
  /// </summary>
  /// <typeparam name="T">Type of data to get or create</typeparam>
  /// <param name="path">Collection/document identifier</param>
  /// <param name="factory">Factory function to create the data if it doesn't exist</param>
  /// <returns>Existing data or newly created and saved data</returns>
  public T GetOrCreate<T>(string path, Func<T> factory) {
    var existingData = Get<T>(path);

    if (existingData != null && !existingData.Equals(default(T))) {
      return existingData;
    }

    var newData = factory();
    Save(path, newData);
    return newData;
  }

  /// <summary>
  /// Gets from database or creates and saves data using default constructor if it doesn't exist
  /// </summary>
  public T GetOrCreate<T>(string path) where T : new() {
    return GetOrCreate(path, () => new T());
  }

  /// <summary>
  /// Checks if a document exists in the database
  /// </summary>
  /// <param name="path">Collection/document identifier</param>
  /// <returns>True if document exists</returns>
  public bool Has(string path) {
    try {
      var collection = GetCollection(path);
      return collection.Exists(Query.EQ("_id", path));
    } catch {
      return false;
    }
  }

  /// <summary>
  /// Deletes a document from the database
  /// </summary>
  /// <param name="path">Collection/document identifier</param>
  /// <returns>True if successfully deleted</returns>
  public bool Delete(string path) {
    try {
      var collection = GetCollection(path);
      return collection.Delete(path);
    } catch (Exception ex) {
      Log.Error($"An error occurred while deleting data: {ex.Message}");
      return false;
    }
  }

  /// <summary>
  /// Gets all document identifiers in a collection (simulates folder listing)
  /// </summary>
  /// <param name="folderPath">Collection prefix to filter by</param>
  /// <param name="includeSubdirectories">Whether to include nested paths</param>
  /// <returns>Array of document identifiers</returns>
  public string[] GetFilesInFolder(string folderPath = "", bool includeSubdirectories = false) {
    try {
      var collections = _db.GetCollectionNames().ToList();
      var results = new List<string>();

      foreach (var collectionName in collections) {
        var collection = _db.GetCollection(collectionName);
        var docs = collection.FindAll();

        foreach (var doc in docs) {
          if (doc.TryGetValue("_id", out var idValue)) {
            var id = idValue.AsString;

            if (string.IsNullOrEmpty(folderPath)) {
              results.Add(id);
            } else if (id.StartsWith(folderPath)) {
              if (includeSubdirectories) {
                results.Add(id);
              } else {
                // Apenas itens diretos (sem sub-paths)
                var relativePath = id.Substring(folderPath.Length).TrimStart('/', '\\');
                if (!relativePath.Contains("/") && !relativePath.Contains("\\")) {
                  results.Add(id);
                }
              }
            }
          }
        }
      }

      return results.ToArray();
    } catch (Exception ex) {
      Log.Error($"An error occurred while getting files in folder: {ex.Message}");
      return Array.Empty<string>();
    }
  }

  /// <summary>
  /// Gets all collection names (simulates directory listing)
  /// </summary>
  public string[] GetDirectoriesInFolder(string folderPath = "", bool includeSubdirectories = false) {
    try {
      var collections = _db.GetCollectionNames().ToArray();

      if (string.IsNullOrEmpty(folderPath)) {
        return collections;
      }

      return collections.Where(c => c.StartsWith(folderPath)).ToArray();
    } catch (Exception ex) {
      Log.Error($"An error occurred while getting directories in folder: {ex.Message}");
      return Array.Empty<string>();
    }
  }

  /// <summary>
  /// Clears cache/optimizes database (checkpoint operation)
  /// </summary>
  public void ClearCache(string path = null) {
    try {
      // LiteDB não tem cache externo como JSON, mas podemos fazer checkpoint
      _db.Checkpoint();
    } catch (Exception ex) {
      Log.Error($"An error occurred during cache clear: {ex.Message}");
    }
  }

  /// <summary>
  /// Saves all pending operations (forces checkpoint)
  /// </summary>
  public void SaveAll() {
    try {
      _db.Checkpoint();
    } catch (Exception ex) {
      Log.Error($"An error occurred while saving all data: {ex.Message}");
    }
  }

  /// <summary>
  /// Creates a backup of the database file
  /// </summary>
  /// <param name="backupLocation">Optional custom backup location</param>
  /// <param name="saveName">Optional save name to include in the backup filename</param>
  /// <returns>Path to the created backup file, or null if backup failed</returns>
  public async Task<string> CreateBackup(string backupLocation = null, string saveName = null) {
    return await Task.Run(() => {
      try {
        var dbPath = GetFullPath();

        if (!File.Exists(dbPath)) {
          Log.Warning($"Database file '{dbPath}' does not exist. No backup created.");
          return null;
        }

        // Force checkpoint before backup
        SaveAll();

        var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        string safeSaveName = null;

        if (!string.IsNullOrWhiteSpace(saveName)) {
          var parts = saveName.Split(Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries);
          safeSaveName = string.Join("_", parts);
        }

        var backupFileName = safeSaveName == null
          ? $"{_databaseName}_backup_{timestamp}.db"
          : $"{_databaseName}_backup_{safeSaveName}_{timestamp}.db";

        var backupPath = backupLocation ?? BepInEx.Paths.ConfigPath;
        var backupFolderPath = Path.Combine(backupPath, $"{_databaseName} Backups");
        var fullBackupPath = Path.Combine(backupFolderPath, backupFileName);

        Directory.CreateDirectory(backupFolderPath);
        CleanupOldBackups(backupFolderPath, _maxBackups);

        // Copia o arquivo do banco
        File.Copy(dbPath, fullBackupPath, true);

        Log.Info($"Database backup created successfully for '{_databaseName}'");
        return fullBackupPath;
      } catch (Exception ex) {
        Log.Error($"Failed to create database backup: {ex.Message}");
        return null;
      }
    });
  }

  /// <summary>
  /// Restores database from a backup file
  /// </summary>
  /// <param name="backupFilePath">Path to the backup file</param>
  /// <param name="clearExisting">If true, clears existing data before restoring</param>
  /// <returns>True if restoration was successful</returns>
  public async Task<bool> RestoreFromBackup(string backupFilePath, bool clearExisting = false) {
    return await Task.Run(() => {
      try {
        if (!File.Exists(backupFilePath)) {
          Log.Error($"Backup file not found: {backupFilePath}");
          return false;
        }

        var dbPath = GetFullPath();

        // Fecha o banco antes de restaurar
        _db.Dispose();

        if (clearExisting && File.Exists(dbPath)) {
          File.Delete(dbPath);
        }

        // Copia o backup
        File.Copy(backupFilePath, dbPath, true);

        // Reabre o banco (precisaria reinicializar a instância, mas isso é complicado)
        // Aqui apenas logamos sucesso, o usuário precisaria recriar a instância
        Log.Info($"Database restored successfully from: {backupFilePath}");
        Log.Warning("Please restart the application or recreate the Database instance for changes to take effect.");

        return true;
      } catch (Exception ex) {
        Log.Error($"Failed to restore database from backup: {ex.Message}");
        return false;
      }
    });
  }

  /// <summary>
  /// Cleans up old backup files
  /// </summary>
  private void CleanupOldBackups(string backupPath, int maxBackups = 10) {
    try {
      if (!Directory.Exists(backupPath)) {
        return;
      }

      var backupPattern = $"{_databaseName}_backup_*.db";
      var backupFiles = Directory.GetFiles(backupPath, backupPattern, SearchOption.TopDirectoryOnly);

      if (backupFiles.Length <= maxBackups) {
        return;
      }

      var sortedFiles = backupFiles
        .Select(file => new FileInfo(file))
        .OrderBy(fileInfo => fileInfo.CreationTime)
        .ToArray();

      int filesToDelete = sortedFiles.Length - maxBackups;

      for (int i = 0; i < filesToDelete && i < sortedFiles.Length; i++) {
        try {
          File.Delete(sortedFiles[i].FullName);
          Log.Info($"Deleted old backup: {sortedFiles[i].Name}");
        } catch (Exception ex) {
          Log.Warning($"Failed to delete old backup {sortedFiles[i].Name}: {ex.Message}");
        }
      }

      Log.Info($"Cleanup completed. Kept {Math.Min(maxBackups, sortedFiles.Length - filesToDelete)} backup(s).");
    } catch (Exception ex) {
      Log.Warning($"Error during backup cleanup: {ex.Message}");
    }
  }

  /// <summary>
  /// Disposes the database connection
  /// </summary>
  public void Dispose() {
    DisableAutoBackup();
    _db?.Dispose();
  }
}