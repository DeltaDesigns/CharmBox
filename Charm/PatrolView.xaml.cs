using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Controls;
using Field;
using Field.General;

namespace Charm;

public partial class PatrolView : UserControl
{
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
            items.Add(new PatrolItem
            {
                Name = patrol.PatrolNameString,
                Description = patrol.PatrolDescriptionString,
                Objective = $"{patrol.PatrolObjectiveString}",
                Hash = patrol.PatrolHash
            });
        }

        return items;
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
}

