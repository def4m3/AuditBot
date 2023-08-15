using DSharpPlus;
using DSharpPlus.Entities;
using System.Diagnostics.Metrics;
using System.Reflection.Metadata.Ecma335;
using System.Threading.Channels;

public class Program
{
    private readonly static ulong _auditChannelId = ; // PUT AUDIT CHANNEL ID HERE
    private static DiscordChannel? _auditChannel;
    static void Main(string[] args)
    {
        MainAsync().GetAwaiter().GetResult();
    }

    static async Task MainAsync()
    {

        var discord = new DiscordClient(new DiscordConfiguration
        {
            Token = "", // PUT YOUR BOT TOKEN HERE
            TokenType = TokenType.Bot,
            Intents = DiscordIntents.All,
            MinimumLogLevel = Microsoft.Extensions.Logging.LogLevel.Debug,
            LogTimestampFormat = "MMM dd yyyy - hh:mm:ss tt",
            AutoReconnect = true,
        });

        _auditChannel = discord.GetChannelAsync(_auditChannelId).Result ?? throw new Exception("U need to put audit channel id in the variable");

        discord.GuildBanAdded += Discord_GuildBanAdded;
        discord.GuildBanRemoved += Discord_GuildBanRemoved;
        discord.GuildMemberAdded += Discord_GuildMemberAdded;
        discord.GuildMemberRemoved += Discord_GuildMemberRemoved;
        discord.MessageDeleted += Discord_MessageDeleted;
        discord.MessageUpdated += Discord_MessageUpdated;


        await discord.ConnectAsync();
        await Task.Delay(-1);
    }

    private static Task Discord_MessageUpdated(DiscordClient sender, DSharpPlus.EventArgs.MessageUpdateEventArgs args)
    {
        if (args.Author.IsBot) return Task.CompletedTask;

        var messageUptatedEmbed = new DiscordEmbedBuilder()
        {
            Color = DiscordColor.MidnightBlue,
            Timestamp = DateTime.Now,
            Author = new DiscordEmbedBuilder.EmbedAuthor()
            {
                Name = $"Message was updated",
                IconUrl = _auditChannel.Guild.IconUrl
            },
            Thumbnail = new DiscordEmbedBuilder.EmbedThumbnail()
            {
                Url = args.Author.AvatarUrl
            }
        };
        messageUptatedEmbed.AddField("When", $"<t:{args.Message.EditedTimestamp.Value.ToUnixTimeSeconds()}:R>",true);
        messageUptatedEmbed.AddField("Member", args.Author.Mention, true);
        messageUptatedEmbed.AddField("Message", $"was: `{args.MessageBefore.Content}`\n\n become: `{args.Message.Content}`\n");

        _auditChannel.SendMessageAsync(messageUptatedEmbed);

        return Task.CompletedTask;
    }

    private static Task Discord_MessageDeleted(DiscordClient sender, DSharpPlus.EventArgs.MessageDeleteEventArgs args)
    {
        if (args.Message.Author.IsBot) return Task.CompletedTask;

        var auditLogs = args.Guild.GetAuditLogsAsync(1, action_type: AuditLogActionType.MessageDelete).Result;

        var lastLog = auditLogs.FirstOrDefault();

        var messageDeletedEmbed = new DiscordEmbedBuilder()
        {
            Color = DiscordColor.MidnightBlue,
            Timestamp = DateTime.Now,
            Author = new DiscordEmbedBuilder.EmbedAuthor()
            {
                Name = $"Message was deleted by " + lastLog.UserResponsible.Username,
                IconUrl = _auditChannel.Guild.IconUrl
            },
            Thumbnail = new DiscordEmbedBuilder.EmbedThumbnail()
            {
                Url = args.Message.Author.AvatarUrl
            },
            
        };

        messageDeletedEmbed.AddField("Message Author", args.Message.Author.Mention);
        messageDeletedEmbed.AddField("Message", "`" + args.Message.Content + "`");

        _auditChannel.SendMessageAsync(messageDeletedEmbed);

        return Task.CompletedTask;
    }

