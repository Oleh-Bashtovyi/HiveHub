import axios from 'axios';
import { API_ROUTES } from '../const/api-routes';

export const axiosInstance = axios.create({
    baseURL: API_ROUTES.BASE,
    headers: {
        'Content-Type': 'application/json',
    },
});