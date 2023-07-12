﻿using System;
using System.Linq;
using System.Reflection;
using System.Windows.Controls;
using Charm.Objects;

namespace Charm.Views;

public partial class GridListView<TViewModel> : GridListView, IControl where TViewModel : IViewModel
{
    public void Load()
    {
        Load<TViewModel>();
    }
}

public partial class GridListView : UserControl
{
    private bool _hasLoaded = false;
    public Type ViewModelType { get; set; }

    public GridListView()
    {
        InitializeComponent();
    }

    // todo this can probably be moved to a ViewModel
    // If we're not already loaded, load the list items and prepare the list view to use the file view via dependency injection.
    public void Load<TViewModel>() where TViewModel : IViewModel
    {
        if (_hasLoaded)
        {
            return;
        }
        _hasLoaded = true;

        ViewModelType = typeof(TViewModel);

        // Presuming ViewType is a ViewModel, we need to find what UserControl type it is
        // Find the IAbstractFileView interface and get TView from it
        // Type typeOfControl = ViewType.GetInterfaces()
        //     .First(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IAbstractFileView<,>))
        //     .GenericTypeArguments[0];
        //
        // fileViewModel = Activator.CreateInstance(ViewType);
        //
        //
        // // todo should be UserControl not ListControl
        // UserControl fileView = (UserControl) Activator.CreateInstance(typeOfControl);
        // fileView.DataContext = fileViewModel;
        //
        // FileContentPresenter.Content = fileView;

        BaseListViewModel listViewModel = new BaseListViewModel();
        GridControl.DataContext = listViewModel;
        // ListControl.DataType = DataType;
        // ListControl.ListItemType = ListItemType;
        OnListItemClicked onListItemClicked = ListItemClicked;

        GridControl.LoadView<TViewModel>(onListItemClicked);

        // Load list items
        // typeof(BaseListViewModel)
        //     .GetMethod("LoadView", BindingFlags.Public | BindingFlags.Instance)
        //     ?.MakeGenericMethod(ListItemType, DataType)
        //     .Invoke(listViewModel, new object[] { this });
    }

    public bool ListItemClicked(IListItem listItem)
    {
        dynamic? itemData = ViewModelType
            .GetInterfaces()
            .First(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IModel<>))
            .GetMethod("GetDataFromItem", BindingFlags.Public | BindingFlags.Static)
            ?.Invoke(null, new object[] { listItem });

        UserControl viewToShow = (UserControl)ViewModelType
            .GetMethod("GetView", BindingFlags.Public | BindingFlags.Static)
            ?.Invoke(null, new object[] { itemData });


        if (viewToShow == null)
        {
            return false;
        }

        // FileContentPresenter.Content = viewToShow;
        return true;
    }
}

