const SOURCE_ID = "bs-drm";
const SOURCE_NAME = "DumbRequestManager";

function _scriptManifest() {
    return {
        name: SOURCE_NAME,
        description: "Button events for the DumbRequestManager Beat Saber mod",
        author: "TheBlackParrot",
        website: "https://github.com/TheBlackParrot/DumbRequestManager",
        version: "0.0.4",
        firebotVersion: "5",
        startupOnly: true
    }
}
function _defaultParameters() {
    return {
        address: {
            type: "string",
            description: "IP/hostname of the WebSocket connection",
            default: "localhost"
        },
        port: {
            type: "number",
            description: "Port number of the WebSocket connection",
            default: 13338
        }
    }
}

module.exports = {
    getScriptManifest: _scriptManifest,
    getDefaultParameters: _defaultParameters,
    
    run: (runRequest) => {
        const eventManager = runRequest.modules.eventManager;
        const replaceVariableManager = runRequest.modules.replaceVariableManager;
        const logger = runRequest.modules.logger; // this is awful, let me have my regular console please
        
        let isBSDRMQueueOpen = false;
        
        // gee howdy i hope firebot installs itself to the same folder each and every time
        // i am not telling people to install node and install ws through a terminal. nuh uh. y'all provide ws already
        const ws = require(`../../../../../../../Local/firebot/app-${runRequest.firebot.version}/resources/app/node_modules/ws`);
        
        logger.debug("Registering event source...");
        eventManager.registerEventSource({
            id: SOURCE_ID,
            name: SOURCE_NAME,
            events: [
                {
                    id: `play-button-pressed`,
                    name: "Play Button Pressed",
                    description: "When you want to play a request"
                },
                {
                    id: `skip-button-pressed`,
                    name: "Skip Button Pressed",
                    description: "When a request is skipped"
                },
                {
                    id: `ban-button-pressed`,
                    name: "Ban Button Pressed",
                    description: "When you ban a map from being requested"
                },
                {
                    id: `link-button-pressed`,
                    name: "Link Button Pressed",
                    description: "When you link the selected request to your chat"
                },
                {
                    id: `poke-button-pressed`,
                    name: "Poke Button Pressed",
                    description: "When you want to grab the attention of the next requester in queue"
                },
                {
                    id: `queue-gate-changed`,
                    name: "Queue Gate Changed",
                    description: "When the queue opens or closes"
                },
                {
                    id: `connection-opened`,
                    name: "Connection Opened",
                    description: "When the connection to DRM opens"
                },
                {
                    id: `connection-closed`,
                    name: "Connection Closed",
                    description: "When the connection to DRM closes"
                },
                {
                    id: `queue-cleared`,
                    name: "Queue Cleared",
                    description: "When the queue is cleared"
                }
            ]
        });

        logger.debug("Registering replace variable...");
        if(!replaceVariableManager._registeredVariableHandlers.has("bsDRMMapInfo")) {
            replaceVariableManager.registerReplaceVariable({
                definition: {
                    handle: "bsDRMMapInfo",
                    usage: "bsDRMMapInfo[key]",
                    triggers: {
                        event: [
                            `${SOURCE_ID}:play-button-pressed`,
                            `${SOURCE_ID}:skip-button-pressed`,
                            `${SOURCE_ID}:ban-button-pressed`,
                            `${SOURCE_ID}:link-button-pressed`,
                            `${SOURCE_ID}:poke-button-pressed`
                        ]
                    },
                    description: "The map information attached to the button press, see https://github.com/TheBlackParrot/DumbRequestManager#map-data for map data keys",
                    possibleDataOutput: ["text"]
                },
                evaluator: (trigger, key) => {
                    let data = trigger.metadata.eventData;
                    if(key in data) {
                        return data[key];
                    }
                    return "";
                }
            });
        }

        if(!replaceVariableManager._registeredVariableHandlers.has("bsDRMQueueOpen")) {
            replaceVariableManager.registerReplaceVariable({
                definition: {
                    handle: "bsDRMQueueOpen",
                    usage: "bsDRMQueueOpen",
                    description: "Boolean determining the queue's open/closed status",
                    possibleDataOutput: ["bool"]
                },
                evaluator: () => {
                    return isBSDRMQueueOpen;
                }
            });
        }
        
        let currentReconnectDelay = 5000;

        function startDRMSocket() {
            logger.info("Starting WebSocket connection to DRM...");
            let socket;
            try {
                socket = new ws.WebSocket(`ws://${runRequest.parameters.address}:${runRequest.parameters.port}`);
            } catch (e) {
                // shh
            }
            let reconnectTimeout;
            
            socket.on("close", function() {
                clearTimeout(reconnectTimeout);

                eventManager.triggerEvent(SOURCE_ID, "connection-closed");
                
                logger.warn(`Connection to DRM lost`);
                reconnectTimeout = setTimeout(startDRMSocket, currentReconnectDelay);
            });
            socket.on("error", function() {
                clearTimeout(reconnectTimeout);
                logger.warn(`Connection to DRM could not be established, trying to reconnect in ${currentReconnectDelay / 1000} seconds...`);

                reconnectTimeout = setTimeout(startDRMSocket, currentReconnectDelay);

                currentReconnectDelay *= 1.5;
                currentReconnectDelay = Math.min(Math.ceil(currentReconnectDelay), 90000);
            })

            socket.on("open", function() {
                clearTimeout(reconnectTimeout);
                
                currentReconnectDelay = 5000;
                logger.info("Connected to DRM");
                eventManager.triggerEvent(SOURCE_ID, "connection-opened");
            });

            socket.on("message", function(message) {
                logger.debug(`received DRM message`);
                let event = JSON.parse(message);
                
                switch(event.EventType) {
                    case "pressedPlay":
                        eventManager.triggerEvent(SOURCE_ID, "play-button-pressed", event.Data);
                        break;
                        
                    case "pressedSkip":
                        eventManager.triggerEvent(SOURCE_ID, "skip-button-pressed", event.Data);
                        break;

                    case "pressedBan":
                        eventManager.triggerEvent(SOURCE_ID, "ban-button-pressed", event.Data);
                        break;

                    case "pressedLink":
                        eventManager.triggerEvent(SOURCE_ID, "link-button-pressed", event.Data);
                        break;

                    case "pressedPoke":
                        eventManager.triggerEvent(SOURCE_ID, "poke-button-pressed", event.Data);
                        break;
                        
                    case "queueOpen":
                        isBSDRMQueueOpen = event.Data ?? false;
                        eventManager.triggerEvent(SOURCE_ID, "queue-gate-changed");
                        break;
                        
                    case "queueCleared":
                        eventManager.triggerEvent(SOURCE_ID, "queue-cleared");
                        break;
                }
            });
        }
        
        startDRMSocket();
    }
}