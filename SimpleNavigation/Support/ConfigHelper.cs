using System;
using System.Buffers.Text;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection.Emit;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

using Windows.Storage;
using Windows.Storage.Streams;

namespace SimpleNavigation;

// Use these extension methods to store and retrieve local and roaming app data
// More details regarding storing and retrieving app data at https://docs.microsoft.com/windows/apps/design/app-settings/store-and-retrieve-app-data
public static class ConfigHelper
{
    private const string FileExtension = ".json";
    private const string FileName = "Config";

    #region [Tested Methods]
    public static bool DoesConfigExist()
    {
        if (File.Exists(Path.Combine(Directory.GetCurrentDirectory(), $"{FileName}{FileExtension}")))
        {
            return true;
        }

        return false;
    }

    public static string ToJson(this Dictionary<string, Dictionary<string, string>> source, bool indented = true)
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = indented,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };
        return JsonSerializer.Serialize(source, options);
    }

    public static T? DeserializeFromFile<T>(string filePath, ref string error)
    {
        try
        {
            string jsonString = File.ReadAllText(filePath);
            T? result = System.Text.Json.JsonSerializer.Deserialize<T>(jsonString);
            error = string.Empty;
            return result;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"{nameof(DeserializeFromFile)}: {ex.Message}");
            error = ex.Message;
            return default(T);
        }
    }

    public static bool SerializeToFile<T>(T obj, string filePath, ref string error)
    {
        if (obj == null || string.IsNullOrEmpty(filePath))
            return false;

        try
        {
            string jsonString = System.Text.Json.JsonSerializer.Serialize(obj);
            File.WriteAllText(filePath, jsonString);
            error = string.Empty;
            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"{nameof(SerializeToFile)}: {ex.Message}");
            error = ex.Message;
            return false;
        }
    }


    public static void SaveEncryptedLocalUser(string data)
    {
        if (string.IsNullOrEmpty(data))
            return;

        if (App.IsPackaged)
        {
            Task.Run(async () =>
            {
                var folder = ApplicationData.Current.LocalFolder;
                var file = await folder.CreateFileAsync($"{FileName}{FileExtension}", CreationCollisionOption.ReplaceExisting);
                await FileIO.WriteTextAsync(file, data);
            });
        }
        else
        {
            using (var dest = File.Create(Path.Combine(Directory.GetCurrentDirectory(), "EncryptedUser.txt"), 1024, FileOptions.Encrypted))
            {
                dest.Write(Encoding.UTF8.GetBytes(data), 0, data.Length);
            }
        }
    }

    /// <summary>
    /// Basic config saver.
    /// </summary>
    public static async Task<bool> SaveConfig(Config? obj, bool encrypt = false)
    {
        if (obj == null)
            return false;

        var options = new JsonSerializerOptions { IncludeFields = true, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull, WriteIndented = true };

        if (App.IsPackaged)
        {
            var folder = ApplicationData.Current.LocalFolder;
            using FileStream createStream = File.Create(Path.Combine(folder.Path, $"{FileName}{FileExtension}"), 2048, encrypt ? FileOptions.Encrypted : FileOptions.None);
            await JsonSerializer.SerializeAsync(createStream, obj, options);
            await createStream.DisposeAsync();
        }
        else
        {
            #region [Synchronous Writing]
            //string outputString = JsonSerializer.Serialize(obj, options);
            //File.WriteAllText(Path.Combine(Directory.GetCurrentDirectory(), $"{FileName}{FileExtension}"), outputString);
            #endregion

            #region [Asynchronous Writing]
            using FileStream createStream = File.Create(Path.Combine(Directory.GetCurrentDirectory(), $"{FileName}{FileExtension}"), 2048, encrypt ? FileOptions.Encrypted : FileOptions.None);
            await JsonSerializer.SerializeAsync(createStream, obj, options);
            await createStream.DisposeAsync();
            #endregion
        }

        return true;
    }

    /// <summary>
    /// Basic config loader.
    /// </summary>
    public static async Task<Config?> LoadConfig()
    {
        var options = new JsonSerializerOptions { IncludeFields = true, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull, WriteIndented = true };

        if (App.IsPackaged)
        {
            var folder = ApplicationData.Current.LocalFolder;
            var file = await folder.GetFileAsync($"{FileName}{FileExtension}");
            using FileStream openStream = File.OpenRead(file.Path);
            return await JsonSerializer.DeserializeAsync<Config>(openStream, options) ?? new Config();
        }
        else
        {
            #region [Synchronous Reading]
            //string readString = File.ReadAllText(Path.Combine(Directory.GetCurrentDirectory(), $"{FileName}{FileExtension}"));
            //Config readData = JsonSerializer.Deserialize<Config>(readString, options) ?? new Config();
            #endregion

            #region [Asynchronous Reading]
            using FileStream openStream = File.OpenRead(Path.Combine(Directory.GetCurrentDirectory(), $"{FileName}{FileExtension}"));
            return await JsonSerializer.DeserializeAsync<Config>(openStream, options) ?? new Config();
            #endregion
        }
    }

    /// <summary>
    /// https://docs.microsoft.com/en-us/dotnet/standard/serialization/system-text-json-how-to?pivots=dotnet-5-0
    /// </summary>
    public static async Task JsonSerializingTest(Config? obj, bool encrypt = false)
    {
        if (obj == null)
            return;

        var options = new JsonSerializerOptions { IncludeFields = true, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull, WriteIndented = true };

        // Basic serialize from object...
        //string jsonString = JsonSerializer.Serialize<Config>(obj);

        // Basic deserialize to object...
        //obj = JsonSerializer.Deserialize<Config>(jsonString);

        #region [Synchronous Writing]
        //string outputString = JsonSerializer.Serialize(obj, options);
        //File.WriteAllText(Path.Combine(Directory.GetCurrentDirectory(), $"{FileName}{FileExtension}"), outputString);
        #endregion

        #region [Asynchronous Writing]
        using FileStream createStream = File.Create(Path.Combine(Directory.GetCurrentDirectory(), $"{FileName}{FileExtension}"), 2048, encrypt ? FileOptions.Encrypted : FileOptions.None);
        await JsonSerializer.SerializeAsync(createStream, obj, options);
        await createStream.DisposeAsync();
        #endregion


        #region [Synchronous Reading]
        //string readString = File.ReadAllText(Path.Combine(Directory.GetCurrentDirectory(), $"{FileName}{FileExtension}"));
        //Config readExample1 = JsonSerializer.Deserialize<Config>(readString, options)!;
        #endregion

        #region [Asynchronous Reading]
        using FileStream openStream = File.OpenRead(Path.Combine(Directory.GetCurrentDirectory(), $"{FileName}{FileExtension}"));
        Config readExample2 = await JsonSerializer.DeserializeAsync<Config>(openStream, options) ?? new Config();
        #endregion
    }

    public static void BinaryReadTest()
    {
        var data = Convert.FromHexString("0A0D02033132333435363738397FFF");
        System.IO.BinaryReader br = new System.IO.BinaryReader(new System.IO.MemoryStream(data));
        var val1 = br.ReadInt32();
    }

    public static void BinaryWriteTest()
    {
        // Store monitor info - we won't restore on original screen if original monitor layout has changed
        using (var data = new System.IO.MemoryStream())
        {
            using (var sw = new System.IO.BinaryWriter(data, Encoding.UTF8, false))
            {
                //sw.BaseStream
                sw.Write("StringValue");
                sw.Write((int)1);
                sw.Write((double)10.2);
                sw.Write((float)20.1);
                sw.Write(true);
                sw.Flush();

                //using (var fileStream = new StreamWriter(sw.BaseStream))
                //{
                //    fileStream.BaseStream.Seek(0, SeekOrigin.Begin);
                //    fileStream.WriteLine("[{0}]", DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss.fff tt"));
                //}
            }
        }
    }
    #endregion

    public static bool IsRoamingStorageAvailable(this ApplicationData appData)
    {
        return appData.RoamingStorageQuota == 0;
    }

    public static async Task SaveAsync<T>(this StorageFolder folder, string name, T content)
    {
        var file = await folder.CreateFileAsync(GetFileName(name), CreationCollisionOption.ReplaceExisting);
        var fileContent = JsonSerializer.Serialize<T>(content);
        await FileIO.WriteTextAsync(file, fileContent);
    }

    public static async ValueTask<T?> ReadAsync<T>(this StorageFolder folder, string name)
    {
        if (!File.Exists(Path.Combine(folder.Path, GetFileName(name))))
        {
            return default;
        }

        var file = await folder.GetFileAsync($"{name}.json");
        var fileContent = await FileIO.ReadTextAsync(file);

        return JsonSerializer.Deserialize<T>(fileContent);
    }

    public static void SaveAsync<T>(this ApplicationDataContainer settings, string key, T value)
    {
        settings.SaveString(key, JsonSerializer.Serialize<T>(value));
    }

    public static void SaveString(this ApplicationDataContainer settings, string key, string value)
    {
        settings.Values[key] = value;
    }

    public static T? ReadAsync<T>(this ApplicationDataContainer settings, string key)
    {
        object? obj;

        if (settings.Values.TryGetValue(key, out obj))
        {
            return JsonSerializer.Deserialize<T>((string)obj);
        }

        return default;
    }

    public static async Task<StorageFile> SaveFileAsync(this StorageFolder folder, byte[] content, string fileName, CreationCollisionOption options = CreationCollisionOption.ReplaceExisting)
    {
        if (content == null)
            throw new ArgumentNullException(nameof(content));

        if (string.IsNullOrEmpty(fileName))
            throw new ArgumentException("File name is null or empty. Specify a valid file name", nameof(fileName));

        var storageFile = await folder.CreateFileAsync(fileName, options);
        await FileIO.WriteBytesAsync(storageFile, content);
        return storageFile;
    }

    public static async Task<byte[]?> ReadBytesAsync(this StorageFolder folder, string fileName)
    {
        var item = await folder.TryGetItemAsync(fileName).AsTask().ConfigureAwait(false);

        if ((item != null) && item.IsOfType(StorageItemTypes.File))
        {
            var storageFile = await folder.GetFileAsync(fileName);
            var content = await storageFile.ReadStorageBytesAsync();
            return content;
        }

        return null;
    }

    public static async Task<byte[]?> ReadStorageBytesAsync(this StorageFile file)
    {
        if (file != null)
        {
            using IRandomAccessStream stream = await file.OpenReadAsync();
            using var reader = new Windows.Storage.Streams.DataReader(stream.GetInputStreamAt(0));
            await reader.LoadAsync((uint)stream.Size);
            var bytes = new byte[stream.Size];
            reader.ReadBytes(bytes);
            return bytes;
        }

        return null;
    }

    /// <summary>
    /// Returns a <see cref="Windows.Foundation.Collections.IPropertySet"/>.
    /// This represents a settings map object based on the <see cref="ApplicationDataContainer"/>.
    /// </summary>
    public static IDictionary<string, object>? GetPersistenceStorage(string value, bool createIfMissing = true)
    {
        if (App.IsPackaged)
        {
            if (ApplicationData.Current?.LocalSettings?.Containers.TryGetValue(value, out var container) == true)
                return container.Values;
            else if (createIfMissing)
                return ApplicationData.Current?.LocalSettings?.CreateContainer(value, ApplicationDataCreateDisposition.Always)?.Values;
        }
        return null;
    }

    static string GetFileName(string name)
    {
        return string.Concat(name, FileExtension);
    }
}

/// <summary>
/// Sample property class.
/// </summary>
public class Config
{
    // NOTE: If you don't use the "var opts = JsonSerializerOptions { IncludeFields = true };"
    // when serializing a class then you must add the [JsonInclude] property above each field.

    [JsonInclude]
    [JsonPropertyName("version")]
    public string? version;

    [JsonInclude]
    [JsonPropertyName("theme")]
    public string? theme;

    [JsonInclude]
    [JsonPropertyName("time")]
    public DateTime time;

    [JsonInclude]
    [JsonPropertyName("firstrun")]
    public bool firstRun;

    public override string ToString() => JsonSerializer.Serialize<Config>(this, new JsonSerializerOptions { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull });
}
