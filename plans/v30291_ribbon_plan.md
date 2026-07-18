# v3.0.29.1 — Üst Ribbon: Sekmeli Komut Şeridi + Quick Access

**Plan tarihi:** 2026-07-18  
**Kaynak:** [`Roadmap.md>FAZ D2>v3.0.29.1`](../../.gemini/antigravity/scratch/TrainService/Roadmap.md:579)  
**Test kimlik bloğu:** T330–T335  

---

## 1. Mevcut Durum Analizi

### 1.1. Mevcut Toolbar (KALDIRILACAK)

[`EditorView.xaml`](../../.gemini/antigravity/scratch/TrainService/src/TrainService.App/Views/Pages/EditorView.xaml:75) şu anda sağ-üst köşede yüzen (`ZIndex=1000`) bir `StackPanel` toolbar içeriyor:
- Select, Track, Route, Switch, Save, Undo, Redo butonları

Bu toolbar tamamen kaldırılacak, yerine **üst şerit (ribbon)** + **Quick Access bar** konulacak.

### 1.2. Mevcut Klavye Kısayolları (KORUNACAK)

[`EditorView.xaml.cs`](../../.gemini/antigravity/scratch/TrainService/src/TrainService.App/Views/Pages/EditorView.xaml.cs:20) `PreviewKeyDown` handler'ı:
- `S` → SetTool("Select"), `T` → SetTool("Track"), `R` → SetTool("Route"), `F8` → SetTool("Switch")
- Diğer tuşlar → `Viewport.ToolController.KeyDown(e.Key)`'e yönlendirilir

[`EditorView.xaml`](../../.gemini/antigravity/scratch/TrainService/src/TrainService.App/Views/Pages/EditorView.xaml:10) `InputBindings`:
- `Ctrl+Z` → UndoCommand, `Ctrl+Y` → RedoCommand, `Ctrl+S` → SaveCommand, `F9` → ToggleSnapCommand

### 1.3. Mevcut EditorViewModel Komutları

[`EditorViewModel.cs`](../../.gemini/antigravity/scratch/TrainService/src/TrainService.App/ViewModels/EditorViewModel.cs:14):
- `SaveCommand`, `UndoCommand`, `RedoCommand`, `SetToolCommand(string)`, `ToggleSnapCommand`, `DebugAddLineCommand`
- `ToolChangeRequested` → `Action<string>` delegate
- EKSİK: `DeleteCommand`, `CopyCommand`, `CutCommand`, `PasteCommand`, `ActiveToolName` özelliği

### 1.4. Mevcut Araçlar

| Parametre | Sınıf | Kısayol |
|-----------|-------|---------|
| "Select" | `SelectTool` | S |
| "Track" | `TrackTool` | T |
| "Route" | `RouteTool` | R |
| "Switch" | `SwitchTool` | F8 |
| (yok) | `HybridTool` | H (eklenmeli) |
| (yok) | `RampTool` | (eklenmeli) |

---

## 2. Yeni/Değişen Dosyalar

```
src/TrainService.App/
├── Controls/
│   └── Ribbon/
│       ├── RibbonDefinition.cs      (YENİ — veri modeli)
│       ├── RibbonControl.xaml        (YENİ — WPF user control)
│       └── RibbonControl.xaml.cs     (YENİ — code-behind)
├── ViewModels/
│   └── EditorViewModel.cs           (DEĞİŞECEK — yeni komutlar + ActiveToolName)
├── Views/
│   └── Pages/
│       ├── EditorView.xaml           (DEĞİŞECEK — ribbon entegrasyonu)
│       └── EditorView.xaml.cs        (DEĞİŞECEK — Hybrid + Ramp tuş eşlemesi)
tests/TrainService.App.Tests/
└── T330_T335_RibbonTests.cs         (YENİ — ViewModel testleri)
```

---

## 3. Detaylı Tasarım

### 3.1. `RibbonDefinition.cs` — VERİ MODELİ

