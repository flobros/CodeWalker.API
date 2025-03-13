# CodeWalker API

This repository provides an API for interacting with CodeWalker, enabling file extraction, XML conversion, and texture handling for GTA V assets.

## Prerequisites

Before using this API, you must download and set up CodeWalker:

1. **Download CodeWalker**
   - Clone or download CodeWalker from its official repository:
     ```sh
     git clone https://github.com/dexyfex/CodeWalker.git
     ```
   - Build CodeWalker using Visual Studio.

2. **Download and Setup the CodeWalker API**
   - Clone this repository:
     ```sh
     git clone https://github.com/flobros/CodeWalker.API
     ```
   - Navigate to the folder:
     ```sh
     cd CodeWalker.API
     ```
   - Restore dependencies and build:
     ```sh
     dotnet restore
     dotnet build
     ```

## Running the API

Start the API server with:

```sh
 dotnet run
```

By default, the API runs on `http://localhost:5024/`.

## API Endpoints

### 1. Import XML Files into an RPF Archive

This endpoint allows importing multiple XML files into an RPF archive while ensuring textures are correctly placed.

#### **With `outputFolder` (Writes to Disk + Imports to RPF)**
```sh
curl -X POST "http://localhost:5024/api/import-xml" ^
    -H "Content-Type: application/x-www-form-urlencoded" ^
    -d "filePaths=C:\GTA_YDR_FILES\out\prop_alien_egg_01.ydr.xml" ^
    -d "filePaths=C:\GTA_YDR_FILES\out\apa_mp_apa_yacht.ydr.xml" ^
    -d "rpfArchivePath=C:\Program Files\Rockstar Games\Grand Theft Auto V\modstore\new.rpf" ^
    -d "outputFolder=C:\GTA_YDR_FILES\out"
```
**Response:**
```json
[
  {
    "filePath": "C:\\GTA_YDR_FILES\\out\\prop_alien_egg_01.ydr.xml",
    "message": "File imported successfully into RPF.",
    "filename": "prop_alien_egg_01.ydr",
    "rpfArchivePath": "C:\\Program Files\\Rockstar Games\\Grand Theft Auto V\\modstore\\new.rpf",
    "outputFilePath": "C:\\GTA_YDR_FILES\\out\\prop_alien_egg_01.ydr",
    "textureFolder": "C:\\GTA_YDR_FILES\\out\\prop_alien_egg_01"
  }
]
```

#### **Without `outputFolder` (Only Imports to RPF, No File Saving)**
```sh
curl -X POST "http://localhost:5024/api/import-xml" ^
    -H "Content-Type: application/x-www-form-urlencoded" ^
    -d "filePaths=C:\GTA_YDR_FILES\out\prop_alien_egg_01.ydr.xml" ^
    -d "filePaths=C:\GTA_YDR_FILES\out\apa_mp_apa_yacht.ydr.xml" ^
    -d "rpfArchivePath=C:\Program Files\Rockstar Games\Grand Theft Auto V\modstore\new.rpf"
```
**Response:**
```json
[
  {
    "filePath": "C:\\GTA_YDR_FILES\\out\\prop_alien_egg_01.ydr.xml",
    "message": "File imported successfully into RPF.",
    "filename": "prop_alien_egg_01.ydr",
    "rpfArchivePath": "C:\\Program Files\\Rockstar Games\\Grand Theft Auto V\\modstore\\new.rpf",
    "outputFilePath": null,
    "textureFolder": "C:\\GTA_YDR_FILES\\out\\prop_alien_egg_01"
  }
]
```

### 2. Download Files from an RPF Archive

#### **With XML Conversion (Downloads files and converts to XML)**
```sh
curl -X GET "http://localhost:5024/api/download-files?filenames=prop_alien_egg_01.ydr&filenames=apa_mp_apa_yacht.ydr&xml=true&outputFolderPath=C:\GTA_YDR_FILES\out"
```
**Response:**
```json
[
  {"filename":"prop_alien_egg_01.ydr","message":"XML and related files saved successfully.","xmlFilePath":"C:\\GTA_YDR_FILES\\out\\prop_alien_egg_01.ydr.xml"}
]
```

#### **Without XML Conversion (Downloads files as they are)**
```sh
curl -X GET "http://localhost:5024/api/download-files?filenames=prop_alien_egg_01.ytd&outputFolderPath=C:\GTA_YDR_FILES\out"
```
**Response:**
```json
[
  {"filename":"prop_alien_egg_01.ydr","message":"File saved successfully.","outputFilePath":"C:\\GTA_YDR_FILES\\out\\prop_alien_egg_01.ydr"}
]
```

### 3. Search for Files in RPF Archives
```sh
curl -X GET "http://localhost:5024/api/search-file?query=prop_alien_egg"
```
**Response:**
```json
[
  "x64c.rpf\\levels\\gta5\\props\\lev_des\\lev_des.rpf\\prop_alien_egg_01.ydr",
  "modstore\\new.rpf\\prop_alien_egg_01.ydr"
]
```

## License

This project is licensed under the MIT License. See `LICENSE` for more details.

