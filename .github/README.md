# Deployment

Документація з розгортання **UniiaAnonim.TGBot** у прод. Бот працює через Telegram **webhook** і потребує публічного HTTPS-endpoint. Прод хоститься на домашньому **Proxmox**, публікується через **Cloudflare Tunnel**, а CI/CD виконується на **GitHub Actions** із деплоєм через **self-hosted runner**.

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
docker compose: [ bot ] ←→ [ cloudflared ] ──tunnel──► Cloudflare ──► https://bot.uniia.com.ua
                                                                              │
                                                                   Telegram webhook
```

Ключова ідея: секрети живуть у **GitHub Environment `production`** і потрапляють на сервер лише в момент деплою (їх записує self-hosted runner локально на VM). У репозиторії секретів немає.

---

## Файли

| Файл | Призначення |
|------|-------------|
| `Dockerfile` | Multi-stage build .NET 10; у фінальний образ додано `curl` + `HEALTHCHECK` на `/health` |
| `docker-compose.yml` | Сервіси `bot` + `cloudflared`, спільна мережа, ліміти, ротація логів |
| `.github/workflows/deploy.yml` | Пайплайн: guard → build/push → release-notes → deploy |

Файли, що **генеруються на VM** під час деплою (у `~/uniia-bot`, не комітяться):

| Файл | Вміст | Права |
|------|-------|-------|
| `.env` | `BOT_IMAGE`, `TUNNEL_TOKEN` (підстановка в compose) | `600` |
| `bot.env` | змінні застосунку (`Telegram__*`, `GoogleSheets__*`, `GoogleCalendar__*`, `GoogleDrive__*`, `BaseUrl`) | `600` |
| `appsettings.Production.json` | Google service-account JSON, той самий на всі три інтеграції (`GoogleSheets`/`GoogleCalendar`/`GoogleDrive`:`CredentialsJson`) | `644` (читає non-root контейнер; теку захищає `700`) |

---

## Конфігурація GitHub

У репозиторії: **Settings → Environments → `production`**.

### Secrets

| Secret | Мапиться на | Опис |
|--------|-------------|------|
| `TELEGRAM_BOT_TOKEN` | `Telegram__BotToken` | токен бота від BotFather |
| `TELEGRAM_SECRET_TOKEN` | `Telegram__SecretToken` | випадковий рядок (`openssl rand -hex 32`) для валідації webhook |
| `GOOGLE_SHEETS_SPREADSHEET_ID` | `GoogleSheets__SpreadsheetId` | ID таблиці |
| `GOOGLE_CALENDAR_ID` | `GoogleCalendar__CalendarId` | ID цільового Google Calendar |
| `GOOGLE_DRIVE_FOLDER_ID` | `GoogleDrive__FolderId` | ID папки на Google Drive, куди складаються файли (документи по календарних подіях) |
| `GOOGLE_CREDENTIALS_JSON` | `GoogleSheets`/`GoogleCalendar`/`GoogleDrive`:`CredentialsJson` | весь сирий JSON service-account; один і той самий ключ пишеться у всі три секції (один сервісний акаунт з увімкненими Sheets/Calendar/Drive API) |
| `CLOUDFLARE_TUNNEL_TOKEN` | env конектора `cloudflared` | токен тунелю з Cloudflare Zero Trust |

### Variables

| Variable | Приклад | Опис |
|----------|---------|------|
| `BASE_URL` | `https://bot.uniia.com.ua` | публічний URL бота (для `BaseUrl` і реєстрації webhook) |
| `GS_USER_SHEET_NAME` | `Users` | назва аркуша для `User` |
| `GS_USER_RANGE` | `A2:Z` | діапазон для `User` |
| `GS_STAGE_SHEET_NAME` | `Stages` | назва аркуша для `Stage` |
| `GS_STAGE_RANGE` | `A2:C` | діапазон для `Stage` |
| `GS_CALENDAR_EVENT_SHEET_NAME` | `CalendarEvents` | назва аркуша для `CalendarEvent` |
| `GS_CALENDAR_EVENT_RANGE` | `A2:B` | діапазон для `CalendarEvent` |

> `GoogleDrive__UseServiceAccount` у проді хардкодиться в `true` прямо у workflow (не є секретом/змінною) — на проді має бути завжди так, інакше застосунок піде в інтерактивний OAuth-флоу, який на сервері неможливо пройти.

> Додати нову змінну застосунку = один рядок у кроці `Stage deploy files` (`bot.env`) + (за потреби) новий secret/variable. `docker-compose.yml` чіпати не треба.

---

## Інфраструктура (одноразове налаштування)

### Proxmox VM
- Debian 12, 2 vCPU / 4 GB / 30 GB, **Start at boot**, QEMU guest agent.
- Зафіксований внутрішній IP (DHCP reservation).
- SSH по ключах (без root/паролів), `fail2ban`, `unattended-upgrades`.
- Docker Engine + Compose plugin; користувач у групі `docker`.
- `git` (потрібен для `actions/checkout`).

### Self-hosted runner
- Зареєстрований під користувачем VM (у групі `docker`), як systemd-сервіс (`svc.sh install/start`).
- **Мітки:** `self-hosted`, `prod` (workflow вимагає `runs-on: [self-hosted, prod]`).
  > Мітка `prod` має бути саме в **labels** раннера, а не лише в його імені.

### Cloudflare Tunnel (через дашборд Zero Trust)
- Тунель `bot-prod`, конектор запускається контейнером `cloudflared` (токен у `CLOUDFLARE_TUNNEL_TOKEN`).
- **Public hostname:** `bot.uniia.com.ua` → Service `HTTP` `bot:8080`, Path `api/webhook`.
- DNS-запис `bot` (CNAME, proxied) створюється автоматично.

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
docker ps                                   # bot — healthy, cloudflared — running
cd ~/uniia-bot && docker compose logs -f bot   # очікувано: "Webhook successfully set."
```

- Cloudflare Zero Trust → Tunnels → `bot-prod` = **Healthy**.
- Написати боту в Telegram — оновлення має дійти.
- **Логи:** https://bot.uniia.com.ua/main/logs/

---

## Rollback

Деплоїться образ із тегом релізу, тож відкат — це запуск попередньої версії:

```bash
cd ~/uniia-bot
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
| Webhook не приходить | Перевірити `BASE_URL`, статус тунелю, public hostname `api/webhook`, `Telegram__SecretToken`. |
| Логи `/app/Logs` не пишуться | Очікувано: контейнер non-root. Логи доступні через `docker compose logs` (Console + json-file). |
