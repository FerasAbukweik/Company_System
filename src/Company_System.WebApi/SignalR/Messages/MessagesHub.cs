using HR_System.Core.Constraints;
using HR_System.Core.DTO.Message;
using HR_System.Core.Interfaces.ServiceContracts.ICookieServices;
using HR_System.Core.Interfaces.ServiceContracts.IMessageServices;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;

namespace HR_System.SignalR.Messages;

public class MessagesHub(ICookiesServices cookiesServices,
    IOptions<CookieKeys> cookieKeys,
    IMessageService messageService) : Hub<IMessagesHub>
{
    public override async Task OnConnectedAsync()
    {
        var groupName = generateGroupName();
        if(groupName == null) return;
        
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
    }

    public async Task SendMessage(MessageAddDTO newMessage)
    {
        var userIdResult = cookiesServices.GetValue<Guid>(cookieKeys.Value.AccessToken);
        if (!userIdResult.IsSuccess) return;

        var addMessageResult = await messageService.AddAsync(newMessage, userIdResult.Value);
        if(!addMessageResult.IsSuccess) return;
        
        var groupName = generateGroupName();
        if(groupName == null) return;
        
        await Clients.Group(groupName).ReceiveMessage(addMessageResult.Value!);
    }

    public async Task NotifyTyping()
    {
        var groupName = generateGroupName();
        if(groupName == null) return;
        
        await Clients.OthersInGroup(groupName).NotifyTyping();
    }

    public async Task NotifyStoppedTyping()
    {
        var groupName = generateGroupName();
        if(groupName == null) return;
        
        await Clients.OthersInGroup(groupName).NotifyStoppedTyping();
    }
    
    
    
    
    
    
    
    
    
    
    // helper methods

    private string? generateGroupName()
    {
        var userIdResult = cookiesServices.GetValue<Guid>(cookieKeys.Value.AccessToken);
        if (!userIdResult.IsSuccess) return null;

        var otherUserId = GetOtherPersonId();
        if(otherUserId is null) return null;
        
        
        if (string.Compare(userIdResult.Value.ToString(), otherUserId, StringComparison.Ordinal) > 0)
            return $"{userIdResult.Value}-{otherUserId}";
        
        return $"{otherUserId}-{userIdResult.Value}";
    }

    private string? GetOtherPersonId()
    {
        var httpContext = Context.GetHttpContext();
        if(httpContext == null)
            return null;

        if (!httpContext.Request.Query.TryGetValue("userId", out var idString))
            return null;
        
        return idString;
    }
}