using System.Text;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Field;
using Field.Entities;
using Field.General;
using Field.Models;
using Field.Statics;
using System.Linq;
using System.Threading.Tasks;


namespace Charm;

public partial class DevView : UserControl
{
    private static MainWindow _mainWindow = null;
    private FbxHandler _fbxHandler = null;
    static object lockObject = new object();
    private List<TagHash> _cachedTags = new List<TagHash>();

    public DevView()
    {
        InitializeComponent();
    }
    
    private void OnControlLoaded(object sender, RoutedEventArgs routedEventArgs)
    {
        _mainWindow = Window.GetWindow(this) as MainWindow;
        _fbxHandler = new FbxHandler(false);
        HashLocation.Text = $"PKG:\nPKG ID:\nEntry Index:";
    }
    
    private void TagHashBoxKeydown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Return && e.Key != Key.H && e.Key != Key.R && e.Key != Key.E && e.Key != Key.L && e.Key != Key.S)
        {
            return;
        }
        
        string strHash = TagHashBox.Text.Replace(" ", "");
        strHash = Regex.Replace(strHash, @"(\s+|r|h)", "");

        if (strHash.Length >= 16 && e.Key == Key.S)
        {
            //985934379080548352
            SearchBins64(UInt64.Parse(strHash, NumberStyles.Number));
            return;
        }

        if (strHash.Length == 16)
        {
            strHash = TagHash64Handler.GetTagHash64String(strHash);
        }
        if (strHash == "")
        {
            TagHashBox.Text = "INVALID HASH";
            return;
        }
        
        TagHash hash;
        if (strHash.Contains("-"))
        {
            var s = strHash.Split("-");
            var pkgid = Int32.Parse(s[0], NumberStyles.HexNumber);
            var entryindex = Int32.Parse(s[1], NumberStyles.HexNumber);
            hash = new TagHash(PackageHandler.MakeHash(pkgid, entryindex));
        }
        else
        {
            hash = new TagHash(strHash);
        }
        
        if (!hash.IsValid() && strHash.Length != 4)
        {
            TagHashBox.Text = "INVALID HASH";
            return;
        }
        //uint to int
        switch (e.Key)
        {
            case Key.L:
                int type;
                int subtype;
                PackageHandler.GetEntryTypes(hash.Hash, out type, out subtype);

                StringBuilder data = new StringBuilder();
                data.AppendLine($"PKG: {PackageHandler.GetPackageName(hash.GetPkgId())}");
                data.AppendLine($"PKG ID: {hash.GetPkgId()}");
                data.AppendLine($"Type/Subtype: {type}, {subtype}");
                data.AppendLine($"Reference Hash: {hash.Hash}");
                data.AppendLine($"Entry Reference: {PackageHandler.GetEntryReference(hash)}");

                HashLocation.Text = data.ToString();
                break;
            case Key.Return:
                AddWindow(hash);
                break;
            case Key.H:
                OpenHxD(hash);
                break;
            case Key.R:
                TagHash refHash = PackageHandler.GetEntryReference(hash);
                if (!refHash.GetHashString().EndsWith("8080"))
                {
                    OpenHxD(refHash);
                }
                else
                {
                    TagHashBox.Text = $"REF {refHash}";
                }
                break;
            case Key.E:
                Entity entity = PackageHandler.GetTag(typeof(Entity), hash);
                if (entity.Model != null)
                {
                    OpenHxD(entity.Model.Hash);
                }
                else
                {
                    TagHashBox.Text = $"NO MODEL";
                }
                break;
            case Key.S:
                if (strHash.Length == 4)
                {
                    var pkgid = Int32.Parse(strHash, NumberStyles.HexNumber);
                    //PackageHandler.GetAllTagsWithTypes
                    var tags = PackageHandler.GetTagsWithTypes(pkgid, 8, 0);
                    var tags2 = PackageHandler.GetTagsWithTypes(pkgid, 16, 0);
                    tags.AddRange(tags2);

                    Console.WriteLine($"Tags from {strHash} - {tags.Count + tags2.Count}");
                    Parallel.ForEach(tags, tag =>
                    {
                        SaveBin(tag);
                    });
                    TagHashBox.Text = $"Extracted .bins";
                }
                else
                {
                    //SearchFloatRangeInBins(7.32417f, 7.326f);
                    SearchStringInFiles("map");
                    
                    //if (_cachedTags == null || _cachedTags.Count == 0)
                    //{
                    //    _cachedTags = PackageHandler.GetAllTagsWithTypes(8, 0);
                    //    Console.WriteLine("Caching Tags");
                    //    PackageHandler.CacheHashDataList(_cachedTags.Select(x => x.Hash).ToArray());
                    //}
                    //Parallel.ForEach(_cachedTags, tag =>
                    //{
                    //    SearchRawData(tag, hash.Hash); 
                    //});
                    //Console.WriteLine("Search Complete");
                }
                break;
        }
    }
    
    private void ExportWem(ExportInfo info)
    {
        Wem wem = PackageHandler.GetTag(typeof(Wem), new TagHash(info.Hash));
        string saveDirectory = ConfigHandler.GetExportSavePath() + $"/Sound/{info.Hash}_{info.Name}/";
        Directory.CreateDirectory(saveDirectory);
        wem.SaveToFile($"{saveDirectory}/{info.Name}.wav");
    }

    private void AddWindow(TagHash hash)
    {
        _fbxHandler.Clear();
        // Adds a new tab to the tab control
        DestinyHash reference = PackageHandler.GetEntryReference(hash);
        int hType, hSubtype;
        PackageHandler.GetEntryTypes(hash, out hType, out hSubtype);
        if (hType == 26 && hSubtype == 7)
        {
            var audioView = new TagView();
            audioView.SetViewer(TagView.EViewerType.TagList);
            audioView.MusicPlayer.SetWem(PackageHandler.GetTag(typeof(Wem), hash));
            audioView.MusicPlayer.Play();
            audioView.ExportControl.SetExportFunction(ExportWem, (int)EExportTypeFlag.Full);
            audioView.ExportControl.SetExportInfo(hash);
            _mainWindow.MakeNewTab(hash, audioView);
            _mainWindow.SetNewestTabSelected();
        }
        else if (hType == 32)
        {
            TextureHeader textureHeader = PackageHandler.GetTag(typeof(TextureHeader), new TagHash(hash));
            if (textureHeader.IsCubemap())
            {
                var cubemapView = new CubemapView();
                cubemapView.LoadCubemap(textureHeader);
                _mainWindow.MakeNewTab(hash, cubemapView);
            }
            else
            {
                var textureView = new TextureView();
                textureView.LoadTexture(textureHeader);
                _mainWindow.MakeNewTab(hash, textureView);
            }
            _mainWindow.SetNewestTabSelected();
        }
        else if ((hType == 8 || hType == 16) && hSubtype == 0)
        {
            switch (reference.Hash)
            {
                case 0x80809AD8:
                    FullEntityView entityView = new FullEntityView();
                    _mainWindow.MakeNewTab(hash, entityView);
                    _mainWindow.SetNewestTabSelected();
                    entityView.LoadEntity(hash, _fbxHandler);
                    break;
                case 0x80806D44:
                    StaticView staticView = new StaticView();
                    staticView.LoadStatic(hash, ELOD.MostDetail);
                    _mainWindow.MakeNewTab(hash, staticView);
                    _mainWindow.SetNewestTabSelected();
                    break;
                case 0x808093AD:
                    MapView mapView = new MapView();
                    mapView.LoadMap(hash, ELOD.LeastDetail);
                    _mainWindow.MakeNewTab(hash, mapView);
                    _mainWindow.SetNewestTabSelected();
                    break;
                case 0x80808E8E:
                    ActivityView activityView = new ActivityView();
                    activityView.LoadActivity(hash);
                    _mainWindow.MakeNewTab(hash, activityView);
                    _mainWindow.SetNewestTabSelected();
                    break;
                case 0x808099EF:
                    var stringView = new TagView();
                    stringView.SetViewer(TagView.EViewerType.TagList);
                    stringView.TagListControl.LoadContent(ETagListType.Strings, hash, true);
                    _mainWindow.MakeNewTab(hash, stringView);
                    _mainWindow.SetNewestTabSelected();
                    break;
                case 0x808097B8:
                    var dialogueView = new DialogueView();
                    dialogueView.Load(hash);
                    _mainWindow.MakeNewTab(hash, dialogueView);
                    _mainWindow.SetNewestTabSelected();
                    break;
                case 0x80808be0:
                    Animation animation = PackageHandler.GetTag(typeof(Animation), hash);
                    animation.Load();
                    break;
                default:
                    MessageBox.Show("Unknown reference: " + Endian.U32ToString(reference));
                    break;
            }
        }
        else
        {
            throw new NotImplementedException();
        }
    }

    private void OpenHxD(TagHash hash)
    {
        string savePath = ConfigHandler.GetExportSavePath() + "/temp";
        if (!Directory.Exists(savePath))
        {
            Directory.CreateDirectory(savePath);
        }
        string path = $"{savePath}/{hash.GetPkgId().ToString("x4")}_{PackageHandler.GetEntryReference(hash)}_{hash}.bin";
        using (var fileStream = new FileStream(path, FileMode.Create))
        {
            using (var writer = new BinaryWriter(fileStream))
            {
                byte[] data = new DestinyFile(hash).GetData();
                writer.Write(data);
            }
        }
        new Process
        {
            StartInfo = new ProcessStartInfo($@"{path}")
            {
                UseShellExecute = true
            }
        }.Start();
    }

    private void SaveBin(TagHash hash)
    {
        lock (lockObject)
        {
            string savePath = ConfigHandler.GetExportSavePath() + "/temp";
            if (!Directory.Exists(savePath))
            {
                Directory.CreateDirectory(savePath);
            }
            string path = $"{savePath}/{hash.GetPkgId().ToString("x4")}_{PackageHandler.GetEntryReference(hash)}_{hash.GetHashString()}.bin";
            using (var fileStream = new FileStream(path, FileMode.Create))
            {
                using (var writer = new BinaryWriter(fileStream))
                {
                    byte[] data = new DestinyFile(hash).GetData();
                    writer.Write(data);
                }
            }
        }
    }

    public static void SearchStringInFiles(string searchString)
    {
        string savePath = ConfigHandler.GetExportSavePath() + "/temp";
        string[] binFiles = Directory.GetFiles(savePath, "*.bin");

        StringComparison comparisonType = StringComparison.OrdinalIgnoreCase; // Case-insensitive comparison

        foreach (string filePath in binFiles)
        {
            using (FileStream fs = File.OpenRead(filePath))
            {
                byte[] buffer = new byte[searchString.Length];
                int bytesRead;
                long positionOffset = 0;

                while ((bytesRead = fs.Read(buffer, 0, buffer.Length)) > 0)
                {
                    string fileContent = Encoding.ASCII.GetString(buffer, 0, bytesRead);
                    if (fileContent.Equals(searchString, comparisonType))
                    {
                        Console.WriteLine($"{filePath}: Match found at offset: 0x{positionOffset:X}");
                    }

                    positionOffset += bytesRead;
                }
            }
        }

        Console.WriteLine("Search complete.");
    }

    public static void SearchFloatRangeInBins(float minValue, float maxValue)
    {
        string savePath = ConfigHandler.GetExportSavePath() + "/temp";
        string[] binFiles = Directory.GetFiles(savePath, "*.bin");

        Console.WriteLine($"Searching for float values in the range [{minValue}, {maxValue}]");

        foreach (string filePath in binFiles)
        {
            using (FileStream fs = File.OpenRead(filePath))
            {
                byte[] buffer = new byte[sizeof(float)];
                int bytesRead;
                long positionOffset = 0;

                while ((bytesRead = fs.Read(buffer, 0, buffer.Length)) > 0)
                {
                    for (int i = 0; i < bytesRead - sizeof(float) + 1; i++)
                    {
                        float value = BitConverter.ToSingle(buffer, i);
                        if (value >= minValue && value <= maxValue)
                        {
                            long position = positionOffset + fs.Position - buffer.Length + i;
                            Console.WriteLine($"{filePath}: Match found: {value} at offset: 0x{position:X}");
                        }
                    }

                    positionOffset += bytesRead;
                }
            }
        }

        Console.WriteLine("Search complete.");
    }

    public static void SearchRawData(TagHash tag, UInt32 searchValue) //Search for uint32 value from a provided taghash
    {
        byte[] byteArray = new DestinyFile(tag).GetData();
        int bufferSize = sizeof(UInt32);
        int bytesToProcess = byteArray.Length - bufferSize + 1;

        for (int i = 0; i < bytesToProcess; i++)
        {
            UInt32 value = BitConverter.ToUInt32(byteArray, i);
            if (value == searchValue)
            {
                int type;
                int subtype;
                PackageHandler.GetEntryTypes(tag.Hash, out type, out subtype);
                Console.WriteLine($"{PackageHandler.GetEntryReference(tag)}_{tag.GetHashString()}: Match found at offset: 0x{i.ToString("X")} | Type {type}, Subtype {subtype}");
            }
        }
        // Clear the byte array to free up memory
        byteArray = null;
    }

    public static void SearchRawData64(TagHash tag, UInt64 searchValue)
    {
        byte[] byteArray = new DestinyFile(tag).GetData();
        int bufferSize = sizeof(UInt64);
        int bytesToProcess = byteArray.Length - bufferSize + 1;

        for (int i = 0; i < bytesToProcess; i++)
        {
            UInt64 value = BitConverter.ToUInt64(byteArray, i);
            if (value == searchValue)
            {
                int type;
                int subtype;
                PackageHandler.GetEntryTypes(tag.Hash, out type, out subtype);
                Console.WriteLine($"{PackageHandler.GetEntryReference(tag)}_{tag.GetHashString()}: Match found at offset: 0x{i.ToString("X")} | Type {type}, Subtype {subtype}");
            }
        }
        // Clear the byte array to free up memory
        byteArray = null;
    }

    public static void SearchBins64(UInt64 searchValue) //totally not from chatgpt
    {
        if(searchValue == 0)
        {
            Console.WriteLine("Search Value is 0");
            return;
        }
        string savePath = ConfigHandler.GetExportSavePath() + "/temp";

        string[] binFiles = Directory.GetFiles(savePath, "*.bin");
        Console.WriteLine($"Searching for h64 in {savePath}");

        foreach (string filePath in binFiles)
        {
            // Convert the search value to a byte array
            byte[] searchBytes = BitConverter.GetBytes(searchValue);

            // Read the binary file
            using (FileStream fs = File.OpenRead(filePath))
            {
                byte[] buffer = new byte[searchBytes.Length];
                int bytesRead;

                while ((bytesRead = fs.Read(buffer, 0, buffer.Length)) > 0)
                {
                    // Compare the read bytes with the search bytes
                    for (int i = 0; i < bytesRead - searchBytes.Length + 1; i++)
                    {
                        if (ByteArrayEquals(buffer, i, searchBytes))
                        {
                            // Match found, do something with the position or handle the match
                            long position = fs.Position - buffer.Length + i;
                            Console.WriteLine($"{filePath}");
                            Console.WriteLine("Match found at position: 0x" + position.ToString("X"));
                        }
                    }
                }
            }
        }
        Console.WriteLine("Search complete.");
    }

    public static void SearchBins32(UInt32 searchValue) //Search for uint32 values in .bins in the /temp/ folder
    {
        string savePath = ConfigHandler.GetExportSavePath() + "/temp";
        int found = 0;
        string[] binFiles = Directory.GetFiles(savePath, "*.bin");
        Console.WriteLine($"Searching for {searchValue} in {savePath}");

        foreach (string filePath in binFiles)
        {
            //Console.WriteLine("Searching in: " + filePath);
            // Read the binary file
            using (FileStream fs = File.OpenRead(filePath))
            {
                byte[] buffer = new byte[sizeof(Int32)];
                int bytesRead;

                long positionOffset = 0;

                while ((bytesRead = fs.Read(buffer, 0, buffer.Length)) > 0)
                {
                    for (int i = 0; i < bytesRead - sizeof(UInt32) + 1; i++)
                    {
                        Int32 value = BitConverter.ToInt32(buffer, i);
                        if (value == searchValue)
                        {
                            long position = positionOffset + fs.Position - buffer.Length + i;
                            Console.WriteLine($"{filePath}");
                            Console.WriteLine("Match found at offset: 0x" + position.ToString("X"));
                            found++;
                        }
                    }
                    positionOffset += bytesRead;
                }
            }
        }
        if(found == 0)
        {
            Console.WriteLine("Found 0 matches, trying h64...");
            SearchBins64(TagHash64Handler.Get64From32(searchValue));
        }    

        Console.WriteLine("Search complete.");
    }

    public static bool ByteArrayEquals(byte[] array1, int startIndex, byte[] array2)
    {
        for (int i = 0; i < array2.Length; i++)
        {
            if (array1[startIndex + i] != array2[i])
                return false;
        }
        return true;
    }

    private void ExportDevMapButton_OnClick(object sender, RoutedEventArgs e)
    {
        // Not actually a map, but a list of assets that are good for testing
        // The assets are assembled in UE5 so just have to rip the list
        var assets = new List<string>()
        {
            "6C24BB80",
            "a237be80",
            "b540be80",
            "68a8b480",
            "fba4b480",
            "e1c5b280",
            "0F3CBE80",
            "A229BE80",
            "B63BBE80",
            "CB32BE80",
        };

        foreach (var asset in assets)
        {
            StaticView.ExportStatic(new TagHash(asset), asset, EExportTypeFlag.Full, "devmap");
        }
    }

    private void SearchReferencesBins_OnClick(object sender, RoutedEventArgs e)
    {
        string strHash = TagHashBox.Text.Replace(" ", "");
        var hash = GetTagHash(strHash);

        if (!hash.IsValid() && strHash.Length != 4)
        {
            TagHashBox.Text = "INVALID HASH";
            return;
        }
        SearchBins32(hash.Hash);
    }

    private void SearchReferences64Bins_OnClick(object sender, RoutedEventArgs e)
    {
        string strHash = TagHashBox.Text.Replace(" ", "");
        var hash = GetTagHash(strHash);

        if (!hash.IsValid() && strHash.Length != 4)
        {
            TagHashBox.Text = "INVALID HASH";
            return;
        }
        SearchBins64(TagHash64Handler.Get64From32(hash.Hash));
    }

    private void SearchReferences_OnClick(object sender, RoutedEventArgs e)
    {
        string strHash = TagHashBox.Text.Replace(" ", "");
        var hash = GetTagHash(strHash);

        if (!hash.IsValid() && strHash.Length != 4)
        {
            TagHashBox.Text = "INVALID HASH";
            return;
        }

        Console.WriteLine($"Searching for {strHash}. This may take AWHILE...");

        if (_cachedTags == null || _cachedTags.Count == 0)
        {
            _cachedTags = PackageHandler.GetAllTagsWithTypes(Int32.Parse(Type.Text), Int32.Parse(SubType.Text));
            Console.WriteLine("Caching Tags");
            PackageHandler.CacheHashDataList(_cachedTags.Select(x => x.Hash).ToArray());
        }
        Parallel.ForEach(_cachedTags, tag =>
        {
            SearchRawData(tag, hash.Hash);
        });
       
        Console.WriteLine("Search Complete");
    }

    private void SearchReferences64_OnClick(object sender, RoutedEventArgs e)
    {
        string strHash = TagHashBox.Text.Replace(" ", "");
        var hash = GetTagHash(strHash);

        if (!hash.IsValid() && strHash.Length != 4)
        {
            TagHashBox.Text = "INVALID HASH";
            return;
        }

        var hash64 = TagHash64Handler.Get64From32(hash.Hash);
        Console.WriteLine($"Searching for {strHash} ({hash64}). This may take AWHILE...");
        if (hash64 == 0)
        {
            Console.WriteLine($"h64 is 0, not searching");
            return;
        }
        if (_cachedTags == null || _cachedTags.Count == 0)
        {
            _cachedTags = PackageHandler.GetAllTagsWithTypes(Int32.Parse(Type.Text), Int32.Parse(SubType.Text));
            Console.WriteLine("Caching Tags");
            PackageHandler.CacheHashDataList(_cachedTags.Select(x => x.Hash).ToArray());
        }
        Parallel.ForEach(_cachedTags, tag =>
        {
            SearchRawData64(tag, hash64);
        });
       
        Console.WriteLine("Search Complete");
    }

    private TagHash GetTagHash(string strHash)
    {
        strHash = Regex.Replace(strHash, @"(\s+|r|h)", "");

        if (strHash.Length == 16)
        {
            strHash = TagHash64Handler.GetTagHash64String(strHash);
        }
        if (strHash == "")
        {
            TagHashBox.Text = "INVALID HASH";
            return null;
        }

        TagHash hash;
        if (strHash.Contains("-"))
        {
            var s = strHash.Split("-");
            var pkgid = Int32.Parse(s[0], NumberStyles.HexNumber);
            var entryindex = Int32.Parse(s[1], NumberStyles.HexNumber);
            hash = new TagHash(PackageHandler.MakeHash(pkgid, entryindex));
        }
        else
        {
            hash = new TagHash(strHash);
        }
        return hash;
    }
}