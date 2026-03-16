# Stage 1: Build Blazor WASM
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY Web.csproj .
RUN dotnet restore
COPY . .
RUN dotnet publish -c Release -o /app/publish

# Stage 2: Serve with nginx
FROM nginx:alpine AS final
# Landing page at root
COPY landing/index.html /usr/share/nginx/html/index.html
# Blazor app under /admin/
COPY --from=build /app/publish/wwwroot /usr/share/nginx/html/admin
# Same Blazor app under /panel/ (with different index.html for base href)
COPY --from=build /app/publish/wwwroot /usr/share/nginx/html/panel
# Override panel's index.html with the one that has base href="/panel/"
COPY panel-index.html /usr/share/nginx/html/panel/index.html
EXPOSE 80
