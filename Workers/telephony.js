const OPTIONS = {
	INCOMING_MESSAGES_URL: "https://discord.com/api/webhooks/872961650909655050/MUwdTXo_8MlKaO_n4vsHyhT2ydTNDpaj2T0gnNJ-ZntFq6xlEa7fzyq-i1Z4eRP4ZlXv",
	OUTGOING_MESSAGES_URL: "https://discord.com/api/webhooks/872962001054347285/r9QTtf26Siee_d6Xkmk9CIpn7PBoD8eAXhbxZPAssAzU5QV4hx2WA3XxXSnOYYGuBc4P",
	DEBUG_URL: "https://discord.com/api/webhooks/862362281626173451/Mebedh8Naobcnlj5GcPN-g61USfMZcg36EX79lsEufoBmovtG_WRNuc-DAXxp8jv0ra0"
};

const Database = {
    Account: "25d8afc85e67f4647db7334e223ff6b4",
    Authorization: "Bearer 982jEY_yzYvMLZh6noyBiF032HL5urJgxH9pz_j3",
    Namespaces: {
        Events: "940f68b46ddd454bb22aa9300d71f616",
        IncomingMessages: "1edea3fb7b0b4a9d9e420a5cd75306ca",
        OutgoingMessages: "7688f30c919a4afe9b293f834bf58f3a"
    }
};

async function handleIncomingMessage(data) {
	await fetch(OPTIONS.INCOMING_MESSAGES_URL, {
		method: "POST",
		headers: {
			"Accept": "application/json",
			"Content-Type": "application/json"
		},
		body: JSON.stringify({
			embeds: [{
				fields: [{
					name: "Sender",
					value: "```" + data.sender + "```",
					inline: true
				}, {
					name: "Recipient",
					value: "```" + data.recipient + "```",
					inline: true
				}, {
					name: "Content",
					value: "```" + data.content + "```"
				}]
			}]
		})
	});

    //await fetch("https://api.cloudflare.com/client/v4/accounts/01a7362d577a6c3019a474fd6f485823/storage/kv/namespaces/0f2ac74b498b48028cb68387c421e279/values/My-Key", {
//
    //})

    await Messages.put(new Date().toString(), JSON.stringify({
        type: "incoming",
        sender: data.sender,
        recipient: data.recipient,
        content: data.content
    }));
}

async function handleOutgoingMessage(data) {
	await fetch(OPTIONS.OUTGOING_MESSAGES_URL, {
		method: "POST",
		headers: {
			"Accept": "application/json",
			"Content-Type": "application/json"
		},
		body: JSON.stringify({
			embeds: [{
				fields: [{
					name: "Sender",
					value: "```" + data.sender + "```",
					inline: true
				}, {
					name: "Recipient",
					value: "```" + data.recipient + "```",
					inline: true
				}, {
					name: "Status",
					value: "```" + data.status + "```",
					inline: true
				}, {
					name: "Content",
					value: "```" + data.content + "```"
				}]
			}]
		})
	});

    await Messages.put(new Date().toString(), JSON.stringify({
        type: "outgoing",
        sender: data.sender,
        recipient: data.recipient,
        status: data.status,
        content: data.content
    }));
}


