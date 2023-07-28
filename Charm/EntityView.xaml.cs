using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Field;
using Field.Entities;
using Field.General;
using Field.Investment;
using Field.Models;
using HelixToolkit.SharpDX.Core.Model.Scene;
using Internal.Fbx;
using Serilog;
using File = System.IO.File;

namespace Charm;

public partial class EntityView : UserControl
{
    private readonly ILogger _entityLog = Log.ForContext<EntityView>();
    private static MainWindow _mainWindow = null;
    private Entity _loadedEntity = null;
    private static bool source2Models = ConfigHandler.GetS2VMDLExportEnabled();

    public EntityView()
    {
        InitializeComponent();
    }

    private void OnControlLoaded(object sender, RoutedEventArgs routedEventArgs)
    {
        _mainWindow = Window.GetWindow(this) as MainWindow;
    }

    public bool LoadEntity(TagHash entityHash, FbxHandler fbxHandler, bool bBlockRecursion=false)
    {
        fbxHandler.Clear();
        Entity entity = new Entity(entityHash); //PackageHandler.GetTag(typeof(Entity), entityHash); not working for some reason?

        //Scuffed sound export testing
        //if (entity.Header.EntityResources is not null)
        //{
        //    foreach (var e in entity.Header.EntityResources)
        //    {
        //        if (e.ResourceHash.Header.Unk18 is D2Class_79818080 a)
        //        {
        //            foreach (var d2ClassF1918080 in a.WwiseSounds1)
        //            {
        //                if (d2ClassF1918080.Unk10 is D2Class_40668080 b)
        //                {
        //                    if (b.Sound != null)
        //                    {
        //                        Wem wem = PackageHandler.GetTag(typeof(Wem), b.Sound.Hash);
        //                        if (wem.GetData().Length == 1)
        //                            continue;

        //                        var soundSavePath = $"{ConfigHandler.GetExportSavePath()}/Sound/Entity_{entityHash}/";
        //                        Directory.CreateDirectory(soundSavePath);
        //                        b.Sound.ExportSound(soundSavePath);
        //                    }
        //                }
        //            }
        //            foreach (var d2ClassF1918080 in a.WwiseSounds2)
        //            {
        //                if (d2ClassF1918080.Unk10 is D2Class_40668080 b)
        //                {
        //                    if (b.Sound != null)
        //                    {
        //                        Wem wem = PackageHandler.GetTag(typeof(Wem), b.Sound.Hash);
        //                        if (wem.GetData().Length == 1)
        //                            continue;

        //                        var soundSavePath = $"{ConfigHandler.GetExportSavePath()}/Sound/Entity_{entityHash}/";
        //                        Directory.CreateDirectory(soundSavePath);
        //                        b.Sound.ExportSound(soundSavePath);
        //                    }
        //                }
        //            }
        //        }
        //    }
        //}

        _loadedEntity = entity;
        // todo fix this
        //if (entity.AnimationGroup != null && !bBlockRecursion)  // Make a new tab and use that with FullEntityView
        //{
        //    var fev = new FullEntityView();
        //    _mainWindow.MakeNewTab(entityHash, fev);
        //    _mainWindow.SetNewestTabSelected();
        //    return fev.LoadEntity(entityHash, fbxHandler);
        //}
        AddEntity(entity, ModelView.GetSelectedLod(), fbxHandler);
        return LoadUI(fbxHandler);
    }

    public async void LoadEntityFromApi(DestinyHash apiHash, FbxHandler fbxHandler)
    {
        fbxHandler.Clear();
        List<Entity> entities = InvestmentHandler.GetEntitiesFromHash(apiHash);
        foreach (var entity in entities)
        {
            // todo find out why sometimes this is null
            if (entity == null)
            {
                continue;
            }
            AddEntity(entity, ModelView.GetSelectedLod(), fbxHandler);
        }
        LoadUI(fbxHandler);
    }

