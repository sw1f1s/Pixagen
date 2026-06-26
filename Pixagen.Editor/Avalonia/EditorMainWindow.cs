using System.Reflection;
using System.Globalization;
using System.Text.Json.Serialization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Pixagen.Editor.Hosting;
using Pixagen.Editor.Workspace;
using Pixagen.Ecs.Runtime;
using Pixagen.Game.Features.RenderFeature.Components;
using Pixagen.Game.Features.SharedFeature.Components;

namespace Pixagen.Editor.Avalonia;

public sealed class EditorMainWindow : Window
{
    private const string PixelFontFamilyName = "avares://Pixagen.Editor/Assets/Fonts/PressStart2P#Press Start 2P";
    private static readonly FontFamily PixelFont = new(PixelFontFamilyName);

    private readonly EditorWorkspace _workspace;
    private readonly SceneViewportControl _sceneViewport;
    private readonly GameProcessHost _game = new();
    private readonly StackPanel _hierarchyList = new() { Spacing = 2 };
    private readonly StackPanel _assetList = new() { Spacing = 2 };
    private readonly StackPanel _inspector = new() { Spacing = 6 };
    private readonly TextBlock _status = Text("", PixelPalette.TextMuted, 12);
    private readonly Dictionary<EditorDockPaneId, EditorDockPane> _dockPanes = new();
    private readonly Dictionary<EditorDockZoneId, EditorDockZone> _dockZones = new();
    private readonly Dictionary<EditorDockZoneId, EditorDockZoneView> _dockZoneViews = new();
    private ColumnDefinition? _leftDockColumn;
    private ColumnDefinition? _leftSplitterColumn;
    private ColumnDefinition? _rightSplitterColumn;
    private ColumnDefinition? _rightDockColumn;
    private RowDefinition? _bottomSplitterRow;
    private RowDefinition? _bottomDockRow;
    private Control? _leftDockControl;
    private Control? _rightDockControl;
    private Control? _bottomDockControl;
    private Control? _leftSplitter;
    private Control? _rightSplitter;
    private Control? _bottomSplitter;
    private GridLength _lastLeftWidth = new(280);
    private GridLength _lastRightWidth = new(460);
    private GridLength _lastBottomHeight = new(190);
    private bool _showAddComponentPicker;
    private string _componentSearch = string.Empty;

    public EditorMainWindow(EditorWorkspace workspace)
    {
        _workspace = workspace;
        _sceneViewport = new SceneViewportControl(_workspace);
        _sceneViewport.StatusChanged += OnSceneViewportStatusChanged;
        InitializeDocking();

        Title = string.Empty;
        Width = 1440;
        Height = 900;
        MinWidth = 1024;
        MinHeight = 680;
        WindowState = WindowState.Maximized;
        Background = PixelPalette.App;
        FontFamily = PixelFont;
        Content = BuildLayout();

        RefreshAll();
        Closed += (_, _) =>
        {
            _sceneViewport.Dispose();
            _game.Dispose();
        };
    }

    private Control BuildLayout()
    {
        var root = new Grid
        {
            RowDefinitions =
            {
                new RowDefinition(GridLength.Auto),
                new RowDefinition(GridLength.Star),
                new RowDefinition(GridLength.Auto)
            },
            Background = PixelPalette.App
        };

        Control toolbar = BuildToolbar();
        Grid.SetRow(toolbar, 0);
        root.Children.Add(toolbar);

        Control dockArea = BuildDockArea();
        Grid.SetRow(dockArea, 1);
        root.Children.Add(dockArea);

        Border status = new()
        {
            Background = PixelPalette.PanelDeep,
            BorderBrush = PixelPalette.Border,
            BorderThickness = new Thickness(0, 1, 0, 0),
            Padding = new Thickness(10, 6),
            Child = _status
        };
        Grid.SetRow(status, 2);
        root.Children.Add(status);

        return root;
    }

    private Control BuildToolbar()
    {
        var actions = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };

        actions.Children.Add(Button("PLAY", PlayGame, PixelPalette.Accent));
        actions.Children.Add(Button("STOP", StopGame, PixelPalette.Warning));
        actions.Children.Add(Button("SAVE", SaveScene));
        actions.Children.Add(Button("REFRESH", RefreshAssets));

