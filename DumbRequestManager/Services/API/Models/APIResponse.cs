using System.Net;
using System.Text;

namespace DumbRequestManager.Services.API.Models;

public class APIResponse
{
    public int StatusCode { get; set; } = (int)HttpStatusCode.BadRequest;
    public string Payload { get; set; } = APIMessage("Invalid request.");

    public APIResponse(HttpStatusCode statusCode, string payload)
    {
        StatusCode = (int)statusCode;
        Payload = payload;
    }
    
    public APIResponse() {}

    public void Set(HttpStatusCode statusCode, string payload)
    {
        StatusCode = (int)statusCode;
        Payload = payload;
    }

    public byte[] PayloadInBytes => Encoding.Default.GetBytes(Payload);
    public static string APIMessage(string message) => $"{{\"message\": \"{message}\"}}";
    public static string APISingleValueObject(string key, string value) => $"{{\"{key}\": \"{value}\"}}";
}
