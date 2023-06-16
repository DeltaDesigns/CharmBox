using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Field;
using Field.General;
using Field.Entities;
using Serilog;
using Field.Models;
using System.IO;
using Internal.Fbx;

namespace Charm;

public partial class ActivityMapView : UserControl
{
    private readonly ILogger _activityLog = Log.ForContext<ActivityMapView>();
    private string _activeBubble = "";
	private FbxHandler _globalFbxHandler = null;

	public ActivityMapView()
    {
        InitializeComponent();
		_globalFbxHandler = new FbxHandler(false);
	} 
    
    public void LoadUI(Activity activity)
    {
        MapList.ItemsSource = GetMapList(activity);
        ExportControl.SetExportFunction(ExportFull, (int)EExportTypeFlag.Full | (int)EExportTypeFlag.Minimal | (int)EExportTypeFlag.ArrangedMap | (int)EExportTypeFlag.TerrainOnly, true);
        ExportControl.SetExportInfo(activity.Hash);
    }

    private ObservableCollection<DisplayBubble> GetMapList(Activity activity)
    {
        //var vals = PackageHandler.GetAllTagsWithReference(0x8080916a); //6A918080
        //foreach (var val in vals)
        //    Console.WriteLine(val.ToString());

        var maps = new ObservableCollection<DisplayBubble>();

        //foreach (var a in activity.Header.Unk20.Header.StringContainer.Header.StringHashTable)
        //{
        //    Console.WriteLine(activity.Header.Unk20.Header.StringContainer.GetStringFromHash(ELanguage.English, a));
        //}

        //Console.WriteLine($"Events {tag2.Header.Events?.Hash}");
        //if (tag2.Header.Events?.Header.Unk08 is not null)
        //    foreach (var a in tag2.Header.Events?.Header.Unk08)
        //    {
        //        Console.WriteLine($"{a.Unk00}");
        //        foreach (var b in a.Unk20)
        //        {
        //            Console.WriteLine($"{b.Unk00}");
        //        }
        //    }

        foreach (var mapEntry in activity.Header.Unk50)
        {
            foreach (var mapReferences in mapEntry.MapReferences)
            {
                // idk why this can happen but it can, some weird stuff with h64
                // for the child map reference, ive only seen it once so far but the hash for it was just FFFFFFFF in the map reference file
                if (mapReferences.MapReference is null || mapReferences.MapReference.Header.ChildMapReference == null)
                    continue;
                DisplayBubble displayMap = new DisplayBubble();
                displayMap.Name = $"{mapEntry.BubbleName} ({mapEntry.LocationName})";  // assuming Unk10 is 0F978080 or 0B978080
                displayMap.Hash = $"{mapReferences.MapReference.Header.ChildMapReference.Hash}|{mapEntry.BubbleName}";
                maps.Add(displayMap);
            }
        }
        return maps;
    }

    private void GetBubbleContentsButton_OnClick(object sender, RoutedEventArgs e)
    {
		string tag = (sender as Button).Tag as string;
		string[] values = tag.Split('|'); //very hacky way of getting both the hash and bubble name from the button tag, tried doing some Tag<> stuff but couldnt figure it out
		string hash = values[0];
		string name = values[1];

        _activeBubble = name;
        ActiveBubble.Text = $"{_activeBubble}";

		Tag<D2Class_01878080> bubbleMaps = PackageHandler.GetTag<D2Class_01878080>(new TagHash(hash));
		PopulateStaticList(bubbleMaps);
	}

    private void StaticMapPart_OnCheck(object sender, RoutedEventArgs e)
    {
        if ((sender as CheckBox).Tag is null)
            return;

        TagHash hash = new TagHash((sender as CheckBox).Tag as string);
        Tag<D2Class_07878080> map = PackageHandler.GetTag<D2Class_07878080>(hash);
        
        foreach (DisplayStaticMap item in StaticList.Items)
        {
            if(item.Name == "Select all")
                continue;

            if (item.Selected)
            {
				if (map == null)
					continue;

				PopulateDynamicsList(map);
                ActiveEnts.Text = $"{map.Hash} Entities";
            }
        }
    }

