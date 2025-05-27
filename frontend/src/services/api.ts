import axios from 'axios';

const API_URL = 'http://localhost:5158/api';

const api = axios.create({
    baseURL: API_URL,
    headers: {
        'Content-Type': 'application/json',
    },
});

// Add token to requests if it exists
api.interceptors.request.use((config) => {
    const token = localStorage.getItem('token');
    if (token) {
        config.headers.Authorization = `Bearer ${token}`;
    }
    return config;
});

export const authService = {
    login: async (username: string, password: string) => {
        const response = await api.post('/Account/login', { username, password });
        return response.data;
    },
    register: async (username: string, email: string, password: string) => {
        const response = await api.post('/Account/register', { username, email, password });
        return response.data;
    },
};

export const currencyService = {
    getRates: async () => {
        const response = await api.get('/Rate');
        return response.data;
    },
    convertCurrency: async (fromCurrency: string, toCurrency: string, amount: number) => {
        const response = await api.post('/Payment/create', {
            amount,
            currency: fromCurrency
        });
        return response.data;
    },
};

export default api; 