```
📁 src/TrainService.App/Controls/Ribbon/RibbonDefinition.cs
```

```csharp
namespace TrainService.App.Controls.Ribbon;

// ———— Data Models ————

public sealed record RibbonItem(
    string Id,                  // e.g. "Select", "Save", "Undo"
    string Label,               // "Seç", "Kaydet", "Geri Al"
    string IconSymbol,          // Fluent System Icon name e.g. "SelectObject24"
    string? ShortcutText,       // "(S)", "(Ctrl+S)", "(Del)"
    string? CommandName,        // ViewModel relay command name
    string? CommandParameter,   // Optional parameter for SetTool etc.
    string GroupId,             // Which group this belongs to
    bool IsToggle = false,      // Whether it shows toggle state
    bool IsEnabled = true       // Default enabled
);

public sealed record RibbonGroup(
    string Id,          // "navigation", "draw", "edit", "view"
    string Label,       // "" for Giriş default, or group headers later
    List<RibbonItem> Items
);

public sealed record RibbonTab(
    string Id,          // "home", "draw", "edit", "view"
    string Label,       // "GİRİŞ", "ÇİZİM", "DÜZEN", "GÖRÜNÜM"
    List<RibbonGroup> Groups
);

// ———— Static Definition ————

public static class RibbonDefinitions
{
    public static List<RibbonTab> Tabs { get; } = new()
    {
        new RibbonTab("home", "GİRİŞ", new()
        {
            new RibbonGroup("navigation", "", new()
            {
                new("Select", "Seç", "SelectObject24", "(S)", nameof(EditorViewModel.SetToolCommand), "Select", IsToggle: true),
                // "Taşı-yakında" — buton gösterilir ama komut yok; tıklanınca info log + tooltip "Çok yakında"
                new("MoveNearby", "Taşı", "Move24", "", null, null, IsEnabled: false),
                new("Delete", "Sil", "Delete24", "(Del)", nameof(EditorViewModel.DeleteCommand), null),
            }),
            new RibbonGroup("clipboard", "", new()
            {
                new("Copy", "Kopyala", "Copy24", "(Ctrl+C)", nameof(EditorViewModel.CopyCommand), null),
                new("Cut", "Kes", "Cut24", "(Ctrl+X)", nameof(EditorViewModel.CutCommand), null),
                new("Paste", "Yapıştır", "Paste24", "(Ctrl+V)", nameof(EditorViewModel.PasteCommand), null),
            }),
            new RibbonGroup("layer", "", new()
            {
                // Layer ComboBox — special control, defined in XAML not data-driven
                // Layer toggle (göz/kilit) — special control
            }),
        }),
        new RibbonTab("draw", "ÇİZİM", new()
        {
            new RibbonGroup("tools", "", new()
            {
                new("Track", "Ray", "ArrowFlowUpRight24", "(T)", nameof(EditorViewModel.SetToolCommand), "Track", IsToggle: true),
                new("Route", "Hat", "Directions24", "(R)", nameof(EditorViewModel.SetToolCommand), "Route", IsToggle: true),
                new("Hybrid", "Hibrit", "MergeType24", "(H)", nameof(EditorViewModel.SetToolCommand), "Hybrid", IsToggle: true),
                new("Ramp", "Rampa", "ArrowExpandUp24", "", nameof(EditorViewModel.SetToolCommand), "Ramp", IsToggle: true),
                new("Switch", "Makas", "BranchFork24", "(F8)", nameof(EditorViewModel.SetToolCommand), "Switch", IsToggle: true),
            }),
        }),
        new RibbonTab("edit", "DÜZEN", new()
        {
            new RibbonGroup("history", "", new()
            {
                new("Undo", "Geri Al", "ArrowUndo24", "(Ctrl+Z)", nameof(EditorViewModel.UndoCommand), null),
                new("Redo", "Yinele", "ArrowRedo24", "(Ctrl+Y)", nameof(EditorViewModel.RedoCommand), null),
            }),
            new RibbonGroup("modify", "", new()
            {
                new("Delete", "Sil", "Delete24", "(Del)", nameof(EditorViewModel.DeleteCommand), null),
                new("SplitSegment", "Böl", "Split24", "", null, null, IsEnabled: false),  // placeholder
            }),
            new RibbonGroup("placeholder", "", new()
            {
                // Boş grup — ilerisi için, hiç buton gösterme
            }),
        }),
        new RibbonTab("view", "GÖRÜNÜM", new()
        {
            new RibbonGroup("zoom", "", new()
            {
                new("ZoomExtents", "Sığdır", "ZoomFit24", "(Z)", nameof(EditorViewModel.ZoomExtentsCommand), null),
                new("ZoomWindow", "Pencere", "ZoomIn24", "(W)", nameof(EditorViewModel.ZoomWindowCommand), null),
            }),
            new RibbonGroup("display", "", new()
            {
                new("ToggleGrid", "Izgara", "GridDots24", "", nameof(EditorViewModel.ToggleGridCommand), null),
                new("ToggleSnap", "Snap", "SnapToGrid24", "(F9)", nameof(EditorViewModel.ToggleSnapCommand), null),
                // Tema butonu — ilerisi için placeholder
            }),
        }),
    };

    // Quick Access items (always visible, above tabs)
    public static List<RibbonItem> QuickAccessItems { get; } = new()
    {
        new("Save", "Kaydet", "Save24", "(Ctrl+S)", nameof(EditorViewModel.SaveCommand), null),
        new("Undo", "Geri Al", "ArrowUndo24", "(Ctrl+Z)", nameof(EditorViewModel.UndoCommand), null),
        new("Redo", "Yinele", "ArrowRedo24", "(Ctrl+Y)", nameof(EditorViewModel.RedoCommand), null),
    };

    // Helper: get all items across all tabs (for shortcut conflict scanning)
    public static IEnumerable<RibbonItem> AllItems
        => QuickAccessItems.Concat(Tabs.SelectMany(t => t.Groups.SelectMany(g => g.Items)));
}
```

