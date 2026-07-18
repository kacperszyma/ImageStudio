import { HubConnectionBuilder, HubConnection } from "@microsoft/signalr";

const BASE_URL = import.meta.env.VITE_API_URL ?? "http://localhost:5253";

export type GenerationHandlers = {
    /** Server accepted and enqueued the job. The image is not ready yet. */
    onAccepted?: (jobId: string) => void;
    /** Pushed from the provider webhook once the image is ready. */
    onComplete?: (jobId: string, imageUrl: string) => void;
    /** Job failed — either rejected at enqueue or failed/expired later. */
    onFailed?: (reason: string) => void;
};

export function buildGenerationConnection(getToken: () => Promise<string>): HubConnection {
    return new HubConnectionBuilder()
        .withUrl(`${BASE_URL}/generate`, {
            accessTokenFactory: getToken,
        })
        .withAutomaticReconnect()
        .build();
}

/** Wires the server -> client events. Keeps the event-name contract in one place. */
export function registerGenerationHandlers(
    connection: HubConnection,
    handlers: GenerationHandlers,
): void {
    connection.on("GenerationAccepted", (jobId: string) => handlers.onAccepted?.(jobId));

    connection.on("GenerationComplete", (jobId: string, imageUrl: string) =>
        handlers.onComplete?.(jobId, imageUrl),
    );

    // Payload is a message string on enqueue failure, or a jobId on a later
    // webhook failure/expiry; either way it just means "this job failed".
    connection.on("GenerationFailed", (reason: string) => handlers.onFailed?.(String(reason)));
}

/** Client -> server: kick off a generation. Resolves once the server has the request. */
export function startGeneration(
    connection: HubConnection,
    modelSlug: string,
    prompt: string,
): Promise<void> {
    return connection.invoke("Generate", modelSlug, prompt);
}
