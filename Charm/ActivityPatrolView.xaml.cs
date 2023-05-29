using System.Windows.Controls;
using Field.General;

namespace Charm;

public partial class ActivityPatrolView : UserControl
{
    public ActivityPatrolView()
    {
        InitializeComponent();
    }
    
    public void LoadUI(TagHash activityHash)
    {
        TagList.LoadContent(ETagListType.PatrolList, activityHash, true);
    }
}