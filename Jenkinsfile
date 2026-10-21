pipeline {
    agent any

    stages {
        stage('Database') {
            steps {
                script {
                    if (isUnix()) {
                        sh 'docker compose up -d --wait db'
                    } else {
                        bat 'docker compose up -d --wait db'
                    }
                }
            }
        }

        stage('Build') {
            steps {
                dir('TournamentServices') {
                    script {
                        if (isUnix()) {
                            sh 'dotnet restore TournamentServices.sln'
                            sh 'dotnet build TournamentServices.sln --configuration Release --no-restore'
                        } else {
                            bat 'dotnet restore TournamentServices.sln'
                            bat 'dotnet build TournamentServices.sln --configuration Release --no-restore'
                        }
                    }
                }
            }
        }

        stage('Migrate') {
            steps {
                dir('TournamentServices') {
                    script {
                        if (isUnix()) {
                            sh 'dotnet tool restore'
                            sh 'dotnet ef database update --project src/TournamentServices.Infrastructure'
                        } else {
                            bat 'dotnet tool restore'
                            bat 'dotnet ef database update --project src/TournamentServices.Infrastructure'
                        }
                    }
                }
            }
        }

        stage('Test') {
            steps {
                dir('TournamentServices') {
                    script {
                        if (isUnix()) {
                            sh 'dotnet test TournamentServices.sln --configuration Release --no-build'
                        } else {
                            bat 'dotnet test TournamentServices.sln --configuration Release --no-build'
                        }
                    }
                }
            }
        }
    }

    post {
        always {
            script {
                if (isUnix()) {
                    sh 'docker compose down'
                } else {
                    bat 'docker compose down'
                }
            }
        }
    }
}
