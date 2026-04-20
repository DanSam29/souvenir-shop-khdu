# Етап 1: Збірка React додатку
FROM node:20-alpine AS build
WORKDIR /app

# Оголошуємо Build Argument для API URL
ARG REACT_APP_API_URL
ENV REACT_APP_API_URL=$REACT_APP_API_URL

# Копіюємо package.json та встановлюємо залежності
COPY package*.json ./
RUN npm install --legacy-peer-deps

# Копіюємо решту файлів та збираємо проект
COPY . .
RUN npm run build

# Етап 2: Запуск за допомогою Nginx
FROM nginx:stable-alpine
COPY --from=build /app/build /usr/share/nginx/html

# Копіюємо конфігурацію Nginx для підтримки React Router
COPY nginx.conf /etc/nginx/conf.d/default.conf

EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]
