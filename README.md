# Geometry Dash Backup & Verify (Console, .NET 8)

A one-click tool that lets your child back up **Geometry Dash** progress safely:

| Feature | How it behaves |
|---------|----------------|
| **Steam install snapshot** | Full copy into `GD_yyyyMMdd_HHmmss/SteamFiles` every run. |
| **Player progress** (`*.dat`) | Checksummed copy into `GD_…/SaveFiles` every run. |
| **Heavy media** (`Songs`, `Objects`, replays …) | Copied once into `BackupRoot/Assets/SaveAssets`, skipped when unchanged. |
| **Integrity check** | `--verify <snapshot>` re-hashes the saved `.dat` files. |

---

## 1 · Prerequisites  

* Windows 10/11  
* [.NET 8 SDK](https://dotnet.microsoft.com/download) (runtime only is **not** enough for building)  

---

## 2 · Quick start

```bash
git clone https://github.com/<your-account>/gd-backup.git
cd gd-backup

dotnet build -c Release          # one-time build
./bin/Release/net8.0/gd-backup.exe
```

* **First run** creates an `appsettings.json` template and exits.  
  Edit the three paths, save, run again.

### Typical `appsettings.json`

```jsonc
{
  "SaveDir":  "C:\\Users\\Ayman\\AppData\\Local\\GeometryDash",
  "AssetsDir": "C:\\Program Files (x86)\\Steam\\steamapps\\common\\Geometry Dash",
  "BackupRoot": "D:\\Backups\\GD_Backups"
}
```

---

## 3 · Publishing a single self-contained EXE (optional)

```bash
dotnet publish -c Release -r win-x64 -p:PublishSingleFile=true
# Output appears under bin\Release\net8.0\win-x64\publish\
```

Copy the resulting `.exe` anywhere; it carries all dependencies.

---

## 4 · Verifying an old snapshot

```bash
gd-backup.exe --verify D:\Backups\GD_Backups\GD_20250609_104500
```

Returns **OK** or lists mismatched `.dat` files.

---

## 5 · House-keeping tips

| Task | Snippet |
|------|---------|
| **Zip & prune old snapshots** | `for /d %F in (GD_*) do tar -a -cf %F.zip %F && rmdir /s /q %F` |
| **Move MP3 cache to another drive** | `move "%LOCALAPPDATA%\GeometryDash\Songs" E:\GD_Songs` then `mklink /J "%LOCALAPPDATA%\GeometryDash\Songs" E:\GD_Songs` |
| **Run on double-click** | Create a desktop shortcut to the published EXE. |

---

## 6 · License

MIT License – see [LICENSE](LICENSE) for details.

---

> **Enjoy quick, reliable Geometry Dash backups!** Pull requests are welcome for UI wrappers, compression options, or Linux/macOS support.