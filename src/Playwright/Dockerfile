FROM node:20-slim

RUN npx playwright install --with-deps chromium

WORKDIR /app

COPY package.json ./
RUN npm install

COPY . .

EXPOSE 3001

CMD ["node", "index.js"]
