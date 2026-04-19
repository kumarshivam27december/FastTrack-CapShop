pipeline {
  agent any

  options {
    timestamps()
    disableConcurrentBuilds()
  }

  triggers {
    githubPush()
  }

  environment {
    DOCKERHUB_NAMESPACE = 'shivamismyname'
    IMAGE_TAG = "${BUILD_NUMBER}"
    COMPOSE_PROJECT_NAME = 'capshop'
    RAZORPAY_KEY_ID = credentials('razorpay_key_id')
    RAZORPAY_KEY_SECRET = credentials('razorpay_key_secret')
  }

  stages { 
    stage('Checkout') {
      steps {
        checkout scm
      }
    }

    stage('Prepare Auth Config') {
      steps {
        withCredentials([
          string(credentialsId: 'auth_conn_str', variable: 'AUTH_CONN_STR'),
          string(credentialsId: 'auth_jwt_secret', variable: 'AUTH_JWT_SECRET'),
          string(credentialsId: 'auth_google_client_id', variable: 'AUTH_GOOGLE_CLIENT_ID'),
          string(credentialsId: 'auth_twilio_sid', variable: 'AUTH_TWILIO_SID'),
          string(credentialsId: 'auth_twilio_token', variable: 'AUTH_TWILIO_TOKEN'),
          string(credentialsId: 'auth_twilio_phone', variable: 'AUTH_TWILIO_PHONE'),
          string(credentialsId: 'auth_sender_email', variable: 'AUTH_SENDER_EMAIL'),
          string(credentialsId: 'auth_sender_password', variable: 'AUTH_SENDER_PASSWORD')
        ]) {
          sh '''
              set -e
              cd backend/Services/AuthService/CapShop.AuthService

              cat > appsettings.json <<EOF
          {
            "ConnectionStrings": {
              "DefaultConnection": "${AUTH_CONN_STR}"
            },
            "JwtSettings": {
              "Issuer": "CapShop.AuthService",
              "Audience": "CapShop.Client",
              "SecretKey": "${AUTH_JWT_SECRET}",
              "ExpiryMinutes": 120
            },
            "GoogleAuth": {
              "ClientId": "${AUTH_GOOGLE_CLIENT_ID}"
            },
            "Twilio": {
              "AccountSid": "${AUTH_TWILIO_SID}",
              "AuthToken": "${AUTH_TWILIO_TOKEN}",
              "PhoneNumber": "${AUTH_TWILIO_PHONE}"
            },
            "Email": {
              "SmtpHost": "smtp.gmail.com",
              "SmtpPort": "587",
              "SenderEmail": "${AUTH_SENDER_EMAIL}",
              "SenderPassword": "${AUTH_SENDER_PASSWORD}"
            },
            "Otp": {
              "ExpiryMinutes": 5,
              "Length": 6
            },
            "Logging": {
              "LogLevel": {
                "Default": "Information",
                "Microsoft.AspNetCore": "Warning"
              }
            },
            "AllowedHosts": "*"
          }
EOF
          '''
        }
      }
    }

    stage('Prepare Payment Config') {
      steps {
        sh '''
          set -e
          cd backend/Services/PaymentService/CapShop.PaymentService

          cat > appsettings.Production.json <<EOF
          {
            "Razorpay": {
              "KeyId": "${RAZORPAY_KEY_ID}",
              "KeySecret": "${RAZORPAY_KEY_SECRET}"
            }
          }
EOF
        '''
      }
    }

    stage('Sanity Check') {
      steps {
        sh 'docker --version && docker compose version'
      }
    }

    stage('Build Images') {
      steps {
        sh '''
          set -e
          docker compose -f docker-compose.yml -f docker-compose.ci.yml build
        '''
      }
    }

    stage('Docker Login') {
      steps {
        withCredentials([usernamePassword(credentialsId: 'dockerhub_credentials', usernameVariable: 'DOCKERHUB_USER', passwordVariable: 'DOCKERHUB_TOKEN')]) {
          sh '''
            set -e
            echo "$DOCKERHUB_TOKEN" | docker login -u "$DOCKERHUB_USER" --password-stdin
          '''
        }
      }
    }

    stage('Push Images') {
      steps {
        sh '''
          set -e
          docker compose -f docker-compose.yml -f docker-compose.ci.yml push
        '''
      }
    }

    stage('Deploy Locally') {
      steps {
        sh '''
          set -e
          docker rm -f capshop-sqlserver capshop-rabbitmq capshop-redis || true
          docker compose -f docker-compose.yml -f docker-compose.ci.yml down --remove-orphans || true
          docker compose -f docker-compose.yml -f docker-compose.ci.yml pull
          docker compose -f docker-compose.yml -f docker-compose.ci.yml up -d
        '''
      }
    }

    stage('Smoke Test') {
      steps {
        sh '''
          set -e
          # Jenkins runs inside a container, so use a probe container in the same Docker network.
          for i in 1 2 3 4 5 6; do
            if docker run --rm --network capshop_capshop-network curlimages/curl:8.10.1 -fsS http://gateway:5041/gateway/auth/health; then
              exit 0
            fi
            echo "Smoke test attempt $i failed, retrying..."
            sleep 10
          done
          exit 1
        '''
      }
    }
  }

  post {
    always {
      sh 'docker compose -f docker-compose.yml -f docker-compose.ci.yml ps || true'
    }
    failure {
      sh 'docker compose -f docker-compose.yml -f docker-compose.ci.yml logs --tail=150 || true'
    }
  }
}