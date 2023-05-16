using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Packaging;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms.VisualStyles;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.IO;
using System.Reflection;
using Field;
using Field.Entities;
using Field.General;
using Field.Models;
using Internal.Fbx;
using SharpDX.Toolkit.Graphics;

namespace Charm;

public partial class MainMenuView : UserControl
{
    private static MainWindow _mainWindow = null;
    public string GameVersion { get; set; }

    public MainMenuView()
    {
        InitializeComponent();
    }

    private void OnControlLoaded(object sender, RoutedEventArgs routedEventArgs)
    {
        _mainWindow = Window.GetWindow(this) as MainWindow;
        DataContext = this;
        if(ConfigHandler.GetPackagesPath() != "" && File.Exists(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "/paths.cache"))
        {
            GameVersion = $"Game Version:\n{_mainWindow.CheckGameVersion()}";
            LoadTexture(new TextureHeader(new Field.General.TagHash("6A20A080")));
        }
    }

    private void ApiViewButton_OnClick(object sender, RoutedEventArgs e)
    {
        // TagListViewerView apiView = new TagListViewerView();
        // apiView.LoadContent(ETagListType.ApiList);
        DareView apiView = new DareView();
        apiView.LoadContent();
        _mainWindow.MakeNewTab("api", apiView);
        _mainWindow.SetNewestTabSelected();
    }
    
    private void NamedEntitiesBagsViewButton_OnClick(object sender, RoutedEventArgs e)
    {
        TagListViewerView tagListView = new TagListViewerView();
        tagListView.LoadContent(ETagListType.DestinationGlobalTagBagList);
        _mainWindow.MakeNewTab("destination global tag bag", tagListView);
        _mainWindow.SetNewestTabSelected();
    }
    
    private void AllEntitiesViewButton_OnClick(object sender, RoutedEventArgs e)
    {
        TagListViewerView tagListView = new TagListViewerView();
        tagListView.LoadContent(ETagListType.EntityList);
        _mainWindow.MakeNewTab("dynamics", tagListView);
        _mainWindow.SetNewestTabSelected();
    }

    private void ActivitiesViewButton_OnClick(object sender, RoutedEventArgs e)
    {
        TagListViewerView tagListView = new TagListViewerView();
        tagListView.LoadContent(ETagListType.ActivityList);
        _mainWindow.MakeNewTab("activities", tagListView);
        _mainWindow.SetNewestTabSelected();
    }

    private void AllStaticsViewButton_OnClick(object sender, RoutedEventArgs e)
    {
        TagListViewerView tagListView = new TagListViewerView();
        tagListView.LoadContent(ETagListType.StaticsList);
        _mainWindow.MakeNewTab("statics", tagListView);
        _mainWindow.SetNewestTabSelected();    
    }

    private void WeaponAudioViewButton_Click(object sender, RoutedEventArgs e)
    {
        TagListViewerView tagListView = new TagListViewerView();
        tagListView.LoadContent(ETagListType.WeaponAudioGroupList);
        _mainWindow.MakeNewTab("weapon audio", tagListView);
        _mainWindow.SetNewestTabSelected();    
    }

    private void AllAudioViewButton_OnClick(object sender, RoutedEventArgs e)
    {
        TagListViewerView tagListView = new TagListViewerView();
        tagListView.LoadContent(ETagListType.SoundsPackagesList);
        _mainWindow.MakeNewTab("sounds", tagListView);
        _mainWindow.SetNewestTabSelected();    
    }

    private void AllStringsViewButton_OnClick(object sender, RoutedEventArgs e)
    {
        TagListViewerView tagListView = new TagListViewerView();
        tagListView.LoadContent(ETagListType.StringContainersList);
        _mainWindow.MakeNewTab("strings", tagListView);
        _mainWindow.SetNewestTabSelected();      
    }

    private void AllTexturesViewButton_OnClick(object sender, RoutedEventArgs e)
    {
        TagListViewerView tagListView = new TagListViewerView();
        tagListView.LoadContent(ETagListType.TextureList);
        _mainWindow.MakeNewTab("textures", tagListView);
        _mainWindow.SetNewestTabSelected();
    }

    private void GithubButton_OnClick(object sender, RoutedEventArgs e)
    {
        Process.Start(new ProcessStartInfo { FileName = "https://github.com/MontagueM/Charm", UseShellExecute = true });
    }

    private void DiscordButton_OnClick(object sender, RoutedEventArgs e)
    {
        Process.Start(new ProcessStartInfo { FileName = "https://discord.com/invite/destinymodelrips", UseShellExecute = true });
    }

