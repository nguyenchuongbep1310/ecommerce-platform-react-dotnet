import axios from "axios";

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL || ""; // Use relative path to leverage Vite proxy

const api = axios.create({
  baseURL: API_BASE_URL,
  headers: {
    "Content-Type": "application/json",
    "ngrok-skip-browser-warning": "true",
  },
});

export const productService = {
  getAll: async () => {
    try {
      const response = await api.get("/api/Products");
      return response.data;
    } catch (error) {
      console.error("Error fetching products:", error);
      throw error;
    }
  },
  getById: async (id) => {
    try {
      const response = await api.get(`/api/Products/${id}`);
      return response.data;
    } catch (error) {
      console.error(`Error fetching product ${id}:`, error);
      throw error;
    }
  },
};

export default api;
