namespace SanTheThao.Models;

public class Review
{
    public string UserName { get; set; } = "";
    public int Rating { get; set; }
    public string Comment { get; set; } = "";
    public DateTime CreatedAt { get; set; }

    public string StarsText => new string('⭐', Rating);
    public string DateText => CreatedAt.ToString("dd/MM/yyyy");
    public bool HasComment => !string.IsNullOrWhiteSpace(Comment);
}

public class PromoInfo
{
    public string Code { get; set; } = "";
    public string Description { get; set; } = "";
    public int PercentOff { get; set; }
    public decimal MaxDiscount { get; set; }
    public decimal MinTotal { get; set; }
}