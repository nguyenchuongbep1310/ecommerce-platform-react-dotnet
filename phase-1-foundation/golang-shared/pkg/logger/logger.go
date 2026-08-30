package logger

import (
	"context"
	"os"

	"github.com/sirupsen/logrus"
)

type Logger struct {
	*logrus.Logger
}

var globalLogger *Logger

func init() {
	log := logrus.New()
	log.SetOutput(os.Stdout)
	log.SetFormatter(&logrus.JSONFormatter{})

	if os.Getenv("ENVIRONMENT") == "development" {
		log.SetLevel(logrus.DebugLevel)
	} else {
		log.SetLevel(logrus.InfoLevel)
	}

	globalLogger = &Logger{log}
}

func Get() *Logger {
	return globalLogger
}

func (l *Logger) WithContext(ctx context.Context) *logrus.Entry {
	// Extract request ID or correlation ID from context if available
	traceID := ctx.Value("trace-id")
	if traceID != nil {
		return l.WithField("trace_id", traceID)
	}
	return l.WithContext(ctx)
}

func (l *Logger) WithField(key string, value interface{}) *logrus.Entry {
	return l.Logger.WithField(key, value)
}

func (l *Logger) WithFields(fields logrus.Fields) *logrus.Entry {
	return l.Logger.WithFields(fields)
}

func Info(args ...interface{}) {
	globalLogger.Info(args...)
}

func Infof(format string, args ...interface{}) {
	globalLogger.Infof(format, args...)
}

func Error(args ...interface{}) {
	globalLogger.Error(args...)
}

func Errorf(format string, args ...interface{}) {
	globalLogger.Errorf(format, args...)
}

func Debug(args ...interface{}) {
	globalLogger.Debug(args...)
}

func Debugf(format string, args ...interface{}) {
	globalLogger.Debugf(format, args...)
}

func Warn(args ...interface{}) {
	globalLogger.Warn(args...)
}

func Warnf(format string, args ...interface{}) {
	globalLogger.Warnf(format, args...)
}

func Fatal(args ...interface{}) {
	globalLogger.Fatal(args...)
}

func Fatalf(format string, args ...interface{}) {
	globalLogger.Fatalf(format, args...)
}
