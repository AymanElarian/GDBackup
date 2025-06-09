// Program.cs – .NET 8 console app
// Build:   dotnet build -c Release
// Publish: dotnet publish -c Release -r win-x64 -p:PublishSingleFile=true
//
// Double-click  →  new snapshot + verification
// app.exe --verify <folder>  →  re-verify .dat files in an old snapshot

using System.Diagnostics;
using System.Security.Cryptography;
using System.Text.Json;

var cfg = Config.Load("appsettings.json");

// ─────────── verify-only mode
if (args.Length == 2 && args[0] == "--verify")
{
    VerifySnapshot(args[1]);
    return;
}

// ─────────── make sure Geometry Dash isn’t running
while (Process.GetProcessesByName("GeometryDash").Any())
{
    Console.WriteLine("Geometry Dash is running – please close it, then press Enter…");
    Console.ReadLine();
}

// ─────────── directory layout
string stamp        = DateTime.Now.ToString("yyyyMMdd_HHmmss");
string snapshotDir  = Path.Combine(cfg.BackupRoot, $"GD_{stamp}");
string steamSnapDir = Path.Combine(snapshotDir, "SteamFiles");   // FULL copy each run
string saveSnapDir  = Path.Combine(snapshotDir, "SaveFiles");    // .dat files each run
string assetPoolDir = Path.Combine(cfg.BackupRoot, "Assets", "SaveAssets"); // De-duped media

Directory.CreateDirectory(steamSnapDir);
Directory.CreateDirectory(saveSnapDir);
Directory.CreateDirectory(assetPoolDir);

// manifest for .dat checksums
var manifest = new Dictionary<string,string>();

// 1️⃣ STEAM PROGRAM FILES – always copy
FullCopy(cfg.AssetsDir, steamSnapDir);

// 2️⃣ SAVE DIR
foreach (string file in Directory.GetFiles(cfg.SaveDir, "*", SearchOption.AllDirectories))
{
    string rel  = Path.GetRelativePath(cfg.SaveDir, file);
    string ext  = Path.GetExtension(file);

    if (ext.Equals(".dat", StringComparison.OrdinalIgnoreCase))  // small progress files
    {
        string dest = Path.Combine(saveSnapDir, rel);
        Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
        File.Copy(file, dest, overwrite: true);

        manifest[Path.Combine("SaveFiles", rel)] = Sha256(dest);
        Console.WriteLine($"✓ {rel}");
    }
    else                                                        // big media – copy only if missing/changed
    {
        string dest = Path.Combine(assetPoolDir, rel);
        if (File.Exists(dest) &&
            new FileInfo(dest).Length == new FileInfo(file).Length)
            continue;                                           // identical – skip

        Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
        File.Copy(file, dest, overwrite: false);
        Console.WriteLine($"→ asset copied: {rel}");
    }
}

// 3️⃣ write checksum manifest
File.WriteAllText(Path.Combine(snapshotDir, "checksums.json"),
    JsonSerializer.Serialize(manifest, new JsonSerializerOptions { WriteIndented = true }));

Console.WriteLine($"""
    ✅ Snapshot finished
       Steam files   : copied to {steamSnapDir}
       .dat files    : {manifest.Count} verified
       Media assets  : de-duplicated under \Assets\SaveAssets
       Snapshot root : {snapshotDir}
    """);


// ─────────── helper functions
static void FullCopy(string src, string destRoot)
{
    foreach (string dir in Directory.GetDirectories(src, "*", SearchOption.AllDirectories))
        Directory.CreateDirectory(dir.Replace(src, destRoot));

    foreach (string file in Directory.GetFiles(src, "*", SearchOption.AllDirectories))
    {
        string dest = file.Replace(src, destRoot);
        Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
        File.Copy(file, dest, overwrite: true);
    }
}

static string Sha256(string path)
{
    using var s = File.OpenRead(path);
    return Convert.ToHexString(SHA256.HashData(s));
}

static void VerifySnapshot(string folder)
{
    string manifestFile = Path.Combine(folder, "checksums.json");
    if (!File.Exists(manifestFile))
    {
        Console.WriteLine("✖ No checksums.json found.");
        return;
    }

    var manifest = JsonSerializer.Deserialize<Dictionary<string,string>>(File.ReadAllText(manifestFile))!;
    var bad = manifest
        .Where(kvp => !File.Exists(Path.Combine(folder, kvp.Key)) ||
                      Sha256(Path.Combine(folder, kvp.Key)) != kvp.Value)
        .Select(kvp => kvp.Key)
        .ToList();

    Console.WriteLine(bad.Count == 0
        ? "🎉 All .dat files verified OK."
        : $"❌ {bad.Count} corrupted:\n  - " + string.Join("\n  - ", bad));
}

// ─────────── config loader
file record Config(string SaveDir, string AssetsDir, string BackupRoot)
{
    public static Config Load(string path)
    {
        if (!File.Exists(path))
        {
            var bootstrap = new Config(
                SaveDir   : Environment.ExpandEnvironmentVariables(@"%LOCALAPPDATA%\GeometryDash"),
                AssetsDir : @"C:\Program Files (x86)\Steam\steamapps\common\Geometry Dash",
                BackupRoot: Path.Combine(Environment.GetFolderPath(
                            Environment.SpecialFolder.MyDocuments), "GD_Backups"));

            File.WriteAllText(path,
                JsonSerializer.Serialize(bootstrap, new JsonSerializerOptions { WriteIndented = true }));
            Console.WriteLine($"Created {path} – please review paths, then run again.");
            Environment.Exit(0);
        }
        return JsonSerializer.Deserialize<Config>(File.ReadAllText(path))!;
    }
}
