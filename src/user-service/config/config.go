package config

import (
	"os"

	"github.com/joho/godotenv"
)

type Config struct {
	DatabaseURL string
	JWTSecret   string
	Port        string
	Environment string
}

func Load() *Config {
	godotenv.Load()

	return &Config{
		DatabaseURL: getEnv("DATABASE_URL_USER", "postgres://postgres:postgres@localhost:5432/user_db?sslmode=disable"),
		JWTSecret:   getEnv("JWT_SECRET", "your-secret-key"),
		Port:        getEnv("USER_SERVICE_PORT", "5001"),
		Environment: getEnv("ENVIRONMENT", "development"),
	}
}

func getEnv(key, defaultVal string) string {
	if value, exists := os.LookupEnv(key); exists {
		return value
	}
	return defaultVal
}
