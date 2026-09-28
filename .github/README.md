# Розгортання на Orange Pi 5

Реліз у GitHub збирає образ `linux/arm64` на Debian 13 і публікує його в GHCR. На платі образ стягується вручну і лишається запущеним після перезавантаження: політика `unless-stopped` плюс увімкнений демон Docker.

Образ уже містить `appsettings.json` зі стабами. Його не редагують. Бойові значення кладуть у `appsettings.Production.json` поруч із контейнером і монтують поверх: ASP.NET підхоплює той самий набір ключів і замінює стаби.

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "your_connection_string"
  },
  "GeneralOptions": {
    "BaseUrl": "your_base_url",
    "DefaultLanguage": "uk-UA",
    "SymmetricEncryptionKey": "random_guid",
    "HashingKey": "random_guid"
  },
  "Telegram": {
    "SecretToken": "random_guid",
    "BotToken": "random_guid"
  }
}
```

Образ: `ghcr.io/uniia-com-ua/anonim_uniia_bot:<тег-релізу>`.

---

## 1. Перевірити плату

Потрібні Debian і архітектура `aarch64`.

```bash
. /etc/os-release && echo "$ID $VERSION_ID"
uname -m
```

Очікувано: `debian` і `aarch64`.

---

## 2. Встановити Docker

```bash
sudo apt-get update
sudo apt-get install -y ca-certificates curl
sudo install -m 0755 -d /etc/apt/keyrings
sudo curl -fsSL https://download.docker.com/linux/debian/gpg -o /etc/apt/keyrings/docker.asc
sudo chmod a+r /etc/apt/keyrings/docker.asc

echo "deb [arch=$(dpkg --print-architecture) signed-by=/etc/apt/keyrings/docker.asc] https://download.docker.com/linux/debian $(. /etc/os-release && echo "$VERSION_CODENAME") stable" | sudo tee /etc/apt/sources.list.d/docker.list > /dev/null

sudo apt-get update
sudo apt-get install -y docker-ce docker-ce-cli containerd.io docker-compose-plugin
sudo usermod -aG docker "$USER"
sudo systemctl enable --now docker
```

Вийдіть із SSH і зайдіть знову, щоб група `docker` застосувалась. Перевірка:

```bash
docker info >/dev/null && echo "docker ok"
```

`systemctl enable` тримає демон увімкненим після reboot. Контейнер із `--restart unless-stopped` підніметься слідом.

---

## 3. Увійти в GHCR

Репозиторій приватний, тож образ теж приватний. У GitHub: **Settings → Developer settings → Personal access tokens**. Для classic-токена достатньо scope `read:packages`.

```bash
echo 'ВАШ_ТОКЕН' | docker login ghcr.io -u ВАШ_GITHUB_ЛОГІН --password-stdin
```

---

## 4. Заповнити стаби

```bash
mkdir -p "$HOME/uniia-bot"
chmod 700 "$HOME/uniia-bot"
cd "$HOME/uniia-bot"

ENC_KEY="$(openssl rand -hex 32)"
HASH_KEY="$(openssl rand -hex 32)"
SECRET="$(openssl rand -hex 32)"

cat > appsettings.Production.json <<EOF
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=192.168.1.10;Port=5432;Database=uniia;Username=uniia;Password=ЗМІНІТЬ"
  },
  "GeneralOptions": {
    "BaseUrl": "https://bot.example.com",
    "DefaultLanguage": "uk-UA",
    "SymmetricEncryptionKey": "${ENC_KEY}",
    "HashingKey": "${HASH_KEY}"
  },
  "Telegram": {
    "SecretToken": "${SECRET}",
    "BotToken": "ТОКЕН_ВІД_BOTFATHER"
  }
}
EOF

chmod 644 appsettings.Production.json
```

Що замінити:

| Стаб в образі | У проді |
|---------------|---------|
| `your_connection_string` | рядок Npgsql до вже запущеного PostgreSQL |
| `your_base_url` | публічний HTTPS, на який Telegram шле webhook |
| `random_guid` у `SymmetricEncryptionKey` і `HashingKey` | згенеровані вище ключі; після першого запуску їх не змінюють |
| `random_guid` у `BotToken` | токен BotFather |
| `random_guid` у `SecretToken` | секрет webhook |
| `uk-UA` | можна лишити |

`localhost` у рядку підключення з контейнера — це сам контейнер, а не плата. Вкажіть IP машини, де стоїть PostgreSQL.

Контейнер працює від uid `1654`, тому файл має бути `644`. Каталог `700` закриває його від інших користувачів плати.

---

## 5. Стягнути образ і запустити назавжди

Підставте тег опублікованого релізу, наприклад `v1.0.0`.

```bash
cd "$HOME/uniia-bot"

docker pull ghcr.io/uniia-com-ua/anonim_uniia_bot:v1.0.0

docker run -d \
  --name uniia-bot \
  --restart unless-stopped \
  --platform linux/arm64 \
  -p 127.0.0.1:8080:8080 \
  -e ASPNETCORE_ENVIRONMENT=Production \
  -v "$HOME/uniia-bot/appsettings.Production.json:/app/appsettings.Production.json:ro" \
  ghcr.io/uniia-com-ua/anonim_uniia_bot:v1.0.0
```

Порт відкритий лише на самій платі. Публічний HTTPS має проксувати запити на `127.0.0.1:8080`.

---

## 6. Перевірити

```bash
docker ps
docker inspect --format '{{.HostConfig.RestartPolicy.Name}} {{.Os}}/{{.Architecture}}' uniia-bot
curl -fsS http://127.0.0.1:8080/health
docker logs -f uniia-bot
```

Очікувано: контейнер `Up`, політика `unless-stopped`, платформа `linux/arm64`, health відповідає, у логах webhook зареєстрований.

Після `sudo reboot` той самий `docker ps` має знову показати `uniia-bot`.

---

## 7. Оновити версію

```bash
cd "$HOME/uniia-bot"
docker pull ghcr.io/uniia-com-ua/anonim_uniia_bot:v1.1.0
docker stop uniia-bot
docker rm uniia-bot

docker run -d \
  --name uniia-bot \
  --restart unless-stopped \
  --platform linux/arm64 \
  -p 127.0.0.1:8080:8080 \
  -e ASPNETCORE_ENVIRONMENT=Production \
  -v "$HOME/uniia-bot/appsettings.Production.json:/app/appsettings.Production.json:ro" \
  ghcr.io/uniia-com-ua/anonim_uniia_bot:v1.1.0
```

Файл зі стабами не чіпають, якщо секрети не змінювались.

Відкат — ті самі команди з попереднім тегом.

---

## 8. Зупинити

```bash
docker stop uniia-bot
docker rm uniia-bot
```

`--restart unless-stopped` після явного `docker stop` сам контейнер не піднімає.
