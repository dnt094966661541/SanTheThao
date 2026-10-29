namespace SanTheThao.Models;

public class VenueImage
{
    public int Id { get; set; }
    public string Url { get; set; } = "";
    public bool IsMain { get; set; }

    public bool CanSetMain => !IsMain;
    public string FullUrl => SanTheThao.Services.ApiConfig.BaseUrl + Url;
}