    private void AddEntity(Entity entity, ELOD detailLevel, FbxHandler fbxHandler, Animation animation=null)
    {
        var dynamicParts = entity.Load(detailLevel);
        if (dynamicParts.Count == 0)
            return;
        ModelView.SetGroupIndices(new HashSet<int>(dynamicParts.Select(x => x.GroupIndex)));
        dynamicParts = dynamicParts.Where(x => x.GroupIndex == ModelView.GetSelectedGroupIndex()).ToList();
        if (animation != null)
        {
            animation.tra = new Vector3(int.Parse(TraX.Text), int.Parse(TraY.Text), int.Parse(TraZ.Text), true);
            animation.rot = new Vector3(int.Parse(RotX.Text), int.Parse(RotY.Text), int.Parse(RotZ.Text), true);
            animation.flipTra = new Vector3(Convert.ToInt32(FlipTraX.IsChecked), Convert.ToInt32(FlipTraY.IsChecked), Convert.ToInt32(FlipTraZ.IsChecked), true);
            animation.flipRot = new Vector3(Convert.ToInt32(FlipRotX.IsChecked), Convert.ToInt32(FlipRotY.IsChecked), Convert.ToInt32(FlipRotZ.IsChecked), true);
            animation.traXYZ = new [] { TraXX.Text, TraYY.Text, TraZZ.Text };
            animation.rotXYZ = new [] { RotXX.Text, RotYY.Text, RotZZ.Text };
        }
        fbxHandler.AddEntityToScene(entity, dynamicParts, detailLevel, animation);
    }

    private bool LoadUI(FbxHandler fbxHandler)
    {
        MainViewModel MVM = (MainViewModel)ModelView.UCModelView.Resources["MVM"];
        string filePath = $"{ConfigHandler.GetExportSavePath()}/temp.fbx";
        fbxHandler.ExportScene(filePath);
        bool loaded = MVM.LoadEntityFromFbx(filePath);
        fbxHandler.Clear();
        return loaded;
    }

    public static void Export(List<Entity> entities, string name, EExportTypeFlag exportType, EntitySkeleton overrideSkeleton = null, bool skipBlanks = false)
    {
        FbxHandler fbxHandler = new FbxHandler(exportType == EExportTypeFlag.Full);

        List<FbxNode> boneNodes = null;
        if (overrideSkeleton != null)
            boneNodes = fbxHandler.AddSkeleton(overrideSkeleton.GetBoneNodes());
        
        Log.Debug($"Exporting entity model name: {name}");
        string savePath = ConfigHandler.GetExportSavePath();
        string meshName = string.Join("_", name.Split(Path.GetInvalidFileNameChars()));
        if (exportType == EExportTypeFlag.Full)
        {
            savePath += $"/{meshName}";
        }
        Directory.CreateDirectory(savePath);
        
        foreach (var entity in entities)
        {
            var dynamicParts = entity.Load(ELOD.MostDetail);
            fbxHandler.AddEntityToScene(entity, dynamicParts, ELOD.MostDetail, null, boneNodes, skipBlanks);
            if (exportType == EExportTypeFlag.Full)
            {
                entity.SaveMaterialsFromParts(savePath, dynamicParts, ConfigHandler.GetUnrealInteropEnabled() || ConfigHandler.GetS2ShaderExportEnabled(), ConfigHandler.GetSaveCBuffersEnabled());
                entity.SaveTexturePlates(savePath);
            }
            if (source2Models)
            {
                Source2Handler.SaveEntityVMDL($"{savePath}", entity);
            }
		}

        if (exportType == EExportTypeFlag.Full)
        {
            fbxHandler.InfoHandler.SetMeshName(meshName);
            if (ConfigHandler.GetUnrealInteropEnabled())
            {
                fbxHandler.InfoHandler.SetUnrealInteropPath(ConfigHandler.GetUnrealInteropPath());
                AutomatedImporter.SaveInteropUnrealPythonFile(savePath, meshName, AutomatedImporter.EImportType.Entity, ConfigHandler.GetOutputTextureFormat());
            }
            if(ConfigHandler.GetBlenderInteropEnabled())
            {
                AutomatedImporter.SaveInteropBlenderPythonFile(savePath, meshName, AutomatedImporter.EImportType.Entity, ConfigHandler.GetOutputTextureFormat());
            }
        }

        // Scale and rotate
        // fbxHandler.ScaleAndRotateForBlender(boneNodes[0]);
        fbxHandler.InfoHandler.AddType("Entity");
        fbxHandler.ExportScene($"{savePath}/{meshName}.fbx");
        fbxHandler.Dispose();
        Log.Information($"Exported entity model {name} to {savePath.Replace('\\', '/')}/");
    }

