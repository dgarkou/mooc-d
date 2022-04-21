﻿using System.Windows.Input;

namespace MoocDownloader.App.Views;

/// <summary>
/// Interaction logic for LinkView.xaml
/// </summary>
public partial class LinkView
{
    public LinkView()
    {
        InitializeComponent();
    }

    /// <inheritdoc />
    protected override void OnMouseDown(MouseButtonEventArgs e)
    {
        DragMove();
    }
}