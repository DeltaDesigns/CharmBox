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

public partial class ActivityEntityView : UserControl
{
    private readonly ILogger _activityLog = Log.ForContext<ActivityEntityView>();
    private Activity _activeActivity;
	private FbxHandler _globalFbxHandler = null;

	public ActivityEntityView()
    {
        InitializeComponent();
		_globalFbxHandler = new FbxHandler(false);
	} 
    
    public void LoadUI(Activity activity)
    {
        ActivityList.ItemsSource = GetActivityList(activity);
        ExportControl.SetExportFunction(ExportFull, (int)EExportTypeFlag.Full | (int)EExportTypeFlag.Minimal | (int)EExportTypeFlag.ArrangedMap | (int)EExportTypeFlag.TerrainOnly, true);
        ExportControl.SetExportInfo(activity.Hash);
    }

    private ObservableCollection<DisplayActivity> GetActivityList(Activity activity)
    {
        var activities = new ObservableCollection<DisplayActivity>();

        DisplayActivity displayActivity = new DisplayActivity();
        displayActivity.Name = $"{PackageHandler.GetActivityName(activity.Hash)}";
        displayActivity.Hash = $"{activity.Hash}";
        activities.Add(displayActivity);

        if (activity.Header.UnkActivity68 is not null)
        {
            DisplayActivity displayAmbientActivity = new DisplayActivity();
            displayAmbientActivity.Name = $"{PackageHandler.GetActivityName(activity.Header.UnkActivity68.Hash)}";
            displayAmbientActivity.Hash = $"{activity.Header.UnkActivity68.Hash}";
            activities.Add(displayAmbientActivity);
        }
        
        return activities;
    }

    private void GetEntityListButton_OnClick(object sender, RoutedEventArgs e)
    {
        string tag = (sender as Button).Tag as string;
        Activity activity = PackageHandler.GetTag(typeof(Activity), new TagHash(tag));
        _activeActivity = activity;
        ConcurrentBag<DisplayResource> items = new ConcurrentBag<DisplayResource>();

        Parallel.ForEach(activity.Header.Unk50, a =>
        {
            foreach (var b in a.Unk18)
            {
                var resourceParent = CollapseResourceParent(b.UnkEntityReference.Hash);
                if (resourceParent.Count > 0)
                {
                    items.Add(new DisplayResource
                    {
                        Name = $"{b.BubbleName} {b.ActivityPhaseName2}: {resourceParent.Count} Entries",
                        ActivityName = b.ActivityPhaseName2.ToString(),
                        Hash = b.UnkEntityReference.Hash,
                        Count = resourceParent.Count
                    });
                } 
            }
        });

        var sortedItems = new List<DisplayResource>(items);
        sortedItems.Sort((a, b) => a.Name.CompareTo(b.Name));
        sortedItems.Insert(0, new DisplayResource
        {
            Name = "Select all"
        });
        DataEntryList.ItemsSource = sortedItems;
    }

    private List<Tag<D2Class_83988080>> CollapseResourceParent(TagHash hash)
    {
        var entry = PackageHandler.GetTag<D2Class_898E8080>(hash);

        ConcurrentBag<Tag<D2Class_83988080>> items = new ConcurrentBag<Tag<D2Class_83988080>>();

        Parallel.ForEach(entry.Header.Unk18.Header.EntityResources, a =>
        {
            switch (a.EntityResourceParent.Header.EntityResource.Header.Unk18)
            {
                case D2Class_D8928080:
                    var tag = (D2Class_D8928080)a.EntityResourceParent.Header.EntityResource.Header.Unk18;
                    if (tag.Unk84 is not null)
                        if (tag.Unk84.Header.DataEntries.Count > 0)
                        {
                            //Console.WriteLine($"{b.BubbleName} {b.ActivityName} {b.ActivityPhaseName} {b.ActivityPhaseName2}");
                            if (tag.Unk84.Header.DataEntries.Count == 1 && tag.Unk84.Header.DataEntries[0].Entity.HasGeometry()) //Todo combine everything with 1 entry into one main entry
                            {
                                items.Add(tag.Unk84);
                            }
                            else if (tag.Unk84.Header.DataEntries.Count > 1)
                            {
                                items.Add(tag.Unk84);
                            }
                        }
                    break;
                case D2Class_EF8C8080:
                    var tag2 = (D2Class_EF8C8080)a.EntityResourceParent.Header.EntityResource.Header.Unk18;
                    if (tag2.Unk58 is not null)
                        if (tag2.Unk58.Header.DataEntries.Count > 0)
                        {
                            if (tag2.Unk58.Header.DataEntries.Count == 1 && tag2.Unk58.Header.DataEntries[0].Entity.HasGeometry())
                            {
                                items.Add(tag2.Unk58);
                            }
                            else if (tag2.Unk58.Header.DataEntries.Count > 1)
                            {
                                items.Add(tag2.Unk58);
                            }
                        }
                    break;
            }
        });

        return items.ToList();
    }

