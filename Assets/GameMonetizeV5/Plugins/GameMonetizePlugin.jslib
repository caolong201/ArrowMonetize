var functions = {
	InitApi: function(gameKey) {
	
	gameKey = UTF8ToString(gameKey);
	
	 window["SDK_OPTIONS"] = {
        "gameId": gameKey, // Your gameId which is unique for each one of your games; can be found at your GameMonetize.com account.
        "onEvent": function(event) {
            switch (event.name) {
                case "SDK_GAME_START":
                    SendMessage('GameMonetize', 'ResumeGame');
                    break;
                case "SDK_GAME_PAUSE":
                    SendMessage('GameMonetize', 'PauseGame');
                    break;
				case "SDK_ERROR":					
					break;
            }
        },
    };
	(function(d, s, id) {
    var js, fjs = d.getElementsByTagName(s)[0];
    if (d.getElementById(id)) return;
    js = d.createElement(s);
    js.id = id;
    js.src = 'https://api.gamemonetize.com/sdk.js';
    fjs.parentNode.insertBefore(js, fjs);
}(document, 'script', 'gamemonetize-sdk'));
	},
		 
	ShowBanner: function (gameKey)
	{
	  if (typeof sdk !== "undefined")
	  {	  
		sdk.showBanner(); 
	  }
	},
 };
 
mergeInto(LibraryManager.library, functions);
	
