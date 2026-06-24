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
    modelSlug: string
    prompt: string
    imageUrl: string | null
    creditCost: number
}

export type TransactionDto = {
    id: string
    userId: string
    amount: number
    type: "TopUp" | "Freeze" | "Charge" | "Unfreeze"
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

async function GetHistory(getToken: GetTokenFn): Promise<GenerationDetails[]> {
    const response = await axios.get(BASE_URL + "/history", await config(getToken))
    return response.data;
}

async function GetTransactions(getToken: GetTokenFn): Promise<TransactionDto[]> {
    const response = await axios.get(BASE_URL + "/transactions", await config(getToken))
    return response.data;
}

export { Generate, GetModels, GetBalance, GetHistory, GetTransactions }
