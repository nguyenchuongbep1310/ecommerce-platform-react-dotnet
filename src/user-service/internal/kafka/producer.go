package kafka

import (
	"encoding/json"
	"time"

	"github.com/confluentinc/confluent-kafka-go/v2/kafka"
	"github.com/sirupsen/logrus"
)

type UserEventProducer struct {
	producer *kafka.Producer
	logger   *logrus.Logger
}

func NewUserEventProducer(brokers string, logger *logrus.Logger) (*UserEventProducer, error) {
	p, err := kafka.NewProducer(&kafka.ConfigMap{
		"bootstrap.servers": brokers,
	})
	if err != nil {
		return nil, err
	}

	return &UserEventProducer{producer: p, logger: logger}, nil
}

func (p *UserEventProducer) PublishUserRegistered(userID, email string) error {
	event := map[string]interface{}{
		"event_type": "user.registered",
		"user_id":    userID,
		"email":      email,
		"timestamp":  time.Now(),
	}

	data, _ := json.Marshal(event)

	return p.producer.Produce(&kafka.Message{
		TopicPartition: kafka.TopicPartition{
			Topic:     ptrString("user.events"),
			Partition: kafka.PartitionAny,
		},
		Key:   []byte(userID),
		Value: data,
	}, nil)
}

func (p *UserEventProducer) Close() {
	p.producer.Close()
}

func ptrString(s string) *string {
	return &s
}
