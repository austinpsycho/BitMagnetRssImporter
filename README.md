# BitMagnet RSS Importer

A lightweight ASP.NET Razor Pages service that polls torrent RSS feeds, deduplicates items, extracts infohashes, and streams them into a running **bitmagnet** instance via its `/import` API.

Designed to run long-term in a homelab / Arr-style stack (often behind a VPN container).

---

## Features

* Polls multiple RSS / Atom feeds on configurable intervals
* Supports magnet links and `.torrent` URLs
* Deduplicates items per-feed (SQLite)
* Streams new items into bitmagnet
* Web UI to view feed status and add/edit feeds
* Safe for long-running operation (idempotent migrations, conditional GETs)

---

## Requirements

* Docker (recommended)
* A running **bitmagnet** instance
* Network access from this container to bitmagnet’s HTTP API

---

## Quick Start (Docker Compose)

### Minimal setup

```yaml
services:
  bitmagnet-rss-importer:
    image: ghcr.io/austinpsycho/bitmagnet-rss-importer:latest
    container_name: bitmagnet-rss-importer
    volumes:
      - ./bitmagnetrssimporter/data:/data
    ports:
      - "8085:8085"
    restart: unless-stopped
```

Then visit:

```
http://localhost:8085
```

On first startup, the SQLite database will be created automatically in `./bitmagnetrssimporter/data`.

---

## Running Behind a VPN Container (Arr-style stack)

If you are using `network_mode: service:vpn` (e.g. gluetun):

```yaml
services:
  vpn:
    image: qmcgaw/gluetun
    ports:
      - 8085:8085   # expose importer UI

  bitmagnet-rss-importer:
    image: ghcr.io/austinpsycho/bitmagnet-rss-importer:latest
    network_mode: "service:vpn"
    depends_on:
      - vpn
    volumes:
      - ./bitmagnetrssimporter/data:/data
    restart: unless-stopped
```

The importer will still be available at:

```
http://<host-ip>:8085
```

---

## Configuration

### Environment Variables

All configuration is optional — sane defaults are provided.

| Variable                    | Default                        | Description               |
| --------------------------- | ------------------------------ | ------------------------- |
| `ASPNETCORE_URLS`           | `http://0.0.0.0:8085`          | Web UI listen address     |
| `ConnectionStrings__Sqlite` | `Data Source=/data/app.db`     | SQLite database location  |
| `Bitmagnet__ImportUrl`      | `http://localhost:3333/import` | bitmagnet import endpoint |

Example override:

```yaml
environment:
  - Bitmagnet__ImportUrl=http://bitmagnet:3333/import
```

(`__` maps to `:` in ASP.NET config.)

---

## Database

* Uses SQLite
* Schema is managed via EF Core migrations
* Migrations are applied automatically on startup
* Safe to restart / upgrade the container

**Important:**
Always mount `/data` to a volume or bind mount.
Anything stored only inside the container filesystem will be lost on removal.

Recommended:

```yaml
volumes:
  - ./bitmagnetrssimporter/data:/data
```

---

## Web UI

The web UI allows you to:

* View RSS feed status
* See last checked / next due times
* Add, edit, enable, or disable feeds

Auto-refresh is enabled to show up-to-date feed activity.

---

## How Deduplication Works

* Each feed has its own dedupe scope
* Items are identified by:

    1. RSS `<guid>` / `<id>` (preferred)
    2. Item URL
    3. Fallback: title + publish date
* Items are only marked as “seen” **after successful processing**
* Duplicate imports are prevented at the database level

---

## Supported Feeds

Works with most torrent RSS feeds, including:

* Public tracker RSS
* Prowlarr-generated RSS feeds
* Custom indexer feeds

Feeds with DTDs are supported safely (external entities disabled).

---

## Notes & Limitations

* This importer is optimized for **RSS-scale feeds** (typically 15–200 items per poll)
* It is not intended to bulk-import entire tracker histories
* For full backfills, use bitmagnet’s native ingestion tools

---

## License

MIT
