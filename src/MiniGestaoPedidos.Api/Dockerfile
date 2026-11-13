# Build context: raiz do repositório (precisa de global.json e Directory.*.props).
#   docker build -f src/MiniGestaoPedidos.Api/Dockerfile .

# ---------- build ----------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Restore em camada própria: só refaz quando algum .csproj/props muda.
COPY global.json Directory.Build.props Directory.Packages.props ./
COPY src/MiniGestaoPedidos.Domain/MiniGestaoPedidos.Domain.csproj                 src/MiniGestaoPedidos.Domain/
COPY src/MiniGestaoPedidos.Application/MiniGestaoPedidos.Application.csproj       src/MiniGestaoPedidos.Application/
COPY src/MiniGestaoPedidos.Infrastructure/MiniGestaoPedidos.Infrastructure.csproj src/MiniGestaoPedidos.Infrastructure/
COPY src/MiniGestaoPedidos.Api/MiniGestaoPedidos.Api.csproj                       src/MiniGestaoPedidos.Api/

# CA raiz extra OPCIONAL (BuildKit secret "extra_ca"), para redes com inspeção TLS
# (antivírus/proxy corporativo). Ver docker-compose.ca.yml e README.
#  - sem o secret: build normal;
#  - com o secret: confia nele só durante o restore e remove em seguida —
#    o certificado não fica no stage de build nem chega à imagem final.
RUN --mount=type=secret,id=extra_ca,required=false \
    if [ -s /run/secrets/extra_ca ]; then \
        cp /run/secrets/extra_ca /usr/local/share/ca-certificates/extra-ca.crt && update-ca-certificates > /dev/null; \
    fi; \
    dotnet restore src/MiniGestaoPedidos.Api/MiniGestaoPedidos.Api.csproj; status=$?; \
    if [ -f /usr/local/share/ca-certificates/extra-ca.crt ]; then \
        rm /usr/local/share/ca-certificates/extra-ca.crt && update-ca-certificates --fresh > /dev/null; \
    fi; \
    exit $status

COPY src/ src/
RUN dotnet publish src/MiniGestaoPedidos.Api/MiniGestaoPedidos.Api.csproj -c Release -o /app --no-restore

# ---------- runtime ----------
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app .

ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

# Usuário sem privilégios que já vem na imagem oficial.
USER app

ENTRYPOINT ["dotnet", "MiniGestaoPedidos.Api.dll"]