    private void MainMenuImage_OnClick(object sender, RoutedEventArgs e)
    {
        if (MainMenuImage.IsChecked == true)
            LoadTexture(new TextureHeader(new Field.General.TagHash("9E20A080")));
        else
            LoadTexture(new TextureHeader(new Field.General.TagHash("6A20A080")));
    }

    public void LoadTexture(TextureHeader textureHeader)
    {
        BitmapImage bitmapImage = new BitmapImage();
        bitmapImage.BeginInit();
        bitmapImage.StreamSource = textureHeader.GetTexture();
        bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
        // Divide aspect ratio to fit 960x1000
        float widthDivisionRatio = (float)textureHeader.Header.Width / 960;
        float heightDivisionRatio = (float)textureHeader.Header.Height / 1000;
        float transformRatio = Math.Max(heightDivisionRatio, widthDivisionRatio);
        int imgWidth = (int)Math.Floor(textureHeader.Header.Width / transformRatio);
        int imgHeight = (int)Math.Floor(textureHeader.Header.Height / transformRatio);
        bitmapImage.DecodePixelWidth = imgWidth;
        bitmapImage.DecodePixelHeight = imgHeight;
        bitmapImage.EndInit();
        bitmapImage.Freeze();
        Image.Source = bitmapImage;
        Image.Width = 160;
        Image.Height = 160;
    }
    
    private void CinematicsButton_OnClick(object sender, RoutedEventArgs e)
    {
        //Broken atm?
        string activityHash = "4362E580";//"9694EA80";
        Field.Activity activity = PackageHandler.GetTag(typeof(Field.Activity), new TagHash(activityHash));
        EntityResource cinematicResource = ((D2Class_0C468080) activity.Header.Unk40[0].Unk70[0].UnkEntityReference.Header.Unk18.Header.EntityResources[1]
            .EntityResourceParent.Header.EntityResource.Header.Unk18).CinematicEntity.Header.EntityResources.Last().ResourceHash;
        HashSet<string> cinematicModels = new HashSet<string>();
        foreach (D2Class_AE5F8080 groupEntry in ((D2Class_B75F8080)cinematicResource.Header.Unk18).CinematicEntityGroups)
        {
            foreach (D2Class_B15F8080 entityEntry in groupEntry.CinematicEntities)
            {
                var entityWithModel = entityEntry.CinematicEntityModel;
                var entityWithAnims = entityEntry.CinematicEntityAnimations;
                if (entityWithModel != null)
                {
                    cinematicModels.Add(entityWithModel.Hash.ToString());
                    if (entityWithAnims.AnimationGroup != null) // caiatl
                    {
                        foreach (var animation in ((D2Class_F8258080) entityWithAnims.AnimationGroup.Header.Unk18).AnimationGroup.Header.Animations)
                        {
                            if (animation.Animation == null)
                                continue;
                            animation.Animation.ParseTag();
                            animation.Animation.Load();
                            FbxHandler fbxHandler = new FbxHandler();
                            fbxHandler.AddEntityToScene(entityWithModel, entityWithModel.Load(ELOD.MostDetail), ELOD.MostDetail, animation.Animation);
                            fbxHandler.ExportScene($"{ConfigHandler.GetExportSavePath()}/{entityWithModel.Hash}_{animation.Animation.Hash}_{animation.Animation.Header.FrameCount}_{Math.Round((float)animation.Animation.Header.FrameCount/30)}.fbx");
                            fbxHandler.Dispose();
                        }
                    }
                    if (entityWithModel.Hash == "91EBA880" && entityWithAnims.AnimationGroup != null) // player
                    {
                        foreach (var animation in ((D2Class_F8258080) entityWithAnims.AnimationGroup.Header.Unk18).AnimationGroup.Header.Animations)
                        {
                            animation.Animation.ParseTag();
                            animation.Animation.Load();
                            FbxHandler fbxHandler = new FbxHandler();
                            fbxHandler.AddPlayerSkeletonAndMesh();
                            fbxHandler.AddAnimationToEntity(animation.Animation);
                            fbxHandler.ExportScene($"{ConfigHandler.GetExportSavePath()}/player_{animation.Animation.Hash}_{animation.Animation.Header.FrameCount}_{Math.Round((float)animation.Animation.Header.FrameCount/30)}.fbx");
                            fbxHandler.Dispose();
                        }

                        var c = 0;
                    }
                }
                var a = 0;
            }
        }

        var b = 0;
    }

    private void AnimationsButton_OnClick(object sender, RoutedEventArgs e)
    {
        TagListViewerView tagListView = new TagListViewerView();
        tagListView.LoadContent(ETagListType.AnimationPackageList);
        _mainWindow.MakeNewTab("animations", tagListView);
        _mainWindow.SetNewestTabSelected();
    }
}