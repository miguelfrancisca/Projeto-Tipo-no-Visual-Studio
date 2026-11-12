# ---------- build ----------
FROM node:22-alpine AS build
WORKDIR /app

COPY package.json package-lock.json ./

# CA raiz extra OPCIONAL (BuildKit secret "extra_ca"), para redes com inspeção TLS.
# NODE_EXTRA_CA_CERTS vale só para este RUN; o certificado não é copiado para a imagem.
RUN --mount=type=secret,id=extra_ca,required=false \
    if [ -s /run/secrets/extra_ca ]; then export NODE_EXTRA_CA_CERTS=/run/secrets/extra_ca; fi; \
    npm ci

COPY . .
RUN npm run build

# ---------- runtime ----------
FROM nginx:1.27-alpine AS runtime
COPY nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/dist /usr/share/nginx/html
EXPOSE 80
