package usecase

import (
	"context"
	"errors"
	"time"

	"github.com/ecommerce-platform/user-service/internal/domain"
	"github.com/ecommerce-platform/user-service/internal/repository"
	"github.com/golang-jwt/jwt/v5"
	"github.com/google/uuid"
	"github.com/sirupsen/logrus"
	"golang.org/x/crypto/bcrypt"
)

type UserUseCase struct {
	repo      repository.UserRepository
	logger    *logrus.Logger
	jwtSecret string
}

func NewUserUseCase(repo repository.UserRepository, logger *logrus.Logger, jwtSecret string) *UserUseCase {
	return &UserUseCase{repo: repo, logger: logger, jwtSecret: jwtSecret}
}

func (uc *UserUseCase) RegisterUser(ctx context.Context, req domain.RegisterRequest) (*domain.UserDTO, error) {
	// Validate passwords match
	if req.Password != req.ConfirmPassword {
		return nil, errors.New("passwords do not match")
	}

	// Check if user exists
	existing, _ := uc.repo.FindByEmail(ctx, req.Email)
	if existing != nil {
		return nil, errors.New("user already exists")
	}

	// Hash password
	hashedPassword, err := bcrypt.GenerateFromPassword([]byte(req.Password), bcrypt.DefaultCost)
	if err != nil {
		uc.logger.WithError(err).Error("Failed to hash password")
		return nil, errors.New("internal error")
	}

	// Create user
	user := &domain.User{
		ID:        uuid.New().String(),
		Email:     req.Email,
		Password:  string(hashedPassword),
		FirstName: req.FirstName,
		LastName:  req.LastName,
		IsActive:  true,
		CreatedAt: time.Now(),
		UpdatedAt: time.Now(),
	}

	if err := uc.repo.Create(ctx, user); err != nil {
		uc.logger.WithError(err).Error("Failed to create user")
		return nil, errors.New("failed to create user")
	}

	uc.logger.WithField("user_id", user.ID).Info("User registered successfully")

	return &domain.UserDTO{
		ID:        user.ID,
		Email:     user.Email,
		FirstName: user.FirstName,
		LastName:  user.LastName,
		CreatedAt: user.CreatedAt.String(),
	}, nil
}

func (uc *UserUseCase) LoginUser(ctx context.Context, req domain.LoginRequest) (*domain.LoginResponse, error) {
	user, err := uc.repo.FindByEmail(ctx, req.Email)
	if err != nil || user == nil {
		return nil, errors.New("invalid credentials")
	}

	// Verify password
	if err := bcrypt.CompareHashAndPassword([]byte(user.Password), []byte(req.Password)); err != nil {
		return nil, errors.New("invalid credentials")
	}

	// Generate JWT token
	accessToken, expiresAt, err := uc.generateToken(user.ID)
	if err != nil {
		uc.logger.WithError(err).Error("Failed to generate token")
		return nil, errors.New("internal error")
	}

	uc.logger.WithField("user_id", user.ID).Info("User logged in successfully")

	return &domain.LoginResponse{
		Email:        user.Email,
		UserID:       user.ID,
		AccessToken:  accessToken,
		TokenType:    "Bearer",
		ExpiresAt:    expiresAt,
	}, nil
}

func (uc *UserUseCase) GetUserProfile(ctx context.Context, userID string) (*domain.UserDTO, error) {
	user, err := uc.repo.FindByID(ctx, userID)
	if err != nil {
		return nil, errors.New("user not found")
	}

	return &domain.UserDTO{
		ID:        user.ID,
		Email:     user.Email,
		FirstName: user.FirstName,
		LastName:  user.LastName,
		CreatedAt: user.CreatedAt.String(),
	}, nil
}

func (uc *UserUseCase) generateToken(userID string) (string, string, error) {
	expiresAt := time.Now().Add(24 * time.Hour)

	claims := jwt.MapClaims{
		"user_id": userID,
		"exp":     expiresAt.Unix(),
		"iat":     time.Now().Unix(),
	}

	token := jwt.NewWithClaims(jwt.SigningMethodHS256, claims)
	tokenString, err := token.SignedString([]byte(uc.jwtSecret))
	if err != nil {
		return "", "", err
	}

	return tokenString, expiresAt.Format(time.RFC3339), nil
}
