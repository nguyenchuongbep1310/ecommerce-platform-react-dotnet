import axios from "axios";

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL || ""; // Use relative path to leverage Vite proxy

const api = axios.create({
  baseURL: API_BASE_URL,
  headers: {
    "Content-Type": "application/json",
  },
});

export const productService = {
  getAll: async (params) => {
    try {
      const response = await api.get("/api/Products", { params });
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

export const orderService = {
  placeOrder: async (payload) => {
    try {
      const response = await api.post("/api/Orders/place", payload);
      return response.data;
    } catch (error) {
      console.error("Error placing order:", error);
      throw error;
    }
  },
  getHistory: async () => {
    try {
      const response = await api.get("/api/Orders/history");
      return response.data;
    } catch (error) {
      console.error("Error fetching order history:", error);
      throw error;
    }
  },
  getById: async (id) => {
    try {
      const response = await api.get(`/api/Orders/${id}`);
      return response.data;
    } catch (error) {
      console.error(`Error fetching order ${id}:`, error);
      throw error;
    }
  },
};

export default api;
