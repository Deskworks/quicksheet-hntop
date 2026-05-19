# quicksheet-hntop

**Top Hacker News stories on your desktop wallpaper.** A [QuickSheet](https://github.com/cemheren/QuickSheet) extension that shows the current top stories from Hacker News with scores and comment counts — without opening a browser.

## Why?

Developers check HN multiple times per day. Instead of context-switching to a browser tab, `hntop:` puts the headlines right on your wallpaper. It's the "replace a browser tab" pattern applied to the feed you already check obsessively.

## Install

In any QuickSheet cell, type:

```
ext: github:Deskworks/quicksheet-hntop
```

QuickSheet clones the repo and starts the extension automatically.

## Usage

```
hntop: 10        # Show top 10 stories (default)
hntop: 5         # Show top 5 stories
hntop: 20        # Show top 20 stories (max 30)
```

### Output

| Column | Content |
|--------|---------|
| A | Rank + Title (truncated at 60 chars) |
| B | ▲ Score |
| C | 💬 Comment count |

Footer shows last update time.

## Data Source

Uses the [HN Firebase API](https://github.com/HackerNews/API) — completely free, no API key required, no rate limits documented. Results are cached for 5 minutes to be polite.

- Top stories: `https://hacker-news.firebaseio.com/v0/topstories.json`
- Item details: `https://hacker-news.firebaseio.com/v0/item/{id}.json`

## Requirements

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [QuickSheet](https://github.com/cemheren/QuickSheet)

## Protocol

This extension uses the [QuickSheet JSON-lines protocol](https://github.com/cemheren/QuickSheet/blob/main/docs/extensions.md). Cells are emitted as flat `{r, c, v}` objects.

## License

MIT
