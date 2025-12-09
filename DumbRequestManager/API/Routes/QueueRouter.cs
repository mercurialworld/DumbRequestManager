using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using DumbRequestManager.API.Models;
using DumbRequestManager.Classes;
using DumbRequestManager.Configuration;
using DumbRequestManager.Managers;
using DumbRequestManager.UI;
using JetBrains.Annotations;
using Newtonsoft.Json;

namespace DumbRequestManager.API.Routes;

[JsonObject(MemberSerialization.OptIn)]
internal struct QueueSpotItem(int spot, NoncontextualizedSong queueItem)
{
    [JsonProperty] private int Spot => spot;
    [JsonProperty] private NoncontextualizedSong QueueItem => queueItem;
}

[UsedImplicitly]
public class QueueRouter 
{
    private static PluginConfig Config => PluginConfig.Instance;
    
    public static async Task<APIResponse> HandleContext(HttpListenerContext context)
    {
        var res = new APIResponse();
        var route = context.Request.Url.Segments;

        if (route.Length <= 2)
        {
            res.StatusCode = (int)HttpStatusCode.OK;
            res.Message = GetStringifiedQueue;
        }
        else
        {
            switch (route[2].Replace("/", string.Empty).ToLower())
            {
                case "where":
                    res = HandleWhere(route.Last().Replace("/", string.Empty));
                    break;
                case "clear":
                    res = HandleClear();
                    break;
                case "open":
                    res = await HandleOpen(route);
                    break;
                case "move":
                    res = HandleMove(route);
                    break;
                case "shuffle":
                    res = HandleShuffle();
                    break;
                case "status":
                    res = HandleQueueStatus();
                    break;
            }
        }

        return res;
    }

    private static APIResponse HandleQueueStatus()
    {
        return new APIResponse(HttpStatusCode.OK, APIResponse.APISingleValueObject("QueueOpen", JsonConvert.SerializeObject(Config.QueueOpenStatus)));
    }

    private static APIResponse HandleShuffle()
    {
        QueueManager.Shuffle();
        
        return new APIResponse(HttpStatusCode.OK,  APIResponse.APIMessage("Queue shuffled"));
    }

    private static APIResponse HandleMove(string[] route)
    {
        var res = new APIResponse();

        if (route.Length != 5 || !int.TryParse(route[3].Replace("/", string.Empty), out int existingSpot))
        {
            return res;
        }

        existingSpot--;

        const int minIndex = 0;
        int maxIndex = QueueManager.QueuedSongs.Count - 1;

        if (!int.TryParse(route[4].Replace("/", string.Empty), out int newSpot))
        {
            switch (route[4].Replace("/", string.Empty).ToLower())
            {
                case "top":
                    newSpot = 0;
                    break;
                case "bottom":
                    newSpot = maxIndex;
                    break;
                default:
                    return res;
            }
        }
        else
        {
            newSpot--;
        }

        if (!(minIndex <= existingSpot && existingSpot <= maxIndex) || !(minIndex <= newSpot && newSpot <= maxIndex))
        {
            res.Message = APIResponse.APIMessage("Indices out of bounds");
            return res;
        }
        
        NoncontextualizedSong oldSong = QueueManager.QueuedSongs[existingSpot];
        QueueManager.QueuedSongs.RemoveAt(existingSpot);
        QueueManager.QueuedSongs.Insert(newSpot, oldSong);

        QueueViewController.RefreshQueue();
                            
        res.StatusCode = (int)HttpStatusCode.OK;
        res.Message = APIResponse.APIMessage("Moved entry");

        return res;
    }

    private static string GetStringifiedQueue => JsonConvert.SerializeObject(QueueManager.QueuedSongs);

    private static APIResponse HandleWhere(string user)
    {
        int index = 0;
        List<QueueSpotItem> relevantQueueItems = [];
        
        QueueManager.QueuedSongs.ForEach(x =>
        {
            index++;

            if (x.User == user)
            {
                relevantQueueItems.Add(new QueueSpotItem(index, x));
            }
        });
        
        return new APIResponse(HttpStatusCode.OK, JsonConvert.SerializeObject(relevantQueueItems));
    }

    private static APIResponse HandleClear()
    {
        QueueManager.QueuedSongs.Clear();

        QueueViewController.RefreshQueue();
        ChatRequestButton.Instance.UseAttentiveButton(false);

        return new APIResponse(HttpStatusCode.OK, APIResponse.APIMessage("Queue cleared"));
    }

    private static async Task<APIResponse> HandleOpen(string[] path)
    {
        var res = new APIResponse();

        if (path.Length <= 3 || !bool.TryParse(path[3].Replace("/", string.Empty), out bool openResult)) return res;
        
        res.StatusCode = (int)HttpStatusCode.OK;
        res.Message = APIResponse.APIMessage("Queue gate changed");
        await SideSettingsViewController.Instance.SetState(openResult);

        return res;
    }
}