    private static Task Discord_GuildBanRemoved(DiscordClient sender, DSharpPlus.EventArgs.GuildBanRemoveEventArgs args)
    {

        var auditLogs = args.Guild.GetAuditLogsAsync(1, action_type: AuditLogActionType.Unban).Result;

        var lastLog = auditLogs.FirstOrDefault();

        var guildBanRemovedEmbed = new DiscordEmbedBuilder()
        {
            Color = DiscordColor.MidnightBlue,
            Timestamp = DateTime.Now,
            Author = new DiscordEmbedBuilder.EmbedAuthor()
            {
                Name = "Member was unbanned by " + lastLog.UserResponsible.Username,
                IconUrl = _auditChannel.Guild.IconUrl
            },
            Thumbnail = new DiscordEmbedBuilder.EmbedThumbnail()
            {
                Url = args.Member.AvatarUrl
            },

        };

        guildBanRemovedEmbed.AddField("Member", args.Member.Mention);
        guildBanRemovedEmbed.AddField("Account was registered", $"<t:{args.Member.CreationTimestamp.ToUnixTimeSeconds()}:f>");

        _auditChannel.SendMessageAsync(guildBanRemovedEmbed);

        return Task.CompletedTask;
    }

    private static Task Discord_GuildBanAdded(DiscordClient sender, DSharpPlus.EventArgs.GuildBanAddEventArgs args)
    {
        var auditLogs = args.Guild.GetAuditLogsAsync(1, action_type: AuditLogActionType.Ban).Result;

        var lastLog = auditLogs.FirstOrDefault();

        var guildBanAddedEmbed = new DiscordEmbedBuilder()
        {
            Color = DiscordColor.MidnightBlue,
            Timestamp = DateTime.Now,
            Author = new DiscordEmbedBuilder.EmbedAuthor()
            {
                Name = "Member was banned by " + lastLog.UserResponsible.Username,
                IconUrl = _auditChannel.Guild.IconUrl
            },
            Thumbnail = new DiscordEmbedBuilder.EmbedThumbnail()
            {
                Url = args.Member.AvatarUrl
            },

        };

        guildBanAddedEmbed.AddField("Member", args.Member.Mention);
        guildBanAddedEmbed.AddField("Account was registered", $"<t:{args.Member.CreationTimestamp.ToUnixTimeSeconds()}:f>");
        
        _auditChannel.SendMessageAsync(guildBanAddedEmbed);

        return Task.CompletedTask;
    }

    private static Task Discord_GuildMemberRemoved(DiscordClient sender, DSharpPlus.EventArgs.GuildMemberRemoveEventArgs args)
    {
        var auditLog = args.Guild.GetAuditLogsAsync(1).Result.FirstOrDefault();

        if (auditLog.ActionType == AuditLogActionType.Ban) return Task.CompletedTask;

        var roles = args.Member.Roles;
        var rolesString = string.Join(", ", roles.Select(r => r.Mention));

        var guildMemberRemovedEmbed = new DiscordEmbedBuilder()
        {
            Color = DiscordColor.MidnightBlue,
            Timestamp = DateTime.Now,
            Author = new DiscordEmbedBuilder.EmbedAuthor()
            {
                Name = "Member left the server",
                IconUrl = _auditChannel.Guild.IconUrl
            },
            Thumbnail = new DiscordEmbedBuilder.EmbedThumbnail()
            {
                Url = args.Member.AvatarUrl
            },

        };


        guildMemberRemovedEmbed.AddField("Member", args.Member.Mention);
        guildMemberRemovedEmbed.AddField("Roles", rolesString);
        guildMemberRemovedEmbed.AddField("Account was registered", $"<t:{args.Member.CreationTimestamp.ToUnixTimeSeconds()}:f>");
        guildMemberRemovedEmbed.AddField("Joined at", $"<t:{args.Member.JoinedAt.ToUnixTimeSeconds()}:f>");

        _auditChannel.SendMessageAsync(guildMemberRemovedEmbed);

        return Task.CompletedTask;
    }

    private static Task Discord_GuildMemberAdded(DiscordClient sender, DSharpPlus.EventArgs.GuildMemberAddEventArgs args)
    {
        var guildMemberAddedEmbed = new DiscordEmbedBuilder()
        {
            Color = DiscordColor.MidnightBlue,
            Timestamp = DateTime.Now,
            Author = new DiscordEmbedBuilder.EmbedAuthor()
            {
                Name = "Member joined the server",
                IconUrl = _auditChannel.Guild.IconUrl
            },
            Thumbnail = new DiscordEmbedBuilder.EmbedThumbnail()
            {
                Url = args.Member.AvatarUrl
            },

        };


        guildMemberAddedEmbed.AddField("Member", args.Member.Mention);
        guildMemberAddedEmbed.AddField("Account was registered", $"<t:{args.Member.CreationTimestamp.ToUnixTimeSeconds()}:f>");
        guildMemberAddedEmbed.AddField("Joined at", $"<t:{args.Member.JoinedAt.ToUnixTimeSeconds()}:f>");

        _auditChannel.SendMessageAsync(guildMemberAddedEmbed);

        return Task.CompletedTask;
    }
}