**Tasarım kararı:** Veri modeli `record` kullanır (immutable, equality yerleşik). `RibbonDefinitions` statik sınıfı tüm şerit tanımını TEK KAYNAK'ta toplar. XAML `ItemsControl` ile bu veriyi bağlar. `IsToggle = true` olan araç butonları, `ActiveToolName` ile karşılaştırma yaparak vurgulanır.

### 3.2. `RibbonControl.xaml` — WPF KULLANICI KONTROLÜ

```
📁 src/TrainService.App/Controls/Ribbon/RibbonControl.xaml
📁 src/TrainService.App/Controls/Ribbon/RibbonControl.xaml.cs
```

**XAML yapısı:**

```xml
<UserControl x:Class="TrainService.App.Controls.Ribbon.RibbonControl"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:ui="http://schemas.lepo.co/wpfui/2022/xaml"
             xmlns:ribbon="clr-namespace:TrainService.App.Controls.Ribbon">
    <Grid>
        <Grid.RowDefinitions>
            <RowDefinition Height="32"/>   <!-- Quick Access -->
            <RowDefinition Height="Auto"/>  <!-- Tab headers -->
            <RowDefinition Height="*"/>     <!-- Tab content -->
        </Grid.RowDefinitions>

        <!-- Row 0: Quick Access Bar -->
        <Border Grid.Row="0" Background="#2A2A2A" BorderBrush="#333" BorderThickness="0,0,0,1">
            <ItemsControl ItemsSource="{Binding RibbonQuickAccess}" 
                          HorizontalAlignment="Left" Margin="4,0">
                <ItemsControl.ItemsPanel>
                    <ItemsPanelTemplate>
                        <StackPanel Orientation="Horizontal"/>
                    </ItemsPanelTemplate>
                </ItemsControl.ItemsPanel>
                <ItemsControl.ItemTemplate>
                    <DataTemplate DataType="{x:Type ribbon:RibbonItem}">
                        <ui:Button Command="{Binding DataContext.FindCommand(CommandName), 
                                    RelativeSource={RelativeSource AncestorType=UserControl}}"
                                   CommandParameter="{Binding CommandParameter}"
                                   Icon="{ui:SymbolIcon Symbol={Binding IconSymbol}}"
                                   ToolTip="{Binding Label, StringFormat='{}{0} {1}'}"
                                   Appearance="Transparent"
                                   Width="32" Height="28" Margin="1,0"
                                   ToolTipService.ShowOnDisabled="True"/>
                    </DataTemplate>
                </ItemsControl.ItemTemplate>
            </ItemsControl>
        </Border>

        <!-- Row 1: Tab Headers -->
        <ItemsControl Grid.Row="1" ItemsSource="{Binding RibbonTabs}">
            <ItemsControl.ItemsPanel>
                <ItemsPanelTemplate>
                    <StackPanel Orientation="Horizontal" Background="#1E1E1E"/>
                </ItemsPanelTemplate>
            </ItemsControl.ItemsPanel>
            <ItemsControl.ItemTemplate>
                <DataTemplate DataType="{x:Type ribbon:RibbonTab}">
                    <ToggleButton Content="{Binding Label}"
                                  IsChecked="{Binding IsSelected, Mode=TwoWay}"
                                  Style="{StaticResource RibbonTabToggleStyle}"
                                  FontSize="11" FontWeight="SemiBold"
                                  Padding="12,4"/>
                </DataTemplate>
            </ItemsControl.ItemTemplate>
        </ItemsControl>

        <!-- Row 2: Active Tab Content (groups + items) -->
        <ContentControl Grid.Row="2" Content="{Binding SelectedTab}">
            <ContentControl.ContentTemplate>
                <DataTemplate DataType="{x:Type ribbon:RibbonTab}">
                    <ItemsControl ItemsSource="{Binding Groups}" 
                                  Background="#252525" 
                                  BorderBrush="#333" BorderThickness="0,1,0,0">
                        <ItemsControl.ItemsPanel>
                            <ItemsPanelTemplate>
                                <StackPanel Orientation="Horizontal" Margin="4,2"/>
                            </ItemsPanelTemplate>
                        </ItemsControl.ItemsPanel>
                        <ItemsControl.ItemTemplate>
                            <DataTemplate DataType="{x:Type ribbon:RibbonGroup}">
                                <StackPanel Orientation="Vertical" Margin="8,2">
                                    <!-- Group items -->
                                    <ItemsControl ItemsSource="{Binding Items}">
                                        <ItemsControl.ItemsPanel>
                                            <ItemsPanelTemplate>
                                                <WrapPanel Orientation="Horizontal" MaxWidth="200"/>
                                            </ItemsPanelTemplate>
                                        </ItemsControl.ItemsPanel>
                                        <ItemsControl.ItemTemplate>
                                            <DataTemplate DataType="{x:Type ribbon:RibbonItem}">
                                                <ui:Button Command="{Binding DataContext.FindCommand(CommandName),
                                                            RelativeSource={RelativeSource AncestorType=UserControl}}"
                                                           CommandParameter="{Binding CommandParameter}"
                                                           Icon="{ui:SymbolIcon Symbol={Binding IconSymbol}}"
                                                           ToolTip="{Binding Label, StringFormat='{}{0} {1}'}"
                                                           Appearance="Primary"
                                                           Width="48" Height="44" Margin="2"
                                                           FontSize="10">
                                                    <ui:Button.Content>
                                                        <TextBlock Text="{Binding Label}" 
                                                                   HorizontalAlignment="Center"/>
                                                    </ui:Button.Content>
                                                </ui:Button>
                                            </DataTemplate>
                                        </ItemsControl.ItemTemplate>
                                    </ItemsControl>
                                    <!-- Group label -->
                                    <TextBlock Text="{Binding Label}" 
                                               FontSize="9" Foreground="#888"
                                               HorizontalAlignment="Center"
                                               Margin="0,2,0,0"/>
                                </StackPanel>
                            </DataTemplate>
                        </ItemsControl.ItemTemplate>
                    </ItemsControl>
                </DataTemplate>
            </ContentControl.ContentTemplate>
        </ContentControl>
    </Grid>
</UserControl>
```

