using './main.bicep'

param namePrefix = 'kubapc-dev'
param staticWebAppLocation = 'westeurope'
param postgresAdministratorLogin = 'kubapc_admin'
param databaseName = 'kubapc'
param applicationDatabaseLogin = 'kubapc_app'

param postgresAdministratorPassword = readEnvironmentVariable('POSTGRES_ADMIN_PASSWORD')
param applicationDatabasePassword = readEnvironmentVariable('APP_DB_PASSWORD')
param jwtSecret = readEnvironmentVariable('JWT_SECRET')