    private void PopulateStaticList(Tag<D2Class_01878080> bubbleMaps)
    {  
        ConcurrentBag<DisplayStaticMap> items = new ConcurrentBag<DisplayStaticMap>();
        Parallel.ForEach(bubbleMaps.Header.MapResources, m =>
        { 
            if (m.MapResource.Header.DataTables.Count > 1)
            {
                if (m.MapResource.Header.DataTables[1].DataTable.Header.DataEntries.Count > 0)
                {
                    StaticMapData tag = m.MapResource.Header.DataTables[1].DataTable.Header.DataEntries[0].DataResource.StaticMapParent.Header.StaticMap; //dataresource is D2Class_C96C8080
                    items.Add(new DisplayStaticMap
                    {
                        Hash = m.MapResource.Hash,
                        Name = $"{m.MapResource.Hash}: {tag.Header.Instances.Count} instances, {tag.Header.Statics.Count} uniques",
                        Instances = tag.Header.Instances.Count
                    });
                }
            }
        });

        FbxHandler dynamicPoints = new FbxHandler(false);
        Parallel.ForEach(bubbleMaps.Header.MapResources, m => //Need to do this after the main static maps have been added so we can get the map ambient entities that arent in the static maps
        {
            if (m.MapResource.Header.DataTables.Count > 0)
            {
                foreach (var a in m.MapResource.Header.DataTables)
                {
                    //Console.WriteLine($"{a.DataTable.Hash}");
                    foreach (var b in a.DataTable.Header.DataEntries)
                    {
                        dynamicPoints.AddEmptyToScene($"{a.DataTable.Hash} {b.DataResource}", b.Translation, b.Rotation);
                        //if (b.DataResource is D2Class_D4688080 a1)
                        //{
                        //    EntityModel model = new(a1.Unk10.Hash);
                        //    var parts = model.Load(ELOD.MostDetail, b.Entity.ModelParentResource);
                            
                        //    foreach (var part in parts)
                        //    {
                        //        if (part.Material.Header.PSTextures.Count == 0)
                        //        {
                        //            continue;
                        //        }
                        //        Console.WriteLine($"{part.Material.Hash}");
                        //        dynamicPoints.AddMeshPartToScene(part, part.Index, $"{model.Hash}_{part.Index}_{part.GroupIndex}");
                        //        part.Material.SaveAllTextures($"{ConfigHandler.GetExportSavePath()}/test/");
                        //    }
                        //}

                        //if (b.DataResource is D2Class_C36C8080 a1)
                        //{
                        //    Console.WriteLine($"{a.DataTable.Hash}");
                        //    if (a1.Unk10 is not null)
                        //    {
                        //        foreach (var a3 in a1.Unk10.Header.Unk4C.Header.InstanceBounds)
                        //        {
                        //            dynamicPoints.AddEmptyToScene($"{a3.Unk24}", (a3.Corner1 + a3.Corner2) / 2, new Vector4(0, 0, 0, 1));
                        //        }

                        //        foreach (var a2 in a1.Unk10.Header.Unk08)
                        //        {

                        //            //foreach (var a3 in a2.Unk00.Header.)
                        //            //{
                        //            //    Console.WriteLine($"{a3.Vertices1.Hash} {a3.Vertices2?.Hash} {a3.Parts.Count}");
                        //            //}

                        //            EntityModel model = new(a2.Unk00.Header.Unk08.Hash);
                        //            var parts = model.Load(ELOD.MostDetail, b.Entity.ModelParentResource);
                        //            //Console.WriteLine($"{parts.Count}");
                        //            foreach (var part in parts)
                        //            {
                        //                if (part.Material.Header.PSTextures.Count == 0)
                        //                {
                        //                    continue;
                        //                }

                        //                dynamicPoints.AddMeshPartToScene(part, part.Index, $"{model.Hash}_{part.Index}_{part.GroupIndex}");
                        //                part.Material.SaveAllTextures($"{ConfigHandler.GetExportSavePath()}/test/");
                        //            }
                        //        }
                        //    }      
                        //}
                        //if (b.DataResource is D2Class_406A8080 a1)
                        //{
                        //    if(a1.Unk10 is not null)
                        //        Console.WriteLine($"{a1.Unk10.Header.Unk08.Buffer.Hash}");      
                        //}
                        //if (b.DataResource is D2Class_636A8080 a2)
                        //{
                        //    //Console.WriteLine($"{a.DataTable.Hash} {a2.Unk10.Header.Unk30.Count}");
                        //    for (int i = 0; i < a2.Unk10.Header.Unk30.Count; i++)
                        //    {
                        //        //var vec = a2.Unk10.Header.Unk30[i].UnkD0.Header.Unk40[a2.Unk10.Header.Unk30[i].UnkDF].Unk00;
                        //        //Console.WriteLine($"{a2.Unk10.Header.Unk30[i].UnkD0.Hash} {a2.Unk10.Header.Unk58.Header.InstanceBounds[i].Unk24}");
                        //        //dynamicPoints.AddEmptyToScene($"{a2.Unk10.Header.Unk30[i].UnkCC.Hash} {a2.Unk10.Header.Unk58.Header.InstanceBounds[i].Unk24} {i}", a2.Unk10.Header.Unk40[i].Translation, a2.Unk10.Header.Unk40[i].Rotation);
                        //    }
                        //}

                        if (b.DataResource is not null && b.DataResource is not D2Class_C96C8080)
                        {
                            if (!items.Contains(new DisplayStaticMap { Hash = m.MapResource.Hash }))
                            {
                                m.MapResource.Header.DataTables.ForEach(data => //Why do I gotta loop through again for it to work?
                                {
                                    data.DataTable.Header.DataEntries.ForEach(entry =>
                                    {
                                        if(entry.Entity.HasGeometry())
                                        {
                                            items.Add(new DisplayStaticMap
                                            {
                                                Hash = m.MapResource.Hash,
                                                Name = $"{m.MapResource.Hash}: Misc Entities",
                                                Instances = 0
                                            });
                                        }
                                    });
                                });
                            }
                        }
                    }
                }
            }
        });
        var sortedItems = new List<DisplayStaticMap>(items);
        sortedItems.Sort((a, b) => b.Instances.CompareTo(a.Instances));
        sortedItems.Insert(0, new DisplayStaticMap
        {
            Name = "Select all"
        });
        StaticList.ItemsSource = sortedItems;
        dynamicPoints.ExportScene($"{ConfigHandler.GetExportSavePath()}/{bubbleMaps.Hash.GetHashString()}_Empties.fbx");
        dynamicPoints.Dispose();
    }

