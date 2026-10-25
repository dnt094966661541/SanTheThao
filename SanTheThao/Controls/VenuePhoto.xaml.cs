namespace SanTheThao.Controls;

public partial class VenuePhoto : ContentView
{
    public static readonly BindableProperty ImageUrlProperty = BindableProperty.Create(
        nameof(ImageUrl), typeof(string), typeof(VenuePhoto), null,
        propertyChanged: (b, _, _) => ((VenuePhoto)b).Refresh());

    public static readonly BindableProperty IconProperty = BindableProperty.Create(
        nameof(Icon), typeof(string), typeof(VenuePhoto), null,
        propertyChanged: (b, _, _) => ((VenuePhoto)b).Refresh());

    public string? ImageUrl
    {
        get => (string?)GetValue(ImageUrlProperty);
        set => SetValue(ImageUrlProperty, value);
    }

    public string? Icon
    {
        get => (string?)GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    public VenuePhoto()
    {
        InitializeComponent();
        SizeChanged += (_, _) => IconLabel.FontSize = Math.Max(18, Math.Min(Width, Height) * 0.45);
    }

    private void Refresh()
    {
        IconLabel.Text = Icon ?? "";

        var hasPhoto = !string.IsNullOrWhiteSpace(ImageUrl) &&
                       Uri.TryCreate(ImageUrl, UriKind.Absolute, out _);

        Photo.Source = hasPhoto ? ImageSource.FromUri(new Uri(ImageUrl!)) : null;
        Photo.IsVisible = hasPhoto;
    }
}