**Code-behind (`RibbonControl.xaml.cs`):**

```csharp
namespace TrainService.App.Controls.Ribbon;

public partial class RibbonControl : UserControl
{
    public static readonly DependencyProperty RibbonTabsProperty =
        DependencyProperty.Register(nameof(RibbonTabs), typeof(ObservableCollection<RibbonTabViewModel>),
            typeof(RibbonControl), new PropertyMetadata(null));

    public static readonly DependencyProperty RibbonQuickAccessProperty =
        DependencyProperty.Register(nameof(RibbonQuickAccess), typeof(ObservableCollection<RibbonItemViewModel>),
            typeof(RibbonControl), new PropertyMetadata(null));

    public ObservableCollection<RibbonTabViewModel> RibbonTabs
    {
        get => (ObservableCollection<RibbonTabViewModel>)GetValue(RibbonTabsProperty);
        set => SetValue(RibbonTabsProperty, value);
    }

    public ObservableCollection<RibbonItemViewModel> RibbonQuickAccess
    {
        get => (ObservableCollection<RibbonItemViewModel>)GetValue(RibbonQuickAccessProperty);
        set => SetValue(RibbonQuickAccessProperty, value);
    }

    // ViewModel'dan komut bulucu — binding'de kullanılır
    public ICommand? FindCommand(string? commandName)
    {
        if (commandName == null || DataContext == null) return null;
        var prop = DataContext.GetType().GetProperty(commandName + "Command");
        return prop?.GetValue(DataContext) as ICommand;
    }

    public RibbonControl()
    {
        InitializeComponent();
    }
}
```