    private void PopulateDynamicsList(Tag<D2Class_07878080> map)//(Tag<D2Class_01878080> bubbleMaps)
    {
        //FbxHandler dynamicPoints = new FbxHandler(false);

        ConcurrentBag<DisplayDynamicMap> items = new ConcurrentBag<DisplayDynamicMap>();
        MainWindow.Progress.SetProgressStages(new List<string> { "Loading Entities" });
        Parallel.ForEach(map.Header.DataTables, data =>
        {
            data.DataTable.Header.DataEntries.ForEach(entry =>
            {
                //if (entry.DataResource is D2Class_6D668080 a) //spatial audio
                //{ 
                //    if (a.AudioContainer is not null)
                //    {
                //        var b = PackageHandler.GetTag<D2Class_38978080>(a.AudioContainer.Hash);
                //        Console.WriteLine($"D2Class_38978080 {b.Hash} : Sounds {b.Header.Unk20.Count} AudioPositions {a.AudioPositions.Count}");
                //        foreach (var loc in a.AudioPositions)
                //        {
                //            Console.WriteLine($"X: {loc.Translation.X} Y: {loc.Translation.Y} Z: {loc.Translation.Z} W: {loc.Translation.W}");
                //        }
                //    }
                //}
                //if (!entry.Entity.HasGeometry() && entry.DataResource is not D2Class_C96C8080)
                //{
                //    Console.WriteLine($"{data.DataTable.Hash} - {entry.Entity.Hash}");
                //}
                if (!items.Contains(new DisplayDynamicMap { Hash = entry.Entity.Hash }))
                {
                    if (entry.Entity.HasGeometry())
                    {
                        items.Add(new DisplayDynamicMap
                        {
                            Name = $"Entity {entry.Entity.Hash}",
                            Hash = entry.Entity.Hash
                        });
                    }
                }
            });
        });
        MainWindow.Progress.CompleteStage();
        var sortedItems = new List<DisplayDynamicMap>(items);
        sortedItems.Sort((a, b) => b.Hash.CompareTo(a.Hash));
        sortedItems.Insert(0, new DisplayDynamicMap
        {
            Name = "Select all",
            Parent = map
        });
        DynamicsList.ItemsSource = sortedItems;
        //dynamicPoints.ExportScene($"{ConfigHandler.GetExportSavePath()}/{map.Hash.GetHashString()}_Empties.fbx");
        //dynamicPoints.Dispose();
    }

