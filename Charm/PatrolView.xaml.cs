using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using Field;
using Field.General;

namespace Charm;

public partial class PatrolView : UserControl
{
    public TextureHeader PatrolIcon = null;
    public PatrolView()
    {
        InitializeComponent();
    }
    
    public void Load(TagHash hash)
    {
        Tag<D2Class_A8418080> patrol = PackageHandler.GetTag<D2Class_A8418080>(hash);
        
        ListView.ItemsSource = GetPatrolItems(patrol.Header.Unk28);
    }

    public List<PatrolItem> GetPatrolItems(List<D2Class_B4418080> patrolTable)
    {
        // List to maintain order of directives
        var items = new List<PatrolItem>();
        Console.WriteLine(patrolTable.Count);

        foreach (var patrol in patrolTable)
        {
            if(patrol.Unk50.Header.UnkC.Header.Unk08[0].Unk04.Header.Unk10 is D2Class_CD3E8080 a) //Probably a better way of doing this
            {
                PatrolIcon = a.Unk00[0].TextureList[0].IconTexture;
            }
            else if (patrol.Unk50.Header.UnkC.Header.Unk08[0].Unk04.Header.Unk10 is D2Class_CB3E8080 b)
            {
                PatrolIcon = b.Unk00[0].TextureList[0].IconTexture;
            }

            items.Add(new PatrolItem
            {
                Name = patrol.PatrolNameString,
                Description = patrol.PatrolDescriptionString,
                Objective = $"{patrol.PatrolObjectiveString}",
                Hash = patrol.PatrolHash,
                Icon = LoadTexture(PatrolIcon)
            });
        }

        return items;
    }

    public BitmapImage LoadTexture(TextureHeader textureHeader)
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
        return bitmapImage;
    }
}

public class PatrolItem
{
    private string _objective;
    
    public string Name { get; set; }
    public string Description { get; set; }

    public string Objective
    {
        get => _objective.Contains("0/0") ? "" : _objective;
        set => _objective = value;
    }
    //public string Unknown { get; set; }
    public string Hash { get; set; }

    public BitmapImage Icon { get; set; }
}

