using UniiaAnonim.TGBot.Shared.Enums;

namespace UniiaAnonim.TGBot.Domain.Models;

public class Channel
    : BaseEntity
{
    public ChannelType Type { get; set; }

    public long ChannelId { get; set; }
}