    public async void ExportFull(ExportInfo info)
    {
        Activity activity = PackageHandler.GetTag(typeof(Activity), new TagHash(info.Hash));
        _activityLog.Debug($"Exporting activity data name: {PackageHandler.GetActivityName(activity.Hash)}, hash: {activity.Hash}");
        Dispatcher.Invoke(() =>
        {
            MapControl.Visibility = Visibility.Hidden;
        });
        var maps = new List<Tag<D2Class_07878080>>();
        bool bSelectAll = false;
        foreach (DisplayStaticMap item in StaticList.Items)
        {
            if (item.Selected && item.Name == "Select all")
            {
                bSelectAll = true;
            }
            else
            {
                if (item.Selected || bSelectAll)
                {
                    maps.Add(PackageHandler.GetTag<D2Class_07878080>(new TagHash(item.Hash)));
                }
            }
        }

        if (maps.Count == 0)
        {
            _activityLog.Error("No maps selected for export.");
            MessageBox.Show("No maps selected for export.");
            return;
        }

        List<string> mapStages = maps.Select((x, i) => $"exporting {i+1}/{maps.Count}").ToList();
        MainWindow.Progress.SetProgressStages(mapStages);
        // MainWindow.Progress.SetProgressStages(new List<string> { "exporting activity map data parallel" });
        Parallel.ForEach(maps, map =>
        {
            if (info.ExportType == EExportTypeFlag.Full)
            {
                MapView.ExportFullMap(map, activity, _activeBubble);
                MapView.ExportTerrainMap(map, activity, _activeBubble);
            }
            else if (info.ExportType == EExportTypeFlag.TerrainOnly)
            {
                MapView.ExportTerrainMap(map, activity, _activeBubble);
            }
            else if (info.ExportType == EExportTypeFlag.Minimal)
            {
                MapView.ExportMinimalMap(map, info.ExportType, activity, _activeBubble);
            }
            else
            {
                MapView.ExportMinimalMap(map, info.ExportType, activity, _activeBubble);
            }
            
            MainWindow.Progress.CompleteStage();
        });
        // MapView.ExportFullMap(staticMapData);
            // MainWindow.Progress.CompleteStage();

        Dispatcher.Invoke(() =>
        {
            MapControl.Visibility = Visibility.Visible;
        });
        _activityLog.Information($"Exported activity data name: {PackageHandler.GetActivityName(activity.Hash)}, hash: {activity.Hash}");
        MessageBox.Show("Activity map data exported completed.");
    }

