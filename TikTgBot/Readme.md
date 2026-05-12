# TikTgBot

A Telegram bot that detects supported video links in allowed chats and replies with the downloaded video file.

The bot currently supports:

- TikTok videos
- Instagram Reels
- YouTube Shorts

## Features

- Watches Telegram messages for supported video URLs
- Downloads videos using `yt-dlp`
- Replies directly to the original message with the video
- Restricts processing to configured chat IDs
- Skips files larger than the configured Telegram upload limit used by the bot
- Supports polling or webhook-based Telegram bot operation
- Supports optional SOCKS5 proxy configuration
- Supports running locally or in Docker

## Requirements

- .NET SDK 10.0 or newer
- `ffmpeg`
- `yt-dlp`
- A Telegram bot token

When running with Docker, `ffmpeg`, Python, and `yt-dlp` are installed by the included Dockerfile.

## Configuration

Copy the example settings file:

```bash
cp TikTgBot/appsettings.Example.json TikTgBot/appsettings.json
```

Then edit `TikTgBot/appsettings.json`.

Example:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning",
      "Microsoft.Hosting.Lifetime": "Information"
    }
  },
  "BotConfig": {
    "token": "<token>",
    "webhook": {
      "enabled": false,
      "url": "",
      "host": "localhost",
      "port": 8080
    },
    "sockS5Address": "sock.address.here",
    "sockS5Port": 1080,
    "sockS5User": "sockUser",
    "sockS5Password": "sockPassword",
    "useSOCKS5": false
  },
  "Configuration": {
    "Chats": [
      123456789
    ],
    "Cookies": {
      "Instagram": "instagram_cookies.txt",
      "Youtube": "instagram_cookies.txt",
      "TikTok": "tiktok_cookies.txt"
    }
  }
}
```

### Important settings

| Setting | Description |
| --- | --- |
| `BotConfig.token` | Telegram bot token from BotFather |
| `BotConfig.webhook.enabled` | Enables webhook mode when set to `true` |
| `BotConfig.webhook.url` | Public webhook URL Telegram should send updates to |
| `BotConfig.webhook.host` | Local host/interface used by the webhook listener |
| `BotConfig.webhook.port` | Local port used by the webhook listener |
| `BotConfig.useSOCKS5` | Enables SOCKS5 proxy support |
| `BotConfig.sockS5Address` | SOCKS5 proxy host |
| `BotConfig.sockS5Port` | SOCKS5 proxy port |
| `BotConfig.sockS5User` | SOCKS5 proxy username |
| `BotConfig.sockS5Password` | SOCKS5 proxy password |
| `Configuration.Chats` | Telegram chat IDs where the bot is allowed to respond |
| `Configuration.Cookies.Instagram` | Path to Instagram cookies file |
| `Configuration.Cookies.Youtube` | Path to YouTube cookies file |
| `Configuration.Cookies.TikTok` | Path to TikTok cookies file |

## Getting chat IDs

Add the bot to the target chat, send a message, and use one of the common Telegram bot API methods or tools to inspect updates and find the chat ID.

Only chats listed in `Configuration.Chats` will be processed.

## Cookies

Some platforms may require cookies for reliable downloads.

Cookie files should be placed where the application can access them, for example:

```text
TikTgBot/instagram_cookies.txt
TikTgBot/youtube_cookies.txt
TikTgBot/tiktok_cookies.txt
```

Make sure the paths match the values in `appsettings.json`.



## Supported URL types

The bot handles links from:

- `tiktok.com`
- `vm.tiktok.com`
- `vt.tiktok.com`
- `youtube.com/shorts/...`
- `instagram.com/.../reel/...`
