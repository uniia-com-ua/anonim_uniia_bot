# Deployment

Документація з розгортання **UniiaAnonim.TGBot** у прод. Бот працює через Telegram **webhook** і потребує публічного HTTPS-endpoint. Прод хоститься на домашньому **Proxmox**, публікується через **Cloudflare Tunnel**, а CI/CD виконується на **GitHub Actions** із деплоєм через **self-hosted runner**.

Образ уже містить `appsettings.json` зі стабами. Під час деплою runner записує ті самі ключі в `bot.env`, і змінні середовища перекривають стаби.

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

---

## Архітектура

```
GitHub Release (published, target = main)
        │
        ▼
GitHub Actions (ubuntu-latest)
  ├─ guard          → перевірка, що коміт релізу є в main
  ├─ build          → dotnet test → docker build → push у GHCR (:<tag> + :latest)
  └─ release-notes  → опис релізу: посилання + автоматичний changelog
        │
        ▼  (self-hosted runner сам забирає джобу — без вхідних портів)
deploy (self-hosted, prod) на Proxmox VM
  ├─ генерує .env / bot.env / appsettings.Production.json із GitHub Secrets
  └─ docker compose pull && up -d
        │
        ▼
docker compose: [ postgres ] ←→ [ bot ] ←→ [ cloudflared ] ──tunnel──► Cloudflare ──► https://<BASE_URL>
                                                                              │
                                                                   Telegram webhook
```

Ключова ідея: секрети живуть у **GitHub Environment `production`** і потрапляють на сервер лише в момент деплою (їх записує self-hosted runner локально на VM). У репозиторії секретів немає.

Каталог на VM — `~/anonim-uniia-bot`, щоб не перетнутися з деплоєм `uniia_tg_bot` у `~/uniia-bot`, якщо обидва раннери сидять на одній машині.

---

## Файли

| Файл | Призначення |
|------|-------------|
| `Dockerfile` | Multi-stage build .NET 10; у фінальний образ додано `curl` + `HEALTHCHECK` на `/health` |
| `docker-compose.yml` | Сервіси `bot` + `postgres` + `dozzle` + `cloudflared`, спільна мережа, том `pgdata`, ліміти, ротація логів |
| `.github/workflows/deploy.yml` | Пайплайн: guard → build/push → release-notes → deploy |

Файли, що **генеруються на VM** під час деплою (у `~/anonim-uniia-bot`, не комітяться):

| Файл | Вміст | Права |
|------|-------|-------|
| `.env` | `BOT_IMAGE`, `TUNNEL_TOKEN`, `POSTGRES_DB/USER/PASSWORD` (підстановка в compose) | `600` |
| `bot.env` | перекриття стабів: `ConnectionStrings__DefaultConnection`, `GeneralOptions__*`, `Telegram__*` | `600` |
| `appsettings.Production.json` | порожній `{}`: скалярні налаштування йдуть через `bot.env` | `644` (читає non-root контейнер; теку захищає `700`) |

---

## Конфігурація GitHub

У репозиторії: **Settings → Environments → `production`**.

### Secrets

| Secret | Мапиться на | Замість стаба |
|--------|-------------|---------------|
| `POSTGRES_PASSWORD` | пароль БД + `ConnectionStrings__DefaultConnection` | `your_connection_string` |
| `SYMMETRIC_ENCRYPTION_KEY` | `GeneralOptions__SymmetricEncryptionKey` | `random_guid` |
| `HASHING_KEY` | `GeneralOptions__HashingKey` | `random_guid` |
| `TELEGRAM_BOT_TOKEN` | `Telegram__BotToken` | `random_guid` |
| `TELEGRAM_SECRET_TOKEN` | `Telegram__SecretToken` | `random_guid` |
| `CLOUDFLARE_TUNNEL_TOKEN` | env конектора `cloudflared` | — |

