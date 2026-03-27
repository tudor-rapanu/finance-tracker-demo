#!/usr/bin/env zsh
set -euo pipefail

SCRIPT_DIR="${0:a:h}"
REPO_ROOT="${SCRIPT_DIR:h}"
API_DIR="${REPO_ROOT}/src/FinanceTracker.API"

if [[ ! -d "${API_DIR}" ]]; then
  echo "Could not find API project at ${API_DIR}" >&2
  exit 1
fi

cd "${API_DIR}"

echo "Setting local user-secrets for FinanceTracker.API"
echo "Values are stored locally and not committed to source control."

read -r "connectionString?Connection string (ConnectionStrings:DefaultConnection): "
read -s -r "jwtSecret?JWT secret (JwtSettings:SecretKey): "
echo
read -r "issuer?JWT issuer (JwtSettings:Issuer) [FinanceTrackerAPI]: "
issuer=${issuer:-FinanceTrackerAPI}
read -r "audience?JWT audience (JwtSettings:Audience) [FinanceTrackerClient]: "
audience=${audience:-FinanceTrackerClient}
read -r "expiryHours?JWT expiry hours (JwtSettings:ExpiryHours) [24]: "
expiryHours=${expiryHours:-24}
read -r "exchangeApiKey?ExchangeRateApi key (optional, press Enter to skip): "

dotnet user-secrets set "ConnectionStrings:DefaultConnection" "${connectionString}" >/dev/null
dotnet user-secrets set "JwtSettings:SecretKey" "${jwtSecret}" >/dev/null
dotnet user-secrets set "JwtSettings:Issuer" "${issuer}" >/dev/null
dotnet user-secrets set "JwtSettings:Audience" "${audience}" >/dev/null
dotnet user-secrets set "JwtSettings:ExpiryHours" "${expiryHours}" >/dev/null

if [[ -n "${exchangeApiKey}" ]]; then
  dotnet user-secrets set "ExchangeRateApi:ApiKey" "${exchangeApiKey}" >/dev/null
fi

echo "Done. Current non-sensitive keys:"
dotnet user-secrets list | sed 's/=.*/=<hidden>/'
