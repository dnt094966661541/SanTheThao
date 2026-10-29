namespace SanTheThao.Api.Models;

public class MatchMessage
{
    public int Id { get; set; }
    public Guid MatchId { get; set; }
    public Guid UserId { get; set; }
    public string Text { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}

public record SendChatRequest(string Text);
public record ChatMessageDto(int Id, string UserName, string Text, DateTime CreatedAt, bool IsMine);
public record ChatResponse(List<ChatMessageDto> Messages, bool CanSend, int ParticipantCount, string Title);