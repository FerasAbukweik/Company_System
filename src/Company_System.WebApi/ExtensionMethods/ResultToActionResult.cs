using System.Net;
using HR_System.Core.common;
using Microsoft.AspNetCore.Mvc;

namespace HR_System.ExtensionMethods;

public static class ResultToActionResult
{
    public static ActionResult ToActionResult (this Result result)
    {
        return new ObjectResult(result.ErrorMessage) { StatusCode = (int)result.StatusCode };
    }

    public static ActionResult<T> ToActionResult<T> (this Result<T> result)
    {
        return result.StatusCode switch 
        {
            HttpStatusCode.OK => new OkObjectResult(result.Value),
            HttpStatusCode.Created => new ObjectResult(result.Value) { StatusCode = (int)HttpStatusCode.Created },
            _ => ToActionResult((Result)result)
        };
    }
}