**Tasarım kararı:** `FindCommand` reflection kullanır — bu YALNIZCA ribbon yükleme anında çalışır (hot-path değil), kabul edilebilir. Alternatif (Command bindings dictionary) aşırı mühendislik olur.

### 3.3. `EditorViewModel.cs` — DEĞİŞİKLİKLER

**Eklenecek özellikler:**

```csharp
// ———— Yeni Özellikler ————

[ObservableProperty]
private string _activeToolName = "Select";  // Varsayılan araç

[ObservableProperty]
private string _selectedRibbonTabId = "home";

// Ribbon için ViewModel'lar
public ObservableCollection<RibbonTabViewModel> RibbonTabs { get; }
public ObservableCollection<RibbonItemViewModel> RibbonQuickAccess { get; }

// ———— Yeni Komutlar ————

[RelayCommand]
private void Delete()
{
    var selected = SelectionService.GetSelectedEntities(Document).ToList();
    if (selected.Count == 0) return;
    var cmd = new DeleteEntitiesCommand(selected);
    CommandStack.Do(cmd, Document);
    _logBus.Success("Editor", $"Silindi: {selected.Count} nesne");
}

[RelayCommand]
private void Copy()
{
    ClipboardService.Copy(SelectionService.GetSelectedEntities(Document));
    _logBus.Info("Editor", $"Kopyalandı: {ClipboardService.CopiedCount} nesne");
}

[RelayCommand]
private void Cut()
{
    ClipboardService.Cut(SelectionService.GetSelectedEntities(Document));
    var cmd = new DeleteEntitiesCommand(ClipboardService.CutEntities.ToList());
    CommandStack.Do(cmd, Document);
    _logBus.Info("Editor", $"Kesildi: {ClipboardService.CutCount} nesne");
}

[RelayCommand]
private void Paste()
{
    ClipboardService.Paste(Document, CommandStack);
    _logBus.Success("Editor", "Yapıştırıldı.");
}

[RelayCommand]
private void ZoomExtents()
{
    // Viewport'a iletilmek üzere event — EditorView.xaml.cs yakalar
    ZoomExtentsRequested?.Invoke();
}

[RelayCommand]
private void ZoomWindow()
{
    ZoomWindowRequested?.Invoke();
}

[RelayCommand]
private void ToggleGrid()
{
    ToggleGridRequested?.Invoke();
}

// ———— Etkinlikler ————

public event Action? ZoomExtentsRequested;
public event Action? ZoomWindowRequested;
public event Action? ToggleGridRequested;
```

