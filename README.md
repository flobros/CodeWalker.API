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

**Request:**

```sh
curl -X POST "http://localhost:5024/api/import-xml" ^
    -H "Content-Type: application/x-www-form-urlencoded" ^
    -d "filePaths=C:\GTA_YDR_FILES\out\prop_alien_egg_01.ydr.xml" ^
    -d "filePaths=C:\GTA_YDR_FILES\out\apa_mp_apa_yacht.ydr.xml" ^
    -d "rpfArchivePath=C:\Program Files\Rockstar Games\Grand Theft Auto V\modstore\new.rpf" ^
    -d "outputFolder=C:\your\fivem\folder"
```

### 2. Download Files from an RPF Archive

This endpoint extracts multiple files from the RPF archive, optionally converting them to XML.

#### **Download and Convert Multiple Files to XML:**
```sh
curl -X GET "http://localhost:5024/api/download-files?filenames=prop_alien_egg_01.ydr&filenames=apa_mp_apa_yacht.ydr&xml=true&outputFolderPath=C:\GTA_YDR_FILES\out"
```
**Response:**
```json
[
  {"filename":"prop_alien_egg_01.ydr","message":"XML and related files saved successfully.","xmlFilePath":"C:\\GTA_YDR_FILES\\out\\prop_alien_egg_01.ydr.xml"},
  {"filename":"apa_mp_apa_yacht.ydr","message":"XML and related files saved successfully.","xmlFilePath":"C:\\GTA_YDR_FILES\\out\\apa_mp_apa_yacht.ydr.xml"}
]
```

#### **Download Raw Files Without XML Conversion:**
```sh
curl -X GET "http://localhost:5024/api/download-files?filenames=prop_alien_egg_01.ytd&outputFolderPath=C:\GTA_YDR_FILES\out"
```

This request extracts the raw file from the RPF archive and saves it in the specified output folder.

### 3. Search for Files in RPF Archives

Find files matching a given name inside RPF archives.

**Request:**

```sh
curl -X GET "http://localhost:5024/api/search?query=prop_alien_egg"
```

## License

This project is licensed under the MIT License. See `LICENSE` for more details.

