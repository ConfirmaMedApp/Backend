namespace Backend.DTOs.MailerSend;

public sealed record MailerSendEmailRequestDto
(
    MailerSendFrom from,
    List<MailerSendTo> to,
    string subject,
    string html
);

public sealed record MailerSendFrom(string email, string name);
public sealed record MailerSendTo(string email, string name);
