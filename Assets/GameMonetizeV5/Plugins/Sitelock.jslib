mergeInto(LibraryManager.library, {
    SitelockCheck: function() {
        // Disable console logs
        console.log = console.error = console.warn = console.info = function() {};

        var currentLocation = window.location.toString();
        if (
            currentLocation.startsWith("http://localhost") || 
            currentLocation.startsWith("http://localhost:") ||
            window.location.hostname === "127.0.0.1" || 
            currentLocation.startsWith("file:///")
        ) {
            console.log("Running on localhost or directly from filesystem. Bypassing sitelock checks.");
            return; // Exit the function early, bypassing sitelock checks.
        }


        var topurl;

        function getDomainWithoutSubdomain(url) {
            const urlParts = new URL(url).hostname.split('.');
            return urlParts.slice(urlParts.length === 4 ? -3 : -2).join('.');
        }

        try {
            try {
                topurl = document.location.ancestorOrigins[0] || document.referrer || (window.location != window.parent.location ? window.parent.location : window.location);
            } catch (e) {
                topurl = document.referrer || (window.location != window.parent.location ? window.parent.location : window.location);
            }
        } catch (e) {
            topurl = window.location != window.parent.location ? window.parent.location : window.location;
        }

        topurl = getDomainWithoutSubdomain(topurl);

        if (topurl !== null) {
            const blreq = new XMLHttpRequest();

            blreq.addEventListener("load", function() {
                const bljson = this.responseText;
                if (!bljson.includes(topurl)) {
                    console.log("Sitelock Debug: Bl Pass");

                    try {
                        const runningurl = getDomainWithoutSubdomain(window.location);
                        const wlreq = new XMLHttpRequest();

                        wlreq.addEventListener("load", function() {
                            const wljson = this.responseText;
                            if (wljson.indexOf(runningurl) !== -1) {
                                console.log("Sitelock Debug: Pass");
                            } else {
                                console.log("Sitelock Debug: Tripped");
                                SendMessage('GameMonetize', 'QuitGame');
                            }
                        });

                        wlreq.addEventListener("error", function() {
                            console.error("Sitelock Debug: Transfer Error");
                            SendMessage('GameMonetize', 'QuitGame');
                        });

                        wlreq.open("GET", "https://api.gamemonetize.com/lock.json", true);
                        wlreq.send();

                    } catch (e) {
                        console.log("Sitelock Debug: Error");
                        console.error(e);
                    }

                } else {
                    console.log("Sitelock Debug: Bl Tripped");
                    SendMessage('GameMonetize', 'QuitGame');
                }
            });

            blreq.addEventListener("error", function() {
                console.error("Sitelock Debug: Bl Transfer Error");
                SendMessage('GameMonetize', 'QuitGame');
            });

            blreq.open("GET", "https://api.gamemonetize.com/blacklist.json", true);
            blreq.send();

        } else {
            console.log("Sitelock Debug: Bl Sandbox");
            SendMessage('GameMonetize', 'QuitGame');
        }
    }
});
