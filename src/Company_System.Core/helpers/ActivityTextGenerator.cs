using HR_System.Core.Domain.Entities;
using HR_System.Core.Enums;

namespace HR_System.Core.helpers;

public static class ActivityTextGenerator
{
    public static string GetTaskTitle(AppTask task)
        => $"Task: {task.Title}";

    public static string GetTaskDescription(AppTask task, string currUserName)
        => $"New Status: {task.Status}\nChanged by: {currUserName}";

    public static string GetApprovalTitle(Approval approval)
        => approval.Type == ApprovalTypeEnum.Task ? "Task Approval" : 
            approval.Type == ApprovalTypeEnum.Holiday ? "Holiday Approval" :
            "Unknown Approval";

    public static string GetApprovalDescription(Approval approval)
    {
        var manager = approval.Manager?.UserName ?? "Unknown";
        var employee = approval.UserRequesting?.UserName ?? "Unknown";

        var request = approval.Type switch
        {
            ApprovalTypeEnum.Holiday => "Holiday Request",
            ApprovalTypeEnum.Task => $"Task: {approval.Task?.Title}",
            _ => "Unknown Request"
        };

        return
            $"Manager: {manager}\n" +
            $"Employee: {employee}\n" +
            $"{request}\n" +
            $"Approval Status: {approval.Status}";
    }
}