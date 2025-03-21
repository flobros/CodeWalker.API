
# CodeWalker API

CodeWalker API is a .NET 9 web API that allows extracting, converting, and interacting with GTA V assets using CodeWalker's libraries.

## 📥 Download & Install

You can download the latest pre-built release from the [Releases](https://github.com/flobros/CodeWalker.API/releases) page. Extract and run the API without needing to build it manually.

## 🔧 Configuration

The API reads its configuration from an `appsettings.json` file. You can also create an `appsettings.Development.json` file for local overrides.

### Example `appsettings.json`:

```json
{
  "GTAPath": "C:\\Program Files\\Rockstar Games\\Grand Theft Auto V",
  "Port": 5555
}
```

## 🚀 Running the API

To start the API, simply execute:

```sh
CodeWalker.API.exe
```

The API will run on the configured port (default: `5555`).

## 🛠 Features

- Extract and convert GTA V files (YDR, YTD, etc.)
- Export models to XML
- Extract textures
- Search for files within RPF archives
- Import XML files back into RPF

## 📌 Usage

Once the API is running, you can interact with it using Swagger UI or HTTP requests.

### 🌐 Swagger UI (Recommended for Testing)

No need for additional tools! Just open the Swagger UI in your browser to test endpoints interactively:

```
http://localhost:5555
```

You can use Swagger to send requests and inspect responses without deploying the API.

### 📝 Example Requests

#### 🔍 Search for a File

```sh
curl "http://localhost:5555/api/search-file?filename=prop_alien_egg_01.ydr"
```

#### 📥 Download & Convert Files to XML

```sh
curl "http://localhost:5555/api/download-files?fullPaths=x64c.rpf/levels/gta5/props/lev_des.rpf/prop_alien_egg_01.ydr&xml=true&outputFolderPath=C:\\GTA_FILES"
```

#### 📤 Import XML Files into RPF

```sh
curl -X POST http://localhost:5555/api/import-xml \
     -H "Content-Type: application/x-www-form-urlencoded" \
     -d "filePaths=C:\\GTA_FILES\\prop_alien_egg_01.ydr.xml" \
     -d "filePaths=C:\\GTA_FILES\\ap1_02_planes003.ydr.xml" \
     -d "rpfArchivePath=C:\\Program Files\\Rockstar Games\\Grand Theft Auto V\\modstore\\new.rpf" \
     -d "outputFolder=C:\\GTA_FILES\\out"
```

## 🛠 Building from Source

If you prefer to build the API manually, follow these steps:

1. Clone the repository:
   ```sh
   git clone https://github.com/flobros/CodeWalker.API.git
   cd CodeWalker.API
   ```

2. Restore dependencies:
   ```sh
   dotnet restore
   ```

3. Build the project:
   ```sh
   dotnet build --configuration Release
   ```

4. Run the API:
   ```sh
   dotnet run
   ```

## 📜 License

This project is released under the MIT License.

## 🤝 Contributing

Pull requests are welcome! If you encounter issues, feel free to open an [issue](https://github.com/flobros/CodeWalker.API/issues).