        var toolbar = new Grid
        {
            Background = PixelPalette.PanelDeep,
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Auto),
                new ColumnDefinition(GridLength.Star)
            }
        };
        Grid.SetColumn(actions, 1);
        toolbar.Children.Add(actions);

        return new Border
        {
            Background = PixelPalette.PanelDeep,
            BorderBrush = PixelPalette.Border,
            BorderThickness = new Thickness(0, 0, 0, 1),
            Padding = new Thickness(10, 8),
            Child = toolbar
        };
    }

    private void InitializeDocking()
    {
        AddDockPane(EditorDockPaneId.Hierarchy, "Hierarchy", EditorDockZoneId.Left, Scroll(_hierarchyList));
        AddDockPane(EditorDockPaneId.Scene, "Scene", EditorDockZoneId.Center, BuildScenePanel(), isRequired: true);
        AddDockPane(EditorDockPaneId.Inspector, "Inspector", EditorDockZoneId.Right, Scroll(_inspector));
        AddDockPane(EditorDockPaneId.Assets, "Assets", EditorDockZoneId.Bottom, Scroll(_assetList));
    }

    private void AddDockPane(
        EditorDockPaneId id,
        string title,
        EditorDockZoneId zoneId,
        Control content,
        bool isRequired = false)
    {
        _dockPanes.Add(id, new EditorDockPane(id, title, content, isRequired));
        EditorDockZone zone = GetDockZone(zoneId);
        zone.Panes.Add(id);
        zone.ActivePane ??= id;
    }

    private EditorDockZone GetDockZone(EditorDockZoneId id)
    {
        if (!_dockZones.TryGetValue(id, out EditorDockZone? zone))
        {
            zone = new EditorDockZone(id);
            _dockZones.Add(id, zone);
        }

        return zone;
    }

    private Control BuildDockArea()
    {
        _leftDockColumn = new ColumnDefinition(new GridLength(280)) { MinWidth = 180 };
        _leftSplitterColumn = new ColumnDefinition(new GridLength(6));
        var centerDockColumn = new ColumnDefinition(GridLength.Star) { MinWidth = 320 };
        _rightSplitterColumn = new ColumnDefinition(new GridLength(6));
        _rightDockColumn = new ColumnDefinition(new GridLength(460)) { MinWidth = 260 };
        var mainDockRow = new RowDefinition(GridLength.Star) { MinHeight = 240 };
        _bottomSplitterRow = new RowDefinition(new GridLength(6));
        _bottomDockRow = new RowDefinition(new GridLength(190)) { MinHeight = 110 };

        var dock = new Grid
        {
            Background = PixelPalette.App,
            ColumnDefinitions =
            {
                _leftDockColumn,
                _leftSplitterColumn,
                centerDockColumn,
                _rightSplitterColumn,
                _rightDockColumn
            },
            RowDefinitions =
            {
                mainDockRow,
                _bottomSplitterRow,
                _bottomDockRow
            }
        };

        _leftDockControl = BuildDockZone(EditorDockZoneId.Left);
        Grid.SetColumn(_leftDockControl, 0);
        Grid.SetRow(_leftDockControl, 0);
        Grid.SetRowSpan(_leftDockControl, 3);
        dock.Children.Add(_leftDockControl);

        Control center = BuildDockZone(EditorDockZoneId.Center);
        Grid.SetColumn(center, 2);
        Grid.SetRow(center, 0);
        dock.Children.Add(center);

        _rightDockControl = BuildDockZone(EditorDockZoneId.Right);
        Grid.SetColumn(_rightDockControl, 4);
        Grid.SetRow(_rightDockControl, 0);
        Grid.SetRowSpan(_rightDockControl, 3);
        dock.Children.Add(_rightDockControl);

        _bottomDockControl = BuildDockZone(EditorDockZoneId.Bottom);
        Grid.SetColumn(_bottomDockControl, 2);
        Grid.SetRow(_bottomDockControl, 2);
        dock.Children.Add(_bottomDockControl);

        _leftSplitter = Splitter(GridResizeDirection.Columns);
        Grid.SetColumn(_leftSplitter, 1);
        Grid.SetRow(_leftSplitter, 0);
        Grid.SetRowSpan(_leftSplitter, 3);
        dock.Children.Add(_leftSplitter);

        _rightSplitter = Splitter(GridResizeDirection.Columns);
        Grid.SetColumn(_rightSplitter, 3);
        Grid.SetRow(_rightSplitter, 0);
        Grid.SetRowSpan(_rightSplitter, 3);
        dock.Children.Add(_rightSplitter);

        _bottomSplitter = Splitter(GridResizeDirection.Rows);
        Grid.SetColumn(_bottomSplitter, 2);
        Grid.SetRow(_bottomSplitter, 1);
        dock.Children.Add(_bottomSplitter);

        RenderDockLayout();
        return dock;
    }

    private Control BuildDockZone(EditorDockZoneId zoneId)
    {
        var tabs = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 2,
            VerticalAlignment = VerticalAlignment.Center
        };

        var actions = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 2,
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Center
        };

        var header = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Auto)
            }
        };
        header.Children.Add(tabs);
        Grid.SetColumn(actions, 1);
        header.Children.Add(actions);

        var contentHost = new Grid
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
            ClipToBounds = true
        };
        Control empty = EmptyDockZone(zoneId);
        contentHost.Children.Add(empty);

        var root = new Grid
        {
            RowDefinitions =
            {
                new RowDefinition(GridLength.Auto),
                new RowDefinition(GridLength.Star)
            },
            Margin = new Thickness(4),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch
        };

        Border headerBorder = new()
        {
            Background = PixelPalette.Header,
            BorderBrush = PixelPalette.Border,
            BorderThickness = new Thickness(1, 1, 1, 0),
            Padding = new Thickness(4),
            Child = header
        };
        Grid.SetRow(headerBorder, 0);
        root.Children.Add(headerBorder);

        Border body = new()
        {
            Background = PixelPalette.Panel,
            BorderBrush = PixelPalette.Border,
            BorderThickness = new Thickness(1),
            Padding = new Thickness(6),
            Child = contentHost,
            ClipToBounds = true,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch
        };
        Grid.SetRow(body, 1);
        root.Children.Add(body);

        _dockZoneViews[zoneId] = new EditorDockZoneView(tabs, actions, contentHost, empty);
        return root;
    }

    private void RenderDockLayout()
    {
        foreach (EditorDockPane pane in _dockPanes.Values)
        {
            pane.Content.IsVisible = false;
        }

        foreach (EditorDockZoneView view in _dockZoneViews.Values)
        {
            view.Tabs.Children.Clear();
            view.Actions.Children.Clear();
            view.EmptyContent.IsVisible = false;
        }

        foreach (EditorDockZoneId zoneId in Enum.GetValues<EditorDockZoneId>())
        {
            if (!_dockZoneViews.TryGetValue(zoneId, out EditorDockZoneView? view))
            {
                continue;
            }

            EditorDockZone zone = GetDockZone(zoneId);
            NormalizeDockZone(zone);
            if (zone.ActivePane is null)
            {
                view.Tabs.Children.Add(Text(ZoneTitle(zoneId), PixelPalette.TextMuted, 11));
                view.EmptyContent.IsVisible = true;
                continue;
            }

            foreach (EditorDockPaneId paneId in zone.Panes)
            {
                EditorDockPane pane = _dockPanes[paneId];
                EnsurePaneRenderedInZone(pane, zoneId);
                pane.Content.IsVisible = paneId == zone.ActivePane;
                view.Tabs.Children.Add(DockTab(zoneId, paneId, paneId == zone.ActivePane));
            }

            EditorDockPane activePane = _dockPanes[zone.ActivePane.Value];
            BuildDockActions(view.Actions, activePane.Id);
        }

        UpdateDockZoneVisibility();
    }

    private void EnsurePaneRenderedInZone(EditorDockPane pane, EditorDockZoneId zoneId)
    {
        if (pane.RenderedZone == zoneId)
        {
            return;
        }

        if (pane.RenderedZone is not null &&
            _dockZoneViews.TryGetValue(pane.RenderedZone.Value, out EditorDockZoneView? previousView))
        {
            previousView.ContentHost.Children.Remove(pane.Content);
        }
        else
        {
            foreach (EditorDockZoneView view in _dockZoneViews.Values)
            {
                view.ContentHost.Children.Remove(pane.Content);
            }
        }

        EditorDockZoneView targetView = _dockZoneViews[zoneId];
        targetView.ContentHost.Children.Add(pane.Content);
        pane.RenderedZone = zoneId;
    }

    private void NormalizeDockZone(EditorDockZone zone)
    {
        zone.Panes.RemoveAll(paneId => !_dockPanes.ContainsKey(paneId));
        if (zone.ActivePane is not null && zone.Panes.Contains(zone.ActivePane.Value))
        {
            return;
        }

        zone.ActivePane = zone.Panes.Count > 0 ? zone.Panes[0] : null;
    }

    private Control DockTab(EditorDockZoneId zoneId, EditorDockPaneId paneId, bool active)
    {
        EditorDockPane pane = _dockPanes[paneId];
        Button tab = Button(pane.Title.ToUpperInvariant(), () =>
        {
            GetDockZone(zoneId).ActivePane = paneId;
            RenderDockLayout();
        }, active ? PixelPalette.Accent : PixelPalette.TextMuted);
        tab.Background = active ? PixelPalette.HeaderActive : PixelPalette.PanelDeep;
        tab.BorderBrush = active ? PixelPalette.Accent : PixelPalette.Border;
        tab.Padding = new Thickness(8, 4);
        tab.FontSize = 10;
        ToolTip.SetTip(tab, $"Switch to {pane.Title}");
        return tab;
    }

    private void BuildDockActions(StackPanel actions, EditorDockPaneId paneId)
    {
        EditorDockPane pane = _dockPanes[paneId];
        if (pane.IsRequired)
        {
            return;
        }

        foreach (EditorDockZoneId zoneId in Enum.GetValues<EditorDockZoneId>())
        {
            Button button = DockActionButton(ZoneButtonText(zoneId), () => MoveDockPane(paneId, zoneId));
            button.IsEnabled = FindDockZone(paneId) != zoneId;
            ToolTip.SetTip(button, $"Dock to {ZoneTitle(zoneId)}");
            actions.Children.Add(button);
        }
    }

    private Button DockActionButton(string text, Action action)
    {
        Button button = Button(text, action, PixelPalette.TextMuted);
        button.Background = PixelPalette.PanelDeep;
        button.Padding = new Thickness(6, 3);
        button.FontSize = 10;
        button.MinWidth = 24;
        return button;
    }

    private void MoveDockPane(EditorDockPaneId paneId, EditorDockZoneId targetZoneId)
    {
        EditorDockPane pane = _dockPanes[paneId];
        if (pane.IsRequired && targetZoneId != EditorDockZoneId.Center)
        {
            _workspace.SetStatus($"{pane.Title} stays in the main dock");
            UpdateStatus();
            return;
        }

        EditorDockZoneId? sourceZoneId = FindDockZone(paneId);
        if (sourceZoneId == targetZoneId)
        {
            return;
        }

        if (sourceZoneId is not null)
        {
            RememberDockZoneSize(sourceZoneId.Value);
            EditorDockZone sourceZone = GetDockZone(sourceZoneId.Value);
            sourceZone.Panes.Remove(paneId);
            if (sourceZone.ActivePane == paneId)
            {
                sourceZone.ActivePane = sourceZone.Panes.Count > 0 ? sourceZone.Panes[0] : null;
            }
        }

        EditorDockZone targetZone = GetDockZone(targetZoneId);
        if (!targetZone.Panes.Contains(paneId))
        {
            targetZone.Panes.Add(paneId);
        }

        targetZone.ActivePane = paneId;
        _workspace.SetStatus($"{pane.Title} docked to {ZoneTitle(targetZoneId)}");
        RenderDockLayout();
        UpdateStatus();
    }

    private void RememberDockZoneSize(EditorDockZoneId zoneId)
    {
        switch (zoneId)
        {
            case EditorDockZoneId.Left when _leftDockColumn is not null:
                _lastLeftWidth = RememberedLength(_leftDockColumn.Width, _leftDockColumn.ActualWidth, _lastLeftWidth);
                break;
            case EditorDockZoneId.Right when _rightDockColumn is not null:
                _lastRightWidth = RememberedLength(_rightDockColumn.Width, _rightDockColumn.ActualWidth, _lastRightWidth);
                break;
            case EditorDockZoneId.Bottom when _bottomDockRow is not null:
                _lastBottomHeight = RememberedLength(_bottomDockRow.Height, _bottomDockRow.ActualHeight, _lastBottomHeight);
                break;
        }
    }

    private void UpdateDockZoneVisibility()
    {
        SetColumnDockVisibility(
            EditorDockZoneId.Left,
            _leftDockColumn,
            _leftSplitterColumn,
            _leftDockControl,
            _leftSplitter,
            180,
            ref _lastLeftWidth);

        SetColumnDockVisibility(
            EditorDockZoneId.Right,
            _rightDockColumn,
            _rightSplitterColumn,
            _rightDockControl,
            _rightSplitter,
            260,
            ref _lastRightWidth);

        SetRowDockVisibility(
            EditorDockZoneId.Bottom,
            _bottomDockRow,
            _bottomSplitterRow,
            _bottomDockControl,
            _bottomSplitter,
            110,
            ref _lastBottomHeight);
    }

    private void SetColumnDockVisibility(
        EditorDockZoneId zoneId,
        ColumnDefinition? dockColumn,
        ColumnDefinition? splitterColumn,
        Control? dockControl,
        Control? splitter,
        double minWidth,
        ref GridLength lastWidth)
    {
        if (dockColumn is null || splitterColumn is null || dockControl is null || splitter is null)
        {
            return;
        }

        bool visible = GetDockZone(zoneId).Panes.Count > 0;
        if (visible)
        {
            dockColumn.MinWidth = minWidth;
            dockColumn.Width = NonCollapsedLength(lastWidth, new GridLength(minWidth));
            splitterColumn.Width = new GridLength(6);
            dockControl.IsVisible = true;
            splitter.IsVisible = true;
            return;
        }

        RememberDockZoneSize(zoneId);
        dockControl.IsVisible = false;
        splitter.IsVisible = false;
        dockColumn.MinWidth = 0;
        dockColumn.Width = new GridLength(0);
        splitterColumn.Width = new GridLength(0);
    }

    private void SetRowDockVisibility(
        EditorDockZoneId zoneId,
        RowDefinition? dockRow,
        RowDefinition? splitterRow,
        Control? dockControl,
        Control? splitter,
        double minHeight,
        ref GridLength lastHeight)
    {
        if (dockRow is null || splitterRow is null || dockControl is null || splitter is null)
        {
            return;
        }

        bool visible = GetDockZone(zoneId).Panes.Count > 0;
        if (visible)
        {
            dockRow.MinHeight = minHeight;
            dockRow.Height = NonCollapsedLength(lastHeight, new GridLength(minHeight));
            splitterRow.Height = new GridLength(6);
            dockControl.IsVisible = true;
            splitter.IsVisible = true;
            return;
        }

        RememberDockZoneSize(zoneId);
        dockControl.IsVisible = false;
        splitter.IsVisible = false;
        dockRow.MinHeight = 0;
        dockRow.Height = new GridLength(0);
        splitterRow.Height = new GridLength(0);
    }

    private static GridLength NonCollapsedLength(GridLength preferred, GridLength fallback)
    {
        if (preferred.IsAbsolute && preferred.Value <= 1)
        {
            return fallback;
        }

        return preferred;
    }

    private static GridLength RememberedLength(GridLength current, double actual, GridLength previous)
    {
        if (current.IsAbsolute && current.Value > 1)
        {
            return current;
        }

        if (actual > 1)
        {
            return new GridLength(actual);
        }

        return previous;
    }

    private EditorDockZoneId? FindDockZone(EditorDockPaneId paneId)
    {
        foreach (KeyValuePair<EditorDockZoneId, EditorDockZone> pair in _dockZones)
        {
            if (pair.Value.Panes.Contains(paneId))
            {
                return pair.Key;
            }
        }

        return null;
    }

    private static Control EmptyDockZone(EditorDockZoneId zoneId)
    {
        return new Border
        {
            Background = PixelPalette.PanelDeep,
            BorderBrush = PixelPalette.Border,
            BorderThickness = new Thickness(1),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
            Child = new TextBlock
            {
                Text = ZoneTitle(zoneId).ToUpperInvariant(),
                Foreground = PixelPalette.TextMuted,
                FontFamily = PixelFont,
                FontSize = 11,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            }
        };
    }

    private static Control Splitter(GridResizeDirection direction)
    {
        bool columns = direction == GridResizeDirection.Columns;
        return new GridSplitter
        {
            ResizeDirection = direction,
            ResizeBehavior = GridResizeBehavior.PreviousAndNext,
            ShowsPreview = false,
            Background = PixelPalette.Border,
            Width = columns ? 6 : double.NaN,
            Height = columns ? double.NaN : 6,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch
        };
    }

    private static string ZoneButtonText(EditorDockZoneId zoneId)
    {
        return zoneId switch
        {
            EditorDockZoneId.Left => "L",
            EditorDockZoneId.Center => "C",
            EditorDockZoneId.Right => "R",
            EditorDockZoneId.Bottom => "B",
            _ => "?"
        };
    }

    private static string ZoneTitle(EditorDockZoneId zoneId)
    {
        return zoneId switch
        {
            EditorDockZoneId.Left => "Left",
            EditorDockZoneId.Center => "Center",
            EditorDockZoneId.Right => "Right",
            EditorDockZoneId.Bottom => "Bottom",
            _ => "Dock"
        };
    }

    private Control BuildScenePanel()
    {
        return new Border
        {
            Background = PixelPalette.PanelDeep,
            BorderBrush = PixelPalette.Border,
            BorderThickness = new Thickness(1),
            Margin = new Thickness(4),
            Child = BuildSceneViewportHost()
        };
    }

    private Control BuildSceneViewportHost()
    {
        var viewportHost = new Grid
        {
            Background = Brushes.Transparent,
            Focusable = true,
            Cursor = new Cursor(StandardCursorType.Cross),
            Children =
            {
                _sceneViewport
            }
        };
        _sceneViewport.AttachInputLayer(viewportHost);
        return viewportHost;
    }

    private void RefreshAll(bool reloadScene = false)
    {
        RefreshHierarchy();
        RefreshAssetsList();
        RefreshInspector();
        UpdateStatus();
        if (reloadScene)
        {
            _sceneViewport.ReloadPreviewScene();
        }
        else
        {
            _sceneViewport.UpdateOverlay(_workspace);
        }
    }

    private void RefreshHierarchy()
    {
        _hierarchyList.Children.Clear();
        foreach (EditorSceneNode node in _workspace.Scene.FlatNodes)
        {
            Button row = RowButton(
                node.DisplayName,
                ReferenceEquals(_workspace.Selection.SceneNode, node),
                () =>
                {
                    _workspace.SelectSceneObject(node);
                    ResetAddComponentPicker();
                    RefreshAll();
                });
            row.Padding = new Thickness(8 + node.Depth * 14, 5, 8, 5);
            _hierarchyList.Children.Add(row);
        }
    }

    private void RefreshAssetsList()
    {
        _assetList.Children.Clear();
        foreach (EditorAssetEntry asset in _workspace.Assets.Assets)
        {
            _assetList.Children.Add(RowButton(
                $"{asset.Kind}  {asset.Name}",
                _workspace.Selection.Asset == asset,
                () =>
                {
                    _workspace.SelectAsset(asset);
                    ResetAddComponentPicker();
                    RefreshAll();
                }));
        }
    }

    private void RefreshInspector()
    {
        _inspector.Children.Clear();
        switch (_workspace.Selection.Kind)
        {
            case EditorSelectionKind.SceneObject when _workspace.Selection.SceneNode is { } node:
                DrawSceneObjectInspector(node);
                break;
            case EditorSelectionKind.Asset when _workspace.Selection.Asset is { } asset:
                DrawAssetInspector(asset);
                break;
            default:
                _inspector.Children.Add(Text("No selection", PixelPalette.TextMuted, 13));
                break;
        }
    }

    private void DrawSceneObjectInspector(EditorSceneNode node)
    {
        _inspector.Children.Add(Text(node.DisplayName, PixelPalette.Text, 18));

        for (int i = 0; i < node.Object.Components.Count; i++)
        {
            DrawComponentInspector(node, i, node.Object.Components[i]);
        }

        _inspector.Children.Add(BuildAddComponentPanel(node));
    }

    private void DrawAssetInspector(EditorAssetEntry asset)
    {
        _inspector.Children.Add(Text(asset.Name, PixelPalette.Text, 18));
        _inspector.Children.Add(KeyValue("Kind", asset.Kind.ToString()));
        _inspector.Children.Add(KeyValue("Path", asset.RelativePath));
        _inspector.Children.Add(KeyValue("Size", $"{asset.SizeBytes:N0} bytes"));
        _inspector.Children.Add(KeyValue("Full", asset.FullPath));
    }

    private void DrawComponentInspector(EditorSceneNode node, int componentIndex, IComponent component)
    {
        Type type = component.GetType();
        bool removable = !EditorComponentCatalog.IsProtectedComponent(type);
        _inspector.Children.Add(ComponentHeader(
            type.Name,
            removable ? () => RemoveComponent(node, componentIndex, type.Name) : null));

        FieldInfo[] fields = GetEditableFields(type);
        if (fields.Length == 0)
        {
            _inspector.Children.Add(Text("No editable fields", PixelPalette.TextMuted, 11));
            return;
        }

        foreach (FieldInfo field in fields)
        {
            object? value = field.GetValue(component);
            _inspector.Children.Add(FieldRow(
                field.Name,
                BuildValueEditor(
                    field.FieldType,
                    value,
                    updated => CommitComponentField(node, componentIndex, field, updated))));
        }
    }

    private Control BuildAddComponentPanel(EditorSceneNode node)
    {
        var panel = new StackPanel
        {
            Spacing = 6,
            Margin = new Thickness(0, 8, 0, 0)
        };

        panel.Children.Add(Button("ADD COMPONENT", () =>
        {
            _showAddComponentPicker = !_showAddComponentPicker;
            RefreshInspector();
        }, PixelPalette.Accent));

        if (!_showAddComponentPicker)
        {
            return panel;
        }

        TextBox search = EditorTextInput(_componentSearch, text =>
        {
            _componentSearch = text;
            return true;
        });
        search.PlaceholderText = "Search components";
        panel.Children.Add(search);

        var results = new StackPanel { Spacing = 2 };
        void RebuildResults()
        {
            results.Children.Clear();
            EditorComponentDescriptor[] descriptors = EditorComponentCatalog
                .GetAddableComponents(node, _componentSearch)
                .Take(80)
                .ToArray();

            if (descriptors.Length == 0)
            {
                results.Children.Add(Text("No components found", PixelPalette.TextMuted, 11));
                return;
            }

            foreach (EditorComponentDescriptor descriptor in descriptors)
            {
                results.Children.Add(RowButton(
                    FormatComponentOption(descriptor),
                    false,
                    () => AddComponent(node, descriptor)));
            }
        }

        search.TextChanged += (_, _) =>
        {
            _componentSearch = search.Text ?? string.Empty;
            RebuildResults();
        };
        RebuildResults();
        panel.Children.Add(results);

        return panel;
    }

    private void AddComponent(EditorSceneNode node, EditorComponentDescriptor descriptor)
    {
        IComponent component = EditorComponentCatalog.CreateDefault(descriptor.Type, node.DisplayName);
        bool added = _workspace.Scene.AddComponent(node, component);
        _workspace.SetStatus(added
            ? $"{descriptor.DisplayName} added"
            : $"{descriptor.DisplayName} already exists");
        if (added)
        {
            ResetAddComponentPicker();
        }

        RefreshAll(reloadScene: added);
    }

    private void RemoveComponent(EditorSceneNode node, int componentIndex, string componentName)
    {
        if ((uint)componentIndex >= (uint)node.Object.Components.Count)
        {
            _workspace.SetStatus($"{componentName} not found");
            RefreshAll();
            return;
        }

        Type type = node.Object.Components[componentIndex].GetType();
        if (EditorComponentCatalog.IsProtectedComponent(type))
        {
            _workspace.SetStatus($"{componentName} is required");
            RefreshAll();
            return;
        }

        bool removed = _workspace.Scene.RemoveComponent(node, componentIndex);
        _workspace.SetStatus(removed ? $"{componentName} removed" : $"{componentName} not found");
        RefreshAll(reloadScene: removed);
    }

    private bool CommitComponentField(
        EditorSceneNode node,
        int componentIndex,
        FieldInfo field,
        object? value)
    {
        if ((uint)componentIndex >= (uint)node.Object.Components.Count)
        {
            _workspace.SetStatus("Component not found");
            UpdateStatus();
            return false;
        }

        try
        {
            object boxed = node.Object.Components[componentIndex];
            field.SetValue(boxed, value);
            if (boxed is not IComponent component)
            {
                _workspace.SetStatus($"{field.Name} is not a component value");
                UpdateStatus();
                return false;
            }

            bool updated = _workspace.Scene.ReplaceComponent(node, componentIndex, component);
            _workspace.SetStatus(updated ? $"{field.Name} updated" : $"{field.Name} not found");
            RefreshAll(reloadScene: updated);
            return updated;
        }
        catch (Exception exception)
        {
            _workspace.SetStatus($"{field.Name}: {exception.Message}");
            UpdateStatus();
            return false;
        }
    }

    private Control BuildValueEditor(Type type, object? value, Func<object?, bool> commit)
    {
        Type? nullableType = Nullable.GetUnderlyingType(type);
        if (nullableType is not null)
        {
            return BuildNullableValueEditor(nullableType, value, commit);
        }

        if (type == typeof(string))
        {
            return EditorTextInput(value as string ?? string.Empty, text => commit(text));
        }

        if (type == typeof(int))
        {
            return EditorTextInput(FormatValue(value), text =>
            {
                if (!int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed))
                {
                    return false;
                }

                return commit(parsed);
            });
        }

        if (type == typeof(bool))
        {
            var checkBox = new CheckBox
            {
                IsChecked = value is bool boolValue && boolValue,
                Foreground = PixelPalette.Text,
                FontFamily = PixelFont,
                FontSize = 11
            };
            checkBox.PropertyChanged += (_, args) =>
            {
                if (args.Property == ToggleButton.IsCheckedProperty)
                {
                    commit(checkBox.IsChecked == true);
                }
            };
            return checkBox;
        }

        if (type == typeof(Fix))
        {
            return EditorTextInput(FormatFix(value is Fix fix ? fix : Fix.Zero), text =>
            {
                if (!TryParseFix(text, out Fix parsed))
                {
                    return false;
                }

                return commit(parsed);
            });
        }

        if (type == typeof(Vector3))
        {
            Vector3 vector = value is Vector3 typed ? typed : Vector3.Zero;
            return BuildFixTupleEditor(
                ["X", "Y", "Z"],
                [vector.X, vector.Y, vector.Z],
                values => commit(new Vector3(values[0], values[1], values[2])));
        }

        if (type == typeof(Quaternion))
        {
            Quaternion quaternion = value is Quaternion typed ? typed : Quaternion.Identity;
            return BuildFixTupleEditor(
                ["X", "Y", "Z", "W"],
                [quaternion.X, quaternion.Y, quaternion.Z, quaternion.W],
                values => commit(new Quaternion(values[0], values[1], values[2], values[3])));
        }

        if (type == typeof(PixelColor))
        {
            return BuildPixelColorEditor(value is PixelColor color ? color : PixelColor.FromRgb(255, 255, 255), commit);
        }

        if (type.IsEnum)
        {
            Array values = Enum.GetValues(type);
            var combo = new ComboBox
            {
                ItemsSource = values,
                SelectedItem = value,
                Background = PixelPalette.PanelDeep,
                Foreground = PixelPalette.Text,
                BorderBrush = PixelPalette.Border,
                FontFamily = PixelFont,
                FontSize = 11,
                MinHeight = 28
            };
            combo.SelectionChanged += (_, _) =>
            {
                if (combo.SelectedItem is not null)
                {
                    commit(combo.SelectedItem);
                }
            };
            return combo;
        }

        if (type.IsValueType && GetEditableFields(type).Length > 0)
        {
            return BuildNestedStructEditor(type, value ?? CreateDefaultValue(type), commit);
        }

        TextBox fallback = EditorTextInput(FormatValue(value), _ => false);
        fallback.IsReadOnly = true;
        fallback.IsEnabled = false;
        return fallback;
    }

    private Control BuildNullableValueEditor(Type type, object? value, Func<object?, bool> commit)
    {
        var panel = new StackPanel { Spacing = 5 };
        var enabled = new CheckBox
        {
            Content = "Enabled",
            IsChecked = value is not null,
            Foreground = PixelPalette.Text,
            FontFamily = PixelFont,
            FontSize = 11
        };
        enabled.PropertyChanged += (_, args) =>
        {
            if (args.Property != ToggleButton.IsCheckedProperty)
            {
                return;
            }

            commit(enabled.IsChecked == true ? CreateDefaultValue(type) : null);
        };
        panel.Children.Add(enabled);

        if (value is not null)
        {
            panel.Children.Add(BuildValueEditor(type, value, commit));
        }

        return panel;
    }

    private Control BuildNestedStructEditor(Type type, object value, Func<object?, bool> commit)
    {
        var panel = new StackPanel { Spacing = 5 };
        foreach (FieldInfo field in GetEditableFields(type))
        {
            object? fieldValue = field.GetValue(value);
            panel.Children.Add(FieldRow(
                field.Name,
                BuildValueEditor(
                    field.FieldType,
                    fieldValue,
                    updated => commit(WithStructFieldValue(type, value, field, updated)))));
        }

        return panel;
    }

    private Control BuildFixTupleEditor(string[] labels, Fix[] values, Func<Fix[], bool> commit)
    {
        var grid = new Grid
        {
            ColumnSpacing = 4,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
        for (int i = 0; i < labels.Length; i++)
        {
            grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star) { MinWidth = 46 });
        }

        for (int i = 0; i < labels.Length; i++)
        {
            int index = i;
            var column = new StackPanel
            {
                Spacing = 2,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Children =
                {
                    Text(labels[i], PixelPalette.Accent, 10),
                    EditorTextInput(FormatFix(values[i]), text =>
                    {
                        if (!TryParseFix(text, out Fix parsed))
                        {
                            return false;
                        }

                        Fix[] updated = (Fix[])values.Clone();
                        updated[index] = parsed;
                        return commit(updated);
                    }, 0)
                }
            };
            Grid.SetColumn(column, i);
            grid.Children.Add(column);
        }

        return grid;
    }

    private Control BuildPixelColorEditor(PixelColor color, Func<object?, bool> commit)
    {
        var grid = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(new GridLength(30)),
                new ColumnDefinition(GridLength.Star)
            },
            ColumnSpacing = 6
        };

        var swatch = new Border
        {
            Width = 24,
            Height = 24,
            BorderBrush = PixelPalette.Border,
            BorderThickness = new Thickness(1),
            Background = new SolidColorBrush(Color.FromRgb(color.R, color.G, color.B))
        };
        Grid.SetColumn(swatch, 0);
        grid.Children.Add(swatch);

        TextBox input = EditorTextInput(color.ToHex(), text =>
        {
            if (!PixelColor.TryParse(text, out PixelColor parsed))
            {
                return false;
            }

            return commit(parsed);
        });
        Grid.SetColumn(input, 1);
        grid.Children.Add(input);
        return grid;
    }

    private static object CreateDefaultValue(Type type)
    {
        if (type == typeof(string))
        {
            return string.Empty;
        }

        if (type == typeof(Fix))
        {
            return Fix.Zero;
        }

        if (type == typeof(Vector3))
        {
            return Vector3.Zero;
        }

        if (type == typeof(Quaternion))
        {
            return Quaternion.Identity;
        }

        if (type == typeof(PixelColor))
        {
            return PixelColor.FromRgb(255, 255, 255);
        }

        if (type == typeof(MaterialTexture))
        {
            return new MaterialTexture(string.Empty);
        }

        if (type == typeof(MaterialTransparency))
        {
            return new MaterialTransparency(Fix.One);
        }

        if (type.IsEnum)
        {
            Array values = Enum.GetValues(type);
            return values.Length > 0 ? values.GetValue(0)! : Activator.CreateInstance(type)!;
        }

        return Activator.CreateInstance(type)!;
    }

    private static object WithStructFieldValue(Type type, object value, FieldInfo field, object? updatedValue)
    {
        if (type == typeof(MaterialTexture))
        {
            var texture = (MaterialTexture)value;
            return new MaterialTexture(
                field.Name == nameof(MaterialTexture.Asset) ? (string?)updatedValue ?? string.Empty : texture.Asset,
                field.Name == nameof(MaterialTexture.TilingX) && updatedValue is Fix tilingX ? tilingX : texture.TilingX,
                field.Name == nameof(MaterialTexture.TilingY) && updatedValue is Fix tilingY ? tilingY : texture.TilingY,
                field.Name == nameof(MaterialTexture.OffsetX) && updatedValue is Fix offsetX ? offsetX : texture.OffsetX,
                field.Name == nameof(MaterialTexture.OffsetY) && updatedValue is Fix offsetY ? offsetY : texture.OffsetY);
        }

        if (type == typeof(MaterialTransparency))
        {
            var transparency = (MaterialTransparency)value;
            return new MaterialTransparency(
                field.Name == nameof(MaterialTransparency.Opacity) && updatedValue is Fix opacity ? opacity : transparency.Opacity,
                field.Name == nameof(MaterialTransparency.AlphaCutoff) && updatedValue is Fix alphaCutoff ? alphaCutoff : transparency.AlphaCutoff);
        }

        object boxed = value;
        field.SetValue(boxed, updatedValue);
        return boxed;
    }

    private static FieldInfo[] GetEditableFields(Type type)
    {
        return type
            .GetFields(BindingFlags.Public | BindingFlags.Instance)
            .Where(field => field.GetCustomAttribute<JsonIgnoreAttribute>() is null)
            .OrderBy(field => field.MetadataToken)
            .ToArray();
    }

    private static bool TryParseFix(string value, out Fix fix)
    {
        string normalized = value.Trim().Replace(',', '.');
        if (!double.TryParse(
            normalized,
            NumberStyles.Float,
            CultureInfo.InvariantCulture,
            out double parsed))
        {
            fix = Fix.Zero;
            return false;
        }

        fix = Fix.FromDouble(parsed);
        return true;
    }

    private static string FormatFix(Fix value)
    {
        return ((double)value).ToString("0.###", CultureInfo.InvariantCulture);
    }

    private static string FormatComponentOption(EditorComponentDescriptor descriptor)
    {
        string namespaceName = descriptor.NamespaceName;
        int featuresIndex = namespaceName.IndexOf(".Features.", StringComparison.Ordinal);
        if (featuresIndex >= 0)
        {
            namespaceName = namespaceName[(featuresIndex + ".Features.".Length)..];
        }

        namespaceName = namespaceName.Replace(".Components", string.Empty, StringComparison.Ordinal);
        return string.IsNullOrWhiteSpace(namespaceName)
            ? descriptor.DisplayName
            : $"{descriptor.DisplayName}  {namespaceName}";
    }

    private void ResetAddComponentPicker()
    {
        _showAddComponentPicker = false;
        _componentSearch = string.Empty;
    }

    private void OnSceneViewportStatusChanged(string status)
    {
        _workspace.SetStatus(status);
        UpdateStatus();
    }

    private void PlayGame()
    {
        bool started = _game.Play(_workspace);
        _workspace.SetStatus(started
            ? "Game window: running"
            : $"Game window: failed ({_game.LastError})");
        UpdateStatus();
    }

    private void StopGame()
    {
        _game.Stop();
        _workspace.SetStatus("Game window: stopped");
        UpdateStatus();
    }

    private void SaveScene()
    {
        _workspace.SaveScene();
        RefreshAll();
    }

    private void RefreshAssets()
    {
        _workspace.RefreshAssets();
        RefreshAll();
    }

    private void UpdateStatus()
    {
        _status.Text = _workspace.Status;
    }

    private static ScrollViewer Scroll(Control content)
    {
        content.HorizontalAlignment = HorizontalAlignment.Stretch;
        content.VerticalAlignment = VerticalAlignment.Top;

        var scroll = new ScrollViewer
        {
            Content = content,
            VerticalScrollBarVisibility = ScrollBarVisibility.Visible,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            AllowAutoHide = false,
            BringIntoViewOnFocusChange = true,
            IsScrollChainingEnabled = false,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
            ClipToBounds = true,
            Focusable = true
        };
        scroll.AddHandler(
            InputElement.PointerWheelChangedEvent,
            (_, args) => ScrollByWheel(scroll, args),
            RoutingStrategies.Tunnel,
            handledEventsToo: true);
        content.AddHandler(
            InputElement.PointerWheelChangedEvent,
            (_, args) => ScrollByWheel(scroll, args),
            RoutingStrategies.Bubble,
            handledEventsToo: true);
        return scroll;
    }

    private static void ScrollByWheel(ScrollViewer scroll, PointerWheelEventArgs args)
    {
        if (args.Handled)
        {
            return;
        }

        if (Math.Abs(args.Delta.Y) <= double.Epsilon)
        {
            return;
        }

        double maxOffsetY = Math.Max(0, scroll.Extent.Height - scroll.Viewport.Height);
        if (maxOffsetY <= 0)
        {
            return;
        }

        double nextY = Math.Clamp(scroll.Offset.Y - args.Delta.Y * 64, 0, maxOffsetY);
        if (Math.Abs(nextY - scroll.Offset.Y) <= double.Epsilon)
        {
            return;
        }

        scroll.Offset = new Vector(scroll.Offset.X, nextY);
        args.Handled = true;
    }

    private static Control Panel(string title, Control content)
    {
        var root = new Grid
        {
            RowDefinitions =
            {
                new RowDefinition(GridLength.Auto),
                new RowDefinition(GridLength.Star)
            },
            Margin = new Thickness(4),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch
        };

        Border header = new()
        {
            Background = PixelPalette.Header,
            BorderBrush = PixelPalette.Border,
            BorderThickness = new Thickness(1, 1, 1, 0),
            Padding = new Thickness(8, 6),
            Child = Text(title.ToUpperInvariant(), PixelPalette.Text, 13)
        };
        Grid.SetRow(header, 0);
        root.Children.Add(header);

        Border body = new()
        {
            Background = PixelPalette.Panel,
            BorderBrush = PixelPalette.Border,
            BorderThickness = new Thickness(1),
            Padding = new Thickness(6),
            Child = content,
            ClipToBounds = true,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch
        };
        Grid.SetRow(body, 1);
        root.Children.Add(body);

        return root;
    }

    private static Button Button(string text, Action action, IBrush? accent = null)
    {
        var button = new Button
        {
            Content = text,
            Background = PixelPalette.Panel,
            Foreground = accent ?? PixelPalette.Text,
            BorderBrush = accent ?? PixelPalette.Border,
            BorderThickness = new Thickness(1),
            Padding = new Thickness(10, 6),
            FontSize = 12,
            FontFamily = PixelFont
        };
        button.Click += (_, _) => action();
        return button;
    }

    private static Button RowButton(string text, bool selected, Action action)
    {
        var button = Button(text, action, selected ? PixelPalette.Accent : PixelPalette.Text);
        button.Background = selected ? PixelPalette.Selection : PixelPalette.PanelDeep;
        button.HorizontalContentAlignment = HorizontalAlignment.Left;
        button.BorderBrush = selected ? PixelPalette.Accent : PixelPalette.Border;
        return button;
    }

    private static TextBlock TitleText(string value)
    {
        return new TextBlock
        {
            Text = value,
            Foreground = PixelPalette.Accent,
            FontSize = 18,
            FontWeight = FontWeight.Bold,
            FontFamily = PixelFont,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 14, 0)
        };
    }

    private static TextBlock Text(string value, IBrush brush, double size)
    {
        return new TextBlock
        {
            Text = value,
            Foreground = brush,
            FontSize = size,
            FontFamily = PixelFont,
            TextWrapping = TextWrapping.Wrap
        };
    }

    private static Control ComponentHeader(string value, Action? remove)
    {
        var grid = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Auto)
            }
        };

        grid.Children.Add(Text(value, PixelPalette.AccentWarm, 13));
        if (remove is not null)
        {
            Button removeButton = Button("REMOVE", remove, PixelPalette.Warning);
            removeButton.Padding = new Thickness(6, 3);
            removeButton.FontSize = 10;
            Grid.SetColumn(removeButton, 1);
            grid.Children.Add(removeButton);
        }

        return new Border
        {
            Background = PixelPalette.HeaderActive,
            BorderBrush = PixelPalette.Border,
            BorderThickness = new Thickness(1),
            Padding = new Thickness(8, 5),
            Child = grid
        };
    }

    private static Control FieldRow(string key, Control editor)
    {
        var grid = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(new GridLength(116)),
                new ColumnDefinition(GridLength.Star)
            },
            ColumnSpacing = 8,
            Margin = new Thickness(0, 1, 0, 1)
        };

        TextBlock label = Text(key, PixelPalette.Accent, 11);
        label.VerticalAlignment = VerticalAlignment.Center;
        Grid.SetColumn(label, 0);
        grid.Children.Add(label);

        editor.HorizontalAlignment = HorizontalAlignment.Stretch;
        Grid.SetColumn(editor, 1);
        grid.Children.Add(editor);
        return grid;
    }

    private static TextBox EditorTextInput(string value, Func<string, bool> commit, double minWidth = 112)
    {
        TextBlock visibleValue = Text(value, PixelPalette.Text, 11);
        visibleValue.IsHitTestVisible = false;

        var input = new TextBox
        {
            Text = value,
            Background = PixelPalette.PanelDeep,
            Foreground = Brushes.Transparent,
            BorderBrush = PixelPalette.Border,
            BorderThickness = new Thickness(1),
            Padding = new Thickness(6, 4),
            FontFamily = PixelFont,
            FontSize = 11,
            CaretBrush = PixelPalette.Accent,
            SelectionBrush = PixelPalette.Selection,
            SelectionForegroundBrush = PixelPalette.Text,
            PlaceholderForeground = PixelPalette.TextMuted,
            TextAlignment = TextAlignment.Left,
            TextWrapping = TextWrapping.NoWrap,
            HorizontalContentAlignment = HorizontalAlignment.Stretch,
            VerticalContentAlignment = VerticalAlignment.Center,
            MaxLines = 1,
            MinWidth = minWidth,
            MinHeight = 32,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            InnerLeftContent = visibleValue
        };

        void ShowEditorValue()
        {
            visibleValue.IsVisible = false;
            input.Foreground = PixelPalette.Text;
        }

        void ShowDisplayValue()
        {
            visibleValue.Text = input.Text ?? string.Empty;
            visibleValue.IsVisible = true;
            input.Foreground = Brushes.Transparent;
        }

        input.GotFocus += (_, _) => ShowEditorValue();
        input.TextChanged += (_, _) =>
        {
            if (!input.IsFocused)
            {
                visibleValue.Text = input.Text ?? string.Empty;
            }
        };

        bool committed = false;
        void Apply()
        {
            if (committed)
            {
                return;
            }

            string text = input.Text ?? string.Empty;
            if (text == value)
            {
                return;
            }

            if (commit(text))
            {
                committed = true;
            }
            else
            {
                input.Text = value;
            }
        }

        input.LostFocus += (_, _) =>
        {
            Apply();
            ShowDisplayValue();
        };
        input.KeyDown += (_, args) =>
        {
            if (args.Key == Key.Enter)
            {
                Apply();
                args.Handled = true;
            }
        };
        return input;
    }

    private static Control KeyValue(string key, string value)
    {
        return new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(new GridLength(120)),
                new ColumnDefinition(GridLength.Star)
            },
            Children =
            {
                KeyValueText(key, PixelPalette.Accent),
                KeyValueText(value, PixelPalette.TextMuted, 1)
            }
        };
    }

    private static TextBlock KeyValueText(string value, IBrush brush, int column = 0)
    {
        var text = Text(value, brush, 12);
        Grid.SetColumn(text, column);
        return text;
    }

    private static string FormatValue(object? value)
    {
        return value switch
        {
            null => "null",
            string text => text,
            Fix fix => ((double)fix).ToString("0.###"),
            Vector3 vector => $"({(double)vector.X:0.##}, {(double)vector.Y:0.##}, {(double)vector.Z:0.##})",
            Quaternion quaternion => $"({(double)quaternion.X:0.##}, {(double)quaternion.Y:0.##}, {(double)quaternion.Z:0.##}, {(double)quaternion.W:0.##})",
            PixelColor color => color.ToHex(),
            MaterialTexture texture => $"{texture.Asset} x{(double)texture.TilingX:0.##}/{(double)texture.TilingY:0.##}",
            MaterialTransparency transparency => $"opacity {(double)transparency.Opacity:0.##}",
            Enum enumValue => enumValue.ToString(),
            _ => value.ToString() ?? string.Empty
        };
    }

    private enum EditorDockPaneId
    {
        Hierarchy,
        Scene,
        Inspector,
        Assets
    }

    private enum EditorDockZoneId
    {
        Left,
        Center,
        Right,
        Bottom
    }

    private sealed class EditorDockPane
    {
        public EditorDockPane(EditorDockPaneId id, string title, Control content, bool isRequired)
        {
            Id = id;
            Title = title;
            Content = content;
            IsRequired = isRequired;
        }

        public EditorDockPaneId Id { get; }
        public string Title { get; }
        public Control Content { get; }
        public bool IsRequired { get; }
        public EditorDockZoneId? RenderedZone { get; set; }
    }

    private sealed class EditorDockZone
    {
        public EditorDockZone(EditorDockZoneId id)
        {
            Id = id;
        }

        public EditorDockZoneId Id { get; }
        public List<EditorDockPaneId> Panes { get; } = [];
        public EditorDockPaneId? ActivePane { get; set; }
    }

    private sealed class EditorDockZoneView
    {
        public EditorDockZoneView(StackPanel tabs, StackPanel actions, Grid contentHost, Control emptyContent)
        {
            Tabs = tabs;
            Actions = actions;
            ContentHost = contentHost;
            EmptyContent = emptyContent;
        }

        public StackPanel Tabs { get; }
        public StackPanel Actions { get; }
        public Grid ContentHost { get; }
        public Control EmptyContent { get; }
    }
}
