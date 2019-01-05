## System.Data.SQLite binary for Linux

### Version
Current version was built from: `sqlite-netFx-full-source-1.0.109.0`

### Compilation
Install build tools if necessary: `sudo apt-get install libc6-dev`

Download and unzip source code from [System.Data.SQLite website](https://system.data.sqlite.org/index.html/doc/trunk/www/downloads.wiki).
```
cd Setup
chmod +x compile-interop-assembly-release.sh
./compile-interop-assembly-release.sh
```
Copy compiled `libSQLite.Interop.so` file from `bin/2013/Release/bin`.
