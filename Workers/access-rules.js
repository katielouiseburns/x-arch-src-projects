const RESPONSE_OPTIONS = {
    status: 200,
    headers: {
        "Content-Type": "application/json"
    }
};

async function handleRequest(request) {
    const url = new URL(request.url);

    /* WARNING: Only modify fields that have comments on them. */
    /* Ensure you place countries in arrays inside quotes (eg. ["*"] or ["US", "CA"]). */
    /* Make sure true/false (booleans) are not placed in quotes, but written as keyword. */
    /* Keep the code clean, do not mess up spacing, thanks. */

    /* Access rules for card verify tool */
    if (url.pathname === "/access-rules/card_verify.json") return new Response(JSON.stringify({
        Enable: false, /* To enable, write true, to disable, write false. */
        Include: ["*"], /* Inside brackets, include specific countries or write "*" (eg. ["US", "CA"]). */
        Exclude: ["us", "ca", "lt", "lv", "ee", "id"], /* Inside brackets, exclude specific countries, if "*" is used above. */
        Location: null
    }, null, 4), RESPONSE_OPTIONS);

    /* Access rules for card submit tool */
    if (url.pathname === "/access-rules/card_submit.json") return new Response(JSON.stringify({
        Enable: false, /* To enable, write true, to disable, write false. */
        Include: ["*"], /* Inside brackets, include specific countries or write "*" (eg. ["US", "CA"]). */
        Exclude: ["US", "CA", "LV", "LT", "EE"], /* Inside brackets, exclude specific countries, if "*" is used above. */
        Location: "XL" /* Specify the location for the tool */
    }, null, 4), RESPONSE_OPTIONS);

    /* Access rules for paypal tool */
    if (url.pathname === "/access-rules/paypal.json") return new Response(JSON.stringify({
        Enable: false, /* To enable, write true, to disable, write false. */
        Include: ["*"], /* Inside brackets, include specific countries or write "*" (eg. ["US", "CA"]). */
        Exclude: ["LT", "LV", "EE", "US", "CA", "ID", "DK"], /* Inside brackets, exclude specific countries, if "*" is used above. */
        Location: null
    }, null, 4), RESPONSE_OPTIONS);

    return new Response(null, {
        status: 404
    });
}

addEventListener("fetch", (event) => {
    event.respondWith(handleRequest(event.request));
});