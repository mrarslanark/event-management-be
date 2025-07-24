namespace EventManagement.Helpers;

public static class ApiResponse
{
    public static IResult Success(object? data = null, string? message = "Success",
        int statusCode = StatusCodes.Status200OK)
    {
        var response = new Dictionary<string, object?>()
        {
            ["createdAt"] = DateTime.UtcNow,
            ["status"] = statusCode,
            ["message"] = message
        };
        if (data != null)
            response["data"] = data;
        return Results.Json(response, statusCode: statusCode);
    }

    public static IResult Created(string location, object? data = null, string? message = "Resource Created")
    {
        var response = new Dictionary<string, object?>
        {
            ["createdAt"] = DateTime.UtcNow,
            ["status"] = StatusCodes.Status201Created,
            ["message"] = message,
        };
        if (data != null)
            response["data"] = data;
        return Results.Created(location, response);
    }
}