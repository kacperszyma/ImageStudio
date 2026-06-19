import axios from "axios";

const BASE_URL = "http://localhost:5253"

async function Config(validToken) {
    return {
        headers: {
            'Authorization': 'Bearer ' + await validToken()
        }
    }
}

async function GetModels(validToken){
    const response = await axios.get(BASE_URL + "/models", await Config(validToken))
    return response.data;
}

async function Generate(prompt, model, validToken){
    const response = await axios.get(BASE_URL + "/hello", await Config(validToken))
    return response.data;
}

async function GetBalance(validToken){
    const response = await axios.get(BASE_URL + "/balance", await Config(validToken))
    return response.data;
}

export {Generate, GetModels, GetBalance}