    public static void ExportInventoryItem(ApiItem item)
    {
        string name = string.Join("_", $"{item.Item.Header.InventoryItemHash.Hash}_{item.ItemName}"
            .Split(Path.GetInvalidFileNameChars()));
        // Export the model
        // todo bad, should be replaced
        EntitySkeleton overrideSkeleton = new EntitySkeleton(new TagHash("BC38AB80"));
        var val = InvestmentHandler.GetPatternEntityFromHash(item.Item.Header.InventoryItemHash);
        // var resource = (D2Class_6E358080)val.PatternAudio.Header.Unk18;
        // if (resource.PatternAudioGroups[0].WeaponSkeletonEntity != null)
        // {
            // overrideSkeleton = resource.PatternAudioGroups[0].WeaponSkeletonEntity.Skeleton;
        // }
        if (val != null && val.Skeleton != null)
        {
            overrideSkeleton = val.Skeleton;
        }
        EntityView.Export(InvestmentHandler.GetEntitiesFromHash(item.Item.Header.InventoryItemHash),
            name, EExportTypeFlag.Full, overrideSkeleton);
        
        // Export the dye info
        Dictionary<DestinyHash, Dye> dyes = new Dictionary<DestinyHash, Dye>();
        if (item.Item.Header.Unk90 is D2Class_77738080 translationBlock)
        {
            foreach (var dyeEntry in translationBlock.DefaultDyes)
            {
                Dye dye = InvestmentHandler.GetDyeFromIndex(dyeEntry.DyeIndex);
                dyes.Add(InvestmentHandler.GetChannelHashFromIndex(dyeEntry.ChannelIndex), dye);
            }
            foreach (var dyeEntry in translationBlock.LockedDyes)
            {
                Dye dye = InvestmentHandler.GetDyeFromIndex(dyeEntry.DyeIndex);
                dyes.Add(InvestmentHandler.GetChannelHashFromIndex(dyeEntry.ChannelIndex), dye);
            }
        }
        
        string savePath = ConfigHandler.GetExportSavePath();
        string meshName = name;
        savePath += $"/{meshName}";
        Directory.CreateDirectory(savePath);
        AutomatedImporter.SaveBlenderApiFile(savePath, string.Join("_", item.ItemName.Split(Path.GetInvalidFileNameChars())),
            ConfigHandler.GetOutputTextureFormat(), dyes.Values.ToList());
    }

    public void LoadAnimation(TagHash tagHash, FbxHandler fbxHandler)
    {
        Animation animation = PackageHandler.GetTag(typeof(Animation), tagHash);
        // to load an animation into the viewer, we need to save the fbx then load
        fbxHandler.Clear();
        AddEntity(_loadedEntity, ModelView.GetSelectedLod(), fbxHandler, animation);
        LoadUI(fbxHandler);
    }
    
    public void LoadAnimationWithPlayerModels(TagHash tagHash, FbxHandler fbxHandler)
    {
        Animation animation = PackageHandler.GetTag(typeof(Animation), tagHash);
        // to load an animation into the viewer, we need to save the fbx then load
        fbxHandler.Clear();

        fbxHandler.AddPlayerSkeletonAndMesh();

        // Add animation
        animation.Load();
        // animation.SaveToFile($"C:/T/animation_{animHash}.json");
        fbxHandler.AddAnimationToEntity(animation, fbxHandler._globalSkeletonNodes);

        LoadUI(fbxHandler);
    }

    public static void ExportAnimationWithPlayerModels(TagHash tagHash, bool skipModel = true)
    {
        FbxHandler fbxHandler = new(false);
        Animation animation = PackageHandler.GetTag(typeof(Animation), tagHash);

        fbxHandler.AddPlayerSkeletonAndMesh(skipModel);

        // Add animation
        animation.Load();
        // animation.SaveToFile($"C:/T/animation_{animHash}.json");
        fbxHandler.AddAnimationToEntity(animation, fbxHandler._globalSkeletonNodes);
        
        Directory.CreateDirectory($"{ConfigHandler.GetExportSavePath()}/Animations/");
        fbxHandler.ExportScene($"{ConfigHandler.GetExportSavePath()}/Animations/anim_player_{animation.Hash}.fbx");
    }
}