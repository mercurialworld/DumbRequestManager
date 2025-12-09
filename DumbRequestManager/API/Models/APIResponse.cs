using System.Net;
using Newtonsoft.Json;

namespace DumbRequestManager.API.Models;

public class APIResponse
{
    public int StatusCode { get; set; } = (int)HttpStatusCode.BadRequest;
    public string Message { get; set; } = APIMessage("Invalid request.");

    public APIResponse(HttpStatusCode statusCode, string message)
    {
        StatusCode = (int)statusCode;
        Message = message;
    }
    
    public APIResponse() {}
    
    public static string APIMessage(string message) => $"{{\"message\": \"{message}\"}}";
    public static string APISingleValueObject(string key, string value) => $"{{\"{key}\": \"{value}\"}}";
}
