using HR_System.Core.Constraints;
using HR_System.Core.DTO.Message;
using HR_System.Core.Interfaces.ServiceContracts;
using HR_System.ExtensionMethods;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;

namespace HR_System.SignalR.Messages;

public class MessagesHub(IMessagesService messagesService) : Hub<IMessagesHub>
{
    public override async Task OnConnectedAsync()
    {
        var groupName = generateGroupName();
        if(groupName == null) return;
        
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
    }

    public async Task SendMessage(string content)
    {
        var otherUserId = GetOtherPersonId();
        if (otherUserId == null) return;
        
        var currUserId =  GetUserId();
        if(currUserId == null) return;

        MessageAddDTO toAddMessage = new MessageAddDTO()
        {
            Content = content,
            ReceiverId = otherUserId.Value
        };

        var addMessageResult = await messagesService.AddAsync(toAddMessage, currUserId.Value);
        if(!addMessageResult.IsSuccess) return;

        
        var groupName = generateGroupName();
        if(groupName == null) return;
        
        addMessageResult.Value!.IsCurrUserSender = false;
        await Clients.OthersInGroup(groupName).ReceiveMessage(addMessageResult.Value!);
        addMessageResult.Value!.IsCurrUserSender = true;
        await Clients.Caller.ReceiveMessage(addMessageResult.Value!);
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
        var userIdResult = GetUserId();
        if (userIdResult == null) return null;

        var otherUserId = GetOtherPersonId();
        if(otherUserId is null) return null;

        if (string.Compare(userIdResult.Value.ToString(), otherUserId.Value.ToString(), StringComparison.Ordinal) > 0)
            return $"{userIdResult.Value}-{otherUserId.Value}";
        
        return $"{otherUserId.Value}-{userIdResult.Value}";
    }

    private Guid? GetOtherPersonId()
    {
        var httpContext = Context.GetHttpContext();
        if(httpContext == null)
            return null;

        if (!httpContext.Request.Query.TryGetValue("userId", out var idString))
            return null;
        
        if(!Guid.TryParse(idString, out var id))
            return null;
        
        return id;
    }

    private Guid? GetUserId()
    {
        var httpContext = Context.GetHttpContext();
        if(httpContext == null)
            return null;

        var result = httpContext.User.GetUserId();
        if(!result.IsSuccess) return null;
        
        return result.Value;
    }
}