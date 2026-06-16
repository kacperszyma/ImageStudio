import axios from "axios";

async function Generate(propmt, model){
    const response = await axios.get("http://localhost:5253/hello")
    return response.data;
}

export {Generate}