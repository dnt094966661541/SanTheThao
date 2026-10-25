using CommunityToolkit.Mvvm.ComponentModel;

namespace SanTheThao.Models;

public partial class SportChip : ObservableObject
{
    public string Name { get; init; } = "";
    public string Icon { get; init; } = "";

    [ObservableProperty]
    private bool isSelected;
}