**SetToolCommand güncellemesi:**

```csharp
[RelayCommand]
private void SetTool(string toolName)
{
    ActiveToolName = toolName;  // ← YENİ: toggle vurgusu için
    ToolChangeRequested?.Invoke(toolName);
    _logBus.Info("Editor", $"Araç seçildi: {toolName}");
}
```

**Ribbon ViewModel'ların oluşturulması (constructor'da):**

```csharp
// Constructor sonunda ribbon verisini yükle
RibbonTabs = new(RibbonDefinitions.Tabs.Select(t => new RibbonTabViewModel(t)));
RibbonQuickAccess = new(RibbonDefinitions.QuickAccessItems.Select(i => new RibbonItemViewModel(i)));
```

### 3.4. `EditorView.xaml` — DEĞİŞİKLİKLER

Mevcut yapı (`Grid.ColumnDefinitions` ile 250px sol + * sağ) KORUNUR. Değişen kısımlar:

**1. Sağ panelde (`Grid.Column="1"`) ribbon eklenir — en üste:**

```xml
<Grid Grid.Column="1">
    <Grid.RowDefinitions>
        <RowDefinition Height="Auto"/>      <!-- Ribbon -->
        <RowDefinition Height="*"/>         <!-- Canvas -->
    </Grid.RowDefinitions>

    <!-- Ribbon Control -->
    <controls:RibbonControl Grid.Row="0" DataContext="{Binding ViewModel}"/>

    <!-- Canvas (ikinci satır) — mevcut içerik buraya taşınır -->
    <Grid Grid.Row="1">
        <cad:CadViewportControl x:Name="Viewport"/>
        <!-- Floating panels (ZIndex) — taşı, aynen kalır -->
        ...
    </Grid>
</Grid>
```

**2. Eski yüzen toolbar (ZIndex=1000) KALDIRILIR** — satır 75-101 tamamen silinir.

**3. Yeni `InputBindings` eklenir:**

```xml
<Page.InputBindings>
    <KeyBinding Key="Z" Modifiers="Control" Command="{Binding ViewModel.UndoCommand}" />
    <KeyBinding Key="Y" Modifiers="Control" Command="{Binding ViewModel.RedoCommand}" />
    <KeyBinding Key="S" Modifiers="Control" Command="{Binding ViewModel.SaveCommand}" />
    <KeyBinding Key="F9" Command="{Binding ViewModel.ToggleSnapCommand}" />
    <KeyBinding Key="A" Modifiers="Control" Command="{Binding ViewModel.SelectAllCommand}" />
    <KeyBinding Key="C" Modifiers="Control" Command="{Binding ViewModel.CopyCommand}" />
    <KeyBinding Key="X" Modifiers="Control" Command="{Binding ViewModel.CutCommand}" />
    <KeyBinding Key="V" Modifiers="Control" Command="{Binding ViewModel.PasteCommand}" />
    <KeyBinding Key="Z" Command="{Binding ViewModel.ZoomExtentsCommand}" />
    <KeyBinding Key="W" Command="{Binding ViewModel.ZoomWindowCommand}" />
</Page.InputBindings>
```

### 3.5. `EditorView.xaml.cs` — DEĞİŞİKLİKLER

**PreviewKeyDown handler'a eklemeler:**