    private async void ActivityDataEntry_OnClick(object sender, RoutedEventArgs e)
    {
        var s = sender as Button;
        var dc = s.DataContext as DisplayResource;
        MapControl.Clear();
        _activityLog.Debug($"Loading UI for entity resource: {dc.Name}");
        MapControl.Visibility = Visibility.Hidden;
        var lod = MapControl.ModelView.GetSelectedLod();
        if (dc.Name == "Select all")
        {
            var items = DataEntryList.Items.Cast<DisplayResource>().Where(x => x.Name != "Select all");
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
                foreach (DisplayResource item in items)
                {
                    foreach(var entry in CollapseResourceParent(new TagHash(item.Hash)))
                    {
                        MapControl.LoadMap(entry.Hash, lod, true);
                    }
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
                foreach (var entry in CollapseResourceParent(tagHash))
                {
                    MapControl.LoadMap(entry.Hash, lod, true);
                }
                MainWindow.Progress.CompleteStage();
            });
        }
        MapControl.Visibility = Visibility.Visible;
    }

    private void ActivityDataEntry_OnCheck(object sender, RoutedEventArgs e)
    {
        if ((sender as CheckBox).Tag is null)
            return;

        TagHash hash = new TagHash((sender as CheckBox).Tag as string);
        Tag<D2Class_898E8080> dataentry = PackageHandler.GetTag<D2Class_898E8080>(hash);
        foreach (DisplayResource item in DataEntryList.Items)
        {
            if (item.Name == "Select all")
                continue;

            if (item.Selected)
            {
                if (dataentry == null)
                    continue;

                PopulateEntitiesList(dataentry);
            }
        }
    }

    private void PopulateEntitiesList(Tag<D2Class_898E8080> dataentry)//(Tag<D2Class_01878080> bubbleMaps)
    {
        ConcurrentBag<DisplayEntity> items = new ConcurrentBag<DisplayEntity>();

        Parallel.ForEach(CollapseResourceParent(dataentry.Hash), entry =>
        {
            foreach(var data in entry.Header.DataEntries)
            {
                if (!items.Contains(new DisplayEntity { Hash = data.Entity.Hash }))
                {
                    if (data.Entity.HasGeometry())
                    {
                        items.Add(new DisplayEntity
                        {
                            Name = $"Entity {data.Entity.Hash}",
                            Hash = data.Entity.Hash
                        });
                    }
                }
            }
        });
        var sortedItems = new List<DisplayEntity>(items);
        sortedItems.Sort((a, b) => b.Hash.CompareTo(a.Hash));
        sortedItems.Insert(0, new DisplayEntity
        {
            Name = "Select all"
        });
        EntityList.ItemsSource = sortedItems;
    }

    public async void ExportFull(ExportInfo info)
    {
        //Activity activity = PackageHandler.GetTag(typeof(Activity), new TagHash(_activeActivity.Hash));
        _activityLog.Debug($"Exporting activity entity data name: {PackageHandler.GetActivityName(_activeActivity.Hash)}, hash: {_activeActivity.Hash}");
        Dispatcher.Invoke(() =>
        {
            MapControl.Visibility = Visibility.Hidden;
        });
        var maps = new List<DisplayResource>();
        bool bSelectAll = false;
        foreach (DisplayResource item in DataEntryList.Items)
        {
            if (item.Selected && item.Name == "Select all")
            {
                bSelectAll = true;
            }
            else
            {
                if (item.Selected || bSelectAll)
                {
                    maps.Add(item);
                }
            }
        }

        if (maps.Count == 0)
        {
            _activityLog.Error("No entries selected for export.");
            MessageBox.Show("No entries selected for export.");
            return;
        }

        List<string> mapStages = maps.Select((x, i) => $"exporting {i + 1}/{maps.Count}").ToList();
        MainWindow.Progress.SetProgressStages(mapStages);
        // MainWindow.Progress.SetProgressStages(new List<string> { "exporting activity map data parallel" });

        Parallel.ForEach(maps, map =>
        {
            var entries = PackageHandler.GetTag<D2Class_898E8080>(new TagHash(map.Hash));

            string savePath = ConfigHandler.GetExportSavePath() + $"/Maps/{_activeActivity.Header.LocationName}/{PackageHandler.GetActivityName(_activeActivity.Hash).Replace(".", "_")}/{map.Name.Split(" ")[0]}_{map.ActivityName}/";
            if (!Directory.Exists(savePath))
                Directory.CreateDirectory(savePath);

            FbxHandler dynamicHandler = new FbxHandler();
            dynamicHandler.InfoHandler.SetMeshName($"{map.ActivityName}_{map.Hash}_ActivityEntities");
            dynamicHandler.InfoHandler.AddType("ActivityEntities");

            foreach (var data in CollapseResourceParent(entries.Hash))
            {
                foreach(var entry in data.Header.DataEntries)
                {
                    if (entry.Entity.HasGeometry())
                    {
                        if (ConfigHandler.GetIndvidualEntitiesEnabled())
                            ExportDynamics(savePath, entry);

                        dynamicHandler.AddDynamicToScene(entry, entry.Entity.Hash, savePath, ConfigHandler.GetUnrealInteropEnabled() || ConfigHandler.GetS2ShaderExportEnabled(), ConfigHandler.GetSaveCBuffersEnabled());
                        Entity ent = new Entity(entry.Entity.Hash, false);
                        if (ent.Header.EntityResources is null)
                            continue;
                        foreach (var e in ent.Header.EntityResources)
                        {
                            if (e.ResourceHash.Header.Unk18 is D2Class_0E848080 f)
                            {
                                foreach (var g in f.Unk88)
                                {
                                    foreach (var h in g.Unk08)
                                    {
                                        dynamicHandler.AddDynamicToScene(entry, h.Unk08.Hash, savePath, ConfigHandler.GetUnrealInteropEnabled() || ConfigHandler.GetS2ShaderExportEnabled(), ConfigHandler.GetSaveCBuffersEnabled());
                                        Entity ent2 = new Entity(h.Unk08.Hash, false);
                                        if (ent2.Header.EntityResources is null)
                                            continue;

                                        foreach (var e2 in ent2.Header.EntityResources)
                                        {
                                            if (e2.ResourceHash.Header.Unk18 is D2Class_0E848080 f2)
                                            {
                                                foreach (var g2 in f2.Unk88)
                                                {
                                                    foreach (var h2 in g2.Unk08)
                                                    {
                                                        dynamicHandler.AddDynamicToScene(entry, h2.Unk08.Hash, savePath, ConfigHandler.GetUnrealInteropEnabled() || ConfigHandler.GetS2ShaderExportEnabled(), ConfigHandler.GetSaveCBuffersEnabled());
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            dynamicHandler.ExportScene($"{savePath}/{map.ActivityName}_{map.Hash}_ActivityEntities.fbx");
            dynamicHandler.Dispose();
            MainWindow.Progress.CompleteStage();
        });

        Dispatcher.Invoke(() =>
        {
            MapControl.Visibility = Visibility.Visible;
        });
        _activityLog.Information($"Exported activity data name: {PackageHandler.GetActivityName(_activeActivity.Hash)}, hash: {_activeActivity.Hash}");
        MessageBox.Show("Activity map data exported completed.");
    }

    private static void ExportDynamics(string savePath, D2Class_85988080 entry)
    {
        if (!Directory.Exists($"{savePath}/Dynamics/"))
            Directory.CreateDirectory($"{savePath}/Dynamics/");

        FbxHandler singleDynamicHandler = new FbxHandler(false);
        singleDynamicHandler.AddDynamicToScene(entry, entry.Entity.Hash, savePath, ConfigHandler.GetUnrealInteropEnabled() || ConfigHandler.GetS2ShaderExportEnabled(), ConfigHandler.GetSaveCBuffersEnabled());

        if (ConfigHandler.GetS2VMDLExportEnabled())
        {
            Source2Handler.SaveEntityVMDL($"{savePath}/Dynamics", entry.Entity);
        }

        Entity ent = new Entity(entry.Entity.Hash, false);
        if (ent.Header.EntityResources is not null)
        {
            foreach (var e in ent.Header.EntityResources)
            {
                if (e.ResourceHash.Header.Unk18 is D2Class_0E848080 f)
                {
                    foreach (var g in f.Unk88)
                    {
                        foreach (var h in g.Unk08)
                        {
                            singleDynamicHandler.AddDynamicToScene(entry, h.Unk08.Hash, savePath, ConfigHandler.GetUnrealInteropEnabled() || ConfigHandler.GetS2ShaderExportEnabled(), ConfigHandler.GetSaveCBuffersEnabled());
                            Entity ent2 = new Entity(h.Unk08.Hash, false);
                            if (ent2.Header.EntityResources is null)
                                continue;

                            foreach (var e2 in ent2.Header.EntityResources)
                            {
                                if (e2.ResourceHash.Header.Unk18 is D2Class_0E848080 f2)
                                {
                                    foreach (var g2 in f2.Unk88)
                                    {
                                        foreach (var h2 in g2.Unk08)
                                        {
                                            singleDynamicHandler.AddDynamicToScene(entry, h2.Unk08.Hash, savePath, ConfigHandler.GetUnrealInteropEnabled() || ConfigHandler.GetS2ShaderExportEnabled(), ConfigHandler.GetSaveCBuffersEnabled());
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        singleDynamicHandler.ExportScene($"{savePath}/Dynamics/{entry.Entity.Hash}.fbx");
        singleDynamicHandler.Dispose();           
    }

    private async void Entity_OnClick(object sender, RoutedEventArgs e)
    {
        var s = sender as Button;
        var dc = s.DataContext as DisplayEntity;
        Dispatcher.Invoke(() =>
        {
            MapControl.Visibility = Visibility.Hidden;
        });

        if (dc.Name == "Select all")
        {
            var items = EntityList.Items.Cast<DisplayEntity>().Where(x => x.Name != "Select all");
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
                foreach (DisplayEntity item in items)
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
        var dc = s.DataContext as DisplayEntity;
        MapControl.Clear();
        _activityLog.Debug($"Loading UI for entity: {dc.Name}");
        MapControl.Visibility = Visibility.Hidden;
        var lod = MapControl.ModelView.GetSelectedLod();
        if(dc.Name != "Select all")
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

public class DisplayActivity
{
    public string Name { get; set; }
    public string Hash { get; set; }
}

public class DisplayResource
{
    public string Name { get; set; }
    public string Hash { get; set; }
    public string ActivityName { get; set; }
    public int Count { get; set; }

    public bool Selected { get; set; }
}

public class DisplayEntity
{
    public string Name { get; set; }
    public string Hash { get; set; }
    public int Models { get; set; }

    public bool Selected { get; set; }

    public override bool Equals(object obj)
    {
        var other = obj as DisplayEntity;
        return other != null && Hash == other.Hash;
    }

    public override int GetHashCode()
    {
        return Hash?.GetHashCode() ?? 0;
    }
}

//public class DisplayDynamicMap
//{
//    public string Name { get; set; }
//    public string Hash { get; set; }
//    public int Models { get; set; }
    
//    public bool Selected { get; set; }

//    public override bool Equals(object obj)
//    {
//        var other = obj as DisplayDynamicMap;
//        return other != null && Hash == other.Hash;
//    }

//    public override int GetHashCode()
//    {
//        return Hash?.GetHashCode() ?? 0;
//    }
//}