    private async void StaticMap_OnClick(object sender, RoutedEventArgs e)
    {
        var s = sender as Button;
        var dc = s.DataContext as DisplayStaticMap;
        MapControl.Clear();
        _activityLog.Debug($"Loading UI for static map hash: {dc.Name}");
        MapControl.Visibility = Visibility.Hidden;
        var lod = MapControl.ModelView.GetSelectedLod();
        if (dc.Name == "Select all")
        {
            var items = StaticList.Items.Cast<DisplayStaticMap>().Where(x => x.Name != "Select all").Where(x => !x.Name.Contains("Misc Entities"));
            List<string> mapStages = items.Select(x => $"loading to ui: {x.Hash}").ToList();
            if (mapStages.Count == 0)
            {
                _activityLog.Error("No maps selected for export.");
                MessageBox.Show("No maps selected for export.");
                return;
            }
            MainWindow.Progress.SetProgressStages(mapStages);
            await Task.Run(() =>
            {
                foreach (DisplayStaticMap item in items)
                {
                    MapControl.LoadMap(new TagHash(item.Hash), lod);
                    MainWindow.Progress.CompleteStage();
                }
            });
        }
        else
        {
            if (dc.Name.Contains("Misc Entities"))
                return;

            var tagHash = new TagHash(dc.Hash);
            MainWindow.Progress.SetProgressStages(new List<string> {tagHash});
            // cant do this rn bc of lod problems with dupes
            // MapControl.ModelView.SetModelFunction(() => MapControl.LoadMap(tagHash, MapControl.ModelView.GetSelectedLod()));
            await Task.Run(() =>
            {
                MapControl.LoadMap(tagHash, lod); 
                MainWindow.Progress.CompleteStage();
            });
        }
        MapControl.Visibility = Visibility.Visible;
    }

    private async void Entity_OnClick(object sender, RoutedEventArgs e)
    {
        var s = sender as Button;
        var dc = s.DataContext as DisplayDynamicMap;
        Dispatcher.Invoke(() =>
        {
            MapControl.Visibility = Visibility.Hidden;
        });

        if (dc.Name == "Select all")
        {
            var items = DynamicsList.Items.Cast<DisplayDynamicMap>().Where(x => x.Name != "Select all");
            List<string> entStages = items.Select(x => $"exporting entity: {x.Hash}").ToList();
            if (entStages.Count == 0)
            {
                _activityLog.Error("No entities selected for export.");
                MessageBox.Show("No entities selected for export.");
                return;
            }
            MainWindow.Progress.SetProgressStages(entStages);
            await Task.Run(() =>
            {
                foreach (DisplayDynamicMap item in items)
                {
                    Entity ent = new Entity(new TagHash(item.Hash));
                    FbxHandler entHandler = new FbxHandler();
                    entHandler.InfoHandler.SetMeshName(ent.Hash.GetHashString());

                    List<FbxNode> skeletonNodes = new List<FbxNode>();
                    if (ent.Skeleton != null)
                    {
                        skeletonNodes = entHandler.AddSkeleton(ent.Skeleton.GetBoneNodes());
                    }

                    Log.Debug($"Exporting entity model name: {ent.Hash}");
                    string savePath = ConfigHandler.GetExportSavePath();
                    string meshName = string.Join("_", ent.Hash.ToString().Split(Path.GetInvalidFileNameChars()));
                    
                    savePath += $"/{meshName}";
                    
                    Directory.CreateDirectory(savePath);

                   
                    var dynamicParts = ent.Load(ELOD.MostDetail, true);
                    entHandler.AddEntityToScene(ent, dynamicParts, ELOD.MostDetail, null, skeletonNodes);

                    ent.SaveMaterialsFromParts(savePath, dynamicParts, ConfigHandler.GetUnrealInteropEnabled() || ConfigHandler.GetS2ShaderExportEnabled(), ConfigHandler.GetSaveCBuffersEnabled());
                    ent.SaveTexturePlates(savePath);

                    entHandler.InfoHandler.AddType("Entity");
                    entHandler.ExportScene($"{savePath}/{meshName}.fbx");
                    entHandler.Dispose();
                    Log.Information($"Exported entity model {ent.Hash} to {savePath.Replace('\\', '/')}/");

                    MainWindow.Progress.CompleteStage();
                }
            });
        }
        else
        {
            var tagHash = new TagHash(dc.Hash);
            MainWindow.Progress.SetProgressStages(new List<string> { $"exporting entity: {tagHash}" });
            // cant do this rn bc of lod problems with dupes
            // MapControl.ModelView.SetModelFunction(() => MapControl.LoadMap(tagHash, MapControl.ModelView.GetSelectedLod()));
            await Task.Run(() =>
            {
                Entity ent = new Entity(new TagHash(dc.Hash));
                FbxHandler entHandler = new FbxHandler();
                entHandler.InfoHandler.SetMeshName(ent.Hash.GetHashString());

                List<FbxNode> skeletonNodes = new List<FbxNode>();
                if (ent.Skeleton != null)
                {
                    skeletonNodes = entHandler.AddSkeleton(ent.Skeleton.GetBoneNodes());
                }

                Log.Debug($"Exporting entity model name: {ent.Hash}");
                string savePath = ConfigHandler.GetExportSavePath();
                string meshName = string.Join("_", ent.Hash.ToString().Split(Path.GetInvalidFileNameChars()));

                savePath += $"/{meshName}";

                Directory.CreateDirectory(savePath);

                var dynamicParts = ent.Load(ELOD.MostDetail, true);
                entHandler.AddEntityToScene(ent, dynamicParts, ELOD.MostDetail, null, skeletonNodes);

                ent.SaveMaterialsFromParts(savePath, dynamicParts, ConfigHandler.GetUnrealInteropEnabled() || ConfigHandler.GetS2ShaderExportEnabled(), ConfigHandler.GetSaveCBuffersEnabled());
                ent.SaveTexturePlates(savePath);

                entHandler.InfoHandler.AddType("Entity");
                entHandler.ExportScene($"{savePath}/{meshName}.fbx");
                entHandler.Dispose();
                Log.Information($"Exported entity model {ent.Hash} to {savePath.Replace('\\', '/')}/");

                MainWindow.Progress.CompleteStage();
            });
        }
        Dispatcher.Invoke(() =>
        {
            MapControl.Visibility = Visibility.Visible;
        });
    }

