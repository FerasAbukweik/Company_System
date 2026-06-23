using System.Net;

namespace HR_System.Core.Helpers;

public class Result
{
    public bool IsSuccess { get; private set; }
    public string? ErrorMessage { get; private set; }
    public HttpStatusCode StatusCode { get; private set; }

    protected Result(bool isSuccess, string? errorMessage, HttpStatusCode statusCode)
    {
        IsSuccess = isSuccess;
        ErrorMessage = errorMessage;
        StatusCode = statusCode;
    }

    public static Result Success(HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        return new Result(true, null, statusCode);
    }

    public static Result Failure(string message, HttpStatusCode statusCode = HttpStatusCode.BadRequest)
    {
        return new Result(false, message, statusCode);
    }
    
    public Result<NewT> MapFailure<NewT>()
    {
        return Result<NewT>.Failure(ErrorMessage ?? "no error message", StatusCode);
    }
}
public class Result<T> : Result
{
    public T? Value { get; private set; }

    private Result(T? value, bool isSuccess, string? errorMessage, HttpStatusCode statusCode)
        : base(isSuccess, errorMessage, statusCode)
    {
        Value = value;
    }

    public static Result<T> Success(T value, HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        return new Result<T>(value , true , null , statusCode);
    }
    public new static Result<T> Failure(string message, HttpStatusCode statusCode = HttpStatusCode.BadRequest)
    {
        return new Result<T>(default , false , message , statusCode);
    }
}