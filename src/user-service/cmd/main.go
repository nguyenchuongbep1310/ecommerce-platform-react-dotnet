package main

import (
	"fmt"
	"log"
	"os"

	"github.com/ecommerce-platform/user-service/internal/handler"
	"github.com/ecommerce-platform/user-service/internal/middleware"
	"github.com/ecommerce-platform/user-service/internal/repository"
	"github.com/ecommerce-platform/user-service/internal/usecase"
	"github.com/ecommerce-platform/user-service/config"
	"github.com/gin-gonic/gin"
	"github.com/sirupsen/logrus"
	"gorm.io/driver/postgres"
	"gorm.io/gorm"
)

func main() {
	// Load config
	cfg := config.Load()

	// Setup database
	db, err := gorm.Open(postgres.Open(cfg.DatabaseURL), &gorm.Config{})
	if err != nil {
		log.Fatalf("Failed to connect to database: %v", err)
	}

	// Setup logger
	logger := logrus.New()
	logger.SetOutput(os.Stdout)
	logger.SetFormatter(&logrus.JSONFormatter{})

	// Initialize repositories
	userRepo := repository.NewPostgresUserRepository(db)

	// Initialize use cases
	userUC := usecase.NewUserUseCase(userRepo, logger, cfg.JWTSecret)

	// Initialize handlers
	userHandler := handler.NewUserHandler(userUC, logger)

	// Setup Gin router
	router := gin.Default()

	// Routes
	router.POST("/auth/register", userHandler.Register)
	router.POST("/auth/login", userHandler.Login)
	router.GET("/auth/profile", middleware.AuthMiddleware(cfg.JWTSecret, logger), userHandler.GetProfile)

	// Health check
	router.GET("/health", func(c *gin.Context) {
		c.JSON(200, gin.H{"status": "ok"})
	})

	port := cfg.Port
	fmt.Printf("Starting UserService on port %s\n", port)
	if err := router.Run(":" + port); err != nil {
		log.Fatalf("Failed to start server: %v", err)
	}
}
