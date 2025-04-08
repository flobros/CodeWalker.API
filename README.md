# CodeWalker API

CodeWalker API is a .NET 9 web API that allows extracting, converting, and interacting with GTA V assets using CodeWalker's libraries.

## 📥 Download & Install

You can download the latest pre-built release from the [Releases](https://github.com/flobros/CodeWalker.API/releases) page. Extract and run the API without needing to build it manually.

## 🔧 Configuration

The API now reads its configuration from a `Config/userconfig.json` file located in the installation directory. You can modify this file to customize paths and ports used by the API.

### Example `Config/userconfig.json`:

```json
{
  "CodewalkerOutputDir": "C:\\GTA_FILES\\cw_out",
  "BlenderOutputDir": "C:\\GTA_FILES\\blender_out",
  "FivemOutputDir": ""C:\\GTA_FILES\\fivem_out",
  "RpfArchivePath": "C:\\Program Files\\Rockstar Games\\Grand Theft Auto V\\modstore\\new.rpf",
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
- Import XML or raw files back into RPF
- Replace existing files in RPFs

## 🌐 Swagger UI (Recommended for Testing)

Open the Swagger UI in your browser to test endpoints interactively:

```
http://localhost:5555
```

## 📝 Example Requests

(See detailed examples for downloading, importing, replacing, and searching files in the original request)

## 📜 License

This project is released under the MIT License.

## 🤝 Contributing

Pull requests are welcome! If you encounter issues, feel free to open an [issue](https://github.com/flobros/CodeWalker.API/issues).