	private async void EntityView_OnClick(object sender, RoutedEventArgs e)
	{
		var s = sender as Button;
		var dc = s.DataContext as DisplayDynamicMap;
		MapControl.Clear();
		_activityLog.Debug($"Loading UI for entity: {dc.Name}");
		MapControl.Visibility = Visibility.Hidden;
		var lod = MapControl.ModelView.GetSelectedLod();
		if (dc.Name == "Select all")
		{
            var items = dc.Parent.Header.DataTables;
            List<string> mapStages = items.Select(x => $"loading to ui {x.DataTable.Hash}").ToList();
            if (mapStages.Count == 0)
            {
                _activityLog.Error("No maps selected for export.");
                MessageBox.Show("No maps selected for export.");
                return;
            }
            MainWindow.Progress.SetProgressStages(mapStages);
            await Task.Run(() =>
            {
                foreach (var datatable in dc.Parent.Header.DataTables)
                {
                    MapControl.LoadMap(new TagHash(datatable.DataTable.Hash), lod, true);
                    MainWindow.Progress.CompleteStage();
                }
            });
        }
		else
		{
			var tagHash = new TagHash(dc.Hash);
			MainWindow.Progress.SetProgressStages(new List<string> { tagHash });
			// cant do this rn bc of lod problems with dupes
			// MapControl.ModelView.SetModelFunction(() => MapControl.LoadMap(tagHash, MapControl.ModelView.GetSelectedLod()));
			await Task.Run(() =>
			{
				Entity ent = new Entity(new TagHash(dc.Hash));
				MapControl.LoadEntity(ent, _globalFbxHandler);
				MainWindow.Progress.CompleteStage();
			});
		}
		MapControl.Visibility = Visibility.Visible;
	}

	public void Dispose()
    {
        MapControl.Dispose();
    }
}

public class DisplayBubble
{
    public string Name { get; set; }
    public string Hash { get; set; }
}

public class DisplayStaticMap
{
    public string Name { get; set; }
    public string Hash { get; set; }
    public int Instances { get; set; }
    
    public bool Selected { get; set; }

    public override bool Equals(object obj)
    {
        var other = obj as DisplayStaticMap;
        return other != null && Hash == other.Hash;
    }

    public override int GetHashCode()
    {
        return Hash?.GetHashCode() ?? 0;
    }
}

public class DisplayDynamicMap
{
    public string Name { get; set; }
    public string Hash { get; set; }
    public Tag<D2Class_07878080> Parent { get; set; }
    
    public bool Selected { get; set; }

    public override bool Equals(object obj)
    {
        var other = obj as DisplayDynamicMap;
        return other != null && Hash == other.Hash;
    }

    public override int GetHashCode()
    {
        return Hash?.GetHashCode() ?? 0;
    }
}