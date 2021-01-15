# CosmicRipper

Azure CosmosDb Ripper to download all data from an Azure CosmosDb.

## How to download everything:

1. Go to your Azure CosmosDb and select `Keys` from the menu
2. Copy the primary or secondary connection string
3. Download this source code
4. Run `dotnet run "<connection-string>" <name-of-database>`

The app will create a directory which matches the name of the database and today's date.

Inside that directory there will be a new folder for each container/collection in the Azure CosmosDb and all items will be saved as JSON documents named by their `id`.