`POSTGRES_PASSWORD`, `TELEGRAM_SECRET_TOKEN`, `SYMMETRIC_ENCRYPTION_KEY` і `HASHING_KEY` — випадкові рядки (`openssl rand -hex 32`; hex не ламає рядок підключення). Продублюй їх у менеджері паролів: GitHub не показує секрет назад.

- Ключі шифрування й хешування після першого прод-запуску не змінюють, інакше наявні дані в БД стануть непридатними.
- `POSTGRES_PASSWORD` застосовується лише при першій ініціалізації тому `pgdata`. Якщо змінити секрет пізніше, спершу зміни пароль у самій БД (`ALTER USER anonim_uniia_bot PASSWORD '...'`), інакше бот не підключиться.
- Рядок підключення workflow збирає сам: `Host=postgres;Port=5432;Database=anonim_uniia_bot;Username=anonim_uniia_bot;Password=<POSTGRES_PASSWORD>`.

### Variables

| Variable | Приклад | Замість стаба |
|----------|---------|---------------|
| `BASE_URL` | `https://anonim.uniia.com.ua` | `your_base_url` |
| `DEFAULT_LANGUAGE` | `uk-UA` | `uk-UA` (можна лишити) |

> Додати нову змінну застосунку = один рядок у кроці `Stage deploy files` (`bot.env`) + (за потреби) новий secret/variable. `docker-compose.yml` чіпати не треба.

---

## Інфраструктура (одноразове налаштування)

### Proxmox VM
- Debian 12, 2 vCPU / 4 GB / 30 GB, **Start at boot**, QEMU guest agent.
- Зафіксований внутрішній IP (DHCP reservation).
- SSH по ключах (без root/паролів), `fail2ban`, `unattended-upgrades`.
- Docker Engine + Compose plugin; користувач у групі `docker`.
- `git` (потрібен для `actions/checkout`).
- Та сама VM, що й для `uniia_tg_bot`: проєкти compose (`uniia-bot` і `anonim-uniia-bot`) мають окремі мережі й не публікують порти на хост, тож не конфліктують.

