using System.Windows.Controls;
using Field;
using Field.General;

namespace Charm;

public partial class ActivitySoundView : UserControl
{
    public ActivitySoundView()
    {
        InitializeComponent();
    }
    
    // Activity only has one music table ever so no taglist
    public void LoadUI(TagHash activityHash)
    {
        TagList.LoadContent(ETagListType.MapSoundsList, activityHash, true);
    }
}