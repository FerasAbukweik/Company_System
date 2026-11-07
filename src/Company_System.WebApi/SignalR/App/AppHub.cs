using HR_System.Core.Abstraction.RepositoryContracts;
using HR_System.Core.Abstraction.ServiceContracts;
using HR_System.Core.DTO.Message;
using HR_System.Core.DTO.Notification;
using HR_System.Core.Enums;
using HR_System.ExtensionMethods;
using HR_System.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace HR_System.SignalR.App;

[Authorize]
public class AppHub(
    IMessagesService messagesService,
    IApplicationUsersRepository usersRepository,
    INotificationsService notificationsService,
    INotificationPublisher publisher,
    ApplicationDbContext dbContext) : Hub<IAppClient>
{
    public override async Task OnConnectedAsync()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, UserGroup(CurrentUserId()), Context.ConnectionAborted);
        await base.OnConnectedAsync();
    }

    public async Task AddToGroup(Guid otherUserId)
    {
        await EnsureRecipientAsync(otherUserId);
        await Groups.AddToGroupAsync(Context.ConnectionId, ConversationGroup(otherUserId), Context.ConnectionAborted);
        JoinedUsers.Add(otherUserId);
    }

    public async Task RemoveFromGroup(Guid otherUserId)
    {
        JoinedUsers.Remove(otherUserId);
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, ConversationGroup(otherUserId), Context.ConnectionAborted);
    }

    public async Task SendMessage(Guid otherUserId, string content)
    {
        EnsureJoined(otherUserId);
        await EnsureRecipientAsync(otherUserId);
        var currentUserId = CurrentUserId();

        await using var transaction = await dbContext.Database.BeginTransactionAsync(Context.ConnectionAborted);
        var result = await messagesService.AddAsync(new MessageAddDTO
        {
            ReceiverId = otherUserId,
            Content = content
        }, currentUserId, Context.ConnectionAborted);
        if (!result.IsSuccess) throw new HubException(result.ErrorMessage);

        var notification = await notificationsService.AddAsync(new NotificationAddDTO
        {
            UserId = otherUserId,
            Title = "New chat message",
            Content = content,
            TargetPath = "/org-tree",
            SourceKey = $"message:{result.Value!.Id}",
            Type = NotificationTypeEnum.Message
        }, Context.ConnectionAborted);
        if (!notification.IsSuccess) throw new HubException(notification.ErrorMessage);

        await transaction.CommitAsync(Context.ConnectionAborted);
        await publisher.FlushAsync();
        await Clients.Group(ConversationGroup(otherUserId)).ReceiveMessage(result.Value!);
    }

    public Task NotifyTyping(Guid otherUserId)
    {
        EnsureJoined(otherUserId);
        return Clients.OthersInGroup(ConversationGroup(otherUserId)).NotifyTyping(CurrentUserId());
    }

    public Task NotifyStoppedTyping(Guid otherUserId)
    {
        EnsureJoined(otherUserId);
        return Clients.OthersInGroup(ConversationGroup(otherUserId)).NotifyStoppedTyping(CurrentUserId());
    }


    // The connection can only join conversations containing its authenticated user.
    private HashSet<Guid> JoinedUsers
    {
        get
        {
            if (!Context.Items.TryGetValue(nameof(JoinedUsers), out var value))
                Context.Items[nameof(JoinedUsers)] = value = new HashSet<Guid>();
            return (HashSet<Guid>)value!;
        }
    }

    private Guid CurrentUserId()
    {
        var result = Context.User!.GetUserId();
        if (!result.IsSuccess) throw new HubException("Authentication required");
        return result.Value;
    }

    private async Task EnsureRecipientAsync(Guid otherUserId)
    {
        if (otherUserId == Guid.Empty || otherUserId == CurrentUserId())
            throw new HubException("Invalid recipient");

        var user = await usersRepository.GetByIdAsync(otherUserId, [user => user.OrganizationHierarchy], Context.ConnectionAborted);
        if (user is null || user.OrganizationHierarchy is { IsActive: false })
            throw new HubException("Recipient not found");
    }

    private void EnsureJoined(Guid otherUserId)
    {
        if (!JoinedUsers.Contains(otherUserId)) throw new HubException("Join the conversation first");
    }

    private string ConversationGroup(Guid otherUserId)
    {
        var ids = new[] { CurrentUserId().ToString(), otherUserId.ToString() };
        Array.Sort(ids, StringComparer.Ordinal);
        return $"{ids[1]}-{ids[0]}";
    }

    public static string UserGroup(Guid userId) => $"user:{userId:D}";
}
