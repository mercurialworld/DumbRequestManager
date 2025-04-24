This custom Firebot script only adds events for button presses in-game by connecting to DRM's WebSocket API, no other functionality is added.

To use this, place the script in a folder within `%APPDATA%\Firebot\v5\profiles\<your profile name>\scripts`.   
The script will auto-run once added (via the Custom Scripts section in Firebot's settings menu) and once Firebot starts up.

> [!NOTE]
> If/when connection is dropped, it will aggressively attempt to re-establish connection at first (5 seconds), and will slowly back off up to a maximum of 90 seconds over repeated connection attempts.

> [!NOTE]
> The script assumes a Windows-styled user directory structure, as it pulls in Node.js modules directly from the Firebot installation.