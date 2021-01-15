using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos;
using Newtonsoft.Json;

namespace CosmicRipper
{
    public static class Program
    {
        private static void Log(string message, ConsoleColor col = ConsoleColor.Gray)
        {
            var prev = Console.ForegroundColor;
            Console.ForegroundColor = col;
            Console.WriteLine(message, col);
            Console.ForegroundColor = prev;
        }

        private static int ExitWithError(string message)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(message);
            return -1;
        }

        private static async Task<List<string>> GetContainersAsync(this Database database)
        {
            using var feed =
                database.GetContainerQueryStreamIterator(
                    "SELECT value c.id FROM c ");
            var containers = new List<string>();

            while (feed.HasMoreResults)
            {
                using var response = await feed.ReadNextAsync();
                response.EnsureSuccessStatusCode();

                using var streamReader = new StreamReader(response.Content);
                var content = await streamReader.ReadToEndAsync();
                var json = JsonConvert.DeserializeObject<dynamic>(content);
                containers.AddRange(json
                    .DocumentCollections
                    .ToObject<List<string>>());
            }

            return containers;
        }

        public static async Task<int> Main(string[] args)
        {
            var consoleColor = Console.ForegroundColor;

            try
            {
                if (args == null || args.Length != 2)
                    return ExitWithError(
                        "Please provide the connection string and database name as arguments.");

                var connectionString = args[0];
                var databaseName = args[1];

                var client = new CosmosClient(
                    connectionString);

                var database = client.GetDatabase(databaseName);
                var containers = await database.GetContainersAsync();

                Log($"Found {containers.Count} container(s).", ConsoleColor.Magenta);

                var backupDir =
                    $"{databaseName}-{DateTime.Now:yyyy-MM-dd}";
                Log($"Creating backup folder: {backupDir}", ConsoleColor.Magenta);
                Directory.CreateDirectory(backupDir);

                foreach (var containerName in containers)
                {
                    Log($"Creating container folder {containerName}.", ConsoleColor.Cyan);
                    var dir = Path.Combine(backupDir, containerName);
                    Directory.CreateDirectory(dir);

                    var container = database.GetContainer(containerName);

                    using var feed = container.GetItemQueryIterator<dynamic>("SELECT * FROM c");
                    while (feed.HasMoreResults)
                    {
                        var response = await feed.ReadNextAsync();

                        foreach (var item in response)
                        {
                            var id = item.id.ToString();

                            Log($"Downloading item with id {id}");

                            await File.WriteAllTextAsync(
                                Path.Combine(dir, $"{id}.json"),
                                item.ToString(),
                                Encoding.UTF8);
                        }
                    }
                }

                Log("Finished backup up entire CosmosDb database.", ConsoleColor.Green);
                return 0;
            }
            catch(Exception ex)
            {
                return ExitWithError(ex.Message);
            }
            finally
            {
                Console.ForegroundColor = consoleColor;
            }
        }
    }
}