```csharp
// Mevcut S/T/R/F8 blokları korunur, ALTINA eklenir:
else if (e.Key == Key.H && Keyboard.Modifiers == ModifierKeys.None)
{
    if (ViewModel.SetToolCommand.CanExecute("Hybrid"))
        ViewModel.SetToolCommand.Execute("Hybrid");
    e.Handled = true;
}
else if (e.Key == Key.R && Keyboard.Modifiers == ModifierKeys.None)
{
    // Route zaten var — Ramp için farklı tuş gerekir mi?
    // Rampa için şimdilik kısayol YOK (ribbon'dan erişilir)
}
else if (e.Key == Key.Delete && Keyboard.Modifiers == ModifierKeys.None)
{
    if (ViewModel.DeleteCommand.CanExecute(null))
        ViewModel.DeleteCommand.Execute(null);
    e.Handled = true;
}
```

**ToolChangeRequested handler'ına eklemeler:**

```csharp
ViewModel.ToolChangeRequested += (toolName) =>
{
    switch (toolName)
    {
        case "Select": Viewport.ToolController.SetTool(new SelectTool()); break;
        case "Track":  Viewport.ToolController.SetTool(new TrackTool()); break;
        case "Route":  Viewport.ToolController.SetTool(new RouteTool()); break;
        case "Hybrid": Viewport.ToolController.SetTool(new HybridTool()); break;
        case "Ramp":   Viewport.ToolController.SetTool(new RampTool()); break;
        case "Switch": Viewport.ToolController.SetTool(new SwitchTool()); break;
    }
};
```

**Zoom/Toggle event bağlantıları (Loaded içinde):**

```csharp
ViewModel.ZoomExtentsRequested += () => Viewport.ZoomExtents();
ViewModel.ZoomWindowRequested += () => Viewport.ZoomWindow();
ViewModel.ToggleGridRequested += () => Viewport.ToggleGrid();
```

---

## 4. Test Planı — T330–T335

```
📁 tests/TrainService.App.Tests/T330_T335_RibbonTests.cs
```

Test kategorisi `App.Tests` (App katmanı ViewModel testleri). Gerçek EditorViewModel + RibbonDefinitions kullanılır, DI mock gerekmez (bağımlılıklar parametre olarak geçilir).

### T330 — SetTool eşlemesi (tüm araçlar ribbon'da tanımlı)
```
Assert: Her RibbonItem (IsToggle=true) için CommandParameter != null
Assert: CommandParameter değerleri ("Select","Track","Route","Hybrid","Ramp","Switch")
        EditorViewModel.SetToolCommand ile çağrılabilir
```

### T331 — ActiveToolName senkronu
```
Arrange: new EditorViewModel(...)
Act:    ViewModel.SetToolCommand.Execute("Track")
Assert: ViewModel.ActiveToolName == "Track"
Act:    ViewModel.SetToolCommand.Execute("Hybrid")
Assert: ViewModel.ActiveToolName == "Hybrid"
```

### T332 — RibbonDefinition bütünlüğü
```
Assert: Tüm RibbonItem.Id'ler benzersiz (AllItems.Select(i=>i.Id).Distinct().Count() == AllItems.Count())
Assert: Her RibbonItem.CommandName null değilse, EditorViewModel'de ilgili *Command property'si var
        (reflection: typeof(EditorViewModel).GetProperty(id+"Command") != null)
Assert: QuickAccessItems.Count == 3 (Save, Undo, Redo)
Assert: Tabs.Count == 4 (Giriş, Çizim, Düzen, Görünüm)
```

### T333 — Kısayol çakışma taraması
```
Assert: Hiçbir ShortcutText aynı değil (AllItems.Select(i=>i.ShortcutText).Where(s=>s!=null)
        grup by s -> hepsi Count==1)
```

### T334 — DeleteCommand temel test
```
Arrange: Document'a 1 adet segment ekle, seç
Act:    ViewModel.DeleteCommand.Execute(null)
Assert: Document.GetAllEntities().Count() == 0
Assert: CommandStack.CanUndo == true
Act:    ViewModel.UndoCommand.Execute(null)
Assert: Document.GetAllEntities().Count() == 1
```