### Self-hosted runner
- Окремий раннер для цього репо на тій самій VM, у своїй теці (`~/actions-runner-anonim`, ім'я `bot-prod-anonim`), під користувачем у групі `docker`, як systemd-сервіс (`svc.sh install/start`).
- **Мітки:** `self-hosted`, `prod` (workflow вимагає `runs-on: [self-hosted, prod]`).
  > Мітка `prod` має бути саме в **labels** раннера, а не лише в його імені.

### Cloudflare Tunnel (через дашборд Zero Trust)
- **Окремий тунель** (`anonim-bot-prod`), не той, що в `uniia_tg_bot`. Два `cloudflared` з одним токеном стають репліками одного тунелю, і Cloudflare розкидає запити між ними, а `bot:8080` у кожному compose — інший застосунок.
- Конектор запускається контейнером `cloudflared` (токен у `CLOUDFLARE_TUNNEL_TOKEN`).
- **Public hostnames** (`anonim.uniia.com.ua`):

  | Path | Service |
  |------|---------|
  | `api/webhook` | `HTTP` `bot:8080` |
  | `main/logs` | `HTTP` `dozzle:8080` |

- `main/logs` закрити **Cloudflare Access** (Self-hosted application, Allow → свій email): Dozzle бачить логи всіх контейнерів на VM.
- DNS-запис (CNAME, proxied) створюється автоматично.

### PostgreSQL
- Контейнер `postgres:17-alpine` у цьому ж compose, дані в іменованому томі `anonim-uniia-bot_pgdata`.
- Бот стартує лише після `pg_isready` (`depends_on: service_healthy`) і сам накочує міграції (`MigrateAsync`).
- Порт на хост не публікується; доступ для адміністрування — `docker compose exec postgres psql -U anonim_uniia_bot anonim_uniia_bot`.
- ⚠️ Ніколи не запускати `docker compose down -v`: це видалить том із даними.

### GHCR
- Образи приватні (репо приватне). `build` пушить з `GITHUB_TOKEN` (`packages: write`), `deploy` тягне (`packages: read`).
- Якщо `docker compose pull` → `denied`: **Package → Settings → Manage Actions access** → дати репо доступ.

---

## Як випустити реліз (деплой)

1. GitHub → **Releases → Draft a new release**.
2. **Choose a tag** → новий тег (напр. `v1.0.0`), **Target = `main`**.
3. (Опційно) залишити опис порожнім — він згенерується автоматично.
4. **Publish release**.

Далі автоматично:
- `guard` перевіряє, що тег із `main`;
- `build` ганяє тести, збирає й пушить образ `ghcr.io/<owner>/<repo>:v1.0.0` + `:latest`;
- `release-notes` оновлює опис релізу (посилання + changelog);
- `deploy` піднімає новий образ на VM.

---

## Перевірка після деплою

```bash
# на VM
docker ps                                      # bot і postgres — healthy, cloudflared — running
cd ~/anonim-uniia-bot && docker compose logs -f bot   # очікувано: webhook зареєстрований
```

- Cloudflare Zero Trust → Tunnels → конектор = **Healthy**.
- Написати боту в Telegram — оновлення має дійти.
- **Логи:** `<BASE_URL>/main/logs/`

---

## Бекап БД

Щоденний дамп через cron на VM (`crontab -e`), зберігаються останні 14 днів:

```cron
0 3 * * * mkdir -p $HOME/backups && cd $HOME/anonim-uniia-bot && docker compose exec -T postgres pg_dump -U anonim_uniia_bot -Fc anonim_uniia_bot > $HOME/backups/anonim-$(date +\%F).dump && find $HOME/backups -name 'anonim-*.dump' -mtime +14 -delete
```

Відновлення:

```bash
cd ~/anonim-uniia-bot
docker compose stop bot
docker compose exec -T postgres pg_restore -U anonim_uniia_bot -d anonim_uniia_bot --clean --if-exists < ~/backups/anonim-YYYY-MM-DD.dump
docker compose start bot
```

Дампи лежать на тій самій VM. Від втрати VM захищає бекап Proxmox (`vzdump` за розкладом); для захисту від втрати всього сервера дампи варто копіювати ще й за межі нього.

---

## Rollback

Деплоїться образ із тегом релізу, тож відкат — це запуск попередньої версії:

```bash
cd ~/anonim-uniia-bot
# у .env вказати попередній тег образу
sed -i 's#^BOT_IMAGE=.*#BOT_IMAGE=ghcr.io/<owner>/<repo>:v0.9.0#' .env
docker compose pull && docker compose up -d
```

(або повторно опублікувати/створити реліз на потрібному комміті).

---

## Типові проблеми

| Симптом | Причина / рішення |
|---------|-------------------|
| Джоба `deploy` висить на `Waiting for a runner...` | Раннеру бракує мітки `prod`. Додати label у Settings → Actions → Runners. |
| `Access to the path '/app/appsettings.Production.json' is denied` | Файл недоступний non-root юзеру контейнера. Має бути `chmod 0644` (вже у workflow). |
| `docker compose pull` → `denied` | GHCR-пакет не має доступу від репо. Налаштувати Manage Actions access. |
| Бот падає на міграції / `password authentication failed` | `POSTGRES_PASSWORD` змінили після першої ініціалізації тому. Змінити пароль у БД через `ALTER USER` або повернути старий секрет. |
| Запити першого бота інколи приходять не туди | Обидва `cloudflared` запущені з одним токеном. Для цього бота потрібен окремий тунель. |
| Webhook не приходить | Перевірити `BASE_URL`, статус тунелю, public hostname `api/webhook`, `Telegram__SecretToken`. |
| Логи `/app/Logs` не пишуться | Очікувано: контейнер non-root. Логи доступні через `docker compose logs` (Console + json-file). |
