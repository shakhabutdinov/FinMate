namespace aspnet.DTOs;

public record ChatMessageDto(Guid Id, string Content, bool IsFromAI, DateTime CreatedAt);
public record SendMessageDto(string Content);
