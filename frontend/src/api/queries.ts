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

export type GenerationDetails = {
    jobId: string
    modelSlug: string
    prompt: string
    imageUrl: string | null
    creditCost: number
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

async function GetHistory(getToken: GetTokenFn): Promise<GenerationDetails[]> {
    const response = await axios.get(BASE_URL + "/history", await config(getToken))
    return response.data;
}

async function GetTransactions(getToken: GetTokenFn): Promise<TransactionDto[]> {
    const response = await axios.get(BASE_URL + "/transactions", await config(getToken))
    return response.data;
}

export type TransactionResult =
    | { kind: 'transaction'; data: TransactionDetailDto }
    | { kind: 'generation'; generationId: string }

async function GetTransactionDetail(id: string, getToken: GetTokenFn): Promise<TransactionResult> {
    const token = await getToken()
    const response = await fetch(BASE_URL + `/transactions/${id}`, {
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

export { Generate, GetModels, GetBalance, GetHistory, GetTransactions, GetTransactionDetail, GetGenerationDetail }
