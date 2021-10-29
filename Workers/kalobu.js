async function handleRequest(request) {
    const url = new URL(request.url);
    url.hostname = "katiemccurdy.com";

    return await fetch(url, {
        method: request.method,
        headers: request.headers,
        body: request.body
    });
}

addEventListener("fetch", (event) => {
    event.respondWith(handleRequest(event.request));
});