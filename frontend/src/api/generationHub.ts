import { HubConnectionBuilder, HubConnection } from "@microsoft/signalr";

const BASE_URL = "http://localhost:5253";

export function buildGenerationConnection(getToken: () => Promise<string>): HubConnection {
    return new HubConnectionBuilder()
        .withUrl(`${BASE_URL}/generate`, {
            accessTokenFactory: getToken,
        })
        .withAutomaticReconnect()
        .build();
}
