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
  "FivemOutputDir": "C:\\GTA_FILES\\fivem_out",
  "RpfArchivePath": "C:\\Program Files\\Rockstar Games\\Grand Theft Auto V\\modstore\\new.rpf",
  "GTAPath": "C:\\Program Files\\Rockstar Games\\Grand Theft Auto V",
  "Port": 5555
}
```

## 🔄 Service Reloading

The API now supports automatic and manual service reloading when the GTA path changes:

### Automatic Reload
When you update the `GTAPath` configuration via the API, the system automatically:
- Detects the GTA path change
- Reloads RPF decryption keys from the new path
- **Immediately recreates all services** (RpfService, GameFileCache)
- **Preheats the new services** (loads RPF entries and caches)
- Preloads known meta types for optimal performance

### Manual Reload
You can manually trigger a service reload using the `/api/reload-services` endpoint:

```bash
POST http://localhost:5555/api/reload-services
```

This is useful when you want to ensure all services are using the latest configuration immediately.

### Service Status
Check the current service status using the `/api/service-status` endpoint:

```bash
GET http://localhost:5555/api/service-status
```

This returns the current GTA path, reload version, and service readiness status.

### Graceful Startup with Invalid GTA Path
The API now starts gracefully even when the initial GTA path is invalid:

- **No Hard Exit**: The API will start and run even with an invalid GTA path
- **Helpful Messages**: Clear error messages guide users to fix the configuration
- **Service Status**: Use `/api/service-status` to check if services are ready
- **Easy Fix**: Use `/api/set-config` to set a valid GTA path and automatically reload services

When the GTA path is invalid, API endpoints will return a `503 Service Unavailable` status with helpful error messages and instructions on how to fix the issue.

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
- **Automatic service reloading when GTA path changes**
- **Manual service reloading via API endpoint**

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