async function handleRequest(request) {
    const url = new URL(request.url);

    if (request.method === "POST" && url.pathname === "/v1/events") {
        const event = await request.json();

        //await fetch(OPTIONS.DEBUG_URL, {
        //    method: "POST",
        //    headers: {
        //        "Accept": "application/json",
        //        "Content-Type": "application/json"
        //    },
        //    body: JSON.stringify({
        //        content: "```json\n" + JSON.stringify(event, null, 4) + "\n```"
        //    })
        //});

        if (event.data && event.data.record_type === "event") {
            await Events.put(event.data.id, JSON.stringify(event, null, 4));

            if (event.data.event_type === "message.received") {
                // event.data.payload.from.phone_number
                // event.data.payload.to[].phone_number
                // event.data.payload.text
                // event.data.payload.type (SMS/MMS)

                for (let i = 0; i < event.data.payload.to.length; i++) {
                    await handleIncomingMessage({
                        sender: event.data.payload.from.phone_number,
                        recipient: event.data.payload.to[i].phone_number,
                        content: event.data.payload.text
                    });
                }
            }
            
            if (event.data.event_type === "message.sent") {
                // event.data.payload.from.phone_number
                // event.data.payload.to[].phone_number
                // event.data.payload.to[].status
                // event.data.payload.text
                // event.data.payload.type (SMS/MMS)

                for (let i = 0; i < event.data.payload.to.length; i++) {
                    await handleOutgoingMessage({
                        sender: event.data.payload.from.phone_number,
                        recipient: event.data.payload.to[i].phone_number,
                        content: event.data.payload.text,
                        status: event.data.payload.to[i].status
                    });
                }

                /* statuses
                    queued	The message is queued up on Telnyx's side.
                    sending	The message is currently being sent to an upstream provider.
                    sent	The message has been sent to the upstream provider.
                    delivered	The upstream provider has confirmed delivery of the message.
                    sending_failed	Telnyx has failed to send the message to the upstream provider. Please reach out to our support if you have received this status.
                    delivery_failed	The upstream provider has failed to send the message to the receiver. Please reach out to our support if you have received this status.
                    delivery_unconfirmed There is no indication whether or not the message has reached the receiver. Please reach out to our support if you have received this status
                */
            }

            if (event.data.event_type === "message.finalized") {
                // event.data.payload.from.phone_number
                // event.data.payload.to[].phone_number
                // event.data.payload.to[].status
                // event.data.payload.text
                // event.data.payload.type (SMS/MMS)

                for (let i = 0; i < event.data.payload.to.length; i++) {
                    await handleOutgoingMessage({
                        sender: event.data.payload.from.phone_number,
                        recipient: event.data.payload.to[i].phone_number,
                        content: event.data.payload.text,
                        status: event.data.payload.to[i].status
                    });
                }
            }

            if (event.data.event_type === "call.initiated") {
                // event.data.payload.from
                // event.data.payload.to
                // event.data.payload.direction (incoming/outgoing)

                if (event.data.payload.direction === "incoming") {
                    
                    /*

                    await fetch(`https://api.telnyx.com/v2/calls/${event.data.payload.call_control_id}/actions/answer`, {
                        method: "POST",
                        headers: {
                            "Accept": "application/json",
                            "Content-Type": "application/json",
                            "Authorization": CONFIG.AUTHORIZATION
                        },
                        body: JSON.stringify({ })
                    });
                    */
/*
                    await fetch(`https://apif.telnyx.com/v2/calls/${event.data.payload.call_control_id}/actions/transfer`, {
                        method: "POST",
                        headers: {
                            "Accept": "application/json",
                            "Content-Type": "application/json",
                            "Authorization": CONFIG.AUTHORIZATION
                        },
                        body: JSON.stringify({
                            to: "+18002752273"
                        })
                    });
                    */
/*
                    await fetch(`https://api.telnyx.com/v2/calls/${event.data.payload.call_control_id}/actions/playback_start`, {
                        method: "POST",
                        headers: {
                            "Accept": "application/json",
                            "Content-Type": "application/json",
                            "Authorization": CONFIG.AUTHORIZATION
                        },
                        body: JSON.stringify({
                            audio_url: "https://cdn.discordapp.com/attachments/857050186810916886/862378484092895252/f5c88490-df32-11eb-927b-c585b354387c.mp3"
                        })
                    });*/
                }
            }

            if (event.data.event_type === "call.answered") {
                // event.data.payload.from
                // event.data.payload.to
            }

            if (event.data.event_type === "call.hangup") {
                // event.data.payload.from
                // event.data.payload.to
                // event.data.payload.hangup_cause
                // event.data.payload.hangup_source
                // event.data.payload.sip_hangup_cause
            }

            return new Response(null, {
                status: 200
            });
        } else {
            return new Response(null, {
                status: 400
            });
        }
    }

    if (request.method === "GET" && url.pathname === "/v2/messages/incoming") {
        const message = {
            sender: url.searchParams.get("msisdn"),
            recipient: url.searchParams.get("to"),
            content: url.searchParams.get("text")
        };

        await handleIncomingMessage({
            sender: isNaN(message.sender) ? message.sender : `+${message.sender}`,
            recipient: isNaN(message.recipient) ? message.recipient : `+${message.recipient}`,
            content: message.content
        });

        return new Response(null, {
            status: 200
        });
    }

    if (request.method === "GET" && url.pathname === "/v2/messages/outgoing") {
        // Handle outgoing Vonage SMS

        return new Response(null, {
            status: 200
        });
    }

    if (request.method === "GET" && url.pathname === "/inbox/raw-data") {
        const list = await Messages.list({ limit: 32 });
        const result = {};

        for (let i = 0; i < list.keys.length; i++) {
            result[list.keys[i].name] = await Messages.get(list.keys[i].name, { type: "json" });
        }

        return new Response(JSON.stringify(result, null, 4), {
            status: 200,
            headers: {
                "Content-Type": "application/json"
            }
        });
    }

    return new Response(null, {
        status: 404
    });
}

addEventListener("fetch", (event) => {
    event.respondWith(handleRequest(event.request));
});