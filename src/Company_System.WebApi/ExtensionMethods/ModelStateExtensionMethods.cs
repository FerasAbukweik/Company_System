using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace HR_System.ExtensionMethods;

public static class ModelStateExtensionMethods
{
    public static string GetErrorsString(this ModelStateDictionary modelState)
    {
        var errorsArray = modelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToArray();

        return String.Join(" | ", errorsArray);
    }
}