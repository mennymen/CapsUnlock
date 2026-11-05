# CapsUnlock

A small, dependency-free JavaScript library and command-line tool for common text transformations.

## Requirements

Node.js 18 or newer.

## Library

```js
import {
  invertCase,
  sentenceCase,
  titleCase,
  transformText,
} from "caps-unlock";

titleCase("hELLO, WORLD!"); // "Hello, World!"
sentenceCase("hELLO WORLD. HOW ARE YOU?"); // "Hello world. How are you?"
invertCase("Hello, 123!"); // "hELLO, 123!"
transformText("hello", "uppercase"); // "HELLO"
```

Available functions are `upperCase`, `lowerCase`, `invertCase`, `titleCase`,
`sentenceCase`, `trim`, and `collapseWhitespace`. `transformText` accepts one of
`uppercase`, `lowercase`, `invert`, `title`, `sentence`, `trim`, or
`collapse-whitespace`.

Case conversion uses JavaScript's Unicode-aware string casing. Title case
capitalizes each word, lowercases its remaining letters, and treats hyphens and
punctuation as word separators. Sentence case lowercases the text and
capitalizes the first letter and letters after `.`, `!`, or `?` followed by
whitespace. `collapseWhitespace` replaces each run of whitespace with one space
without trimming the ends.

## CLI

```sh
npx caps-unlock title "hELLO, WORLD!"
echo "hello world" | npx caps-unlock uppercase
```

Run `caps-unlock --help` for the supported operations.

## Windows global shortcuts

The Windows companion registers system-wide shortcuts that transform the
currently selected text in most apps:

- `Ctrl+Shift+U`: uppercase
- `Ctrl+Shift+L`: lowercase
- `Ctrl+Shift+I`: invert case
- `Ctrl+Shift+T`: title case

Requires Windows, Node.js 18 or newer, and the .NET 8 SDK. From the repository
folder, start the background utility with:

```powershell
dotnet run --project .\windows\CapsUnlock.Hotkeys\CapsUnlock.Hotkeys.csproj
```

Select text in another app and press a shortcut. CapsUnlock reads the selection
and document through Windows UI Automation, replaces the selected range in the
document value, and restores the transformed range selection. This preserves
multi-line text without simulating Enter keystrokes. It never reads from, writes
to, or clears the clipboard. Apps must expose an editable value and a consistent
text range through UI Automation; unsupported apps or controls receive a
notification and are left unchanged. Exit the utility from its notification-area
icon. Shortcuts can be unavailable if another application already uses them.

## Development

```sh
npm test
```
