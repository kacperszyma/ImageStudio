import axios from "axios";

const BASE_URL = "http://localhost:5253"

type GetTokenFn = () => Promise<string>

async function config(getToken: GetTokenFn) {
    return {
        headers: {
            'Authorization': 'Bearer ' + await getToken()
        }
    }
}

export type ImageModel = { slug: string; creditCost: number }

export type PebblePackage = {
    nameId: string
    dollarPrice: number
    pebbleAmount: number
    discountAmount: number
}

export type GenerationDetails = {
    jobId: string
    modelSlug: string
    prompt: string
    imageUrl: string | null
    creditCost: number
    status: string
}

export type GenerationDetailDto = {
    jobId: string
    modelSlug: string
    prompt: string
    imageUrl: string | null
    creditCost: number
    status: string
    createdAt: string
    completedAt: string | null
    duration: string | null
    balanceBefore: number
    balanceAfter: number
}

export type TransactionDto = {
    id: string
    userId: string
    amount: number
    type: "TopUp" | "Freeze" | "Charge" | "Unfreeze"
    createdAt: string
}

export type TransactionDetailDto = {
    id: string
    userId: string
    amount: number
    type: "TopUp" | "Freeze" | "Charge" | "Unfreeze"
    createdAt: string
    generationJobId: string | null
}

// Real-money purchase of a Pebble package (GET /transactions).
export type PurchaseDto = {
    id: string
    packageNameId: string
    dollarAmount: number
    pebbleAmount: number
    createdAt: string
}

async function GetModels(getToken: GetTokenFn): Promise<ImageModel[]> {
    const response = await axios.get(BASE_URL + "/models", await config(getToken))
    return response.data;
}

async function Generate(_prompt: string, _model: string, getToken: GetTokenFn): Promise<unknown> {
    const response = await axios.get(BASE_URL + "/hello", await config(getToken))
    return response.data;
}

async function GetBalance(getToken: GetTokenFn): Promise<number> {
    const response = await axios.get(BASE_URL + "/balance", await config(getToken))
    return response.data;
}

async function GetHistory(getToken: GetTokenFn, limit?: number): Promise<GenerationDetails[]> {
    const response = await axios.get(BASE_URL + "/history", {
        ...await config(getToken),
        params: limit ? { limit } : undefined,
    })
    return response.data;
}

// Pebble charge history — one entry per generation (GET /spend).
async function GetSpendingHistory(getToken: GetTokenFn): Promise<TransactionDto[]> {
    const response = await axios.get(BASE_URL + "/spend", await config(getToken))
    return response.data;
}

// Real-money purchase history (GET /transactions).
async function GetPurchases(getToken: GetTokenFn): Promise<PurchaseDto[]> {
    const response = await axios.get(BASE_URL + "/transactions", await config(getToken))
    return response.data;
}

export type TransactionResult =
    | { kind: 'transaction'; data: TransactionDetailDto }
    | { kind: 'generation'; generationId: string }

async function GetSpendDetail(id: string, getToken: GetTokenFn): Promise<TransactionResult> {
    const token = await getToken()
    const response = await fetch(BASE_URL + `/spend/${id}`, {
        headers: { Authorization: `Bearer ${token}` },
    })
    if (!response.ok) throw new Error(String(response.status))

    const finalPath = new URL(response.url).pathname
    const genMatch = finalPath.match(/^\/generations\/(.+)$/)
    if (genMatch) return { kind: 'generation', generationId: genMatch[1] }

    return { kind: 'transaction', data: await response.json() }
}

async function GetGenerationDetail(id: string, getToken: GetTokenFn): Promise<GenerationDetailDto> {
    const response = await axios.get(BASE_URL + `/generations/${id}`, await config(getToken))
    return response.data;
}

async function GetPackages(getToken: GetTokenFn): Promise<PebblePackage[]> {
    const response = await axios.get(BASE_URL + "/packages", await config(getToken))
    return response.data;
}

// Creates a Stripe Checkout session for the given package and returns the
// client secret used to mount the embedded checkout.
async function CreateCheckoutSession(packageId: string, getToken: GetTokenFn): Promise<string> {
    const response = await axios.post(
        BASE_URL + "/checkout",
        { packageId },
        await config(getToken),
    )
    return response.data.clientSecret;
}

async function RedeemSession(sessionId: string, getToken: GetTokenFn): Promise<void> {
    await axios.post(BASE_URL + "/checkout/redeem", { sessionId }, await config(getToken))
}

export { Generate, GetModels, GetBalance, GetHistory, GetSpendingHistory, GetPurchases, GetSpendDetail, GetGenerationDetail, GetPackages, CreateCheckoutSession, RedeemSession }