### T335 — Clipboard komutları senkronu
```
Arrange: Document'a 2 segment ekle, seç
Act:    ViewModel.CopyCommand.Execute(null)
Assert: ClipboardService.CopiedCount == 2
Act:    ViewModel.PasteCommand.Execute(null)
Assert: Document.GetAllEntities().Count() == 4  (2 yapıştırıldı)
```

---

## 5. İş Akışı (AGENTS.md kapsamında)

### Adım 1: Plan onayı (BURADAYIZ)
- Kullanıcı planı okur ve onaylar.

### Adım 2: TDD — KIRMIZI
1. `T330_T335_RibbonTests.cs` yazılır (hepsi boş `Assert.True(false)` ile kırmızı)
2. `dotnet test -c Release --filter "T330|T331|T332|T333|T334|T335"` → HAM çıktı (kırmızı)

### Adım 3: Kod
1. `RibbonDefinition.cs` — veri modeli + statik tanım
2. `RibbonControl.xaml` + `.xaml.cs` — WPF kontrolü
3. `EditorViewModel.cs` — yeni özellikler/komutlar
4. `EditorView.xaml` — ribbon entegrasyonu, eski toolbar kaldırma
5. `EditorView.xaml.cs` — yeni tool eşlemeleri, zoom/grid event'leri

### Adım 4: TDD — YEŞİL
1. `dotnet test -c Release --filter "T330|T331|T332|T333|T334|T335"` → yeşil HAM çıktı
2. Tüm testler `dotnet test -c Release` → yeşil

### Adım 5: `dotnet run` → DUR
1. Uygulama çalıştırılır
2. Kullanıcı manuel turu:
   - M1: Tüm ribbon sekmeleri görünür mü?
   - M2: Her araç butonu aktif tool'u değiştiriyor mu?
   - M3: Tooltip'lerde kısayol görünüyor mu?
   - M4: Klavye kısayolları (S/T/R/H/F8/Del) hala çalışıyor mu?
   - M5: Quick Access bar'da Save/Undo/Redo var mı?
   - M6: Regexyon — SelectTool, TrackTool, RouteTool, SwitchTool çalışıyor mu?

### Adım 6: Mühür
- `tools/muhur.ps1 v3.0.29.1` ile rapor
- Kullanıcı "pushla" derse commit+push

---

## 6. MÜHÜRLÜ DAVRANIŞ KONTROLÜ (AGENTS.md Y5)

| Davranış | Durum |
|----------|-------|
| F9 = snap toggle | KORUNUR — InputBindings'te aynen kalır |
| Esc = İPTAL | KORUNUR — ToolController.KeyDown üzerinden |
| Enter/sağ-tık = COMMIT | KORUNUR — ToolController.KeyDown üzerinden |
| SabitKatmanlar (11111111/22222222/33333333) | ETKİLENMEZ — Cad katmanı |
| IsVisible/IsSelectable tek-kaynak | ETKİLENMEZ |
| CadColors merkezî renk | ETKİLENMEZ |
| Radyal menü Idle guard | ETKİLENMEZ — bu sürümde radyal değişmez |

---

## 7. EK: Mimaride Değişen/Dokunulmayan Katmanlar

| Katman | Dokunulur mu? | Gerekçe |
|--------|--------------|---------|
| `TrainService.App` (XAML/ViewModel) | **EVET** | Yeni ribbon kontrolü + ViewModel genişletme |
| `TrainService.Cad` | **HAYIR** | Sadece mevcut `HybridTool`/`RampTool` referansı alınır, yeni kod YOK |
| `TrainService.Core` | **HAYIR** | Tamamen dokunulmaz |
| `TrainService.Data` | **HAYIR** | Tamamen dokunulmaz |
| `TrainService.Messaging` | **HAYIR** | Tamamen dokunulmaz |
| `TrainService.App.Tests` | **EVET** | Yeni T330–T335 testleri |
