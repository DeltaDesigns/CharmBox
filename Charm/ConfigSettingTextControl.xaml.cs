using System.Windows.Controls;

namespace Charm;

public partial class ConfigSettingTextControl : UserControl
{
    public ConfigSettingTextControl()
    {
        InitializeComponent();
        DataContext = this;
    }
    
    public string SettingName { get; set; }
    
    public string SettingValue { get; set; }
}