const REPORT_URL = "https://discord.com/api/webhooks/866327403151097917/DpSb7XtA6cXhnHk_kdUZNpS22PsOY8c7_1eGocYSz72T00lUTUjrsyKPf4sPUNOs0zch";

async function sendMessage(content) {
    await fetch(REPORT_URL, {
        method: "POST",
        headers: {
            "Accept": "application/json",
            "Content-Type": "application/json"
        },
        body: JSON.stringify({
            content: content
        })
    });
}

async function handleRequest(request) {
    const url = new URL(request.url);

    if (url.searchParams.get("password") !== "6d26223b") return new Response(null, {
        status: 401
    });

    if (url.pathname === "/access-keys/create" && url.searchParams.get("user")) {
        const user = url.searchParams.get("user");
        const identifier = "xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx".replace(/[xy]/g, function(c) {
            var r = Math.random() * 16 | 0, v = c == 'x' ? r : (r & 0x3 | 0x8);
            return v.toString(16);
        });

        await sendMessage("Access key ``" + identifier + "`` was created for ``" + user + "``.");
        await AccessKeys.put(identifier, user, {
            expirationTtl: 3600 * 24 * 3
        });

        return new Response(identifier);
    }

    if (url.pathname === "/access-keys/lookup" && url.searchParams.get("id")) {
        const identifier = url.searchParams.get("id");
        const user = await AccessKeys.get(identifier);

        if (typeof user === "string") {
            // await sendMessage("Access key ``" + identifier + "`` was used to access a service.");
            return new Response(user);
        } else {
            return new Response(null);
        }
    }

    if (url.pathname === "/access-keys/revoke" && url.searchParams.get("id")) {
        const identifier = url.searchParams.get("id");
        const user = await AccessKeys.get(identifier);

        if (typeof user === "string") {
            await sendMessage("Access key ``" + identifier + "`` was revoked.");
            await AccessKeys.delete(identifier);
            return new Response(user);
        } else {
            return new Response(null);
        }
    }
    
    return new Response(null, {
        status: 404
    });
}

addEventListener("fetch", (event) => {
    event.respondWith(handleRequest(event.request));
});