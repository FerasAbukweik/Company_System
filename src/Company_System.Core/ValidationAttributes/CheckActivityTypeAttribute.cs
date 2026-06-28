using System.ComponentModel.DataAnnotations;
using HR_System.Core.Enums;

namespace HR_System.Core.ValidationAttributes;

public class CheckActivityTypeAttribute(string taskIdName, string approvalIdName) : ValidationAttribute
{
    private bool IsTaskRequired(ActivityTypeEnum activityType)
    {
        return activityType == ActivityTypeEnum.TaskAdded || activityType == ActivityTypeEnum.TaskCompleted || 
               activityType == ActivityTypeEnum.TaskPendingApproval || activityType == ActivityTypeEnum.TaskRejected;
    }
    
    private bool IsApprovalRequired(ActivityTypeEnum activityType)
    {
        return activityType == ActivityTypeEnum.ApprovalApproved || activityType == ActivityTypeEnum.ApprovalRejected ||
               activityType == ActivityTypeEnum.ApprovalPending;;
    }
    
    
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is null)
            return ValidationResult.Success;

        if (value is not ActivityTypeEnum activityType)
            return new ValidationResult(string.Format("{0} " + $"Should be of Type {nameof(ActivityTypeEnum)}" , validationContext.DisplayName));

        var requiredName = "";
        if (IsTaskRequired(activityType)) requiredName = taskIdName;
        if(IsApprovalRequired(activityType)) requiredName = approvalIdName;
        
        if(string.IsNullOrWhiteSpace(requiredName))
            return ValidationResult.Success;
        
        
        var containerObject = validationContext.ObjectInstance;
        var requiredPropertyInfo = containerObject.GetType().GetProperty(requiredName);
        if(requiredPropertyInfo is null)
            return  new ValidationResult(string.Format("{0} name wasn't found", requiredName));
        
        var required = requiredPropertyInfo.GetValue(containerObject);
        if(required is null ||
           (required is Guid guidValue && guidValue == Guid.Empty) || 
           (required is string stringValue && (string.IsNullOrWhiteSpace(stringValue) || stringValue == Guid.Empty.ToString())))
            return  new ValidationResult(string.Format("{0} Is required", requiredName));


        return ValidationResult.Success;
    }
}