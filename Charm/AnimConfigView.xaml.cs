using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using Field;

namespace Charm;

public partial class AnimConfigView : UserControl
{
    public AnimConfigView()
    {
        InitializeComponent();
    }
    
    public void OnControlLoaded(object sender, RoutedEventArgs e)
    {
        PopulateConfigPanel();
    }

    private void PopulateConfigPanel()
    {
        AnimConfigPanel.Children.Clear();

        TextBlock header = new TextBlock();
        header.Text = "Animation Settings";
        header.FontSize = 30;
        AnimConfigPanel.Children.Add(header);

        // Helmet Hash
        ConfigSettingTextControl chh = new ConfigSettingTextControl();
        chh.SettingName = "Animation Helmet Hash";
        var hval = ConfigHandler.GetAnimationHelmetHash();
        chh.SettingValue = hval == "" ? "Not set" : hval;
        chh.ChangeButton.Click += AnimationHelmetHash_OnClick;
        AnimConfigPanel.Children.Add(chh);

        // Arms Hash
        ConfigSettingTextControl cah = new ConfigSettingTextControl();
        cah.SettingName = "Animation Arms Hash";
        var aval = ConfigHandler.GetAnimationArmsHash();
        cah.SettingValue = aval == "" ? "Not set" : aval;
        cah.ChangeButton.Click += AnimationArmsHash_OnClick;
        AnimConfigPanel.Children.Add(cah);

        // Chest Hash
        ConfigSettingTextControl cch = new ConfigSettingTextControl();
        cch.SettingName = "Animation Chest Hash";
        var cval = ConfigHandler.GetAnimationChestHash();
        cch.SettingValue = cval == "" ? "Not set" : cval;
        cch.ChangeButton.Click += AnimationChestHash_OnClick;
        AnimConfigPanel.Children.Add(cch);

        // Legs Hash
        ConfigSettingTextControl clh = new ConfigSettingTextControl();
        clh.SettingName = "Animation Legs Hash";
        var lval = ConfigHandler.GetAnimationLegsHash();
        clh.SettingValue = lval == "" ? "Not set" : lval;
        clh.ChangeButton.Click += AnimationLegsHash_OnClick;
        AnimConfigPanel.Children.Add(clh);

        // Class Item Hash
        ConfigSettingTextControl ccih = new ConfigSettingTextControl();
        ccih.SettingName = "Animation Class Item Hash";
        var ciVal = ConfigHandler.GetAnimationClassItemHash();
        ccih.SettingValue = ciVal == "" ? "Not set" : ciVal;
        ccih.ChangeButton.Click += AnimationClassItemHash_OnClick;
        AnimConfigPanel.Children.Add(ccih);
    }

    private void AnimationHelmetHash_OnClick(object sender, RoutedEventArgs e)
    {
        ConfigHandler.TrySetAnimationHelmetHash((AnimConfigPanel.Children[1] as ConfigSettingTextControl).SettingValue);
        PopulateConfigPanel();
    }

    private void AnimationArmsHash_OnClick(object sender, RoutedEventArgs e)
    {
        ConfigHandler.TrySetAnimationArmsHash((AnimConfigPanel.Children[2] as ConfigSettingTextControl).SettingValue);
        PopulateConfigPanel();
    }

    private void AnimationChestHash_OnClick(object sender, RoutedEventArgs e)
    {
        ConfigHandler.TrySetAnimationChestHash((AnimConfigPanel.Children[3] as ConfigSettingTextControl).SettingValue);
        PopulateConfigPanel();
    }

    private void AnimationLegsHash_OnClick(object sender, RoutedEventArgs e)
    {
        ConfigHandler.TrySetAnimationLegsHash((AnimConfigPanel.Children[4] as ConfigSettingTextControl).SettingValue);
        PopulateConfigPanel();
    }

    private void AnimationClassItemHash_OnClick(object sender, RoutedEventArgs e)
    {
        ConfigHandler.TrySetAnimationClassItemHash((AnimConfigPanel.Children[5] as ConfigSettingTextControl).SettingValue);
        PopulateConfigPanel();
    }
}